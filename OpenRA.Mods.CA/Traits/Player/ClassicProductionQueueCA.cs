#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Attach this to the player actor (not a building!) to define a new shared build queue.",
		"Will only work together with the Production: trait on the actor that actually does the production.",
		"You will also want to add PrimaryBuildings: to let the user choose where new units should exit.")]
	public class ClassicProductionQueueCAInfo : ClassicProductionQueueInfo
	{
		[Desc("If true, ignore BuildAtProductionType when calculating build duration, so all structures for this queue are counted.")]
		public readonly bool CombinedBuildSpeedReduction = false;

		[Desc("If true, any units being produced that have been replaced by an upgrade will be completed.")]
		public readonly bool CompleteUpgradedInProgress = false;

		public override object Create(ActorInitializer init) { return new ClassicProductionQueueCA(init, this); }
	}

	public class ClassicProductionQueueCA : ClassicProductionQueue
	{
		public readonly new ClassicProductionQueueCAInfo Info;
		readonly Actor self;
		PowerManager playerPower;
		HashSet<string> lastBuildableNames = new HashSet<string>();

		public ClassicProductionQueueCA(ActorInitializer init, ClassicProductionQueueCAInfo info)
			: base(init, info)
		{
			self = init.Self;
			Info = info;
		}

		public override int GetBuildTime(ActorInfo unit, BuildableInfo bi)
		{
			if (developerMode.FastBuild)
				return 0;

			var time = GetBaseBuildTime(unit, bi);

			if (Info.SpeedUp)
			{
				var type = bi.BuildAtProductionType ?? Info.Type;

				// difference with ClassicProductionQueue, prevents BuildAtProductionType from overriding the type
				if (Info.CombinedBuildSpeedReduction)
					type = Info.Type;

				var selfsameProductionsCount = self.World.ActorsWithTrait<Production>()
					.Count(p => !p.Trait.IsTraitDisabled && !p.Trait.IsTraitPaused && p.Actor.Owner == self.Owner && p.Trait.Info.Produces.Contains(type));

				var speedModifier = selfsameProductionsCount.Clamp(1, Info.BuildTimeSpeedReduction.Length) - 1;
				time = (time * Info.BuildTimeSpeedReduction[speedModifier]) / 100;
			}

			return time;
		}

		// copied from ProductionQueue to bypass ClassicProductionQueue.GetBuildTime()
		public virtual int GetBaseBuildTime(ActorInfo unit, BuildableInfo bi)
		{
			if (developerMode.FastBuild)
				return 0;

			var time = bi.BuildDuration;
			if (time == -1)
				time = GetProductionCost(unit);

			var modifiers = unit.TraitInfos<IProductionTimeModifierInfo>()
				.Select(t => t.GetProductionTimeModifier(techTree, Info.Type))
				.Append(bi.BuildDurationModifier)
				.Append(Info.BuildDurationModifier);

			return Util.ApplyPercentageModifiers(time, modifiers);
		}

		// overrides ProductionQueue.TickInner() so that the new ReplaceOrCancelUnbuildableItems() is called
		protected override void TickInner(Actor self, bool allProductionPaused)
		{
			ReplaceOrCancelUnbuildableItems();

			if (Queue.Count > 0 && !allProductionPaused)
				Queue[0].Tick(playerResources);
		}

		// copied from ProductionQueue.CancelUnbuildableItems(), amended to allow in-place replacements due to upgrades
		protected void ReplaceOrCancelUnbuildableItems()
		{
			if (Queue.Count == 0)
			{
				// reset lastBuildableNames to ensure checks will be done immediately when queue is populated again
				lastBuildableNames = new HashSet<string>();
				return;
			}

			var buildableNames = BuildableItems().Select(b => b.Name).ToHashSet();

			// if buildables haven't changed since last tick we don't need to do anything else
			if (lastBuildableNames == buildableNames)
				return;

			var rules = self.World.Map.Rules;
			var replacements = new Dictionary<string, ReplacementDetails>();

			if (playerPower == null)
				playerPower = self.Owner.PlayerActor.TraitOrDefault<PowerManager>();

			// EndProduction removes the item from the queue, so we enumerate
			// by index in reverse to avoid issues with index reassignment
			for (var i = Queue.Count - 1; i >= 0; i--)
			{
				if (buildableNames.Contains(Queue[i].Item))
					continue;

				Queue[i] = GetReplacement(Queue[i], replacements, buildableNames, rules, out bool replaced);
				if (replaced)
					continue;

				// Refund what's been paid so far
				playerResources.GiveCash(Queue[i].TotalCost - Queue[i].RemainingCost);
				EndProduction(Queue[i]);
			}

			lastBuildableNames = buildableNames;
		}

		ProductionItem GetReplacement(ProductionItem queueItem, Dictionary<string, ReplacementDetails> replacements, HashSet<string> buildableNames, Ruleset rules, out bool replaced)
		{
			replaced = false;

			// if started already, and not set to complete any in progress, don't replace (will be cancelled and refunded)
			if (queueItem.Item == null || (queueItem.Started && !Info.CompleteUpgradedInProgress))
				return queueItem;

			if (!replacements.ContainsKey(queueItem.Item))
			{
				var upgradeableTo = rules.Actors[queueItem.Item].TraitInfoOrDefault<UpgradeableToInfo>();
				var replacement = new ReplacementDetails();

				if (upgradeableTo != null)
				{
					var replacementName = upgradeableTo.Actors.Where(a => buildableNames.Contains(a)).FirstOrDefault();
					if (replacementName != null)
					{
						replacement.Info = rules.Actors[replacementName];
						var valued = replacement.Info.TraitInfoOrDefault<ValuedInfo>();
						replacement.Cost = valued != null ? valued.Cost : 0;
					}
				}

				replacements[queueItem.Item] = replacement;
			}

			if (replacements[queueItem.Item].Info != null)
			{
				// if a replacement is buildable, but we've already started producing, we should be able to finish production (as CompleteUpgradedInProgress is true)
				if (queueItem.Started)
				{
					replaced = true;
					return queueItem;
				}

				var r = replacements[queueItem.Item];

				var replacementItem = new ProductionItem(this, r.Info.Name, r.Cost, playerPower, () => self.World.AddFrameEndTask(_ =>
				{
					// Make sure the item hasn't been invalidated between the ProductionItem ticking and this FrameEndTask running
					if (!Queue.Any(j => j.Done && j.Item == r.Info.Name))
						return;

					var isBuilding = r.Info.HasTraitInfo<BuildingInfo>();
					if (isBuilding)
						return;

					if (BuildUnit(r.Info))
						Game.Sound.PlayNotification(rules, self.Owner, "Speech", Info.ReadyAudio, self.Owner.Faction.InternalName);
				}));

				replaced = true;
				return replacementItem;
			}

			return queueItem;
		}
	}

	public class ReplacementDetails
	{
		public ActorInfo Info;
		public int Cost;
	}
}
