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
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Manages unit upgrades.")]
	public class UpgradesManagerInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new UpgradesManager(init.Self, this); }
	}

	public class UpgradesManager
	{
		readonly Actor self;
		Dictionary<string, UpgradeInfo> upgrades;
		Dictionary<string, List<int>> unlockedUpgradeTypes;

		public int Hash { get; private set; }

		public Dictionary<string, List<int>> UnlockedUpgradeTypes => unlockedUpgradeTypes;

		public UpgradesManager(Actor self, UpgradesManagerInfo info)
		{
			this.self = self;
			upgrades = new Dictionary<string, UpgradeInfo>();
			unlockedUpgradeTypes = new Dictionary<string, List<int>>();
			Hash = 0;
		}

		public event Action<string> UpgradeCompleted;

		public UpgradeInfo UpgradeableActorCreated(Upgradeable upgradeable, string upgradeType, string sourceActorType, string targetActorType, int cost, int buildDuration, int buildDurationModifier)
		{
			if (!upgrades.ContainsKey(upgradeType))
			{
				var upgradeInfo = GetUpgradeInfo(sourceActorType, targetActorType, cost, buildDuration, buildDurationModifier);
				upgrades.Add(upgradeType, upgradeInfo);
			}

			if (IsUnlocked(upgradeType))
				upgradeable.Unlock();

			return upgrades[upgradeType];
		}

		public void UpgradeProviderCreated(string type)
		{
			if (IsUnlocked(type))
				unlockedUpgradeTypes[type].Add(self.World.WorldTick);
			else
			{
				unlockedUpgradeTypes.Add(type, new List<int> { self.World.WorldTick });
				var upgradeables = self.World.ActorsWithTrait<Upgradeable>().Where(x => x.Trait.Info.Type == type && x.Actor.Owner == self.Owner).ToList();

				foreach (var p in upgradeables)
					p.Trait.Unlock();
			}

			UpgradeCompleted?.Invoke(type);
			Update();
		}

		public bool IsUnlocked(string upgradeType)
		{
			return unlockedUpgradeTypes.ContainsKey(upgradeType);
		}

		UpgradeInfo GetUpgradeInfo(string sourceActorType, string targetActorType, int cost, int buildDuration, int buildDurationModifier)
		{
			if (cost == -1)
				cost = CalculateCost(sourceActorType, targetActorType);

			if (buildDuration == -1)
				buildDuration = CalculateBuildDuration(cost, buildDurationModifier);

			var upgradeInfo = new UpgradeInfo()
			{
				Cost = cost,
				BuildDuration = buildDuration,
			};

			if (targetActorType != null && self.World.Map.Rules.Actors.ContainsKey(targetActorType))
			{
				var targetActorInfo = self.World.Map.Rules.Actors[targetActorType];
				var tooltip = targetActorInfo.TraitInfoOrDefault<TooltipInfo>();
				if (tooltip != null)
					upgradeInfo.ActorName = tooltip.Name;
			}

			return upgradeInfo;
		}

		int CalculateCost(string sourceActorType, string targetActorType)
		{
			if (targetActorType == null)
				return 0;

			var sourceActorInfo = self.World.Map.Rules.Actors[sourceActorType];
			var sourceActorValued = sourceActorInfo.TraitInfoOrDefault<ValuedInfo>();
			var sourceActorCost = sourceActorValued?.Cost ?? 0;
			var targetActorInfo = self.World.Map.Rules.Actors[targetActorType];
			var targetActorValued = targetActorInfo.TraitInfoOrDefault<ValuedInfo>();
			var targetActorCost = targetActorValued?.Cost ?? 0;
			return Math.Max(targetActorCost - sourceActorCost, 0);
		}

		int CalculateBuildDuration(int cost, int buildDurationModifier)
		{
			return Util.ApplyPercentageModifiers(cost, new int[] { buildDurationModifier });
		}

		public void Update()
		{
			Hash = Hash + 1;
		}
	}

	public class UpgradeInfo
	{
		public int Cost { get; set; }
		public int BuildDuration { get; set; }
		public string ActorName { get; set; }
	}
}
