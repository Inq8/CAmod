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

		// Default is 3, multipler used to calculate odds of fleeing when faced with with enemy AA
		protected const int MissileUnitMultiplier = 2;

		protected static int CountAntiAirUnits(IEnumerable<Actor> units)
		{
			if (!units.Any())
				return 0;

			var missileUnitsCount = 0;
			foreach (var unit in units)
			{
				if (unit == null || unit.Info.HasTraitInfo<AircraftInfo>())
					continue;

				foreach (var ab in unit.TraitsImplementing<AttackBase>())
				{
					if (ab.IsTraitDisabled || ab.IsTraitPaused)
						continue;

					foreach (var a in ab.Armaments)
					{
						if (a.Weapon.IsValidTarget(AirTargetTypes))
						{
							missileUnitsCount++;
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
			var dangerRadius = owner.SquadManager.Info.AircraftDangerScanRadius;
			detectedEnemyTarget = null;
			var x = (map.MapSize.X % dangerRadius) == 0 ? map.MapSize.X : map.MapSize.X + dangerRadius;
			var y = (map.MapSize.Y % dangerRadius) == 0 ? map.MapSize.Y : map.MapSize.Y + dangerRadius;

			for (var i = 0; i < x; i += dangerRadius * 2)
			{
				for (var j = 0; j < y; j += dangerRadius * 2)
				{
					var pos = new CPos(i, j);
					if (NearToPosSafely(owner, map.CenterOfCell(pos), out detectedEnemyTarget))
					{
						if (needTarget && detectedEnemyTarget == null)
							continue;

						return pos;
					}
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

			if (CountAntiAirUnits(unitsAroundPos) * MissileUnitMultiplier < owner.Units.Count)
			{
				detectedEnemyTarget = unitsAroundPos.Random(owner.Random);
				return true;
			}

			return false;
		}

		protected static bool FullAmmo(Actor a)
		{
			var ammoPools = a.TraitsImplementing<AmmoPool>();
			return ammoPools.All(x => x.HasFullAmmo);
		}

		protected static bool HasAmmo(Actor a)
		{
			var ammoPools = a.TraitsImplementing<AmmoPool>();
			return ammoPools.All(x => x.HasFullAmmo);
		}

		protected static bool ReloadsAutomatically(Actor a)
		{
			var ammoPools = a.TraitsImplementing<AmmoPool>();
			var rearmable = a.TraitOrDefault<Rearmable>();
			if (rearmable == null)
				return true;

			return ammoPools.All(ap => !rearmable.Info.AmmoPools.Contains(ap.Info.Name));
		}

		protected static bool IsRearm(Actor a)
		{
			if (a.IsIdle)
				return false;

			var activity = a.CurrentActivity;
			var type = activity.GetType();
			if (type == typeof(Resupply))
				return true;

			var next = activity.NextActivity;
			if (next == null)
				return false;

			var nextType = next.GetType();
			if (nextType == typeof(Resupply))
				return true;

			return false;
		}

		// Checks the number of anti air enemies around units
		protected virtual bool ShouldFlee(SquadCA owner)
		{
			return ShouldFlee(owner, enemies => CountAntiAirUnits(enemies) * MissileUnitMultiplier > owner.Units.Count);
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
				return;

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

			if (!NearToPosSafely(owner, owner.TargetActor.CenterPosition))
			{
				owner.FuzzyStateMachine.ChangeState(owner, new AirFleeState(), true);
				return;
			}

			foreach (var a in owner.Units)
			{
				if (BusyAttack(a))
					continue;

				if (!ReloadsAutomatically(a))
				{
					if (IsRearm(a))
						continue;

					if (!HasAmmo(a))
					{
						owner.Bot.QueueOrder(new Order("ReturnToBase", a, false));
						continue;
					}
				}

				if (CanAttackTarget(a, owner.TargetActor))
					owner.Bot.QueueOrder(new Order("Attack", a, Target.FromActor(owner.TargetActor), false));
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

			foreach (var a in owner.Units)
			{
				if (!ReloadsAutomatically(a) && !FullAmmo(a))
				{
					if (IsRearm(a))
						continue;

					owner.Bot.QueueOrder(new Order("ReturnToBase", a, false));
					continue;
				}

				owner.Bot.QueueOrder(new Order("Move", a, Target.FromCell(owner.World, RandomBuildingLocation(owner)), false));
			}

			owner.FuzzyStateMachine.ChangeState(owner, new AirIdleState(), true);
		}

		public void Deactivate(SquadCA owner) { }
	}
}
