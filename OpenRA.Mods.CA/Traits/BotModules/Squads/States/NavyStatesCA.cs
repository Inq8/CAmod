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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	abstract class NavyStateBaseCA : StateBaseCA
	{
		protected virtual bool ShouldFlee(SquadCA owner)
		{
			return ShouldFlee(owner, enemies => !AttackOrFleeFuzzyCA.Default.CanAttack(owner.Units, enemies));
		}

		protected Actor FindClosestEnemy(SquadCA owner)
		{
			var first = owner.Units.First();

			// Navy squad AI can exploit enemy naval production to find path, if any.
			// (Way better than finding a nearest target which is likely to be on Ground)
			// You might be tempted to move these lookups into Activate() but that causes null reference exception.
			var domainIndex = first.World.WorldActor.Trait<DomainIndex>();
			var locomotor = first.Trait<Mobile>().Locomotor;

			var navalProductions = owner.World.ActorsHavingTrait<Building>().Where(a
				=> owner.SquadManager.Info.NavalProductionTypes.Contains(a.Info.Name)
				&& domainIndex.IsPassable(first.Location, a.Location, locomotor)
				&& a.AppearsHostileTo(first));

			if (navalProductions.Any())
			{
				var nearest = navalProductions.ClosestTo(first);

				// Return nearest when it is FAR enough.
				// If the naval production is within MaxBaseRadius, it implies that
				// this squad is close to enemy territory and they should expect a naval combat;
				// closest enemy makes more sense in that case.
				if ((nearest.Location - first.Location).LengthSquared > owner.SquadManager.Info.MaxBaseRadius * owner.SquadManager.Info.MaxBaseRadius)
					return nearest;
			}

			return owner.SquadManager.FindClosestEnemy(first.CenterPosition);
		}
	}

	class NavyUnitsIdleStateCA : NavyStateBaseCA, IState
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
				.Where(owner.SquadManager.IsPreferredEnemyUnit).ToList();

			if (enemyUnits.Count == 0)
			{
				Retreat(owner, flee: false, rearm: true, repair: true);
				return;
			}

			if (AttackOrFleeFuzzyCA.Default.CanAttack(owner.Units, enemyUnits))
			{
				// We have gathered sufficient units. Attack the nearest enemy unit.
				owner.FuzzyStateMachine.ChangeState(owner, new NavyUnitsAttackMoveStateCA(), false);
			}
			else
				owner.FuzzyStateMachine.ChangeState(owner, new NavyUnitsFleeStateCA(), false);
		}

		public void Deactivate(SquadCA owner) { }
	}

	class NavyUnitsAttackMoveStateCA : NavyStateBaseCA, IState
	{
		const int MaxAttemptsToAdvance = 6;
		const int MakeWayTicks = 2;

		// Give tolerance for AI grouping team at start
		int failedAttempts = -(MaxAttemptsToAdvance * 2);
		int makeWay = MakeWayTicks;
		WPos lastPos = WPos.Zero;

		// Optimazing state switch
		bool attackLoop = false;

		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			// Basic check
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid)
			{
				var targetActor = FindClosestEnemy(owner);
				if (targetActor != null)
					owner.TargetActor = targetActor;
				else
				{
					owner.FuzzyStateMachine.ChangeState(owner, new NavyUnitsFleeStateCA(), false);
					return;
				}
			}

			// Initialize leader. Optimize pathfinding by using leader.
			// Drop former "owner.Units.ClosestTo(owner.TargetActor.CenterPosition)",
			// which is the shortest geometric distance, but it has no relation to pathfinding distance in map.
			var leader = owner.Units.FirstOrDefault();
			if (leader == null)
				return;

			// Switch to attack state if we encounter enemy units like ground squad
			var attackScanRadius = WDist.FromCells(owner.SquadManager.Info.AttackScanRadius);

			var enemyActor = owner.SquadManager.FindClosestEnemy(leader.CenterPosition, attackScanRadius);
			if (enemyActor != null)
			{
				owner.TargetActor = enemyActor;
				if (!attackLoop)
				{
					attackLoop = true;
					owner.FuzzyStateMachine.ChangeState(owner, new NavyUnitsAttackStateCA(), true);
				}
				else
					owner.FuzzyStateMachine.RevertToPreviousState(owner, true);
				return;
			}

			// Make sure the guide unit has not been blocked by the rest of the squad
			if (failedAttempts >= MaxAttemptsToAdvance)
			{
				if (makeWay > 0)
				{
					owner.Bot.QueueOrder(new Order("AttackMove", leader, Target.FromCell(owner.World, owner.TargetActor.Location), false));

					var others = owner.Units.Where(u => u != leader);
					owner.Bot.QueueOrder(new Order("Scatter", null, false, groupedActors: others.ToArray()));
					makeWay--;
				}
				else
				{
					// Give some tolerance for AI regrouping
					failedAttempts = 0 - MakeWayTicks;
					makeWay = MakeWayTicks;
				}

				return;
			}

			// Check if the squad is stuck due to the map having a very twisted path
			// or currently bridge and tunnel from TS mod
			if (leader.CenterPosition == lastPos)
				failedAttempts++;
			else
				failedAttempts = 0;

			lastPos = leader.CenterPosition;

			// The same as ground squad regroup
			var occupiedArea = (long)WDist.FromCells(owner.Units.Count).Length * 1024;

			var unitsHurryUp = owner.Units.Where(a => (a.CenterPosition - leader.CenterPosition).LengthSquared >= occupiedArea * 2);
			var leaderWaitCheck = owner.Units.Any(a => (a.CenterPosition - leader.CenterPosition).LengthSquared > occupiedArea * 5);

			if (leaderWaitCheck)
				owner.Bot.QueueOrder(new Order("Stop", leader, false));
			else
				owner.Bot.QueueOrder(new Order("AttackMove", leader, Target.FromCell(owner.World, owner.TargetActor.Location), false));

			owner.Bot.QueueOrder(new Order("AttackMove", null, Target.FromCell(owner.World, leader.Location), false, groupedActors: unitsHurryUp.ToArray()));
		}

		public void Deactivate(SquadCA owner) { }
	}

	class NavyUnitsAttackStateCA : NavyStateBaseCA, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			// Basic check
			if (!owner.IsValid)
				return;

			var leader = owner.Units.FirstOrDefault();
			if (leader == null)
				return;

			// Rescan target to prevent being ambushed and die without fight
			// If there is no threat around, return to AttackMove state for formation
			var attackScanRadius = WDist.FromCells(owner.SquadManager.Info.AttackScanRadius);
			var targetActor = owner.SquadManager.FindClosestEnemy(leader.CenterPosition, attackScanRadius);

			var cannotRetaliate = true;
			List<Actor> followingUnits = new List<Actor>();
			List<Actor> attackingUnits = new List<Actor>();
			if (targetActor == null)
			{
				owner.FuzzyStateMachine.RevertToPreviousState(owner, true);
				return;
			}
			else
			{
				owner.TargetActor = targetActor;

				foreach (var a in owner.Units)
				{
					if (!BusyAttack(a))
					{
						if (CanAttackTarget(a, targetActor))
						{
							attackingUnits.Add(a);
							cannotRetaliate = false;
						}
						else
							followingUnits.Add(a);
					}
					else
						cannotRetaliate = false;
				}
			}

			// Because ShouldFlee(owner) cannot retreat units while they cannot even fight
			// a unit that they cannot target. Therefore, use `cannotRetaliate` here to solve this bug.
			if (ShouldFlee(owner) || cannotRetaliate)
			{
				owner.FuzzyStateMachine.ChangeState(owner, new NavyUnitsFleeStateCA(), false);
				return;
			}

			owner.Bot.QueueOrder(new Order("AttackMove", null, Target.FromCell(owner.World, leader.Location), false, groupedActors: followingUnits.ToArray()));
			owner.Bot.QueueOrder(new Order("Attack", null, Target.FromActor(owner.TargetActor), false, groupedActors: attackingUnits.ToArray()));
		}

		public void Deactivate(SquadCA owner) { }
	}

	class NavyUnitsFleeStateCA : NavyStateBaseCA, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			Retreat(owner, flee: true, rearm: true, repair: true);
			owner.FuzzyStateMachine.ChangeState(owner, new NavyUnitsIdleStateCA(), false);
		}

		public void Deactivate(SquadCA owner) { }
	}
}
