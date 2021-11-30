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
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Manages AI squads.")]
	public class SquadManagerBotModuleCAInfo : ConditionalTraitInfo
	{
		[Desc("Actor types that are valid for naval squads.")]
		public readonly HashSet<string> NavalUnitsTypes = new HashSet<string>();

		[Desc("Actor types that should generally be excluded from attack squads.")]
		public readonly HashSet<string> ExcludeFromSquadsTypes = new HashSet<string>();

		[Desc("Actor types that should generally be excluded from air squads.")]
		public readonly HashSet<string> ExcludeFromAirSquadsTypes = new HashSet<string>();

		[Desc("Actor types that are considered construction yards (base builders).")]
		public readonly HashSet<string> ConstructionYardTypes = new HashSet<string>();

		[Desc("Enemy building types around which to scan for targets for naval squads.")]
		public readonly HashSet<string> NavalProductionTypes = new HashSet<string>();

		[Desc("Limit target types for specific air unit squads.")]
		public readonly Dictionary<string, BitSet<TargetableType>> AirSquadTargetTypes = null;

		[Desc("Enemy building types around which to scan for targets for naval squads.")]
		public readonly HashSet<string> StaticAntiAirTypes = new HashSet<string>();

		[Desc("Air threats to prioritise no matter where they are on the map.")]
		public readonly HashSet<string> BigAirThreats = new HashSet<string>();

		[Desc("Minimum number of units AI must have before attacking.")]
		public readonly int SquadSize = 8;

		[Desc("Random number of up to this many units is added to squad size when creating an attack squad.")]
		public readonly int SquadSizeRandomBonus = 30;

		[Desc("Minimum value of units AI must have before attacking.")]
		public readonly int SquadValue = 0;

		[Desc("Random number of up to this value units is added to squad valuee when creating an attack squad.")]
		public readonly int SquadValueRandomBonus = 0;

		[Desc("Delay (in ticks) between giving out orders to units.")]
		public readonly int AssignRolesInterval = 50;

		[Desc("Delay (in ticks) between attempting rush attacks.")]
		public readonly int RushInterval = 600;

		[Desc("Delay (in ticks) between updating squads.")]
		public readonly int AttackForceInterval = 75;

		[Desc("Minimum delay (in ticks) between creating squads.")]
		public readonly int MinimumAttackForceDelay = 0;

		[Desc("Radius in cells around enemy BaseBuilder (Construction Yard) where AI scans for targets to rush.")]
		public readonly int RushAttackScanRadius = 15;

		[Desc("Radius in cells around the base that should be scanned for units to be protected.")]
		public readonly int ProtectUnitScanRadius = 15;

		[Desc("Maximum distance in cells from center of the base when checking for MCV deployment location.",
			"Only applies if RestrictMCVDeploymentFallbackToBase is enabled and there's at least one construction yard.")]
		public readonly int MaxBaseRadius = 20;

		[Desc("Radius in cells that squads should scan for enemies around their position while idle.")]
		public readonly int IdleScanRadius = 10;

		[Desc("Radius in cells that squads should scan for danger around their position to make flee decisions.")]
		public readonly int DangerScanRadius = 10;

		[Desc("Radius in cells that attack squads should scan for enemies around their position when trying to attack.")]
		public readonly int AttackScanRadius = 12;

		[Desc("Radius in cells that protecting squads should scan for enemies around their position.")]
		public readonly int ProtectionScanRadius = 8;

		[Desc("Percent change for air squads (that can attack aircraft) to prioritise enemy aircraft.")]
		public readonly int AirToAirPriority = 85;

		[Desc("Enemy target types to never target.")]
		public readonly BitSet<TargetableType> IgnoredEnemyTargetTypes = default(BitSet<TargetableType>);

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (DangerScanRadius <= 0)
				throw new YamlException("DangerScanRadius must be greater than zero.");
		}

		public override object Create(ActorInitializer init) { return new SquadManagerBotModuleCA(init.Self, this); }
	}

	public class SquadManagerBotModuleCA : ConditionalTrait<SquadManagerBotModuleCAInfo>, IBotEnabled, IBotTick, IBotRespondToAttack, IBotPositionsUpdated, IGameSaveTraitData
	{
		public CPos GetRandomBaseCenter()
		{
			var randomConstructionYard = World.Actors.Where(a => a.Owner == Player &&
				Info.ConstructionYardTypes.Contains(a.Info.Name))
				.RandomOrDefault(World.LocalRandom);

			return randomConstructionYard != null ? randomConstructionYard.Location : initialBaseCenter;
		}

		public readonly World World;
		public readonly Player Player;

		readonly Predicate<Actor> unitCannotBeOrdered;
		readonly List<Actor> unitsHangingAroundTheBase = new List<Actor>();

		// Units that the bot already knows about. Any unit not on this list needs to be given a role.
		readonly List<Actor> activeUnits = new List<Actor>();

		public List<SquadCA> Squads = new List<SquadCA>();

		IBot bot;
		IBotPositionsUpdated[] notifyPositionsUpdated;
		IBotNotifyIdleBaseUnits[] notifyIdleBaseUnits;
		IBotAircraftBuilder[] aircraftBuilders;

		CPos initialBaseCenter;

		int rushTicks;
		int assignRolesTicks;
		int attackForceTicks;
		int minAttackForceDelayTicks;

		int desiredAttackForceValue;
		int desiredAttackForceSize;

		public SquadManagerBotModuleCA(Actor self, SquadManagerBotModuleCAInfo info)
			: base(info)
		{
			World = self.World;
			Player = self.Owner;

			unitCannotBeOrdered = a => a == null || a.Owner != Player || a.IsDead || !a.IsInWorld;
		}

		// Use for proactive targeting.
		public bool IsPreferredEnemyUnit(Actor a)
		{
			if (a == null || a.IsDead || Player.RelationshipWith(a.Owner) != PlayerRelationship.Enemy || a.Info.HasTraitInfo<HuskInfo>() || a.Info.HasTraitInfo<AircraftInfo>() || a.Info.HasTraitInfo<CarrierSlaveInfo>())
				return false;

			var targetTypes = a.GetEnabledTargetTypes();
			return !targetTypes.IsEmpty && !targetTypes.Overlaps(Info.IgnoredEnemyTargetTypes);
		}

		public bool IsPreferredEnemyAircraft(Actor a)
		{
			if (a == null || a.IsDead || Player.RelationshipWith(a.Owner) != PlayerRelationship.Enemy || a.Info.HasTraitInfo<HuskInfo>() || !a.Info.HasTraitInfo<AircraftInfo>() || !a.Info.HasTraitInfo<AttackBaseInfo>() || a.Info.HasTraitInfo<CarrierSlaveInfo>())
				return false;

			var targetTypes = a.GetEnabledTargetTypes();
			return !targetTypes.IsEmpty && !targetTypes.Overlaps(Info.IgnoredEnemyTargetTypes);
		}

		public bool IsAirSquadTargetType(Actor a, SquadCA owner)
		{
			var airSquadUnitType = owner.Units.First().Info.Name;
			if (owner.SquadManager.Info.AirSquadTargetTypes.ContainsKey(airSquadUnitType))
			{
				var targetTypes = a.GetEnabledTargetTypes();

				if (targetTypes.IsEmpty || !targetTypes.Overlaps(owner.SquadManager.Info.AirSquadTargetTypes[airSquadUnitType]))
					return false;
			}

			return true;
		}

		public bool IsNotHiddenUnit(Actor a)
		{
			var visModifiers = a.TraitsImplementing<IVisibilityModifier>();
			foreach (var v in visModifiers)
				if (!v.IsVisible(a, Player))
					return false;

			return true;
		}

		protected override void Created(Actor self)
		{
			notifyPositionsUpdated = self.Owner.PlayerActor.TraitsImplementing<IBotPositionsUpdated>().ToArray();
			notifyIdleBaseUnits = self.Owner.PlayerActor.TraitsImplementing<IBotNotifyIdleBaseUnits>().ToArray();
			aircraftBuilders = self.Owner.PlayerActor.TraitsImplementing<IBotAircraftBuilder>().ToArray();
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs trying to rush in the same tick, randomize their initial rush a little.
			var smallFractionOfRushInterval = Info.RushInterval / 20;
			rushTicks = World.LocalRandom.Next(Info.RushInterval - smallFractionOfRushInterval, Info.RushInterval + smallFractionOfRushInterval);

			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			assignRolesTicks = World.LocalRandom.Next(0, Info.AssignRolesInterval);
			attackForceTicks = World.LocalRandom.Next(0, Info.AttackForceInterval);
			minAttackForceDelayTicks = World.LocalRandom.Next(0, Info.MinimumAttackForceDelay);
		}

		void IBotEnabled.BotEnabled(IBot bot)
		{
			this.bot = bot;
		}

		void IBotTick.BotTick(IBot bot)
		{
			AssignRolesToIdleUnits(bot);
		}

		internal Actor FindClosestEnemy(WPos pos)
		{
			var units = World.Actors.Where(IsPreferredEnemyUnit);
			return units.Where(IsNotHiddenUnit).ClosestTo(pos) ?? units.ClosestTo(pos);
		}

		internal Actor FindClosestEnemy(WPos pos, WDist radius)
		{
			return World.FindActorsInCircle(pos, radius).Where(a => IsPreferredEnemyUnit(a) && IsNotHiddenUnit(a)).ClosestTo(pos);
		}

		void CleanSquads()
		{
			Squads.RemoveAll(s => !s.IsValid);
			foreach (var s in Squads)
			{
				s.Units.RemoveAll(unitCannotBeOrdered);

				if (s.Type == SquadCAType.Air)
				{
					s.NewUnits.RemoveWhere(unitCannotBeOrdered);
					s.RearmingUnits.RemoveWhere(unitCannotBeOrdered);
					s.WaitingUnits.RemoveWhere(unitCannotBeOrdered);
				}
			}
		}

		// HACK: Use of this function requires that there is one squad of this type.
		SquadCA GetSquadOfType(SquadCAType type)
		{
			return Squads.FirstOrDefault(s => s.Type == type);
		}

		SquadCA RegisterNewSquad(IBot bot, SquadCAType type, Actor target = null)
		{
			var ret = new SquadCA(bot, this, type, target);
			Squads.Add(ret);
			return ret;
		}

		void AssignRolesToIdleUnits(IBot bot)
		{
			CleanSquads();

			activeUnits.RemoveAll(unitCannotBeOrdered);
			unitsHangingAroundTheBase.RemoveAll(unitCannotBeOrdered);
			foreach (var n in notifyIdleBaseUnits)
				n.UpdatedIdleBaseUnits(unitsHangingAroundTheBase);

			if (--rushTicks <= 0)
			{
				rushTicks = Info.RushInterval;
				TryToRushAttack(bot);
			}

			if (--attackForceTicks <= 0)
			{
				attackForceTicks = Info.AttackForceInterval;
				foreach (var s in Squads)
					s.Update();
			}

			if (--assignRolesTicks <= 0)
			{
				assignRolesTicks = Info.AssignRolesInterval;
				FindNewUnits(bot);
			}

			if (--minAttackForceDelayTicks <= 0)
			{
				minAttackForceDelayTicks = Info.MinimumAttackForceDelay;
				CreateAttackForce(bot);
			}
		}

		void FindNewUnits(IBot bot)
		{
			var newUnits = World.ActorsHavingTrait<IPositionable>()
				.Where(a => a.Owner == Player &&
					!Info.ExcludeFromSquadsTypes.Contains(a.Info.Name) &&
					!activeUnits.Contains(a));

			foreach (var a in newUnits)
			{
				if (a.Info.HasTraitInfo<AircraftInfo>() && a.Info.HasTraitInfo<AttackBaseInfo>() && !Info.ExcludeFromAirSquadsTypes.Contains(a.Info.Name))
				{
					var airSquads = Squads.Where(s => s.Type == SquadCAType.Air);
					var matchingAirSquadFound = false;

					foreach (var airSquad in airSquads)
					{
						if (airSquad.Units.Any(u => u.Info.Name == a.Info.Name))
						{
							airSquad.Units.Add(a);
							airSquad.NewUnits.Add(a);
							matchingAirSquadFound = true;
							break;
						}
					}

					if (!matchingAirSquadFound)
					{
						var newAirSquad = RegisterNewSquad(bot, SquadCAType.Air);
						newAirSquad.Units.Add(a);
						newAirSquad.NewUnits.Add(a);
					}
				}
				else if (Info.NavalUnitsTypes.Contains(a.Info.Name))
				{
					var navalSquads = Squads.Where(s => s.Type == SquadCAType.Naval);
					var matchingNavalSquadFound = false;

					foreach (var navalSquad in navalSquads)
					{
						if (navalSquad.Units.Any(u => u.Info.Name == a.Info.Name))
						{
							navalSquad.Units.Add(a);
							matchingNavalSquadFound = true;
							break;
						}
					}

					if (!matchingNavalSquadFound)
					{
						var newNavalSquad = RegisterNewSquad(bot, SquadCAType.Naval);
						newNavalSquad.Units.Add(a);
					}
				}
				else
					unitsHangingAroundTheBase.Add(a);

				activeUnits.Add(a);
			}

			// Notifying here rather than inside the loop, should be fine and saves a bunch of notification calls
			foreach (var n in notifyIdleBaseUnits)
				n.UpdatedIdleBaseUnits(unitsHangingAroundTheBase);
		}

		void CreateAttackForce(IBot bot)
		{
			// Create an attack force when we have enough units around our base.
			// (don't bother leaving any behind for defense)
			var idleUnitsValue = 0;

			if (Info.SquadValue > 0)
			{
				foreach (var a in unitsHangingAroundTheBase)
				{
					var valued = a.Info.TraitInfoOrDefault<ValuedInfo>();
					if (valued != null)
						idleUnitsValue += valued.Cost;
				}
			}

			if (idleUnitsValue >= desiredAttackForceValue && unitsHangingAroundTheBase.Count >= desiredAttackForceSize)
			{
				var attackForce = RegisterNewSquad(bot, SquadCAType.Assault);

				foreach (var a in unitsHangingAroundTheBase)
					attackForce.Units.Add(a);

				unitsHangingAroundTheBase.Clear();
				foreach (var n in notifyIdleBaseUnits)
					n.UpdatedIdleBaseUnits(unitsHangingAroundTheBase);

				SetNextDesiredAttackForce();
			}
		}

		void SetNextDesiredAttackForce()
		{
			desiredAttackForceSize = Info.SquadSize + World.LocalRandom.Next(Info.SquadSizeRandomBonus);
			desiredAttackForceValue = 0;

			if (Info.SquadValue > 0)
				desiredAttackForceValue = Info.SquadValue + World.LocalRandom.Next(Info.SquadValueRandomBonus);
		}

		void TryToRushAttack(IBot bot)
		{
			var allEnemyBaseBuilder = AIUtils.FindEnemiesByCommonName(Info.ConstructionYardTypes, Player);

			// TODO: This should use common names & ExcludeFromSquads instead of hardcoding TraitInfo checks
			var ownUnits = activeUnits
				.Where(unit => unit.IsIdle && unit.Info.HasTraitInfo<AttackBaseInfo>()
					&& !unit.Info.HasTraitInfo<AircraftInfo>() && !Info.NavalUnitsTypes.Contains(unit.Info.Name) && !unit.Info.HasTraitInfo<HarvesterInfo>()).ToList();

			if (!allEnemyBaseBuilder.Any() || ownUnits.Count < Info.SquadSize)
				return;

			foreach (var b in allEnemyBaseBuilder)
			{
				// Don't rush enemy aircraft!
				var enemies = World.FindActorsInCircle(b.CenterPosition, WDist.FromCells(Info.RushAttackScanRadius))
					.Where(unit => IsPreferredEnemyUnit(unit) && unit.Info.HasTraitInfo<AttackBaseInfo>() && !(unit.Info.HasTraitInfo<AircraftInfo>() && !Info.ExcludeFromAirSquadsTypes.Contains(unit.Info.Name)) && !Info.NavalUnitsTypes.Contains(unit.Info.Name)).ToList();

				if (AttackOrFleeFuzzyCA.Rush.CanAttack(ownUnits, enemies))
				{
					var target = enemies.Any() ? enemies.Random(World.LocalRandom) : b;
					var rush = GetSquadOfType(SquadCAType.Rush);
					if (rush == null)
						rush = RegisterNewSquad(bot, SquadCAType.Rush, target);

					foreach (var a3 in ownUnits)
						rush.Units.Add(a3);

					return;
				}
			}
		}

		void ProtectOwn(IBot bot, Actor attacker)
		{
			var protectSq = GetSquadOfType(SquadCAType.Protection);
			if (protectSq == null)
				protectSq = RegisterNewSquad(bot, SquadCAType.Protection, attacker);

			if (!protectSq.IsTargetValid)
				protectSq.TargetActor = attacker;

			if (!protectSq.IsValid)
			{
				var ownUnits = World.FindActorsInCircle(World.Map.CenterOfCell(GetRandomBaseCenter()), WDist.FromCells(Info.ProtectUnitScanRadius))
					.Where(unit => unit.Owner == Player && !unit.Info.HasTraitInfo<BuildingInfo>() && !unit.Info.HasTraitInfo<HarvesterInfo>()
						&& !unit.Info.HasTraitInfo<AircraftInfo>() && unit.Info.HasTraitInfo<AttackBaseInfo>());

				foreach (var a in ownUnits)
					protectSq.Units.Add(a);
			}
		}

		void IBotPositionsUpdated.UpdatedBaseCenter(CPos newLocation)
		{
			initialBaseCenter = newLocation;
		}

		void IBotPositionsUpdated.UpdatedDefenseCenter(CPos newLocation) { }

		void IBotRespondToAttack.RespondToAttack(IBot bot, Actor self, AttackInfo e)
		{
			if (!IsPreferredEnemyUnit(e.Attacker))
				return;

			// Protected priority assets, MCVs, harvesters and buildings
			// TODO: Use *CommonNames, instead of hard-coding trait(info)s.
			if (self.Info.HasTraitInfo<HarvesterInfo>() || self.Info.HasTraitInfo<BuildingInfo>() || self.Info.HasTraitInfo<BaseBuildingInfo>())
			{
				foreach (var n in notifyPositionsUpdated)
					n.UpdatedDefenseCenter(e.Attacker.Location);

				ProtectOwn(bot, e.Attacker);
			}
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (IsTraitDisabled)
				return null;

			return new List<MiniYamlNode>()
			{
				new MiniYamlNode("Squads", "", Squads.Select(s => new MiniYamlNode("Squad", s.Serialize())).ToList()),
				new MiniYamlNode("InitialBaseCenter", FieldSaver.FormatValue(initialBaseCenter)),
				new MiniYamlNode("UnitsHangingAroundTheBase", FieldSaver.FormatValue(unitsHangingAroundTheBase
					.Where(a => !unitCannotBeOrdered(a))
					.Select(a => a.ActorID)
					.ToArray())),
				new MiniYamlNode("ActiveUnits", FieldSaver.FormatValue(activeUnits
					.Where(a => !unitCannotBeOrdered(a))
					.Select(a => a.ActorID)
					.ToArray())),
				new MiniYamlNode("RushTicks", FieldSaver.FormatValue(rushTicks)),
				new MiniYamlNode("AssignRolesTicks", FieldSaver.FormatValue(assignRolesTicks)),
				new MiniYamlNode("AttackForceTicks", FieldSaver.FormatValue(attackForceTicks)),
				new MiniYamlNode("MinAttackForceDelayTicks", FieldSaver.FormatValue(minAttackForceDelayTicks)),
			};
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, List<MiniYamlNode> data)
		{
			if (self.World.IsReplay)
				return;

			var initialBaseCenterNode = data.FirstOrDefault(n => n.Key == "InitialBaseCenter");
			if (initialBaseCenterNode != null)
				initialBaseCenter = FieldLoader.GetValue<CPos>("InitialBaseCenter", initialBaseCenterNode.Value.Value);

			var unitsHangingAroundTheBaseNode = data.FirstOrDefault(n => n.Key == "UnitsHangingAroundTheBase");
			if (unitsHangingAroundTheBaseNode != null)
			{
				unitsHangingAroundTheBase.Clear();
				unitsHangingAroundTheBase.AddRange(FieldLoader.GetValue<uint[]>("UnitsHangingAroundTheBase", unitsHangingAroundTheBaseNode.Value.Value)
					.Select(a => self.World.GetActorById(a)).Where(a => a != null));
			}

			var activeUnitsNode = data.FirstOrDefault(n => n.Key == "ActiveUnits");
			if (activeUnitsNode != null)
			{
				activeUnits.Clear();
				activeUnits.AddRange(FieldLoader.GetValue<uint[]>("ActiveUnits", activeUnitsNode.Value.Value)
					.Select(a => self.World.GetActorById(a)).Where(a => a != null));
			}

			var rushTicksNode = data.FirstOrDefault(n => n.Key == "RushTicks");
			if (rushTicksNode != null)
				rushTicks = FieldLoader.GetValue<int>("RushTicks", rushTicksNode.Value.Value);

			var assignRolesTicksNode = data.FirstOrDefault(n => n.Key == "AssignRolesTicks");
			if (assignRolesTicksNode != null)
				assignRolesTicks = FieldLoader.GetValue<int>("AssignRolesTicks", assignRolesTicksNode.Value.Value);

			var attackForceTicksNode = data.FirstOrDefault(n => n.Key == "AttackForceTicks");
			if (attackForceTicksNode != null)
				attackForceTicks = FieldLoader.GetValue<int>("AttackForceTicks", attackForceTicksNode.Value.Value);

			var minAttackForceDelayTicksNode = data.FirstOrDefault(n => n.Key == "MinAttackForceDelayTicks");
			if (minAttackForceDelayTicksNode != null)
				minAttackForceDelayTicks = FieldLoader.GetValue<int>("MinAttackForceDelayTicks", minAttackForceDelayTicksNode.Value.Value);

			var squadsNode = data.FirstOrDefault(n => n.Key == "Squads");
			if (squadsNode != null)
			{
				Squads.Clear();
				foreach (var n in squadsNode.Value.Nodes)
					Squads.Add(SquadCA.Deserialize(bot, this, n.Value));
			}
		}

		public bool CanBuildMoreOfAircraft(ActorInfo actorInfo)
		{
			foreach (var aircraftBuilder in aircraftBuilders)
			{
				if (!aircraftBuilder.IsTraitEnabled())
					continue;

				if (aircraftBuilder.CanBuildMoreOfAircraft(actorInfo))
					return true;
			}

			return false;
		}
	}
}
