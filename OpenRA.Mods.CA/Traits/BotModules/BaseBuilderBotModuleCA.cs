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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Manages AI base construction.")]
	public class BaseBuilderBotModuleCAInfo : ConditionalTraitInfo
	{
		[Desc("Tells the AI what building types are considered construction yards.")]
		public readonly HashSet<string> ConstructionYardTypes = new HashSet<string>();

		[Desc("Tells the AI what building types are considered vehicle production facilities.")]
		public readonly HashSet<string> VehiclesFactoryTypes = new HashSet<string>();

		[Desc("Tells the AI what building types are considered refineries.")]
		public readonly HashSet<string> RefineryTypes = new HashSet<string>();

		[Desc("Tells the AI what building types are considered power plants.")]
		public readonly HashSet<string> PowerTypes = new HashSet<string>();

		[Desc("Tells the AI what building types are considered infantry production facilities.")]
		public readonly HashSet<string> BarracksTypes = new HashSet<string>();

		[Desc("Tells the AI what building types are considered anti-air defenses.")]
		public readonly HashSet<string> AntiAirTypes = new HashSet<string>();

		[Desc("Tells the AI what building types are considered defenses.")]
		public readonly HashSet<string> DefenseTypes = new HashSet<string>();

		[Desc("Tells the AI what building types are considered production facilities.")]
		public readonly HashSet<string> ProductionTypes = new HashSet<string>();

		[Desc("Tells the AI what building types are considered naval production facilities.")]
		public readonly HashSet<string> NavalProductionTypes = new HashSet<string>();

		[Desc("Tells the AI what building types are considered silos (resource storage).")]
		public readonly HashSet<string> SiloTypes = new HashSet<string>();

		[Desc("Production queues AI uses for buildings.")]
		public readonly HashSet<string> BuildingQueues = new HashSet<string> { "Building" };

		[Desc("Production queues AI uses for defenses.")]
		public readonly HashSet<string> DefenseQueues = new HashSet<string> { "Defense" };

		[Desc("Minimum distance in cells from center of the base when checking for building placement.")]
		public readonly int MinBaseRadius = 2;

		[Desc("Radius in cells around the center of the base to expand.")]
		public readonly int MaxBaseRadius = 20;

		[Desc("Maximum number of extra refineries to build (in addition to 2 per construction yard).")]
		public readonly int MaxExtraRefineries = 1;

		[Desc("Minimum excess power the AI should try to maintain.")]
		public readonly int MinimumExcessPower = 0;

		[Desc("The targeted excess power the AI tries to maintain cannot rise above this.")]
		public readonly int MaximumExcessPower = 0;

		[Desc("Increase maintained excess power by this amount for every ExcessPowerIncreaseThreshold of base buildings.")]
		public readonly int ExcessPowerIncrement = 0;

		[Desc("Increase maintained excess power by ExcessPowerIncrement for every N base buildings.")]
		public readonly int ExcessPowerIncreaseThreshold = 1;

		[Desc("Minimum number of refineries to build before building a barracks and a factory.")]
		public readonly int InitialMinimumRefineryCount = 1;

		[Desc("Minimum number of refineries to build after building a barracks and a factory.")]
		public readonly int NormalMinimumRefineryCount = 2;

		[Desc("Additional delay (in ticks) between structure production checks when there is no active production.",
			"StructureProductionRandomBonusDelay is added to this.")]
		public readonly int StructureProductionInactiveDelay = 125;

		[Desc("Additional delay (in ticks) added between structure production checks when actively building things.",
			"Note: this should be at least as large as the typical order latency to avoid duplicated build choices.")]
		public readonly int StructureProductionActiveDelay = 25;

		[Desc("A random delay (in ticks) of up to this is added to active/inactive production delays.")]
		public readonly int StructureProductionRandomBonusDelay = 10;

		[Desc("Delay (in ticks) until retrying to build structure after the last 3 consecutive attempts failed.")]
		public readonly int StructureProductionResumeDelay = 1500;

		[Desc("After how many failed attempts to place a structure should AI give up and wait",
			"for StructureProductionResumeDelay before retrying.")]
		public readonly int MaximumFailedPlacementAttempts = 3;

		[Desc("How many randomly chosen cells with resources to check when deciding refinery placement.")]
		public readonly int MaxResourceCellsToCheck = 3;

		[Desc("Delay (in ticks) until rechecking for new BaseProviders.")]
		public readonly int CheckForNewBasesDelay = 1500;

		[Desc("Chance that the AI will place the defenses in the direction of the closest enemy building.")]
		public readonly int PlaceDefenseTowardsEnemyChance = 100;

		[Desc("Minimum range at which to build defensive structures near a combat hotspot.")]
		public readonly int MinimumDefenseRadius = 5;

		[Desc("Maximum range at which to build defensive structures near a combat hotspot.")]
		public readonly int MaximumDefenseRadius = 20;

		[Desc("Try to build another production building if there is too much cash.")]
		public readonly int NewProductionCashThreshold = 10000;

		[Desc("Only queue construction of a new structure when above this requirement.")]
		public readonly int ProductionMinCashRequirement = 500;

		[Desc("Radius in cells around a factory scanned for rally points by the AI.")]
		public readonly int RallyPointScanRadius = 8;

		[Desc("Radius in cells around each building with ProvideBuildableArea",
			"to check for a 3x3 area of water where naval structures can be built.",
			"Should match maximum adjacency of naval structures.")]
		public readonly int CheckForWaterRadius = 8;

		[Desc("Terrain types which are considered water for base building purposes.")]
		public readonly HashSet<string> WaterTerrainTypes = new HashSet<string> { "Water" };

		[Desc("What buildings to the AI should build.", "What integer percentage of the total base must be this type of building.")]
		public readonly Dictionary<string, int> BuildingFractions = null;

		[Desc("What buildings should the AI have a maximum limit to build.")]
		public readonly Dictionary<string, int> BuildingLimits = null;

		[Desc("When should the AI start building specific buildings.")]
		public readonly Dictionary<string, int> BuildingDelays = null;

		[Desc("Minimum duration between building specific buildings.")]
		public readonly Dictionary<string, int> BuildingIntervals = null;

		[Desc("Enemy building target types I can ignore construction distance from.")]
		public readonly BitSet<TargetableType> IgnoredEnemyBuildingTargetTypes = default(BitSet<TargetableType>);

		[Desc("Unit target types I should not count when scanning for sell condition .")]
		public readonly BitSet<TargetableType> IgnoredUnitTargetTypes = default(BitSet<TargetableType>);

		[Desc("Radius in cells around building being considered for sale to scan for units")]
		public readonly int SellScanRadius = 8;

		public override object Create(ActorInitializer init) { return new BaseBuilderBotModuleCA(init.Self, this); }
	}

	public class BaseBuilderBotModuleCA : ConditionalTrait<BaseBuilderBotModuleCAInfo>, IGameSaveTraitData,
		IBotTick, IBotPositionsUpdated, IBotRespondToAttack
	{
		public CPos GetRandomBaseCenter()
		{
			var randomConstructionYard = world.Actors.Where(a => a.Owner == player &&
				Info.ConstructionYardTypes.Contains(a.Info.Name))
				.RandomOrDefault(world.LocalRandom);

			return randomConstructionYard?.Location ?? initialBaseCenter;
		}

		public CPos DefenseCenter => defenseCenter;

		/// <Summary> Actor, ActorCount </Summary>
		public Dictionary<string, int> BuildingsBeingProduced = null;

		readonly World world;
		readonly Player player;
		PowerManager playerPower;
		PlayerResources playerResources;
		IResourceLayer resourceLayer;
		IBotPositionsUpdated[] positionsUpdatedModules;
		CPos initialBaseCenter;
		CPos defenseCenter;

		readonly BaseBuilderQueueManagerCA[] builders;
		int currentBuilderIndex = 0;

		public BaseBuilderBotModuleCA(Actor self, BaseBuilderBotModuleCAInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			builders = new BaseBuilderQueueManagerCA[info.BuildingQueues.Count + info.DefenseQueues.Count];
		}

		// Use for proactive targeting.
		public bool IsEnemyGroundUnit(Actor a)
		{
			if (a == null || a.IsDead || player.RelationshipWith(a.Owner) != PlayerRelationship.Enemy || a.Info.HasTraitInfo<HuskInfo>() || a.Info.HasTraitInfo<AircraftInfo>() || a.Info.HasTraitInfo<CarrierSlaveInfo>())
				return false;

			var targetTypes = a.GetEnabledTargetTypes();
			return !targetTypes.IsEmpty && !targetTypes.Overlaps(Info.IgnoredUnitTargetTypes);
		}

		public bool IsAllyGroundUnit(Actor a)
		{
			if (a == null || a.IsDead || player.RelationshipWith(a.Owner) != PlayerRelationship.Ally || a.Info.HasTraitInfo<HuskInfo>() || a.Info.HasTraitInfo<AircraftInfo>() || a.Info.HasTraitInfo<CarrierSlaveInfo>())
				return false;

			var targetTypes = a.GetEnabledTargetTypes();
			return !targetTypes.IsEmpty && !targetTypes.Overlaps(Info.IgnoredUnitTargetTypes);
		}

		protected override void Created(Actor self)
		{
			playerPower = self.Owner.PlayerActor.TraitOrDefault<PowerManager>();
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			resourceLayer = self.World.WorldActor.TraitOrDefault<IResourceLayer>();
			positionsUpdatedModules = self.Owner.PlayerActor.TraitsImplementing<IBotPositionsUpdated>().ToArray();
		}

		protected override void TraitEnabled(Actor self)
		{
			var i = 0;

			foreach (var building in Info.BuildingQueues)
			{
				builders[i] = new BaseBuilderQueueManagerCA(this, building, player, playerPower, playerResources, resourceLayer);
				i++;
			}

			foreach (var defense in Info.DefenseQueues)
			{
				builders[i] = new BaseBuilderQueueManagerCA(this, defense, player, playerPower, playerResources, resourceLayer);
				i++;
			}
		}

		void IBotPositionsUpdated.UpdatedBaseCenter(CPos newLocation)
		{
			initialBaseCenter = newLocation;
		}

		void IBotPositionsUpdated.UpdatedDefenseCenter(CPos newLocation)
		{
			defenseCenter = newLocation;
		}

		void IBotTick.BotTick(IBot bot)
		{
			// TODO: this causes pathfinding lag when AI's gets blocked in
			SetRallyPointsForNewProductionBuildings(bot);

			BuildingsBeingProduced = new Dictionary<string, int>();

			// PERF: We tick only one type of valid queue at a time
			// if AI gets enough cash, it can fill all of its queues with enough ticks
			var findQueue = false;
			for (int i = 0, builderIndex = currentBuilderIndex; i < builders.Length; i++)
			{
				if (++builderIndex >= builders.Length)
					builderIndex = 0;

				--builders[builderIndex].WaitTicks;

				var queues = AIUtils.FindQueues(player, builders[builderIndex].Category).ToArray();
				if (queues.Length != 0)
				{
					if (!findQueue)
					{
						currentBuilderIndex = builderIndex;
						findQueue = true;
					}

					// Refresh "BuildingsBeingProduced" only when AI can produce
					if (playerResources.Cash >= Info.ProductionMinCashRequirement)
					{
						foreach (var queue in queues)
						{
							var producing = queue.AllQueued().FirstOrDefault();
							if (producing == null)
								continue;

							if (BuildingsBeingProduced.ContainsKey(producing.Item))
								BuildingsBeingProduced[producing.Item] = BuildingsBeingProduced[producing.Item] + 1;
							else
								BuildingsBeingProduced.Add(producing.Item, 1);
						}
					}
				}
			}

			builders[currentBuilderIndex].Tick(bot);
		}

		void IBotRespondToAttack.RespondToAttack(IBot bot, Actor self, AttackInfo e)
		{
			if (e.Attacker == null || e.Attacker.Disposed)
				return;

			if (e.Attacker.Owner.RelationshipWith(self.Owner) != PlayerRelationship.Enemy)
				return;

			if (!e.Attacker.Info.HasTraitInfo<ITargetableInfo>())
				return;

			if (!self.Info.HasTraitInfo<BuildingInfo>())
				return;

			if (ShouldSell(self, e))
			{
				bot.QueueOrder(new Order("Sell", self, Target.FromActor(self), false)
				{
					SuppressVisualFeedback = true
				});
				AIUtils.BotDebug("AI ({0}): Decided to sell {1}", player.ClientIndex, self);
				return;
			}

			// Protect buildings not suitable for selling
			foreach (var n in positionsUpdatedModules)
				n.UpdatedDefenseCenter(e.Attacker.Location);
		}

		bool ShouldSell(Actor self, AttackInfo e)
		{
			if (!self.Info.HasTraitInfo<SellableInfo>())
				return false;

			if (Info.DefenseTypes.Contains(self.Info.Name))
				return false;

			if (e.DamageState == DamageState.Dead || e.DamageState < DamageState.Medium || e.DamageState == e.PreviousDamageState)
				return false;

			var inMainBase = (self.CenterPosition - self.World.Map.CenterOfCell(initialBaseCenter)).Length < WDist.FromCells(28).Length;
			var chanceThreshold = inMainBase ? 95 : 70;

			if (self.World.LocalRandom.Next(100) < chanceThreshold)
				return false;

			if (Info.ConstructionYardTypes.Contains(self.Info.Name) && AIUtils.CountBuildingByCommonName(Info.ConstructionYardTypes, player) <= 1)
				return false;

			if (Info.BarracksTypes.Contains(self.Info.Name) && AIUtils.CountBuildingByCommonName(Info.BarracksTypes, player) <= 1)
				return false;

			if (Info.VehiclesFactoryTypes.Contains(self.Info.Name) && AIUtils.CountBuildingByCommonName(Info.VehiclesFactoryTypes, player) <= 1)
				return false;

			var enemyUnits = self.World.FindActorsInCircle(self.CenterPosition, WDist.FromCells(Info.SellScanRadius)).Where(IsEnemyGroundUnit).ToList();

			if (enemyUnits.Count > 5)
			{
				var allyUnits = self.World.FindActorsInCircle(self.CenterPosition, WDist.FromCells(Info.SellScanRadius)).Where(IsAllyGroundUnit).ToList();

				if (enemyUnits.Count >= allyUnits.Count * 2)
					return true;
			}

			return false;
		}

		void SetRallyPointsForNewProductionBuildings(IBot bot)
		{
			foreach (var rp in world.ActorsWithTrait<RallyPoint>())
			{
				if (rp.Actor.Owner != player)
					continue;

				if (rp.Trait.Path.Count == 0 || !IsRallyPointValid(rp.Trait.Path[0], rp.Actor.Info.TraitInfoOrDefault<BuildingInfo>()))
				{
					bot.QueueOrder(new Order("SetRallyPoint", rp.Actor, Target.FromCell(world, ChooseRallyLocationNear(rp.Actor)), false)
					{
						SuppressVisualFeedback = true
					});
				}
			}
		}

		// Won't work for shipyards...
		CPos ChooseRallyLocationNear(Actor producer)
		{
			var possibleRallyPoints = world.Map.FindTilesInCircle(producer.Location, Info.RallyPointScanRadius)
				.Where(c => IsRallyPointValid(c, producer.Info.TraitInfoOrDefault<BuildingInfo>()));

			if (!possibleRallyPoints.Any())
			{
				AIUtils.BotDebug("{0} has no possible rallypoint near {1}", producer.Owner, producer.Location);
				return producer.Location;
			}

			return possibleRallyPoints.Random(world.LocalRandom);
		}

		bool IsRallyPointValid(CPos x, BuildingInfo info)
		{
			return info != null && world.IsCellBuildable(x, null, info);
		}

		public bool HasMaxRefineries
		{
			get
			{
				var currentRefineryCount = AIUtils.CountBuildingByCommonName(Info.RefineryTypes, player);

				foreach (var r in Info.RefineryTypes)
				{
					if (BuildingsBeingProduced != null && BuildingsBeingProduced.ContainsKey(r))
						currentRefineryCount += BuildingsBeingProduced[r];
				}

				var currentConstructionYardCount = AIUtils.CountBuildingByCommonName(Info.ConstructionYardTypes, player);
				return currentRefineryCount >= currentConstructionYardCount * 2 + Info.MaxExtraRefineries;
			}
		}

		public bool HasAdequateRefineryCount
		{
			get
			{
				var desiredAmount = HasAdequateBarracksCount && HasAdequateFactoryCount ? Info.NormalMinimumRefineryCount : Info.InitialMinimumRefineryCount;

				// Require at least one refinery, unless we can't build it.
				return AIUtils.CountBuildingByCommonName(Info.RefineryTypes, player) >= desiredAmount ||
					AIUtils.CountBuildingByCommonName(Info.PowerTypes, player) == 0 ||
					AIUtils.CountBuildingByCommonName(Info.ConstructionYardTypes, player) == 0;
			}
		}

		public bool HasAdequateBarracksCount
		{
			get
			{
				// Require at least one barracks, unless we can't build it.
				return AIUtils.CountBuildingByCommonName(Info.BarracksTypes, player) >= 1 ||
					AIUtils.CountBuildingByCommonName(Info.PowerTypes, player) == 0 ||
					AIUtils.CountBuildingByCommonName(Info.RefineryTypes, player) == 0 ||
					AIUtils.CountBuildingByCommonName(Info.ConstructionYardTypes, player) == 0;
			}
		}

		public bool HasAdequateFactoryCount
		{
			get
			{
				// Require at least one factory, unless we can't build it.
				return AIUtils.CountBuildingByCommonName(Info.VehiclesFactoryTypes, player) >= 1 ||
					AIUtils.CountBuildingByCommonName(Info.PowerTypes, player) == 0 ||
					AIUtils.CountBuildingByCommonName(Info.RefineryTypes, player) == 0 ||
					AIUtils.CountBuildingByCommonName(Info.ConstructionYardTypes, player) == 0;
			}
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (IsTraitDisabled)
				return null;

			return new List<MiniYamlNode>()
			{
				new MiniYamlNode("InitialBaseCenter", FieldSaver.FormatValue(initialBaseCenter)),
				new MiniYamlNode("DefenseCenter", FieldSaver.FormatValue(defenseCenter))
			};
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, List<MiniYamlNode> data)
		{
			if (self.World.IsReplay)
				return;

			var initialBaseCenterNode = data.FirstOrDefault(n => n.Key == "InitialBaseCenter");
			if (initialBaseCenterNode != null)
				initialBaseCenter = FieldLoader.GetValue<CPos>("InitialBaseCenter", initialBaseCenterNode.Value.Value);

			var defenseCenterNode = data.FirstOrDefault(n => n.Key == "DefenseCenter");
			if (defenseCenterNode != null)
				defenseCenter = FieldLoader.GetValue<CPos>("DefenseCenter", defenseCenterNode.Value.Value);
		}
	}
}
