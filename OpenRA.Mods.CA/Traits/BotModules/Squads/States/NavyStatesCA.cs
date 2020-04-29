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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	abstract class NavyStateBase : StateBaseCA
	{
		protected bool hadNavalYard = false;
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
			var locomotorInfo = first.Info.TraitInfo<MobileInfo>().LocomotorInfo;

			var navalProductions = owner.World.ActorsHavingTrait<Building>().Where(a
				=> owner.SquadManager.Info.NavalProductionTypes.Contains(a.Info.Name)
				&& domainIndex.IsPassable(first.Location, a.Location, locomotorInfo)
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
	}

	class NavyUnitsIdleState : NavyStateBase, IState
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
				owner.FuzzyStateMachine.ChangeState(owner, new NavyUnitsAttackMoveState(), true);
			}
			else
				owner.FuzzyStateMachine.ChangeState(owner, new NavyUnitsFleeState(), true);
		}

		public void Deactivate(SquadCA owner) { }
	}

	class NavyUnitsAttackMoveState : NavyStateBase, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			// Check if we have an enemy naval yard this tick
			var first = owner.Units.First();
			var domainIndex = first.World.WorldActor.Trait<DomainIndex>();
			var locomotorInfo = first.Info.TraitInfo<MobileInfo>().LocomotorInfo;
			var navalProductions = owner.World.ActorsHavingTrait<Building>().Where(a
				=> owner.SquadManager.Info.NavalProductionTypes.Contains(a.Info.Name)
				&& domainIndex.IsPassable(first.Location, a.Location, locomotorInfo)
				&& a.AppearsHostileTo(first));

			// if target is dead or if we have a newly built naval yard this tick invalidate the current target and select a new one.
			if (!owner.IsTargetValid || (navalProductions.Any() && !hadNavalYard))
			{
				var closestEnemy = FindClosestEnemy(owner);
				if (closestEnemy != null)
					owner.TargetActor = closestEnemy;
				else
				{
					owner.FuzzyStateMachine.ChangeState(owner, new NavyUnitsFleeState(), true);
					hadNavalYard = false;
					return;
				}
			}

			// Save whether we had an Enemy naval yard this tick.
			hadNavalYard = navalProductions.Any();

			foreach (var a in owner.Units)
				owner.Bot.QueueOrder(new Order("AttackMove", a, Target.FromCell(owner.World, owner.TargetActor.Location), false));

			if (ShouldFlee(owner))
			{
				owner.FuzzyStateMachine.ChangeState(owner, new NavyUnitsFleeState(), true);
				hadNavalYard = false;
			}
		}

		public void Deactivate(SquadCA owner) { }
	}

	class NavyUnitsAttackState : NavyStateBase, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid)
			{
				var closestEnemy = FindClosestEnemy(owner);
				if (closestEnemy != null)
					owner.TargetActor = closestEnemy;
				else
				{
					owner.FuzzyStateMachine.ChangeState(owner, new NavyUnitsFleeState(), true);
					return;
				}
			}

			foreach (var a in owner.Units)
				if (!BusyAttack(a))
					owner.Bot.QueueOrder(new Order("Attack", a, Target.FromCell(owner.World, owner.TargetActor.Location), false));

			if (ShouldFlee(owner))
				owner.FuzzyStateMachine.ChangeState(owner, new NavyUnitsFleeState(), true);
		}

		public void Deactivate(SquadCA owner) { }
	}

	class NavyUnitsFleeState : NavyStateBase, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			Retreat(owner, false);
			owner.FuzzyStateMachine.ChangeState(owner, new NavyUnitsIdleState(), true);
		}

		public void Deactivate(SquadCA owner) { owner.Units.Clear(); }
	}
}
