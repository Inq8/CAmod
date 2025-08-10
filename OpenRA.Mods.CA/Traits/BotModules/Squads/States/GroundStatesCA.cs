#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	abstract class GroundStateBaseCA : StateBaseCA
	{
		Actor leader;

		/// <summary>
		/// Elects a unit to lead the squad, other units in the squad will regroup to the leader if they start to spread out.
		/// The leader remains the same unless a new one is forced or the leader is no longer part of the squad.
		/// </summary>
		protected Actor Leader(SquadCA owner)
		{
			if (leader == null || !owner.Units.Contains(leader))
				leader = NewLeader(owner);
			return leader;
		}

		static Actor NewLeader(SquadCA owner)
		{
			IEnumerable<Actor> units = owner.Units;

			// Identify the Locomotor with the most restrictive passable terrain list. For squads with mixed
			// locomotors, we hope to choose the most restrictive option. This means we won't nominate a leader who has
			// more options. This avoids situations where we would nominate a hovercraft as the leader and tanks would
			// fail to follow it because they can't go over water. By forcing us to choose a unit with limited movement
			// options, we maximise the chance other units will be able to follow it. We could still be screwed if the
			// squad has a mix of units with disparate movement, e.g. land units and naval units. We must trust the
			// squad has been formed from a set of units that don't suffer this problem.
			var leastCommonDenominator = units
				.Select(a => a.TraitOrDefault<Mobile>()?.Locomotor)
				.Where(l => l != null)
				.MinByOrDefault(l => l.Info.TerrainSpeeds.Count)
				?.Info.TerrainSpeeds.Count;
			if (leastCommonDenominator != null)
				units = units.Where(a => a.TraitOrDefault<Mobile>()?.Locomotor.Info.TerrainSpeeds.Count == leastCommonDenominator).ToList();

			// Choosing a unit in the center reduces the need for an immediate regroup.
			var centerPosition = units.Select(a => a.CenterPosition).Average();
			return units.MinBy(a => (a.CenterPosition - centerPosition).LengthSquared);
		}

		protected virtual bool ShouldFlee(SquadCA owner)
		{
			return ShouldFlee(owner, enemies => !AttackOrFleeFuzzyCA.Default.CanAttack(owner.Units, enemies));
		}

		protected (Actor Actor, WVec Offset) NewLeaderAndFindClosestEnemy(SquadCA owner, bool highValueCheck = false)
		{
			leader = null; // Force a new leader to be elected, useful if we are targeting a new enemy.
			return owner.SquadManager.FindClosestEnemy(Leader(owner), highValueCheck);
		}

		protected IEnumerable<(Actor Actor, WVec Offset)> FindEnemies(SquadCA owner, IEnumerable<Actor> actors)
		{
			return owner.SquadManager.FindEnemies(
				actors,
				Leader(owner));
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

			if (!owner.IsTargetValid(Leader(owner)))
			{
				var closestEnemy = NewLeaderAndFindClosestEnemy(owner, true);
				owner.SetActorToTarget(closestEnemy);
				if (closestEnemy.Actor == null)
					return;
			}

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
		}

		public void Deactivate(SquadCA owner) { }
	}

	sealed class GroundUnitsAttackMoveStateCA : GroundStateBaseCA, IState
	{
		int lastUpdatedTick;
		CPos? lastLeaderLocation;
		Actor lastTarget;

		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid(Leader(owner)))
			{
				var closestEnemy = NewLeaderAndFindClosestEnemy(owner);
				owner.SetActorToTarget(closestEnemy);
				if (closestEnemy.Actor == null)
				{
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeStateCA(), true);
					return;
				}
			}

			var leader = Leader(owner);
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

			// HACK: Drop back to the idle state if we haven't moved in 2.5 seconds
			// This works around the squad being stuck trying to attack-move to a location
			// that they cannot path to, generating expensive pathfinding calls each tick.
			if (owner.World.WorldTick > lastUpdatedTick + 63)
			{
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateCA(), true);
				return;
			}

			var ownUnits = owner.World.FindActorsInCircle(leader.CenterPosition, WDist.FromCells(owner.Units.Count) / 3)
				.Where(owner.Units.Contains).ToHashSet();

			if (ownUnits.Count < owner.Units.Count)
			{
				// Since units have different movement speeds, they get separated while approaching the target.
				// Let them regroup into tighter formation.
				owner.Bot.QueueOrder(new Order("Stop", leader, false));

				var units = owner.Units.Where(a => !ownUnits.Contains(a)).ToArray();
				owner.Bot.QueueOrder(new Order("AttackMove", null, Target.FromCell(owner.World, leader.Location), false, groupedActors: units));
			}
			else
			{
				var target = owner.SquadManager.FindClosestEnemy(leader, WDist.FromCells(owner.SquadManager.Info.AttackScanRadius));
				if (target.Actor != null)
				{
					owner.SetActorToTarget(target);
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackState(), true);
				}
				else
					owner.Bot.QueueOrder(new Order("AttackMove", null, owner.Target, false, groupedActors: owner.Units.ToArray()));
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

			if (!owner.IsTargetValid(Leader(owner)))
			{
				var closestEnemy = NewLeaderAndFindClosestEnemy(owner);
				owner.SetActorToTarget(closestEnemy);
				if (closestEnemy.Actor == null)
				{
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeStateCA(), true);
					return;
				}
			}

			var leader = Leader(owner);
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

			// HACK: Drop back to the idle state if we haven't moved in 2.5 seconds
			// This works around the squad being stuck trying to attack-move to a location
			// that they cannot path to, generating expensive pathfinding calls each tick.
			if (owner.World.WorldTick > lastUpdatedTick + 63)
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
}
