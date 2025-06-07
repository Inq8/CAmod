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
	[Desc("Tracks player experience and sets grants prerequisites based on it.")]
	public class PlayerExperienceLevelsInfo : ConditionalTraitInfo, Requires<PlayerExperienceInfo>, Requires<TechTreeInfo>, ITechTreePrerequisiteInfo
	{
		[Desc("Experience required to reach each level above level 0.")]
		public readonly int[] LevelXpRequirements = { 50, 250, 500 };

		public readonly string[] LevelPrerequisites = { };

		[Desc("List of factions that can have levels.")]
		public readonly string[] Factions = { };

		[NotificationReference("Speech")]
		[Desc("Speech notification to play when player levels up.")]
		public readonly string LevelUpNotification = null;

		[Desc("Text notification to display when player levels up.")]
		public readonly string LevelUpTextNotification = null;

		[NotificationReference("Sounds")]
		[Desc("Sound notification to play when player levels up.")]
		public readonly string LevelUpSound = null;

		[Desc("Ticks before playing notification.")]
		public readonly int NotificationDelay = 0;

		[Desc("Actor to spawn when player levels up.")]
		[ActorReference]
		public readonly string DummyActor = null;

		IEnumerable<string> ITechTreePrerequisiteInfo.Prerequisites(ActorInfo info)
		{
			return LevelPrerequisites;
		}

		public override object Create(ActorInitializer init) { return new PlayerExperienceLevels(init.Self, this); }
	}

	public class PlayerExperienceLevels : ConditionalTrait<PlayerExperienceLevelsInfo>, ITick, ITechTreePrerequisite, INotifyCreated
	{
		PlayerExperience playerExperience;
		TechTree techTree;
		UpgradesManager upgradesManager;
		readonly int maxLevel;
		readonly bool validFaction;
		int currentLevel;
		int nextLevelXpRequired;
		bool notificationQueued;
		int ticksUntilNotification;
		bool dummyActorQueued;
		int ticksUntilSpawnDummyActor;

		public event Action<int> LevelledUp;

		public PlayerExperienceLevels(Actor self, PlayerExperienceLevelsInfo info)
			: base(info)
		{
			var player = self.Owner;
			validFaction = info.Factions.Length == 0 || info.Factions.Contains(player.Faction.InternalName);

			currentLevel = 0;
			maxLevel = info.LevelXpRequirements.Length;
			nextLevelXpRequired = info.LevelXpRequirements[currentLevel];
			ticksUntilNotification = info.NotificationDelay;
		}

		public bool Enabled => validFaction;

		public int? CurrentLevel => currentLevel;

		public int? XpRequiredForNextLevel => currentLevel >= maxLevel ? null : nextLevelXpRequired;

		public IEnumerable<string> ProvidesPrerequisites
		{
			get
			{
				if (currentLevel > 0)
					return Info.LevelPrerequisites.Take(currentLevel);
				else
					return Enumerable.Empty<string>();
			}
		}

		protected override void Created(Actor self)
		{
			// Special case handling is required for the Player actor.
			// Created is called before Player.PlayerActor is assigned,
			// so we must query other player traits from self, knowing that
			// it refers to the same actor as self.Owner.PlayerActor
			var playerActor = self.Info.Name == "player" ? self : self.Owner.PlayerActor;
			playerExperience = playerActor.Trait<PlayerExperience>();
			techTree = playerActor.Trait<TechTree>();
			upgradesManager = playerActor.Trait<UpgradesManager>();
			base.Created(self);
		}

		void ITick.Tick(Actor self)
		{
			if (Enabled && currentLevel < maxLevel && playerExperience.Experience >= nextLevelXpRequired)
			{
				LevelUp(self);
			}

			if (notificationQueued && --ticksUntilNotification <= 0)
			{
				if (Info.LevelUpNotification != null)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.LevelUpNotification, self.Owner.Faction.InternalName);

				if (Info.LevelUpTextNotification != null)
					TextNotificationsManager.AddTransientLine(self.Owner, string.Format(Info.LevelUpTextNotification, currentLevel));

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

		void LevelUp(Actor self)
		{
			if (Info.LevelUpSound != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Sounds", Info.LevelUpSound, self.Owner.Faction.InternalName);

			currentLevel++;

			LevelledUp?.Invoke(currentLevel);

			if (currentLevel < maxLevel)
				nextLevelXpRequired = Info.LevelXpRequirements[currentLevel];

			techTree.ActorChanged(self);
			notificationQueued = true;

			// if there's an actor that represents the prerequisite, add it to the build order
			if (self.World.Map.Rules.Actors.ContainsKey(Info.LevelPrerequisites[currentLevel - 1]))
				upgradesManager.UpgradeProviderCreated(Info.LevelPrerequisites[currentLevel - 1]);

			if (Info.DummyActor != null)
			{
				dummyActorQueued = true;
				ticksUntilSpawnDummyActor = 1;
			}
		}
	}
}
