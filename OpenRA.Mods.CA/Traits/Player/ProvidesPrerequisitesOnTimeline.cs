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

		[Desc("Prerequisites provided at specific tick counts.")]
		public readonly Dictionary<int, string> Prerequisites = null;

		[Desc("List of factions that can affect this count. Leave blank for any faction.")]
		public readonly string[] Factions = { };

		[Desc("Speech notification to play when player reaches the required count.")]
		public readonly Dictionary<int, string> PrerequisiteGrantedNotifications = null;

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
		UpgradesManager upgradesManager;

		int ticksElapsed;
		HashSet<int> thresholdsPassed;
		string notificationQueued;
		int ticksUntilNotification;
		bool dummyActorQueued;
		int ticksUntilSpawnDummyActor;

		public event Action<int> TicksChanged;

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

		public int MaxTicks => Info.Prerequisites?.Keys.Max() ?? 0;
		public bool Enabled => validFaction;
		public int TicksElapsed => ticksElapsed;
		public int TicksRemaining => MaxTicks - ticksElapsed;
		public int[] Thresholds => Info.Prerequisites?.Keys.ToArray() ?? Array.Empty<int>();
		public int ThresholdsPassed => thresholdsPassed.Count;

		public int TicksUntilNextThreshold
		{
			get
			{
				if (Info.Prerequisites == null || !Info.Prerequisites.Any())
					return 0;

				var nextThreshold = Info.Prerequisites.Keys
					.Where(t => t > ticksElapsed)
					.OrderBy(t => t)
					.FirstOrDefault();

				if (nextThreshold == 0)
					return 0;

				return nextThreshold - ticksElapsed;
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
			upgradesManager = playerActor.Trait<UpgradesManager>();
		}

		void HandlePrerequisiteThreshold(int tick)
		{
			if (Info.Prerequisites == null || !Info.Prerequisites.ContainsKey(tick) || thresholdsPassed.Contains(tick))
				return;

			thresholdsPassed.Add(tick);
			var prerequisite = Info.Prerequisites[tick];

			if (!prerequisitesGranted.Contains(prerequisite))
			{
				prerequisitesGranted.Add(prerequisite);
				techTree.ActorChanged(self);

				// if there's an actor that represents the prerequisite, add it to the build order
				if (self.World.Map.Rules.Actors.ContainsKey(prerequisite))
					upgradesManager.UpgradeProviderCreated(prerequisite);

				if (Info.PrerequisiteGrantedSound != null)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds",
						Info.PrerequisiteGrantedSound, self.Owner.Faction.InternalName);

				if (Info.DummyActor != null)
				{
					if (Info.PrerequisiteGrantedNotifications != null && Info.PrerequisiteGrantedNotifications.ContainsKey(tick))
						notificationQueued = Info.PrerequisiteGrantedNotifications[tick];

					dummyActorQueued = true;
					ticksUntilSpawnDummyActor = 1;
				}
			}
		}

		void ITick.Tick(Actor self)
		{
			if (!Enabled)
				return;

			var previousTicks = ticksElapsed;

			if (ticksElapsed < MaxTicks)
			{
				ticksElapsed++;

				if (previousTicks != ticksElapsed)
					TicksChanged?.Invoke(ticksElapsed);

				HandlePrerequisiteThreshold(ticksElapsed);
			}

			if (notificationQueued != null && --ticksUntilNotification <= 0)
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", notificationQueued, self.Owner.Faction.InternalName);

				if (Info.PrerequisiteGrantedTextNotification != null)
					TextNotificationsManager.AddTransientLine(self.Owner, Info.PrerequisiteGrantedTextNotification);

				notificationQueued = null;
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

		public void AddTicks(int ticks)
		{
			if (!Enabled || ticks <= 0)
				return;

			var initialTicks = ticksElapsed;

			ticksElapsed = Math.Min(ticksElapsed + ticks, MaxTicks);

			if (initialTicks == ticksElapsed)
				return;

			if (initialTicks != ticksElapsed)
				TicksChanged?.Invoke(ticksElapsed);

			if (Info.Prerequisites != null)
			{
				for (int t = initialTicks + 1; t <= ticksElapsed; t++)
					HandlePrerequisiteThreshold(t);
			}
		}
	}
}
