#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Put this on the Player actor. Manages bot harvesters to ensure they always continue harvesting as long as there are resources on the map.")]
	public class HarvesterBotModuleCAInfo : ConditionalTraitInfo
	{
		[Desc("Actor types that are considered harvesters. If harvester count drops below RefineryTypes count, a new harvester is built.",
			"Leave empty to disable harvester replacement. Currently only needed by harvester replacement system.")]
		public readonly HashSet<string> HarvesterTypes = new HashSet<string>();

		[Desc("Number of harvesters per refinery to maintain.")]
		public readonly int HarvestersPerRefinery = 2;

		[Desc("Maximum number of harvesters to build.")]
		public readonly int MaxHarvesters = 8;

		[Desc("Actor types that are counted as refineries. Currently only needed by harvester replacement system.")]
		public readonly HashSet<string> RefineryTypes = new HashSet<string>();

		[Desc("Interval (in ticks) between giving out orders to idle harvesters.")]
		public readonly int ScanForIdleHarvestersInterval = 75;

		[Desc("Interval (in ticks) between checking whether to produce new harvesters.")]
		public readonly int ProduceHarvestersInterval = 375;

		[Desc("Avoid enemy actors nearby when searching for a new resource patch. Should be somewhere near the max weapon range.")]
		public readonly WDist HarvesterEnemyAvoidanceRadius = WDist.FromCells(8);

		public override object Create(ActorInitializer init) { return new HarvesterBotModuleCA(init.Self, this); }
	}

	public class HarvesterBotModuleCA : ConditionalTrait<HarvesterBotModuleCAInfo>, IBotTick
	{
		class HarvesterTraitWrapper
		{
			public readonly Actor Actor;
			public readonly Harvester Harvester;
			public readonly Parachutable Parachutable;
			public readonly Locomotor Locomotor;

			public HarvesterTraitWrapper(Actor actor)
			{
				Actor = actor;
				Harvester = actor.Trait<Harvester>();
				Parachutable = actor.TraitOrDefault<Parachutable>();
				var mobile = actor.Trait<Mobile>();
				Locomotor = mobile.Locomotor;
			}
		}

		readonly World world;
		readonly Player player;
		readonly Func<Actor, bool> unitCannotBeOrdered;
		readonly Dictionary<Actor, HarvesterTraitWrapper> harvesters = new Dictionary<Actor, HarvesterTraitWrapper>();

		IPathFinder pathfinder;
		IResourceLayer resourceLayer;
		ResourceClaimLayer claimLayer;
		IBotRequestUnitProduction[] requestUnitProduction;
		int scanForIdleHarvestersTicks;
		int scanForEnoughHarvestersTicks;

		public HarvesterBotModuleCA(Actor self, HarvesterBotModuleCAInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			unitCannotBeOrdered = a => a.Owner != self.Owner || a.IsDead || !a.IsInWorld;
		}

		protected override void Created(Actor self)
		{
			requestUnitProduction = self.Owner.PlayerActor.TraitsImplementing<IBotRequestUnitProduction>().ToArray();
		}

		protected override void TraitEnabled(Actor self)
		{
			pathfinder = world.WorldActor.Trait<IPathFinder>();
			resourceLayer = world.WorldActor.TraitOrDefault<IResourceLayer>();
			claimLayer = world.WorldActor.TraitOrDefault<ResourceClaimLayer>();

			// Avoid all AIs scanning for idle harvesters on the same tick, randomize their initial scan delay.
			scanForIdleHarvestersTicks = world.LocalRandom.Next(Info.ScanForIdleHarvestersInterval, Info.ScanForIdleHarvestersInterval * 2);

			scanForEnoughHarvestersTicks = Info.ProduceHarvestersInterval;
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (resourceLayer == null || resourceLayer.IsEmpty)
				return;

			if (--scanForIdleHarvestersTicks > 0)
			{
				OrderHarvesters(bot);
				scanForIdleHarvestersTicks = Info.ScanForIdleHarvestersInterval;
			}

			if (--scanForEnoughHarvestersTicks > 0)
			{
				ProduceHarvesters(bot);
				scanForEnoughHarvestersTicks = Info.ProduceHarvestersInterval;
			}
		}

		protected void OrderHarvesters(IBot bot)
		{
			var toRemove = harvesters.Keys.Where(unitCannotBeOrdered).ToList();
			foreach (var a in toRemove)
				harvesters.Remove(a);

			// Find new harvesters
			// TODO: Look for a more performance-friendly way to update this list
			var newHarvesters = world.ActorsHavingTrait<Harvester>().Where(a => a.Owner == player && !harvesters.ContainsKey(a));
			foreach (var a in newHarvesters)
				harvesters[a] = new HarvesterTraitWrapper(a);

			// Find idle harvesters and give them orders:
			foreach (var h in harvesters)
			{
				if (!h.Key.IsIdle)
				{
					var act = h.Key.CurrentActivity as FindAndDeliverResources;

					// Ignore this actor if FindAndDeliverResources is working fine or it is performing a different activity
					if (act == null || !act.LastSearchFailed)
						continue;
				}

				if (h.Value.Parachutable != null && h.Value.Parachutable.IsInAir)
					continue;

				// Tell the idle harvester to quit slacking:
				var newSafeResourcePatch = FindNextResource(h.Key, h.Value);
				AIUtils.BotDebug($"AI: Harvester {h.Key} is idle. Ordering to {newSafeResourcePatch} in search for new resources.");
				bot.QueueOrder(new Order("Harvest", h.Key, newSafeResourcePatch, false));
			}
		}

		protected void ProduceHarvesters(IBot bot)
		{
			// Less harvesters than refineries - build a new harvester
			var unitBuilder = requestUnitProduction.FirstOrDefault(Exts.IsTraitEnabled);
			if (unitBuilder != null && Info.HarvesterTypes.Any())
			{
				var harvInfo = AIUtils.GetInfoByCommonName(Info.HarvesterTypes, player);
				var numHarvesters = AIUtils.CountActorByCommonName(Info.HarvesterTypes, player);

				if (numHarvesters >= Info.MaxHarvesters)
					return;

				var harvCountTooLow = numHarvesters < AIUtils.CountBuildingByCommonName(Info.RefineryTypes, player) * Info.HarvestersPerRefinery;
				if (harvCountTooLow && unitBuilder.RequestedProductionCount(bot, harvInfo.Name) == 0)
					unitBuilder.RequestUnitProduction(bot, harvInfo.Name);
			}
		}

		Target FindNextResource(Actor actor, HarvesterTraitWrapper harv)
		{
			Func<CPos, bool> isValidResource = cell =>
				harv.Harvester.CanHarvestCell(actor, cell) &&
				claimLayer.CanClaimCell(actor, cell);

			List<CPos> path;
			using (var search =
				PathSearch.ToTargetCellByPredicate(
					world, harv.Locomotor, actor, new[] { actor.Location }, isValidResource, BlockedByActor.Stationary,
					loc => world.FindActorsInCircle(world.Map.CenterOfCell(loc), Info.HarvesterEnemyAvoidanceRadius)
						.Where(u => !u.IsDead && actor.Owner.RelationshipWith(u.Owner) == PlayerRelationship.Enemy)
						.Sum(u => Math.Max(WDist.Zero.Length, Info.HarvesterEnemyAvoidanceRadius.Length - (world.Map.CenterOfCell(loc) - u.CenterPosition).Length))))
				path = pathfinder.FindPath(search);

			if (path.Count == 0)
				return Target.Invalid;

			return Target.FromCell(world, path[0]);
		}
	}
}
