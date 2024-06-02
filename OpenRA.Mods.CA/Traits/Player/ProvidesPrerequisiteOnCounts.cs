#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[TraitLocation(SystemActors.Player)]
	public class ProvidesPrerequisiteOnCountsInfo : TraitInfo, ITechTreePrerequisiteInfo
	{
		[Desc("The prerequisite type that this provides.")]
		[FieldLoader.Require]
		public readonly string Prerequisite = null;

		[Desc("The counts required to enable the prerequisite.")]
		[FieldLoader.Require]
		public readonly Dictionary<string, int> RequiredCounts = null;

		[Desc("List of factions that can affect this count. Leave blank for any faction.")]
		public readonly string[] Factions = { };

		[Desc("If true, the prerequisite is permanent once the count is reached.")]
		public readonly bool Permanent = true;

		[NotificationReference("Speech")]
		[Desc("Speech notification to play when player levels up.")]
		public readonly string RequiredCountReachedNotification = null;

		[Desc("Text notification to display when player levels up.")]
		public readonly string RequiredCountReachedTextNotification = null;

		[Desc("Ticks before playing notification.")]
		public readonly int NotificationDelay = 0;

		[Desc("Actor to spawn when player levels up.")]
		[ActorReference]
		public readonly string DummyActor = null;

		IEnumerable<string> ITechTreePrerequisiteInfo.Prerequisites(ActorInfo info)
		{
			return new string[] { Prerequisite };
		}

		public override object Create(ActorInitializer init) { return new ProvidesPrerequisiteOnCount(init, this); }
	}

	public class ProvidesPrerequisiteOnCount : ITechTreePrerequisite, INotifyCreated, ITick
	{
		public readonly ProvidesPrerequisiteOnCountsInfo Info;
		readonly Actor self;
		readonly Dictionary<string, int> counts;
		TechTree techTree;
		bool permanentlyUnlocked;
		bool notificationQueued;
		int ticksUntilNotification;
		bool dummyActorQueued;
		int ticksUntilSpawnDummyActor;

		public ProvidesPrerequisiteOnCount(ActorInitializer init, ProvidesPrerequisiteOnCountsInfo info)
		{
			Info = info;
			self = init.Self;
			counts = new Dictionary<string, int>();
			permanentlyUnlocked = false;
			ticksUntilNotification = info.NotificationDelay;
		}

		bool Enabled
		{
			get
			{
				return permanentlyUnlocked || AllRequiredCountsReached;
			}
		}

		bool AllRequiredCountsReached
		{
			get
			{
				foreach (var kvp in Info.RequiredCounts)
				{
					if (!counts.ContainsKey(kvp.Key) || counts[kvp.Key] < kvp.Value)
						return false;
				}

				return true;
			}
		}

		public string[] Factions => Info.Factions;

		public IEnumerable<string> ProvidesPrerequisites
		{
			get
			{
				if (!Enabled)
					yield break;

				yield return Info.Prerequisite;
			}
		}

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
			if (notificationQueued && --ticksUntilNotification <= 0)
			{
				if (Info.RequiredCountReachedNotification != null)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.RequiredCountReachedNotification, self.Owner.Faction.InternalName);

				if (Info.RequiredCountReachedTextNotification != null)
					TextNotificationsManager.AddTransientLine(Info.RequiredCountReachedTextNotification, self.Owner);

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

		public void Increment(string type)
		{
			if (!counts.ContainsKey(type))
				counts[type] = 0;

			counts[type]++;
			techTree.ActorChanged(self);

			if (AllRequiredCountsReached)
			{
				if (!permanentlyUnlocked)
				{
					notificationQueued = true;

					if (Info.DummyActor != null)
					{
						dummyActorQueued = true;
						ticksUntilSpawnDummyActor = 1;
					}
				}

				if (Info.Permanent)
					permanentlyUnlocked = true;
			}
		}

		public void Decrement(string type)
		{
			counts[type]--;
			techTree.ActorChanged(self);
		}
	}
}
