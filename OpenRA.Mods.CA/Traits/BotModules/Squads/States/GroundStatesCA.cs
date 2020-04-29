#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	abstract class GroundStateBaseCA : StateBaseCA
	{
		protected virtual bool ShouldFlee(SquadCA owner)
		{
			return ShouldFlee(owner, enemies => !AttackOrFleeFuzzyCA.Default.CanAttack(owner.Units, enemies));
		}

		protected Actor FindClosestEnemyBuilding(SquadCA owner)
		{
			return owner.SquadManager.FindClosestEnemyBuilding(owner.Units.First().CenterPosition);
		}

		protected Actor FindClosestEnemy(SquadCA owner)
		{
			return owner.SquadManager.FindClosestEnemy(owner.Units.First().CenterPosition);
		}

		// Retreat units from combat, or for supply only in idle
		protected virtual void Retreat(SquadCA owner, bool resupplyonly)
		{
			// Repair units. One by one to avoid give out mass orders
			var alreadysend = false;

			foreach (var a in owner.Units)
			{
				if (IsRearming(a))
					continue;

				Actor repairBuilding = null;
				var orderId = "Repair";
				var health = a.TraitOrDefault<IHealth>();

				if (!alreadysend && health != null && health.DamageState > DamageState.Undamaged)
				{
					var repairable = a.TraitOrDefault<Repairable>();
					if (repairable != null)
						repairBuilding = repairable.FindRepairBuilding(a);
					else
					{
						var repairableNear = a.TraitOrDefault<RepairableNearCA>();
						if (repairableNear != null)
						{
							orderId = "RepairNear";
							repairBuilding = repairableNear.FindRepairBuilding(a);
						}
					}

					if (repairBuilding != null)
					{
						owner.Bot.QueueOrder(new Order(orderId, a, Target.FromActor(repairBuilding), false));
						alreadysend = true;
						continue;
					}
					else if (!resupplyonly)
						owner.Bot.QueueOrder(new Order("Move", a, Target.FromCell(owner.World, RandomBuildingLocation(owner)), false));
				}
				else if (!resupplyonly)
					owner.Bot.QueueOrder(new Order("Move", a, Target.FromCell(owner.World, RandomBuildingLocation(owner)), false));
			}
		}

		protected Actor ThreatScan(SquadCA owner, Actor teamLeader, WDist scanRadius)
		{
			var enemies = owner.World.FindActorsInCircle(teamLeader.CenterPosition, scanRadius)
					.Where(a => owner.SquadManager.IsEnemyUnit(a) && owner.SquadManager.IsNotHiddenUnit(a));
			return enemies.ClosestTo(teamLeader.CenterPosition);
		}
	}

	class GroundUnitsIdleState : GroundStateBaseCA, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid)
			{
				var closestEnemy = FindClosestEnemy(owner);
				if (closestEnemy == null)
					return;

				owner.TargetActor = closestEnemy;
			}

			var enemyUnits = owner.World.FindActorsInCircle(owner.TargetActor.CenterPosition, WDist.FromCells(owner.SquadManager.Info.IdleScanRadius))
				.Where(owner.SquadManager.IsEnemyUnit).ToList();

			if (enemyUnits.Count == 0)
			{
				Retreat(owner, true);
				return;
			}

			if (AttackOrFleeFuzzyCA.Default.CanAttack(owner.Units, enemyUnits))
			{
				foreach (var u in owner.Units)
					owner.Bot.QueueOrder(new Order("AttackMove", u, Target.FromCell(owner.World, owner.TargetActor.Location), false));

				// We have gathered sufficient units. Attack the nearest enemy unit.
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveState(), true);
			}
			else
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState(), true);
		}

		public void Deactivate(SquadCA owner) { }
	}

	class GroundUnitsAttackMoveState : GroundStateBaseCA, IState
	{
		public const int StuckedInPathCheckTimes = 6;
		public const int FindPathTick = 11;
		public const int ForceReproupTick = 10000;

		// Give tolerance for AI grouping team at start
		internal int StuckedInPath = StuckedInPathCheckTimes + ForceReproupTick;
		internal int TryFindPath = FindPathTick;
		internal WPos[] LastPos = { new WPos(0, 0, 0), new WPos(0, 0, 0) };
		internal int LastPosIndex = 0;
		internal Actor StuckedActor = null;
		internal bool ForceRegroup = false;

		// For game performance on AI orders and pathfinding
		// set to true for first regroup
		internal bool LongRangeAttackMoveAlready = true;

		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid)
			{
				var closestEnemy = FindClosestEnemyBuilding(owner);
				if (closestEnemy != null)
				{
					owner.TargetActor = closestEnemy;
					LongRangeAttackMoveAlready = false;
				}
				else
				{
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState(), true);
					return;
				}
			}

			// 1. Threat scan at beginning
			var teamLeader = owner.Units.ClosestTo(owner.TargetActor.CenterPosition);
			if (teamLeader == null)
				return;
			var teamTail = owner.Units.MaxByOrDefault(a => (a.CenterPosition - owner.TargetActor.CenterPosition).LengthSquared);
			var attackScanRadius = WDist.FromCells(owner.SquadManager.Info.AttackScanRadius);

			var targetActor = ThreatScan(owner, teamLeader, attackScanRadius) ?? ThreatScan(owner, teamTail, attackScanRadius);
			if (targetActor != null)
			{
				owner.TargetActor = targetActor;
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackState(), true);
				return;
			}
			else if (!LongRangeAttackMoveAlready)
			{
				foreach (var a in owner.Units)
					owner.Bot.QueueOrder(new Order("AttackMove", a, Target.FromCell(owner.World, owner.TargetActor.Location), false));
				LongRangeAttackMoveAlready = true;
			}

			// 2. Force going through very twisted path if get lost in path
			if (StuckedInPath <= 0)
			{
				LongRangeAttackMoveAlready = false;

				if (TryFindPath > 0)
				{
					if (!LongRangeAttackMoveAlready)
					{
						foreach (var a in owner.Units)
							owner.Bot.QueueOrder(new Order("AttackMove", a, Target.FromCell(owner.World, owner.TargetActor.Location), false));
						LongRangeAttackMoveAlready = true;
					}

					TryFindPath--;
				}
				else
				{
					// When going through is over, restore the check and force to regroup
					StuckedInPath = StuckedInPathCheckTimes + ForceReproupTick;
					TryFindPath = FindPathTick;
					LongRangeAttackMoveAlready = false;
					ForceRegroup = true;
				}

				return;
			}

			// 3. Check if the squad is stucked due to the map has very twisted path
			// or currently bridge and tunnel from TS mod
			/*
			 * See if a actor (always the same if found) is always in a area.
			 * 100 is the default thresold of the length squared of distance change.
			 * Record at least two positions to find if it stucked.
			 * "LastPosIndex" will switch to 0 or 1 to ensure different index everytime.
			 */

			var regrouper = owner.Units.ClosestTo(new WPos[] { teamLeader.CenterPosition, teamTail.CenterPosition }.Average());
			if (StuckedInPath == StuckedInPathCheckTimes || StuckedActor == null || StuckedActor.IsDead)
				StuckedActor = regrouper;

			if ((StuckedActor.CenterPosition - LastPos[LastPosIndex]).LengthSquared <= 100)
				StuckedInPath--;
			else
				StuckedInPath = StuckedInPathCheckTimes;

			LastPos[LastPosIndex] = StuckedActor.CenterPosition;
			LastPosIndex = LastPosIndex ^ 1;

			// 4. Since units have different movement speeds, they get separated while approaching the target.
			/*
			 * Let them regroup into tighter formation towards "regrouper".
			 * If "ForceRegroup" is on, the squad is just after a force-going-through,
			 * it requires regrouping to the same actor in order to
			 * avoid step back to the complex terrain they just escape from.
			 */

			if (ForceRegroup)
				regrouper = StuckedActor;

			var ownUnits = owner.World.FindActorsInCircle(regrouper.CenterPosition, WDist.FromCells(Exts.ISqrt(owner.Units.Count) * 2))
				.Where(a => a.Owner == owner.Units.First().Owner && owner.Units.Contains(a)).ToHashSet();

			if (ownUnits.Count < owner.Units.Count)
			{
				// Advance or regroup
				owner.Bot.QueueOrder(new Order("Stop", regrouper, false));
				foreach (var unit in owner.Units.Where(a => !ownUnits.Contains(a)))
					owner.Bot.QueueOrder(new Order("AttackMove", unit, Target.FromCell(owner.World, regrouper.Location), false));
				LongRangeAttackMoveAlready = false;

				if (ownUnits.Count == owner.Units.Count)
					ForceRegroup = false;

				foreach (var a in owner.Units)
					owner.Bot.QueueOrder(new Order("AttackMove", a, Target.FromCell(owner.World, owner.TargetActor.Location), false));
			}

			if (ShouldFlee(owner))
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState(), true);
		}

		public void Deactivate(SquadCA owner) { }
	}

	class GroundUnitsAttackState : GroundStateBaseCA, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			// rescan target to prevent being ambushed and die without fight
			// return to AttackMove state for formation
			var teamLeader = owner.Units.ClosestTo(owner.TargetActor.CenterPosition);
			if (teamLeader == null)
				return;
			var teamTail = owner.Units.MaxByOrDefault(a => (a.CenterPosition - owner.TargetActor.CenterPosition).LengthSquared);
			var attackScanRadius = WDist.FromCells(owner.SquadManager.Info.AttackScanRadius);
			var cannotRetaliate = false;
			var targetActor = ThreatScan(owner, teamLeader, attackScanRadius) ?? ThreatScan(owner, teamTail, attackScanRadius);
			if (targetActor == null)
			{
				owner.TargetActor = null;
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveState(), true);
				return;
			}
			else
			{
				cannotRetaliate = true;
				owner.TargetActor = targetActor;

				foreach (var a in owner.Units)
				{
					if (!BusyAttack(a))
					{
						if (CanAttackTarget(a, targetActor))
						{
							owner.Bot.QueueOrder(new Order("Attack", a, Target.FromActor(owner.TargetActor), false));
							cannotRetaliate = false;
						}
						else
							owner.Bot.QueueOrder(new Order("AttackMove", a, Target.FromCell(owner.World, teamLeader.Location), false));
					}
					else
						cannotRetaliate = false;
				}
			}

			if (ShouldFlee(owner) || cannotRetaliate)
			{
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState(), true);
				return;
			}
		}

		public void Deactivate(SquadCA owner) { }
	}

	class GroundUnitsFleeState : GroundStateBaseCA, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			Retreat(owner, false);
			owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleState(), true);
		}

		public void Deactivate(SquadCA owner) { owner.Units.Clear(); }
	}
}
