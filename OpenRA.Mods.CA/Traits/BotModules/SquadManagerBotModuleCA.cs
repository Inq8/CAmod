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
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Manages AI squads.")]
	public class SquadManagerBotModuleCAInfo : ConditionalTraitInfo
	{
		[ActorReference]
		[Desc("Actor types that are valid for naval squads.")]
		public readonly HashSet<string> NavalUnitsTypes = new HashSet<string>();

		[ActorReference]
		[Desc("Actor types that are excluded from ground attacks.")]
		public readonly HashSet<string> AirUnitsTypes = new HashSet<string>();

		[ActorReference]
		[Desc("Actor types that are valid for harasser squads.")]
		public readonly HashSet<string> HarasserTypes = new HashSet<string>();

		[ActorReference]
		[Desc("Actor types that should generally be excluded from attack squads.")]
		public readonly HashSet<string> ExcludeFromSquadsTypes = new HashSet<string>();

		[ActorReference]
		[Desc("Actor types that are considered construction yards (base builders).")]
		public readonly HashSet<string> ConstructionYardTypes = new HashSet<string>();

		[ActorReference]
		[Desc("Enemy building types around which to scan for targets for naval squads.")]
		public readonly HashSet<string> NavalProductionTypes = new HashSet<string>();

		[ActorReference]
		[Desc("Own actor types that are prioritized when defending.")]
		public readonly HashSet<string> ProtectionTypes = new();

		[Desc("Target types are used for identifying aircraft.")]
		public readonly BitSet<TargetableType> AircraftTargetType = new("Air");

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

		[Desc("Enemy target types to never target.")]
		public readonly BitSet<TargetableType> IgnoredEnemyTargetTypes = default;

		// ==============================
		// === CA-SPECIFIC PROPERTIES ===
		// ==============================

		[Desc("Delay (in ticks) between issuing a protection order.")]
		public readonly int ProtectInterval = 50;

		[Desc("Minimum value of units AI must have before attacking.")]
		public readonly int SquadValue = 0;

		[Desc("Random number of up to this value units is added to squad value when creating an attack squad.")]
		public readonly int SquadValueRandomEarlyBonus = 0;

		[Desc("The random number added to squad value increases to this value over the first 20 minutes.")]
		public readonly int SquadValueRandomLateBonus = 0;

		[Desc("Percent change for ground squads to attack a random priority target rather than the closest enemy.")]
		public readonly int HighValueTargetPriority = 0;

		[Desc("Actor types to prioritise based on HighValueTargetPriority.")]
		public readonly HashSet<string> HighValueTargetTypes = new HashSet<string>();

		[Desc("Percent change for air squads (that can attack aircraft) to prioritise enemy aircraft.")]
		public readonly int AirToAirPriority = 85;

		[Desc("Limit target types for specific air unit squads.")]
		public readonly Dictionary<string, BitSet<TargetableType>> AirSquadTargetArmorTypes = null;

		[Desc("Enemy building types around which to scan for targets for naval squads.")]
		public readonly HashSet<string> StaticAntiAirTypes = new HashSet<string>();

		[Desc("Air threats to prioritise above all others.")]
		public readonly HashSet<string> BigAirThreats = new HashSet<string>();

		[Desc("Percent chance to take a less direct route to targets.")]
		public readonly int IndirectRouteChance = 50;

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (DangerScanRadius <= 0)
				throw new YamlException("DangerScanRadius must be greater than zero.");

			if (SquadValueRandomEarlyBonus > SquadValueRandomLateBonus)
				throw new YamlException("SquadValueRandomEarlyBonus cannot be greater than SquadValueRandomLateBonus.");
		}

		public override object Create(ActorInitializer init) { return new SquadManagerBotModuleCA(init.Self, this); }
	}

	public class SquadManagerBotModuleCA : ConditionalTrait<SquadManagerBotModuleCAInfo>, IBotEnabled, IBotTick,
		IBotRespondToAttack, IBotPositionsUpdated, IGameSaveTraitData, INotifyActorDisposing
	{
		public CPos GetRandomBaseCenter()
		{
			var randomConstructionYard = constructionYardBuildings.Actors
				.Where(a => a.Owner == Player)
				.RandomOrDefault(World.LocalRandom);

			return randomConstructionYard?.Location ?? initialBaseCenter;
		}

		public readonly World World;
		public readonly Player Player;

		readonly Predicate<Actor> unitCannotBeOrdered;
		readonly List<Actor> unitsHangingAroundTheBase = new();

		// Units that the bot already knows about. Any unit not on this list needs to be given a role.
		readonly HashSet<Actor> activeUnits = new();

		public List<SquadCA> Squads = new();
		readonly Stack<SquadCA> squadsPendingUpdate = new();
		readonly ActorIndex.NamesAndTrait<BuildingInfo> constructionYardBuildings;

		IBot bot;
		IBotPositionsUpdated[] notifyPositionsUpdated;
		IBotNotifyIdleBaseUnits[] notifyIdleBaseUnits;
		IBotAircraftBuilder[] aircraftBuilders;

		CPos initialBaseCenter;

		int rushTicks;
		int assignRolesTicks;
		int attackForceTicks;
		int minAttackForceDelayTicks;

		int protectOwnTicks; // CA: Protection interval timing
		Actor protectOwnFrom; // CA: Track what we're protecting from

		int desiredAttackForceValue; // CA: Value-based squad thresholds

		Dictionary<string, int> cachedUnitValues = new();

		public SquadManagerBotModuleCA(Actor self, SquadManagerBotModuleCAInfo info)
			: base(info)
		{
			World = self.World;
			Player = self.Owner;

			unitCannotBeOrdered = a => a == null || a.Owner != Player || a.IsDead || !a.IsInWorld;
			constructionYardBuildings = new ActorIndex.NamesAndTrait<BuildingInfo>(World, info.ConstructionYardTypes);
		}

		// Use for proactive targeting.
		public bool IsPreferredEnemyUnit(Actor a)
		{
			return IsValidEnemyUnit(a) && !a.Info.HasTraitInfo<AircraftInfo>();
		}

		public bool IsNotHiddenUnit(Actor a)
		{
			var hasModifier = false;
			var visModifiers = a.TraitsImplementing<IVisibilityModifier>();
			foreach (var v in visModifiers)
			{
				if (v.IsVisible(a, Player))
					return true;

				hasModifier = true;
			}

			return !hasModifier;
		}

		protected override void Created(Actor self)
		{
			notifyPositionsUpdated = self.Owner.PlayerActor.TraitsImplementing<IBotPositionsUpdated>().ToArray();
			notifyIdleBaseUnits = self.Owner.PlayerActor.TraitsImplementing<IBotNotifyIdleBaseUnits>().ToArray();
			aircraftBuilders = self.Owner.PlayerActor.TraitsImplementing<IBotAircraftBuilder>().ToArray();
		}

		protected override void TraitEnabled(Actor self)
		{
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

		internal static Actor ClosestTo(IEnumerable<Actor> ownActors, Actor targetActor)
		{
			// Return actors that can get within weapons range of the target.
			// First, let's determine the max weapons range for each of the actors.
			var target = Target.FromActor(targetActor);
			var ownActorsAndTheirAttackRanges = ownActors
				.Select(a => (Actor: a, AttackBases: a.TraitsImplementing<AttackBase>().Where(Exts.IsTraitEnabled)
					.Where(ab => ab.HasAnyValidWeapons(target)).ToList()))
				.Where(x => x.AttackBases.Count > 0)
				.Select(x => (x.Actor, Range: x.AttackBases.Max(ab => ab.GetMaximumRangeVersusTarget(target))))
				.ToDictionary(x => x.Actor, x => x.Range);

			// Now determine if each actor can either path directly to the target,
			// or if it can path to a nearby location at the edge of its weapon range to the target
			// A thorough check would check each position within the circle, but for performance
			// we'll only check 8 positions around the edge of the circle.
			// We need to account for the weapons range here to account for units such as boats.
			// They can't path directly to a land target,
			// but might be able to get close enough to shore to attack the target from range.
			return ownActorsAndTheirAttackRanges.Keys
				.ClosestToWithPathToAny(targetActor.World, a =>
				{
					var range = ownActorsAndTheirAttackRanges[a].Length;
					var rangeDiag = Exts.MultiplyBySqrtTwoOverTwo(range);
					return new[]
					{
						targetActor.CenterPosition,
						targetActor.CenterPosition + new WVec(range, 0, 0),
						targetActor.CenterPosition + new WVec(-range, 0, 0),
						targetActor.CenterPosition + new WVec(0, range, 0),
						targetActor.CenterPosition + new WVec(0, -range, 0),
						targetActor.CenterPosition + new WVec(rangeDiag, rangeDiag, 0),
						targetActor.CenterPosition + new WVec(-rangeDiag, rangeDiag, 0),
						targetActor.CenterPosition + new WVec(-rangeDiag, -rangeDiag, 0),
						targetActor.CenterPosition + new WVec(rangeDiag, -rangeDiag, 0),
					};
				});
		}

		internal IEnumerable<(Actor Actor, WVec Offset)> FindEnemies(IEnumerable<Actor> actors, Actor sourceActor)
		{
			// Check units are in fact enemies and not hidden.
			// Then check which are in weapons range of the source.
			var activeAttackBases = sourceActor.TraitsImplementing<AttackBase>().Where(Exts.IsTraitEnabled).ToArray();
			var enemiesAndSourceAttackRanges = actors
				.Where(IsPreferredEnemyUnit)
				.Select(a => (Actor: a, AttackBases: activeAttackBases.Where(ab => ab.HasAnyValidWeapons(Target.FromActor(a))).ToList()))
				.Where(x => x.AttackBases.Count > 0)
				.Select(x => (x.Actor, Range: x.AttackBases.Max(ab => ab.GetMaximumRangeVersusTarget(Target.FromActor(x.Actor)))))
				.ToDictionary(x => x.Actor, x => x.Range);

			// Now determine if the source actor can path directly to the target,
			// or if it can path to a nearby location at the edge of its weapon range to the target
			// A thorough check would check each position within the circle, but for performance
			// we'll only check 8 positions around the edge of the circle.
			// We need to account for the weapons range here to account for units such as boats.
			// They can't path directly to a land target,
			// but might be able to get close enough to shore to attack the target from range.
			return enemiesAndSourceAttackRanges.Keys
				.WithPathFrom(sourceActor, a =>
				{
					var range = enemiesAndSourceAttackRanges[a].Length;
					var rangeDiag = Exts.MultiplyBySqrtTwoOverTwo(range);
					return new[]
					{
						WVec.Zero,
						new WVec(range, 0, 0),
						new WVec(-range, 0, 0),
						new WVec(0, range, 0),
						new WVec(0, -range, 0),
						new WVec(rangeDiag, rangeDiag, 0),
						new WVec(-rangeDiag, rangeDiag, 0),
						new WVec(-rangeDiag, -rangeDiag, 0),
						new WVec(rangeDiag, -rangeDiag, 0),
					};
				})
				.Select(x => (x.Actor, x.ReachableOffsets.MinBy(o => o.LengthSquared)));
		}

		internal (Actor Actor, WVec Offset) FindClosestEnemy(Actor sourceActor, bool highValueCheck = false)
		{
			return FindClosestEnemy(World.Actors, sourceActor, highValueCheck);
		}

		internal (Actor Actor, WVec Offset) FindClosestEnemy(Actor sourceActor, WDist radius, bool highValueCheck = false)
		{
			return FindClosestEnemy(World.FindActorsInCircle(sourceActor.CenterPosition, radius), sourceActor, highValueCheck);
		}

		(Actor Actor, WVec Offset) FindClosestEnemy(IEnumerable<Actor> actors, Actor sourceActor, bool highValueCheck = false)
		{
			if (highValueCheck)
			{
				var highValueTargetRoll = World.LocalRandom.Next(0, 100);

				if (Info.HighValueTargetPriority > highValueTargetRoll)
				{
					var highValueTarget = FindHighValueTarget(sourceActor);
					if (highValueTarget != null)
						return (highValueTarget, WVec.Zero);
				}
			}

			// CA: Prioritize buildings over other enemy units
			// First try to find closest building
			var buildings = FindEnemies(actors.Where(IsPreferredEnemyBuilding), sourceActor);
			var closestBuilding = WorldUtils.ClosestToIgnoringPath(buildings, x => x.Actor, sourceActor);

			if (closestBuilding.Actor != null)
				return closestBuilding;

			// Fall back to any enemy if no buildings found
			return WorldUtils.ClosestToIgnoringPath(FindEnemies(actors, sourceActor), x => x.Actor, sourceActor);
		}

		void CleanSquads()
		{
			foreach (var s in Squads)
			{
				s.Units.RemoveWhere(unitCannotBeOrdered);

				if (s.Type == SquadCAType.Air)
				{
					s.NewUnits.RemoveWhere(unitCannotBeOrdered);
					s.RearmingUnits.RemoveWhere(unitCannotBeOrdered);
					s.WaitingUnits.RemoveWhere(unitCannotBeOrdered);
				}
			}

			Squads.RemoveAll(s => !s.IsValid);
		}

		// HACK: Use of this function requires that there is one squad of this type.
		SquadCA GetSquadOfType(SquadCAType type)
		{
			return Squads.FirstOrDefault(s => s.Type == type);
		}

		SquadCA RegisterNewSquad(IBot bot, SquadCAType type, (Actor Actor, WVec Offset) target = default)
		{
			var ret = new SquadCA(bot, this, type, target);
			Squads.Add(ret);
			return ret;
		}

		internal void UnregisterSquad(SquadCA squad)
		{
			activeUnits.ExceptWith(squad.Units);
			squad.Units.Clear();

			// CleanSquads will remove the squad from the Squads list.
			// We can't do that here as this is designed to be called from within Squad.Update
			// and thus would mutate the Squads list we are iterating over.
		}

		void AssignRolesToIdleUnits(IBot bot)
		{
			CleanSquads();

			activeUnits.RemoveWhere(unitCannotBeOrdered);
			unitsHangingAroundTheBase.RemoveAll(unitCannotBeOrdered);
			foreach (var n in notifyIdleBaseUnits)
				n.UpdatedIdleBaseUnits(unitsHangingAroundTheBase);

			if (--attackForceTicks <= 0)
			{
				attackForceTicks = Info.AttackForceInterval;
				foreach (var s in Squads)
					squadsPendingUpdate.Push(s);
			}

			// PERF: Spread out squad updates across multiple ticks.
			var updateCount = Exts.IntegerDivisionRoundingAwayFromZero(squadsPendingUpdate.Count, attackForceTicks);
			for (var i = 0; i < updateCount; i++)
			{
				var squadPendingUpdate = squadsPendingUpdate.Pop();
				if (squadPendingUpdate.IsValid)
					squadPendingUpdate.Update();
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

			if (--protectOwnTicks <= 0 && protectOwnFrom != null)
				ProtectOwn(bot, protectOwnFrom);
		}

		void FindNewUnits(IBot bot)
		{
			var newUnits = World.ActorsHavingTrait<IPositionable>()
				.Where(a => a.Owner == Player &&
					!Info.ExcludeFromSquadsTypes.Contains(a.Info.Name) &&
					!activeUnits.Contains(a));

			foreach (var a in newUnits)
			{
				if (Info.AirUnitsTypes.Contains(a.Info.Name))
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
				else if (Info.HarasserTypes.Contains(a.Info.Name))
				{
					var harasserSquads = Squads.Where(s => s.Type == SquadCAType.Harass);
					var matchingHarasserSquadFound = false;

					foreach (var harasserSquad in harasserSquads)
					{
						if (harasserSquad.Units.Any(u => u.Info.Name == a.Info.Name))
						{
							harasserSquad.Units.Add(a);
							matchingHarasserSquadFound = true;
							break;
						}
					}

					if (!matchingHarasserSquadFound)
					{
						var newHarasserSquad = RegisterNewSquad(bot, SquadCAType.Harass);
						newHarasserSquad.Units.Add(a);
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

			if (Info.SquadValue > 0) // CA: Value-based squad creation instead of just unit count
			{
				foreach (var a in unitsHangingAroundTheBase)
				{
					if (cachedUnitValues.TryGetValue(a.Info.Name, out var value))
						idleUnitsValue += value;
					else
					{
						var valued = a.Info.TraitInfoOrDefault<ValuedInfo>();
						if (valued != null)
						{
							cachedUnitValues[a.Info.Name] = valued.Cost;
							idleUnitsValue += valued.Cost;
						}
					}
				}
			}

			if (idleUnitsValue >= desiredAttackForceValue)
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
			desiredAttackForceValue = Info.SquadValue;

			// Add a random bonus between 0 and a value that scales from SquadValueRandomEarlyBonus at 0 WorldTick to SquadValueRandomLateBonus at 20 minutes WorldTick
			var randomBonus = World.LocalRandom.Next(0, (int)(Info.SquadValueRandomEarlyBonus + (Info.SquadValueRandomLateBonus - Info.SquadValueRandomEarlyBonus) * Math.Min(1, (float)World.WorldTick / (20 * 60 * 25))));
			desiredAttackForceValue += randomBonus;
		}

		void ProtectOwn(IBot bot, Actor attacker)
		{
			protectOwnFrom = null;
			protectOwnTicks = Info.ProtectInterval;

			var protectSq = GetSquadOfType(SquadCAType.Protection);
			protectSq ??= RegisterNewSquad(bot, SquadCAType.Protection, (attacker, WVec.Zero));
			protectSq.Units.RemoveWhere(unitCannotBeOrdered);

			if (protectSq.IsValid && !protectSq.IsTargetValid(protectSq.CenterUnit()))
				protectSq.SetActorToTarget((attacker, WVec.Zero));

			if (!protectSq.IsValid)
			{
				var ownUnits = World.FindActorsInCircle(World.Map.CenterOfCell(GetRandomBaseCenter()), WDist.FromCells(Info.ProtectUnitScanRadius))
					.Where(unit => unit.Owner == Player && unit.Info.HasTraitInfo<AttackBaseInfo>() && !unit.Info.HasTraitInfo<BuildingInfo>()
						&& !unit.Info.HasTraitInfo<HarvesterInfo>() && !unit.Info.HasTraitInfo<AircraftInfo>())
					.WithPathTo(World, attacker.CenterPosition);

				protectSq.Units.UnionWith(ownUnits);
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

				protectOwnFrom = e.Attacker;
			}
		}

	List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (IsTraitDisabled)
				return null;

			return new List<MiniYamlNode>()
			{
				new("Squads", "", Squads.ConvertAll(s => new MiniYamlNode("Squad", s.Serialize()))),
				new("InitialBaseCenter", FieldSaver.FormatValue(initialBaseCenter)),
				new("UnitsHangingAroundTheBase", FieldSaver.FormatValue(unitsHangingAroundTheBase
					.Where(a => !unitCannotBeOrdered(a))
					.Select(a => a.ActorID)
					.ToArray())),
				new("ActiveUnits", FieldSaver.FormatValue(activeUnits
					.Where(a => !unitCannotBeOrdered(a))
					.Select(a => a.ActorID)
					.ToArray())),
				new("RushTicks", FieldSaver.FormatValue(rushTicks)),
				new("AssignRolesTicks", FieldSaver.FormatValue(assignRolesTicks)),
				new("AttackForceTicks", FieldSaver.FormatValue(attackForceTicks)),
				new("MinAttackForceDelayTicks", FieldSaver.FormatValue(minAttackForceDelayTicks)),
			};
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, MiniYaml data)
		{
			if (self.World.IsReplay)
				return;

			var nodes = data.ToDictionary();

			if (nodes.TryGetValue("InitialBaseCenter", out var initialBaseCenterNode))
				initialBaseCenter = FieldLoader.GetValue<CPos>("InitialBaseCenter", initialBaseCenterNode.Value);

			if (nodes.TryGetValue("UnitsHangingAroundTheBase", out var unitsHangingAroundTheBaseNode))
			{
				unitsHangingAroundTheBase.Clear();
				unitsHangingAroundTheBase.AddRange(FieldLoader.GetValue<uint[]>("UnitsHangingAroundTheBase", unitsHangingAroundTheBaseNode.Value)
					.Select(a => self.World.GetActorById(a)).Where(a => a != null));
			}

			if (nodes.TryGetValue("ActiveUnits", out var activeUnitsNode))
			{
				activeUnits.Clear();
				activeUnits.UnionWith(FieldLoader.GetValue<uint[]>("ActiveUnits", activeUnitsNode.Value)
					.Select(a => self.World.GetActorById(a)).Where(a => a != null));
			}

			if (nodes.TryGetValue("RushTicks", out var rushTicksNode))
				rushTicks = FieldLoader.GetValue<int>("RushTicks", rushTicksNode.Value);

			if (nodes.TryGetValue("AssignRolesTicks", out var assignRolesTicksNode))
				assignRolesTicks = FieldLoader.GetValue<int>("AssignRolesTicks", assignRolesTicksNode.Value);

			if (nodes.TryGetValue("AttackForceTicks", out var attackForceTicksNode))
				attackForceTicks = FieldLoader.GetValue<int>("AttackForceTicks", attackForceTicksNode.Value);

			if (nodes.TryGetValue("MinAttackForceDelayTicks", out var minAttackForceDelayTicksNode))
				minAttackForceDelayTicks = FieldLoader.GetValue<int>("MinAttackForceDelayTicks", minAttackForceDelayTicksNode.Value);

			if (nodes.TryGetValue("Squads", out var squadsNode))
			{
				Squads.Clear();
				foreach (var n in squadsNode.Nodes)
					Squads.Add(SquadCA.Deserialize(bot, this, n.Value));
			}
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			constructionYardBuildings?.Dispose();
		}

		// ===========================
		// === CA-SPECIFIC METHODS ===
		// ===========================

		bool IsValidEnemyUnit(Actor a)
		{
			if (a == null || a.IsDead || Player.RelationshipWith(a.Owner) != PlayerRelationship.Enemy ||
				a.Info.HasTraitInfo<HuskInfo>() || a.Info.HasTraitInfo<CarrierSlaveInfo>())
				return false;

			var targetTypes = a.GetEnabledTargetTypes();
			if (targetTypes.IsEmpty || targetTypes.Overlaps(Info.IgnoredEnemyTargetTypes))
				return false;

			return IsNotHiddenUnit(a);
		}

		public bool IsPreferredEnemyBuilding(Actor a)
		{
			return IsValidEnemyUnit(a) && a.Info.HasTraitInfo<RepairableBuildingInfo>();
		}

		public bool IsPreferredEnemyAircraft(Actor a)
		{
			return IsValidEnemyUnit(a) && a.Info.HasTraitInfo<AircraftInfo>() && a.Info.HasTraitInfo<AttackBaseInfo>();
		}

		public bool IsHighValueTarget(Actor a)
		{
			return IsValidEnemyUnit(a) && Info.HighValueTargetTypes.Contains(a.Info.Name);
		}

		public bool IsAirSquadTargetArmorType(Actor a, SquadCA owner)
		{
			if (a == null || a.IsDead)
				return false;

			var airSquadUnitType = owner.Units.First().Info.Name;
			if (owner.SquadManager.Info.AirSquadTargetArmorTypes.ContainsKey(airSquadUnitType))
			{
				var desiredArmorTypes = owner.SquadManager.Info.AirSquadTargetArmorTypes[airSquadUnitType];
				return a.Info.TraitInfos<ArmorInfo>().Any(ai => desiredArmorTypes.Contains(ai.Type.ToString()));
			}

			return true;
		}

		internal Actor FindHighValueTarget(Actor sourceActor) // CA: High-value target prioritization system
		{
			var highValueActors = World.Actors.Where(IsHighValueTarget);
			var reachableHighValueTargets = FindEnemies(highValueActors, sourceActor).ToList();
			return reachableHighValueTargets.Count > 0
				? reachableHighValueTargets.RandomOrDefault(World.LocalRandom).Actor
				: null;
		}

		public bool CanBuildMoreOfAircraft(ActorInfo actorInfo) // CA: Aircraft builder integration
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
