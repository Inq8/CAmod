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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Tracks player experience and sets a level based on it.")]
	public class PlayerExperienceLevelsInfo : ConditionalTraitInfo, Requires<PlayerExperienceInfo>, Requires<TechTreeInfo>
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

		public override object Create(ActorInitializer init) { return new PlayerExperienceLevels(init.Self, this); }
	}

	public class PlayerExperienceLevels : ConditionalTrait<PlayerExperienceLevelsInfo>, ITick, ITechTreePrerequisite
	{
		readonly PlayerExperience playerExperience;
		readonly TechTree techTree;
		readonly int maxLevel;
		readonly bool validFaction;
		int currentLevel;
		int nextLevelXpRequired;

		public PlayerExperienceLevels(Actor self, PlayerExperienceLevelsInfo info)
			: base(info)
		{
			var player = self.Owner;
			validFaction = info.Factions.Length == 0 || info.Factions.Contains(player.Faction.InternalName);

			playerExperience = self.Trait<PlayerExperience>();
			techTree = self.Trait<TechTree>();
			currentLevel = 0;
			maxLevel = info.LevelXpRequirements.Length;
			nextLevelXpRequired = info.LevelXpRequirements[currentLevel];
		}

		public bool Enabled => validFaction;

		public int? CurrentLevel => currentLevel;

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

		void ITick.Tick(Actor self)
		{
			if (Enabled && currentLevel < maxLevel && playerExperience.Experience >= nextLevelXpRequired)
			{
				LevelUp(self);
			}
		}

		void LevelUp(Actor self)
		{
			currentLevel++;

			if (currentLevel < maxLevel)
				nextLevelXpRequired = Info.LevelXpRequirements[currentLevel];

			techTree.ActorChanged(self);

			if (Info.LevelUpNotification != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.LevelUpNotification, self.Owner.Faction.InternalName);

			if (Info.LevelUpTextNotification != null)
				TextNotificationsManager.AddTransientLine(string.Format(Info.LevelUpTextNotification, currentLevel), self.Owner);
		}
	}
}
