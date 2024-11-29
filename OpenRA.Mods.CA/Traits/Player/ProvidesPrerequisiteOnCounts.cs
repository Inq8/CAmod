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
		public readonly string Type = null;

		[Desc("The prerequisite type that this provides.")]
		[FieldLoader.Require]
		public readonly string Prerequisite = null;

		[Desc("The counts required to enable the prerequisite.")]
		[FieldLoader.Require]
		public readonly Dictionary<string, int> RequiredCounts = null;

		[Desc("List of factions that can affect this count. Leave blank for any faction.")]
		public readonly string[] Factions = { };

		[Desc("If true, the prerequisite is permanent once the count is reached.")]
		public readonly bool Permanent = false;

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

	public class ProvidesPrerequisiteOnCount : ITechTreePrerequisite, INotifyCreated, ITick, INotifyCountChanged
	{
		public readonly ProvidesPrerequisiteOnCountInfo Info;
		readonly Actor self;
		readonly Dictionary<string, int> counts;
		readonly bool validFaction;
		TechTree techTree;
		UpgradesManager upgradesManager;
		bool permanentlyUnlocked;
		bool notificationQueued;
		int ticksUntilNotification;
		bool dummyActorQueued;
		int ticksUntilSpawnDummyActor;

		public event Action Incremented;
		public event Action Decremented;
		public event Action Unlocked;
		public event Action<string> UnlockedPermanently;

		public ProvidesPrerequisiteOnCount(ActorInitializer init, ProvidesPrerequisiteOnCountInfo info)
		{
			Info = info;
			self = init.Self;
			counts = new Dictionary<string, int>();
			permanentlyUnlocked = false;
			ticksUntilNotification = info.NotificationDelay;

			var player = self.Owner;
			validFaction = info.Factions.Length == 0 || info.Factions.Contains(player.Faction.InternalName);

			foreach (var count in Info.RequiredCounts)
				counts[count.Key] = 0;
		}

		public bool Enabled
		{
			get
			{
				return validFaction;
			}
		}

		bool PrerequisitesGranted
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

		public Dictionary<string, int> Counts => counts;

		public string[] Factions => Info.Factions;

		public IEnumerable<string> ProvidesPrerequisites
		{
			get
			{
				if (!PrerequisitesGranted)
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
			upgradesManager = playerActor.TraitOrDefault<UpgradesManager>();
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

			if (!permanentlyUnlocked && Info.PermanentAfterUpgrades != null)
			{
				foreach (var upgrade in Info.PermanentAfterUpgrades)
				{
					if (upgradesManager.IsUnlocked(upgrade))
					{
						permanentlyUnlocked = true;
						UnlockedPermanently?.Invoke(upgrade);
						break;
					}
				}
			}
		}

		void INotifyCountChanged.Incremented(string type)
		{
			if (!Enabled || permanentlyUnlocked || !counts.ContainsKey(type))
				return;

			counts[type]++;

			if (counts[type] >= Info.RequiredCounts[type])
				return;

			techTree.ActorChanged(self);

			Incremented?.Invoke();

			if (Info.IncrementSound != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", Info.IncrementSound, self.Owner.Faction.InternalName);

			if (AllRequiredCountsReached)
			{
				Unlocked?.Invoke();
				notificationQueued = true;

				if (Info.DummyActor != null)
				{
					dummyActorQueued = true;
					ticksUntilSpawnDummyActor = 1;
				}

				if (Info.Permanent)
				{
					permanentlyUnlocked = true;
					UnlockedPermanently?.Invoke(null);
				}
			}
		}

		void INotifyCountChanged.Decremented(string type)
		{
			if (!Enabled || permanentlyUnlocked || !counts.ContainsKey(type))
				return;

			counts[type]--;
			techTree.ActorChanged(self);
			Decremented?.Invoke();
		}
	}
}
