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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	abstract class AirStateBase : StateBaseCA
	{
		static readonly BitSet<TargetableType> AirTargetTypes = new BitSet<TargetableType>("Air");

		protected static int CountAntiAirUnits(IEnumerable<Actor> units)
		{
			if (!units.Any())
				return 0;

			var missileUnitsCount = 0;
			foreach (var unit in units)
			{
				if (unit == null)
					continue;

				foreach (var ab in unit.TraitsImplementing<AttackBase>())
				{
					if (ab.IsTraitDisabled || ab.IsTraitPaused)
						continue;

					foreach (var a in ab.Armaments)
					{
						if (a.Weapon.IsValidTarget(AirTargetTypes))
						{
							if (unit.Info.HasTraitInfo<AircraftInfo>())
								missileUnitsCount += 1;
							else
								missileUnitsCount += 3;
							break;
						}
					}
				}
			}

			return missileUnitsCount;
		}

		protected static Actor FindDefenselessTarget(SquadCA owner)
		{
			Actor target = null;
			FindSafePlace(owner, out target, true);
			return target;
		}

		protected static CPos? FindSafePlace(SquadCA owner, out Actor detectedEnemyTarget, bool needTarget)
		{
			var map = owner.World.Map;
			var dangerRadius = owner.SquadManager.Info.DangerScanRadius;
			detectedEnemyTarget = null;
			var initialMaxX = (map.MapSize.X % dangerRadius) == 0 ? map.MapSize.X : map.MapSize.X + dangerRadius;
			var initialMaxY = (map.MapSize.Y % dangerRadius) == 0 ? map.MapSize.Y : map.MapSize.Y + dangerRadius;
			var maxX = initialMaxX;
			var maxY = initialMaxY;

			// Random starting coordinates for scanning the map to find a target.
			var startX = owner.World.LocalRandom.Next(0, map.MapSize.X);
			var startY = owner.World.LocalRandom.Next(0, map.MapSize.Y);
			var initialStartY = startY;

			var scanReset = false;
			var scanDirectionX = startX % 2;
			var scanDirectionY = startY % 2;
			var scanIncrement = dangerRadius * 2;

			for (var x = startX; x <= maxX; x += scanIncrement)
			{
				// On second pass of initial column, maximum y should be the initial y.
				if (x == startX && scanReset)
					maxY = initialStartY;

				for (var y = startY; y <= maxY; y += scanIncrement)
				{
					// Translate to position based on the scan direction.
					var posX = scanDirectionX == 0 ? initialMaxX - x : x;
					var posY = scanDirectionY == 0 ? initialMaxY - y : y;

					var pos = new CPos(posX, posY);
					if (NearToPosSafely(owner, map.CenterOfCell(pos), out detectedEnemyTarget))
					{
						if (needTarget && detectedEnemyTarget == null)
							continue;

						return pos;
					}
				}

				// On first pass of initial column, set to start from y = 0 on subsequent columns.
				if (x == startX && !scanReset)
					startY = 0;

				// If the next iteration will be beyond the maximum, reset x so it starts from 0 on next iteration and set max to where it started.
				if (x + scanIncrement > maxX && !scanReset)
				{
					scanReset = true;
					maxX = startX;
					x = scanIncrement * -1;
				}
			}

			return null;
		}

		public static bool NearToPosSafely(SquadCA owner, WPos loc)
		{
			Actor a;
			return NearToPosSafely(owner, loc, out a);
		}

		protected static bool NearToPosSafely(SquadCA owner, WPos loc, out Actor detectedEnemyTarget)
		{
			detectedEnemyTarget = null;
			var dangerRadius = owner.SquadManager.Info.AircraftDangerScanRadius;
			var unitsAroundPos = owner.World.FindActorsInCircle(loc, WDist.FromCells(dangerRadius))
				.Where(owner.SquadManager.IsEnemyUnit).ToList();

			if (!unitsAroundPos.Any())
				return true;

			if (CountAntiAirUnits(unitsAroundPos) < owner.Units.Count)
			{
				detectedEnemyTarget = unitsAroundPos.Random(owner.Random);
				return true;
			}

			return false;
		}

		// Checks the number of anti air enemies around units
		protected virtual bool ShouldFlee(SquadCA owner)
		{
			return ShouldFlee(owner, enemies => CountAntiAirUnits(enemies) > owner.Units.Count);
		}

		// Retreat units from combat, or for supply only in idle
		protected void Retreat(SquadCA owner, bool resupplyonly)
		{
			// Reload units.
			foreach (var a in owner.Units)
			{
				var ammoPools = a.TraitsImplementing<AmmoPool>().ToArray();
				if (!ReloadsAutomatically(ammoPools, a.TraitOrDefault<Rearmable>()) && !FullAmmo(ammoPools))
				{
					if (IsRearming(a))
						continue;

					owner.Bot.QueueOrder(new Order("ReturnToBase", a, false));
					continue;
				}
				else if (!resupplyonly)
					owner.Bot.QueueOrder(new Order("Move", a, Target.FromCell(owner.World, RandomBuildingLocation(owner)), false));
			}

			// Repair units. One by one to avoid give out mass orders
			foreach (var a in owner.Units)
			{
				if (IsRearming(a))
					continue;

				Actor repairBuilding = null;
				var orderId = "Repair";
				var health = a.TraitOrDefault<IHealth>();

				if (health != null && health.DamageState > DamageState.Undamaged)
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
						owner.Bot.QueueOrder(new Order(orderId, a, Target.FromActor(repairBuilding), true));
						break;
					}
				}
			}
		}
	}

	class AirIdleState : AirStateBase, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			if (ShouldFlee(owner))
			{
				owner.FuzzyStateMachine.ChangeState(owner, new AirFleeState(), true);
				return;
			}

			var e = FindDefenselessTarget(owner);
			if (e == null)
			{
				Retreat(owner, true);
				return;
			}

			owner.TargetActor = e;
			owner.FuzzyStateMachine.ChangeState(owner, new AirAttackState(), true);
		}

		public void Deactivate(SquadCA owner) { }
	}

	class AirAttackState : AirStateBase, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid)
			{
				var a = owner.Units.Random(owner.Random);
				var closestEnemy = owner.SquadManager.FindClosestEnemy(a.CenterPosition);
				if (closestEnemy != null)
					owner.TargetActor = closestEnemy;
				else
				{
					owner.FuzzyStateMachine.ChangeState(owner, new AirFleeState(), true);
					return;
				}
			}

			var teamLeader = owner.Units.ClosestTo(owner.TargetActor.CenterPosition);

			var unitsAroundPos = owner.World.FindActorsInCircle(teamLeader.CenterPosition, WDist.FromCells(owner.SquadManager.Info.DangerScanRadius))
				.Where(a => owner.SquadManager.IsEnemyUnit(a) && owner.SquadManager.IsNotHiddenUnit(a));
			var ambushed = CountAntiAirUnits(unitsAroundPos) > owner.Units.Count;

			if ((!NearToPosSafely(owner, owner.TargetActor.CenterPosition)) || ambushed)
			{
				owner.FuzzyStateMachine.ChangeState(owner, new AirFleeState(), true);
				return;
			}

			foreach (var a in owner.Units)
			{
				if (BusyAttack(a))
					continue;

				var ammoPools = a.TraitsImplementing<AmmoPool>();
				if (!ReloadsAutomatically(ammoPools, a.TraitOrDefault<Rearmable>()))
				{
					if (IsRearming(a))
						continue;

					if (!HasAmmo(ammoPools))
					{
						owner.Bot.QueueOrder(new Order("ReturnToBase", a, false));
						continue;
					}
				}

				if (CanAttackTarget(a, owner.TargetActor))
					owner.Bot.QueueOrder(new Order("Attack", a, Target.FromActor(owner.TargetActor), false));
				else
					owner.Bot.QueueOrder(new Order("Move", a, Target.FromCell(owner.World, RandomBuildingLocation(owner)), false));
			}
		}

		public void Deactivate(SquadCA owner) { }
	}

	class AirFleeState : AirStateBase, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			Retreat(owner, false);

			owner.FuzzyStateMachine.ChangeState(owner, new AirIdleState(), true);
		}

		public void Deactivate(SquadCA owner) { }
	}
}
