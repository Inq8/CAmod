﻿#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Manages AI MCVs.")]
	public class McvManagerBotModuleCAInfo : ConditionalTraitInfo
	{
		[Desc("Actor types that are considered MCVs (deploy into base builders).")]
		public readonly HashSet<string> McvTypes = new HashSet<string>();

		[Desc("Actor types that are considered construction yards (base builders).")]
		public readonly HashSet<string> ConstructionYardTypes = new HashSet<string>();

		[Desc("Actor types that are able to produce MCVs.")]
		public readonly HashSet<string> McvFactoryTypes = new HashSet<string>();

		[Desc("Try to maintain at least this many ConstructionYardTypes, build an MCV if number is below this.")]
		public readonly int MinimumConstructionYardCount = 1;

		[Desc("Delay (in ticks) between looking for and giving out orders to new MCVs.")]
		public readonly int ScanForNewMcvInterval = 20;

		[Desc("Minimum distance in cells from center of the base when checking for MCV deployment location.")]
		public readonly int MinBaseRadius = 2;

		[Desc("Maximum distance in cells from center of the base when checking for MCV deployment location.",
			"Only applies if RestrictMCVDeploymentFallbackToBase is enabled and there's at least one construction yard.")]
		public readonly int MaxBaseRadius = 20;

		[Desc("Distance in cells from center of the base when checking near by enemies for MCV deployment location.")]
		public readonly int EnemyScanRadius = 8;

		[Desc("Should deployment of additional MCVs be restricted to MaxBaseRadius if explicit deploy locations are missing or occupied?")]
		public readonly bool RestrictMCVDeploymentFallbackToBase = true;

		public override object Create(ActorInitializer init) { return new McvManagerBotModuleCA(init.Self, this); }
	}

	public class McvManagerBotModuleCA : ConditionalTrait<McvManagerBotModuleCAInfo>, IBotTick, IBotPositionsUpdated, IGameSaveTraitData
	{
		public CPos GetRandomBaseCenter(bool distanceToBaseIsImportant)
		{
			if (distanceToBaseIsImportant == false)
				return initialBaseCenter;

			var ti = world.Map.Rules.TerrainInfo;
			var resourceTypeIndices = new BitArray(ti.TerrainTypes.Length);

			foreach (var i in world.Map.Rules.Actors["world"].TraitInfos<ResourceLayerInfo>())
				foreach (var t in i.ResourceTypes)
					resourceTypeIndices.Set(ti.GetTerrainIndex(t.Value.TerrainType), true);

			var randomConstructionYard = world.Actors.Where(a => a.Owner == player &&
				Info.ConstructionYardTypes.Contains(a.Info.Name))
				.RandomOrDefault(world.LocalRandom);

			var newResources = world.Map.FindTilesInAnnulus(randomConstructionYard.Location, Info.MaxBaseRadius, world.Map.Grid.MaximumTileSearchRange)
				.Where(a => resourceTypeIndices.Get(world.Map.GetTerrainIndex(a)))
				.Shuffle(world.LocalRandom).FirstOrDefault();

			return newResources;
		}

		readonly World world;
		readonly Player player;

		readonly Predicate<Actor> unitCannotBeOrdered;

		IBotPositionsUpdated[] notifyPositionsUpdated;
		IBotRequestUnitProduction[] requestUnitProduction;

		CPos initialBaseCenter;
		int scanInterval;
		bool firstTick = true;

		public McvManagerBotModuleCA(Actor self, McvManagerBotModuleCAInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			unitCannotBeOrdered = a => a.Owner != player || a.IsDead || !a.IsInWorld;
		}

		protected override void Created(Actor self)
		{
			// Special case handling is required for the Player actor.
			// Created is called before Player.PlayerActor is assigned,
			// so we must query player traits from self, which refers
			// for bot modules always to the Player actor.
			notifyPositionsUpdated = self.TraitsImplementing<IBotPositionsUpdated>().ToArray();
			requestUnitProduction = self.TraitsImplementing<IBotRequestUnitProduction>().ToArray();
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			scanInterval = world.LocalRandom.Next(Info.ScanForNewMcvInterval, Info.ScanForNewMcvInterval * 2);
		}

		void IBotPositionsUpdated.UpdatedBaseCenter(CPos newLocation)
		{
			initialBaseCenter = newLocation;
		}

		void IBotPositionsUpdated.UpdatedDefenseCenter(CPos newLocation) { }

		void IBotTick.BotTick(IBot bot)
		{
			if (firstTick)
			{
				DeployMcvs(bot, false);
				firstTick = false;
			}

			if (--scanInterval <= 0)
			{
				scanInterval = Info.ScanForNewMcvInterval;
				DeployMcvs(bot, true);

				// No construction yards - Build a new MCV
				if (ShouldBuildMCV())
				{
					var unitBuilder = requestUnitProduction.FirstOrDefault(Exts.IsTraitEnabled);
					if (unitBuilder != null)
					{
						var mcvInfo = AIUtils.GetInfoByCommonName(Info.McvTypes, player);
						if (unitBuilder.RequestedProductionCount(bot, mcvInfo.Name) == 0)
							unitBuilder.RequestUnitProduction(bot, mcvInfo.Name);
					}
				}
			}
		}

		bool ShouldBuildMCV()
		{
			// Only build MCV if we don't already have one in the field.
			var allowedToBuildMCV = AIUtils.CountActorByCommonName(Info.McvTypes, player) == 0;
			if (!allowedToBuildMCV)
				return false;

			// Build MCV if we don't have the desired number of construction yards, unless we have no factory (can't build it).
			return AIUtils.CountBuildingByCommonName(Info.ConstructionYardTypes, player) < Info.MinimumConstructionYardCount &&
				AIUtils.CountBuildingByCommonName(Info.McvFactoryTypes, player) > 0;
		}

		void DeployMcvs(IBot bot, bool chooseLocation)
		{
			var newMCVs = world.ActorsHavingTrait<Transforms>()
				.Where(a => a.Owner == player && a.IsIdle && Info.McvTypes.Contains(a.Info.Name));

			foreach (var mcv in newMCVs)
				DeployMcv(bot, mcv, chooseLocation);
		}

		// Find any MCV and deploy them at a sensible location.
		void DeployMcv(IBot bot, Actor mcv, bool move)
		{
			if (move)
			{
				// If we lack a base, we need to make sure we don't restrict deployment of the MCV to the base!
				var restrictToBase = AIUtils.CountBuildingByCommonName(Info.ConstructionYardTypes, player) > 0;

				var transformsInfo = mcv.Info.TraitInfo<TransformsInfo>();
				var desiredLocation = ChooseMcvDeployLocation(transformsInfo.IntoActor, transformsInfo.Offset, restrictToBase);
				if (desiredLocation == null)
					return;

				bot.QueueOrder(new Order("Move", mcv, Target.FromCell(world, desiredLocation.Value), true));
			}

			// If the MCV has to move first, we can't be sure it reaches the destination alive, so we only
			// update base and defense center if the MCV is deployed immediately (i.e. at game start).
			// TODO: This could be adressed via INotifyTransform.
			foreach (var n in notifyPositionsUpdated)
			{
				n.UpdatedBaseCenter(mcv.Location);
				n.UpdatedDefenseCenter(mcv.Location);
			}

			bot.QueueOrder(new Order("DeployTransform", mcv, true));
		}

		CPos? ChooseMcvDeployLocation(string actorType, CVec offset, bool distanceToBaseIsImportant)
		{
			var actorInfo = world.Map.Rules.Actors[actorType];
			var bi = actorInfo.TraitInfoOrDefault<BuildingInfo>();
			if (bi == null)
				return null;

			// Find the buildable cell that is closest to pos and centered around center
			Func<CPos, CPos, int, int, CPos?> findPos = (center, target, minRange, maxRange) =>
			{
				var cells = world.Map.FindTilesInAnnulus(center, minRange, maxRange);

				// Sort by distance to target if we have one
				if (center != target)
					cells = cells.OrderBy(c => (c - target).LengthSquared);
				else
					cells = cells.Shuffle(world.LocalRandom);

				foreach (var cell in cells)
					if (world.CanPlaceBuilding(cell + offset, actorInfo, bi, null))
						return cell;

				return null;
			};

			var baseCenter = GetRandomBaseCenter(distanceToBaseIsImportant);

			CPos? bc = findPos(baseCenter, baseCenter, Info.MinBaseRadius,
				distanceToBaseIsImportant ? Info.MaxBaseRadius : world.Map.Grid.MaximumTileSearchRange);

			if (!bc.HasValue)
				return null;

			baseCenter = bc.Value;

			var wPos = world.Map.CenterOfCell(bc.Value);
			var newBaseRadius = new WDist(Info.EnemyScanRadius * 1024);

			var enemies = world.FindActorsInCircle(wPos, newBaseRadius)
				.Where(a => !a.Disposed && player.RelationshipWith(a.Owner) == PlayerRelationship.Enemy && a.Info.HasTraitInfo<BuildingInfo>());

			if (enemies.Count() > 0)
				return null;

			var self = world.FindActorsInCircle(wPos, newBaseRadius)
				.Where(a => !a.Disposed && a.Owner == player
					&& (Info.McvTypes.Contains(a.Info.Name) || (Info.ConstructionYardTypes.Contains(a.Info.Name) && a.Info.HasTraitInfo<BuildingInfo>())));

			if (self.Count() > 0)
				return null;

			return baseCenter;
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (IsTraitDisabled)
				return null;

			return new List<MiniYamlNode>()
			{
				new MiniYamlNode("InitialBaseCenter", FieldSaver.FormatValue(initialBaseCenter))
			};
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, MiniYaml data)
		{
			if (self.World.IsReplay)
				return;

			var initialBaseCenterNode = data.NodeWithKeyOrDefault("InitialBaseCenter");
			if (initialBaseCenterNode != null)
				initialBaseCenter = FieldLoader.GetValue<CPos>("InitialBaseCenter", initialBaseCenterNode.Value.Value);
		}
	}
}
