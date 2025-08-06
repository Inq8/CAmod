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
	public class ProvidesPrerequisiteOnCountInfo : TraitInfo, ITechTreePrerequisiteInfo
	{
		[Desc("Identifier for the counts.")]
		[FieldLoader.Require]
		public readonly string Type;

		[Desc("The counts required to enable the prerequisite.")]
		[FieldLoader.Require]
		public readonly int RequiredCount;

		[Desc("The prerequisite type that this provides.")]
		[FieldLoader.Require]
		public readonly string Prerequisite;

		[Desc("List of factions that can affect this count. Leave blank for any faction.")]
		public readonly string[] Factions = { };

		[Desc("If true, the prerequisite is permanent once the count is reached.")]
		public readonly bool PermanentWhenReached = false;

		[Desc("If true, the prerequisite is permanent if an upgrade is acquired.")]
		public readonly string[] PermanentAfterUpgrades = null;

		[NotificationReference("Speech")]
		[Desc("Speech notification to play when player reaches the required count.")]
		public readonly string RequiredCountReachedNotification = null;

		[Desc("Text notification to display when player reaches the required count.")]
		public readonly string RequiredCountReachedTextNotification = null;

		[Desc("Ticks before playing notification.")]
		public readonly int NotificationDelay = 0;

		[Desc("Actor to spawn when player reaches the required count.")]
		[ActorReference]
		public readonly string DummyActor = null;

		[NotificationReference("Sounds")]
		[Desc("Sound notification to play when count is incremented.")]
		public readonly string IncrementSound = null;

		IEnumerable<string> ITechTreePrerequisiteInfo.Prerequisites(ActorInfo info)
		{
			return new string[] { Prerequisite };
		}

		public override object Create(ActorInitializer init) { return new ProvidesPrerequisiteOnCount(init, this); }
	}

	public class ProvidesPrerequisiteOnCount : ITechTreePrerequisite, INotifyCreated, ITick
	{
		public readonly ProvidesPrerequisiteOnCountInfo Info;
		readonly Actor self;
		TechTree techTree;
		CountManager countManager;
		UpgradesManager upgradesManager;

		public int CurrentCount
		{
			get
			{
				if (countManager != null && countManager.Counts.TryGetValue(Info.Type, out var count))
					return count;
				return 0;
			}
		}

		bool requiredCountReached => CurrentCount >= Info.RequiredCount;
		bool countLocked;

		bool notificationQueued;
		int ticksUntilNotification;
		bool dummyActorQueued;
		int ticksUntilSpawnDummyActor;

		public event Action Incremented;
		public event Action Decremented;
		public event Action RequiredCountReached;
		public event Action<string> PermanentlyGranted;

		public ProvidesPrerequisiteOnCount(ActorInitializer init, ProvidesPrerequisiteOnCountInfo info)
		{
			Info = info;
			self = init.Self;
			countLocked = false;
			ticksUntilNotification = info.NotificationDelay;

			var player = self.Owner;
			Enabled = info.Factions.Length == 0 || info.Factions.Contains(player.Faction.InternalName);
		}

		public bool Enabled { get; }

		public string[] Factions => Info.Factions;

		public IEnumerable<string> ProvidesPrerequisites
		{
			get
			{
				if (!requiredCountReached)
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
			countManager = playerActor.Trait<CountManager>();
			upgradesManager = playerActor.Trait<UpgradesManager>();
			countManager.Incremented += HandleIncremented;
			countManager.Decremented += HandleDecremented;
			upgradesManager.UpgradeCompleted += HandleUpgrade;
		}

		void ITick.Tick(Actor self)
		{
			if (!Enabled)
				return;

			if (notificationQueued && --ticksUntilNotification <= 0)
			{
				if (Info.RequiredCountReachedNotification != null)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.RequiredCountReachedNotification, self.Owner.Faction.InternalName);

				if (Info.RequiredCountReachedTextNotification != null)
					TextNotificationsManager.AddTransientLine(self.Owner, Info.RequiredCountReachedTextNotification);

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

		// Invoked by CountManager when a count is incremented
		void HandleIncremented(string type, int newCount)
		{
			if (!Enabled || countLocked || type != Info.Type)
				return;

			if (newCount > Info.RequiredCount)
				return;

			techTree.ActorChanged(self);

			Incremented?.Invoke();

			if (Info.IncrementSound != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", Info.IncrementSound, self.Owner.Faction.InternalName);

			if (requiredCountReached)
			{
				RequiredCountReached?.Invoke();
				notificationQueued = true;

				if (Info.DummyActor != null)
				{
					dummyActorQueued = true;
					ticksUntilSpawnDummyActor = 1;
				}

				if (Info.PermanentWhenReached)
				{
					countLocked = true;
					PermanentlyGranted?.Invoke(null);
				}
			}
		}

		// Invoked by CountManager when a count is decremented
		void HandleDecremented(string type, int newCount)
		{
			if (!Enabled || countLocked || type != Info.Type)
				return;

			techTree.ActorChanged(self);
			Decremented?.Invoke();
		}

		void HandleUpgrade(string type)
		{
			if (!Enabled || countLocked || Info.PermanentAfterUpgrades == null || !Info.PermanentAfterUpgrades.Contains(type))
				return;

			countLocked = true;
			PermanentlyGranted?.Invoke(type);
		}
	}
}
