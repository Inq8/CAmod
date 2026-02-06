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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	public class SquadRouteInfo
	{
		public List<CPos> CurrentRoute { get; set; }
		public List<List<CPos>> AlternativeRoutes { get; set; }
		public int CurrentWaypointIndex { get; set; }
	}

	public abstract class GroundStateBaseCA : StateBaseCA
	{
		protected virtual bool ShouldFlee(SquadCA owner)
		{
			return ShouldFlee(owner, enemies => !AttackOrFleeFuzzyCA.Default.CanAttack(owner.Units, enemies));
		}

		protected (Actor Actor, WVec Offset) NewLeaderAndFindClosestEnemy(SquadCA owner, bool highValueCheck = false)
		{
			return owner.SquadManager.FindClosestEnemy(owner.GetLeader(), highValueCheck);
		}

		protected IEnumerable<(Actor Actor, WVec Offset)> FindEnemies(SquadCA owner, IEnumerable<Actor> actors)
		{
			return owner.SquadManager.FindEnemies(
				actors,
				owner.GetLeader());
		}

		protected static Actor ClosestToEnemy(SquadCA owner)
		{
			return SquadManagerBotModuleCA.ClosestTo(owner.Units, owner.TargetActor);
		}
	}

	sealed class GroundUnitsIdleStateCA : GroundStateBaseCA, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			var leader = owner.GetLeader();

			if (!owner.IsTargetValid(leader))
			{
				var closestEnemy = NewLeaderAndFindClosestEnemy(owner, true);
				owner.SetActorToTarget(closestEnemy);
				if (closestEnemy.Actor == null)
					return;
			}

			owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateCA(), true);

			/*
			var enemyUnits =
				FindEnemies(owner,
					owner.World.FindActorsInCircle(owner.Target.CenterPosition, WDist.FromCells(owner.SquadManager.Info.IdleScanRadius)))
				.Select(x => x.Actor)
				.ToList();

			if (enemyUnits.Count == 0)
				return;

			if (AttackOrFleeFuzzyCA.Default.CanAttack(owner.Units, enemyUnits))
			{
				owner.Bot.QueueOrder(new Order("AttackMove", null, owner.Target, false, groupedActors: owner.Units.ToArray()));

				// We have gathered sufficient units. Attack the nearest enemy unit.
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateCA(), true);
			}
			else
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeStateCA(), true);
			*/
		}

		public void Deactivate(SquadCA owner) { }
	}

	public sealed class GroundUnitsAttackMoveStateCA : GroundStateBaseCA, IState
	{
		int lastUpdatedTick;
		CPos? lastLeaderLocation;
		Actor lastTarget;
		List<CPos> currentRoute;
		List<List<CPos>> allRoutes;
		int currentWaypointIndex;
		int lastWaypointUpdateTick;

		public SquadRouteInfo GetRouteInfo()
		{
			if (currentRoute == null || allRoutes == null)
				return new SquadRouteInfo();

			return new SquadRouteInfo
			{
				CurrentRoute = currentRoute,
				AlternativeRoutes = allRoutes.Where(r => r != currentRoute).ToList(),
				CurrentWaypointIndex = currentWaypointIndex
			};
		}

		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			var leader = owner.GetLeader();

			if (!owner.IsTargetValid(leader))
			{
				var closestEnemy = NewLeaderAndFindClosestEnemy(owner, owner.Type == SquadCAType.Harass);
				owner.SetActorToTarget(closestEnemy);
				if (closestEnemy.Actor == null)
				{
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeStateCA(), true);
					return;
				}
			}

			if (leader.Location != lastLeaderLocation)
			{
				lastLeaderLocation = leader.Location;
				lastUpdatedTick = owner.World.WorldTick;
			}

			var targetChanged = false;
			if (owner.TargetActor != lastTarget)
			{
				lastTarget = owner.TargetActor;
				lastUpdatedTick = owner.World.WorldTick;
				targetChanged = true;
			}

			// HACK: Drop back to the idle state if we haven't moved in 4 seconds
			// This works around the squad being stuck trying to attack-move to a location
			// that they cannot path to, generating expensive pathfinding calls each tick.
			if (owner.World.WorldTick > lastUpdatedTick + 100)
			{
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateCA(), true);
				return;
			}

			// If there are enemies within attack scan radius, attack them
			var opportunityTarget = owner.SquadManager.FindClosestEnemy(leader, WDist.FromCells(owner.SquadManager.Info.AttackScanRadius));
			if (opportunityTarget.Actor != null)
			{
				owner.SetActorToTarget(opportunityTarget);
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackState(), true);
				return;
			}

			// Calculate less direct routes
			if ((currentRoute == null || targetChanged) && owner.LeaderLocomotor != null)
			{
				// Get the closest friendly building to the target to use as the starting point
				var friendlyBuildings = owner.World.Actors.Where(a => a.Owner == owner.Bot.Player && a.Info.HasTraitInfo<RepairableBuildingInfo>());
				var possibleStartActors = friendlyBuildings.Concat(new[] { owner.GetLeader() });
				var startActor = WorldUtils.ClosestToIgnoringPath(possibleStartActors, owner.TargetActor);
				var startLocation = startActor != null ? startActor.Location : leader.Location;
				var maxRoutes = 2;

				if (owner.Type == SquadCAType.Harass) {
					maxRoutes = 10;
				} else if (owner.SquadManager.Info.IndirectRouteChance > 0 && owner.World.LocalRandom.Next(100) < owner.SquadManager.Info.IndirectRouteChance) {
					maxRoutes = 7;
				}

				var routes = AIUtils.FindDistinctRoutes(
					owner.World,
					owner.LeaderLocomotor,
					startLocation,
					owner.TargetActor.Location,
					maxRoutes: maxRoutes,
					BlockedByActor.None);

				// Store all routes for the overlay
				allRoutes = new List<List<CPos>>();
				foreach (var r in routes)
					allRoutes.Add(r.Skip(1).ToList());

				// If we're using indirect routes, exclude the first (most direct) route
				if (maxRoutes > 2) {
					routes = routes.Skip(1).ToList();
				}

				if (routes.Count > 0)
				{
					// For harass squads, always use the least direct route to maximize chance of flanking
					currentRoute = owner.Type == SquadCAType.Harass ? routes[routes.Count - 1] : routes.Random(owner.World.LocalRandom);
					currentRoute = currentRoute.Skip(1).ToList();

					if (currentRoute.Count == 0)
					{
						currentRoute = null;
						allRoutes = null;
					}
					else
					{
						currentWaypointIndex = 0;
						lastWaypointUpdateTick = owner.World.WorldTick;
					}
				}
				else
				{
					currentRoute = null;
					allRoutes = null;
				}
			}

			// Follow waypoints in the route if we have one
			if (currentRoute != null
				&& currentRoute.Count > 2
				&& currentWaypointIndex < currentRoute.Count - 1)
			{
				// Move to next waypoint if close enough to current one (4 cells)
				if ((leader.Location - currentRoute[currentWaypointIndex]).LengthSquared < 16
					|| owner.World.WorldTick > lastWaypointUpdateTick + 625) // or stuck for 25 seconds
				{
					currentWaypointIndex++;
					lastWaypointUpdateTick = owner.World.WorldTick;
				}

				var waypoint = currentRoute[currentWaypointIndex];

				owner.Bot.QueueOrder(new Order("AttackMove", null, Target.FromCell(owner.World, waypoint), false,
					groupedActors: owner.Units.ToArray()));
			}
			else
			{
				currentRoute = null;

				// No route available or route too short, use direct attack move
				owner.Bot.QueueOrder(new Order("AttackMove", null, owner.Target, false,
					groupedActors: owner.Units.ToArray()));
			}

			if (ShouldFlee(owner))
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeStateCA(), true);
		}

		public void Deactivate(SquadCA owner) { }
	}

	sealed class GroundUnitsAttackState : GroundStateBaseCA, IState
	{
		int lastUpdatedTick;
		CPos? lastLeaderLocation;
		Actor lastTarget;

		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			var leader = owner.GetLeader();

			if (!owner.IsTargetValid(leader))
			{
				var opportunityTarget = owner.SquadManager.FindClosestEnemy(leader, WDist.FromCells(owner.SquadManager.Info.AttackScanRadius));
				if (opportunityTarget.Actor == null)
				{
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateCA(), true);
					return;
				}

				owner.SetActorToTarget(opportunityTarget);
			}

			if (leader.Location != lastLeaderLocation)
			{
				lastLeaderLocation = leader.Location;
				lastUpdatedTick = owner.World.WorldTick;
			}

			if (owner.TargetActor != lastTarget)
			{
				lastTarget = owner.TargetActor;
				lastUpdatedTick = owner.World.WorldTick;
			}

			// HACK: Drop back to the idle state if we haven't moved in 4 seconds
			// This works around the squad being stuck trying to attack-move to a location
			// that they cannot path to, generating expensive pathfinding calls each tick.
			if (owner.World.WorldTick > lastUpdatedTick + 100)
			{
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateCA(), true);
				return;
			}

			foreach (var a in owner.Units)
				if (!BusyAttack(a))
					owner.Bot.QueueOrder(new Order("AttackMove", a, owner.Target, false));

			if (ShouldFlee(owner))
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeStateCA(), true);
		}

		public void Deactivate(SquadCA owner) { }
	}

	sealed class GroundUnitsFleeStateCA : GroundStateBaseCA, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			GoToRandomOwnBuilding(owner);
			owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateCA(), true);
		}

		public void Deactivate(SquadCA owner) { owner.SquadManager.UnregisterSquad(owner); }
	}

	sealed class HarasserUnitsIdleStateCA : GroundStateBaseCA, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			var leader = owner.GetLeader();

			if (!ShouldHarass(owner))
				return;

			if (!owner.IsTargetValid(leader))
			{
				var closestEnemy = NewLeaderAndFindClosestEnemy(owner, true);
				owner.SetActorToTarget(closestEnemy);
				if (closestEnemy.Actor == null)
					return;
			}

			owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateCA(), true);
		}

		bool ShouldHarass(SquadCA owner)
		{
			switch (owner.Units.Count)
			{
				case 0:
				case 1:
				case 2:
					return false;
				case 3:
					return owner.World.LocalRandom.Next(100) < 5;
				case 4:
					return owner.World.LocalRandom.Next(100) < 10;
				case 5:
				default:
					return true;
			}
		}

		public void Deactivate(SquadCA owner) { }
	}
}
