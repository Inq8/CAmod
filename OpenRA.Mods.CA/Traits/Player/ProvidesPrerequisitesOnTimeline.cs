#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[TraitLocation(SystemActors.Player)]
	public class ProvidesPrerequisitesOnTimelineInfo : TraitInfo, ITechTreePrerequisiteInfo
	{
		[Desc("Identifier.")]
		[FieldLoader.Require]
		public readonly string Type = null;

		[Desc("Maximum number of ticks.")]
		public readonly int MaxTicks = 30000;

		[Desc("Prerequesities provided at percentage thresholds.")]
		public readonly Dictionary<int, string> Prerequisites = null;

		[Desc("List of factions that can affect this count. Leave blank for any faction.")]
		public readonly string[] Factions = { };

		[NotificationReference("Speech")]
		[Desc("Speech notification to play when player reaches the required count.")]
		public readonly string PrerequisiteGrantedNotification = null;

		[Desc("Text notification to display when player reaches the required count.")]
		public readonly string PrerequisiteGrantedTextNotification = null;

		[Desc("Ticks before playing notification.")]
		public readonly int NotificationDelay = 0;

		[Desc("Actor to spawn when player levels up.")]
		[ActorReference]
		public readonly string DummyActor = null;

		[NotificationReference("Sounds")]
		[Desc("Sound notification to play when count is incremented.")]
		public readonly string PrerequisiteGrantedSound = null;

		[Desc("Ticks before playing notification.")]
		public readonly int MaxBoostMultiplier = 1;

		IEnumerable<string> ITechTreePrerequisiteInfo.Prerequisites(ActorInfo info)
		{
			return Prerequisites.Values;
		}

		public override object Create(ActorInitializer init) { return new ProvidesPrerequisitesOnTimeline(init, this); }
	}

	public class ProvidesPrerequisitesOnTimeline : ITechTreePrerequisite, INotifyCreated, ITick
	{
		public readonly ProvidesPrerequisitesOnTimelineInfo Info;
		readonly Actor self;
		readonly HashSet<string> prerequisitesGranted;
		readonly bool validFaction;
		TechTree techTree;

		int ticksElapsed;
		HashSet<int> thresholdsPassed;
		bool notificationQueued;
		int ticksUntilNotification;
		bool dummyActorQueued;
		int ticksUntilSpawnDummyActor;

		public event Action<int> PercentageChanged;

		public ProvidesPrerequisitesOnTimeline(ActorInitializer init, ProvidesPrerequisitesOnTimelineInfo info)
		{
			Info = info;
			self = init.Self;
			ticksElapsed = 0;
			ticksUntilNotification = info.NotificationDelay;
			prerequisitesGranted = new HashSet<string>();
			thresholdsPassed = new HashSet<int>();

			var player = self.Owner;
			validFaction = info.Factions.Length == 0 || info.Factions.Contains(player.Faction.InternalName);
		}

		public bool Enabled => validFaction;
		public int TicksElapsed => ticksElapsed;
		public int TicksRemaining => Info.MaxTicks - ticksElapsed;
		public int PercentageComplete => ticksElapsed * 100 / Info.MaxTicks;
		public int[] Thresholds => Info.Prerequisites?.Keys.ToArray() ?? Array.Empty<int>();
		public int ThresholdsPassed => thresholdsPassed.Count;

		public int TicksUntilNextThreshold
		{
			get
			{
				if (Info.Prerequisites == null || !Info.Prerequisites.Any())
					return 0;

				var nextThreshold = Info.Prerequisites.Keys
					.Where(t => t > PercentageComplete)
					.OrderBy(t => t)
					.FirstOrDefault();

				if (nextThreshold == 0)
					return 0;

				var ticksNeededForThreshold = (Info.MaxTicks * nextThreshold) / 100;
				return ticksNeededForThreshold - ticksElapsed;
			}
		}

		public string[] Factions => Info.Factions;

		public IEnumerable<string> ProvidesPrerequisites => prerequisitesGranted;

		void INotifyCreated.Created(Actor self)
		{
			// Special case handling is required for the Player actor.
			// Created is called before Player.PlayerActor is assigned,
			// so we must query other player traits from self, knowing that
			// it refers to the same actor as self.Owner.PlayerActor
			var playerActor = self.Info.Name == "player" ? self : self.Owner.PlayerActor;
			techTree = playerActor.Trait<TechTree>();
			techTree.ActorChanged(self);
		}

		void ITick.Tick(Actor self)
		{
			if (!Enabled)
				return;

			var previousPercentage = PercentageComplete;

			if (ticksElapsed < Info.MaxTicks)
			{
				ticksElapsed++;

				if (previousPercentage != PercentageComplete)
					PercentageChanged?.Invoke(PercentageComplete);

				if (Info.Prerequisites != null && Info.Prerequisites.ContainsKey(PercentageComplete) && !thresholdsPassed.Contains(PercentageComplete))
				{
					thresholdsPassed.Add(PercentageComplete);
					var prerequisite = Info.Prerequisites[PercentageComplete];

					if (!prerequisitesGranted.Contains(prerequisite))
					{
						prerequisitesGranted.Add(prerequisite);
						techTree.ActorChanged(self);

						if (Info.PrerequisiteGrantedSound != null)
							Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", Info.PrerequisiteGrantedSound, self.Owner.Faction.InternalName);

						if (Info.DummyActor != null)
						{
							notificationQueued = true;
							dummyActorQueued = true;
							ticksUntilSpawnDummyActor = 1;
						}
					}
				}
			}

			if (notificationQueued && --ticksUntilNotification <= 0)
			{
				if (Info.PrerequisiteGrantedNotification != null)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.PrerequisiteGrantedNotification, self.Owner.Faction.InternalName);

				if (Info.PrerequisiteGrantedTextNotification != null)
					TextNotificationsManager.AddTransientLine(Info.PrerequisiteGrantedTextNotification, self.Owner);

				notificationQueued = false;
				ticksUntilNotification = Info.NotificationDelay;
			}

			if (dummyActorQueued && --ticksUntilSpawnDummyActor <= 0)
			{
				self.World.AddFrameEndTask(w =>
				{
					w.CreateActor(Info.DummyActor, new TypeDictionary
					{
						new ParentActorInit(self),
						new LocationInit(CPos.Zero),
						new OwnerInit(self.Owner),
						new FacingInit(WAngle.Zero),
					});
				});
			}
		}
	}
}
