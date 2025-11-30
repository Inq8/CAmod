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
	public class ProvidesPrerequisitesOnCountInfo : TraitInfo, ITechTreePrerequisiteInfo
	{
		[Desc("Identifier for the counts.")]
		[FieldLoader.Require]
		public readonly string Type;

		[Desc("Prerequisites provided at specific counts.")]
		public readonly Dictionary<int, string> Prerequisites = null;

		[Desc("List of factions that can affect this count. Leave blank for any faction.")]
		public readonly string[] Factions = Array.Empty<string>();

		[Desc("If true, prerequisites are permanent once reached.")]
		public readonly bool PermanentWhenReached = false;

		[Desc("If true, prerequisites are permanent once reached if specified upgrade is acquired.")]
		public readonly string[] PermanentAfterUpgrades = null;

		[Desc("Speech notifications to play when player reaches specific counts.")]
		public readonly Dictionary<int, string> CountReachedNotifications = null;

		[Desc("Text notifications to display when player reaches specific counts.")]
		public readonly Dictionary<int, string> CountReachedTextNotifications = null;

		[Desc("Ticks before playing notification.")]
		public readonly int NotificationDelay = 0;

		[Desc("Actor to spawn when player reaches the required count.")]
		[ActorReference]
		public readonly string DummyActor = null;

		[NotificationReference("Sounds")]
		[Desc("Sound notification to play when count is incremented.")]
		public readonly string IncrementSound = null;

		[Desc("If true, adds the to the observer Upgrades tab.")]
		public readonly bool AddToUpgradesTab = false;

		IEnumerable<string> ITechTreePrerequisiteInfo.Prerequisites(ActorInfo info)
		{
			return Prerequisites.Values;
		}

		public override object Create(ActorInitializer init) { return new ProvidesPrerequisitesOnCount(init, this); }
	}

	public class ProvidesPrerequisitesOnCount : ITechTreePrerequisite, INotifyCreated, ITick
	{
		public readonly ProvidesPrerequisitesOnCountInfo Info;
		readonly Actor self;
		readonly HashSet<string> prerequisitesGranted;
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

		readonly HashSet<int> thresholdsPassed;
		readonly HashSet<string> permanentPrerequisites;

		string speechNotificationQueued;
		string textNotificationQueued;
		int ticksUntilNotification;
		bool dummyActorQueued;
		int ticksUntilSpawnDummyActor;

		public event Action Incremented;
		public event Action Decremented;
		public event Action<int> CountThresholdReached;
		public event Action<string> PermanentlyGranted;

		public ProvidesPrerequisitesOnCount(ActorInitializer init, ProvidesPrerequisitesOnCountInfo info)
		{
			Info = info;
			self = init.Self;
			ticksUntilNotification = info.NotificationDelay;
			prerequisitesGranted = new HashSet<string>();
			thresholdsPassed = new HashSet<int>();
			permanentPrerequisites = new HashSet<string>();

			var player = self.Owner;
			Enabled = info.Factions.Length == 0 || info.Factions.Contains(player.Faction.InternalName);
		}

		public bool Enabled { get; }

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

			if ((speechNotificationQueued != null || textNotificationQueued != null) && --ticksUntilNotification <= 0)
			{
				if (speechNotificationQueued != null)
				{
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", speechNotificationQueued, self.Owner.Faction.InternalName);
					speechNotificationQueued = null;
				}

				if (textNotificationQueued != null)
				{
					TextNotificationsManager.AddTransientLine(self.Owner, textNotificationQueued);
					textNotificationQueued = null;
				}

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

				dummyActorQueued = false;
			}
		}

		void HandleCountThreshold(int count)
		{
			if (Info.Prerequisites == null || !Info.Prerequisites.ContainsKey(count) || thresholdsPassed.Contains(count))
				return;

			thresholdsPassed.Add(count);
			var prerequisite = Info.Prerequisites[count];

			if (prerequisitesGranted.Add(prerequisite))
			{
				techTree.ActorChanged(self);

				// If there's an actor that represents the prerequisite, add it to the build order
				if (Info.AddToUpgradesTab && self.World.Map.Rules.Actors.ContainsKey(prerequisite))
					upgradesManager.UpgradeProviderCreated(prerequisite);

				CountThresholdReached?.Invoke(count);

				// Queue count-specific notifications if available
				if (Info.CountReachedNotifications != null && Info.CountReachedNotifications.TryGetValue(count, out var speechNotification))
					speechNotificationQueued = speechNotification;

				if (Info.CountReachedTextNotifications != null && Info.CountReachedTextNotifications.TryGetValue(count, out var textNotification))
					textNotificationQueued = textNotification;

				// Queue notifications if we have any to play
				if (speechNotificationQueued != null || textNotificationQueued != null)
				{
					ticksUntilNotification = Info.NotificationDelay > 0 ? Info.NotificationDelay : 1;
				}

				if (Info.DummyActor != null)
				{
					dummyActorQueued = true;
					ticksUntilSpawnDummyActor = 1;
				}

				// Mark prerequisite as permanent if PermanentWhenReached is true
				if (Info.PermanentWhenReached)
				{
					permanentPrerequisites.Add(prerequisite);
					PermanentlyGranted?.Invoke(null);
				}
			}
		}

		// Invoked by CountManager when a count is incremented
		void HandleIncremented(string type, int newCount)
		{
			if (!Enabled || type != Info.Type)
				return;

			var maxThreshold = Info.Prerequisites.Keys.Max();

			if (newCount > maxThreshold)
				return;

			// Return early if all prerequisites have been permanently unlocked
			if (Info.Prerequisites != null && Info.Prerequisites.Values.All(prerequisite => permanentPrerequisites.Contains(prerequisite)))
				return;

			techTree.ActorChanged(self);

			Incremented?.Invoke();

			// Play increment sound if we haven't exceeded the maximum threshold
			if (Info.IncrementSound != null && Info.Prerequisites != null)
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", Info.IncrementSound, self.Owner.Faction.InternalName);
			}

			HandleCountThreshold(newCount);
		}

		// Invoked by CountManager when a count is decremented
		void HandleDecremented(string type, int newCount)
		{
			if (!Enabled || type != Info.Type)
				return;

			// Remove prerequisites that are no longer valid due to count decrease
			// but keep permanent prerequisites
			if (Info.Prerequisites != null)
			{
				var prerequisitesToRemove = new List<string>();

				foreach (var kvp in Info.Prerequisites)
				{
					var count = kvp.Key;
					var prerequisite = kvp.Value;

					// If the count threshold is no longer met and the prerequisite is not permanent, remove it
					if (newCount < count && prerequisitesGranted.Contains(prerequisite) && !permanentPrerequisites.Contains(prerequisite))
					{
						prerequisitesToRemove.Add(prerequisite);
						thresholdsPassed.Remove(count);
					}
				}

				foreach (var prerequisite in prerequisitesToRemove)
				{
					prerequisitesGranted.Remove(prerequisite);
				}

				if (prerequisitesToRemove.Count > 0)
					techTree.ActorChanged(self);
			}

			Decremented?.Invoke();
		}

		void HandleUpgrade(string type)
		{
			if (!Enabled || Info.PermanentAfterUpgrades == null || !Info.PermanentAfterUpgrades.Contains(type))
				return;

			// Mark all currently granted prerequisites as permanent
			foreach (var prerequisite in prerequisitesGranted.ToArray())
			{
				permanentPrerequisites.Add(prerequisite);
			}

			PermanentlyGranted?.Invoke(type);
		}
	}
}
