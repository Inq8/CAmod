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
				return;

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
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid)
			{
				var closestEnemy = FindClosestEnemyBuilding(owner);
				if (closestEnemy != null)
					owner.TargetActor = closestEnemy;
				else
				{
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState(), true);
					return;
				}
			}

			var leader = owner.Units.ClosestTo(owner.TargetActor.CenterPosition);
			if (leader == null)
				return;

			var ownUnits = owner.World.FindActorsInCircle(leader.CenterPosition, WDist.FromCells(System.Math.Max(owner.Units.Count / 3, 4)))
				.Where(a => a.Owner == owner.Units.First().Owner && owner.Units.Contains(a)).ToHashSet();

			if (ownUnits.Count < owner.Units.Count)
			{
				// Since units have different movement speeds, they get separated while approaching the target.
				// Let them regroup into tighter formation.
				owner.Bot.QueueOrder(new Order("Stop", leader, false));
				foreach (var unit in owner.Units.Where(a => !ownUnits.Contains(a)))
					owner.Bot.QueueOrder(new Order("AttackMove", unit, Target.FromCell(owner.World, leader.Location), false));
			}
			else
			{
				var enemies = owner.World.FindActorsInCircle(leader.CenterPosition, WDist.FromCells(owner.SquadManager.Info.AttackScanRadius))
					.Where(owner.SquadManager.IsEnemyUnit);
				var target = enemies.ClosestTo(leader.CenterPosition);
				if (target != null)
				{
					owner.TargetActor = target;
				}

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

			if (!owner.IsTargetValid)
			{
				var closestEnemy = FindClosestEnemyBuilding(owner);
				if (closestEnemy != null)
					owner.TargetActor = closestEnemy;
				else
				{
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState(), true);
					return;
				}
			}

			foreach (var a in owner.Units)
				if (!BusyAttack(a))
					owner.Bot.QueueOrder(new Order("Attack", a, Target.FromActor(owner.TargetActor), false));

			if (ShouldFlee(owner))
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState(), true);
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

			GoToRandomOwnBuilding(owner);
			owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleState(), true);
		}

		public void Deactivate(SquadCA owner) { owner.Units.Clear(); }
	}
}
