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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	abstract class AirStateBaseCA : StateBaseCA
	{
		static readonly BitSet<TargetableType> AirTargetTypes = new BitSet<TargetableType>("Air");

		protected const int StaticAntiAirMultiplier = 4;

		protected static int CountStaticAntiAir(IEnumerable<Actor> units, SquadCA owner)
		{
			if (!units.Any())
				return 0;

			return units.Count(a => owner.SquadManager.Info.StaticAntiAirTypes.Contains(a.Info.Name)) * StaticAntiAirMultiplier;
		}

		protected static Actor FindAirTarget(SquadCA owner)
		{
			Actor target = null;
			var groundTargetPriority = owner.World.LocalRandom.Next(0, 100);

			if (groundTargetPriority > owner.SquadManager.Info.AirToAirPriority)
				return target;

			var leader = owner.Units.FirstOrDefault();
			if (leader == null)
				return target;

			var canAttackAir = false;
			foreach (var ab in leader.TraitsImplementing<AttackBase>())
			{
				foreach (var a in ab.Armaments)
				{
					if (a.Weapon.IsValidTarget(AirTargetTypes))
					{
						canAttackAir = true;
						break;
					}
				}
			}

			if (canAttackAir)
			{
				var pos = leader.CenterPosition;

				if (owner.SquadManager.Info.BigAirThreats.Any())
				{
					var regularTargetPriority = owner.World.LocalRandom.Next(0, 100);

					if (owner.SquadManager.Info.AirToAirPriority > regularTargetPriority)
					{
						target = owner.World.Actors.Where(a => owner.SquadManager.IsPreferredEnemyAircraft(a) &&
							owner.SquadManager.IsNotHiddenUnit(a) &&
							a.IsTargetableBy(leader) &&
							owner.SquadManager.Info.BigAirThreats.Contains(a.Info.Name))
							.ClosestTo(pos);
					}
				}

				if (target != null)
					return target;

				target = owner.World.Actors.Where(a => owner.SquadManager.IsPreferredEnemyAircraft(a) &&
					owner.SquadManager.IsNotHiddenUnit(a) &&
					a.IsTargetableBy(leader))
					.ClosestTo(pos);
			}

			return target;
		}

		protected static Actor FindDefenselessTarget(SquadCA owner)
		{
			Actor target = null;

			target = FindAirTarget(owner);
			if (target != null)
				return target;

			FindSafePlace(owner, out target, true);
			return target;
		}

		protected static CPos? FindSafePlace(SquadCA owner, out Actor detectedEnemyTarget, bool needTarget)
		{
			var map = owner.World.Map;
			var dangerRadius = owner.SquadManager.Info.DangerScanRadius;
			detectedEnemyTarget = null;

			var columnCount = (map.MapSize.X + dangerRadius - 1) / dangerRadius;
			var rowCount = (map.MapSize.Y + dangerRadius - 1) / dangerRadius;

			var checkIndices = Exts.MakeArray(columnCount * rowCount, i => i).Shuffle(owner.World.LocalRandom);
			foreach (var i in checkIndices)
			{
				var pos = new MPos((i % columnCount) * dangerRadius + dangerRadius / 2, (i / columnCount) * dangerRadius + dangerRadius / 2).ToCPos(map);

				if (NearToPosSafely(owner, map.CenterOfCell(pos), out detectedEnemyTarget))
				{
					if (needTarget && detectedEnemyTarget == null)
						continue;

					return pos;
				}
			}

			return null;
		}

		protected static bool NearToPosSafely(SquadCA owner, WPos loc)
		{
			return NearToPosSafely(owner, loc, out _);
		}

		protected static bool NearToPosSafely(SquadCA owner, WPos loc, out Actor detectedEnemyTarget)
		{
			detectedEnemyTarget = null;
			var dangerRadius = owner.SquadManager.Info.DangerScanRadius;
			var unitsAroundPos = owner.World.FindActorsInCircle(loc, WDist.FromCells(dangerRadius))
				.Where(a => owner.SquadManager.IsPreferredEnemyUnit(a));

			if (!unitsAroundPos.Any())
				return true;

			var possibleTargets = unitsAroundPos.Where(a => owner.SquadManager.IsAirSquadTargetType(a, owner)).ToList();
			var possibleAntiAir = unitsAroundPos.ToList();

			if (CountStaticAntiAir(possibleAntiAir, owner) < owner.Units.Count)
			{
				if (possibleTargets.Any())
					detectedEnemyTarget = possibleTargets.Random(owner.Random);

				return true;
			}

			return false;
		}

		// Checks the number of anti air enemies around units
		protected virtual bool ShouldFlee(SquadCA owner)
		{
			return ShouldFlee(owner, enemies => CountStaticAntiAir(enemies, owner) > owner.Units.Count);
		}
	}

	class AirIdleStateCA : AirStateBaseCA, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			if (ShouldFlee(owner))
			{
				owner.FuzzyStateMachine.ChangeState(owner, new AirFleeStateCA(), true);
				return;
			}

			var e = FindDefenselessTarget(owner);
			if (e == null)
				return;

			owner.TargetActor = e;
			owner.FuzzyStateMachine.ChangeState(owner, new AirAttackStateCA(), true);
		}

		public void Deactivate(SquadCA owner) { }
	}

	class AirAttackStateCA : AirStateBaseCA, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			var newTarget = false;

			if (!owner.IsValid)
				return;

			// target is no longer valid, find a new target
			if (!owner.IsTargetValid)
			{
				var closestEnemy = FindAirTarget(owner);
				if (closestEnemy == null)
				{
					var a = owner.Units.Random(owner.Random);

					// copied from FindClosestEnemy() so that non-air squads don't check IsAirSquadTargetType unnecessarily
					var units = a.World.Actors.Where(t => owner.SquadManager.IsPreferredEnemyUnit(t) && owner.SquadManager.IsAirSquadTargetType(t, owner));
					closestEnemy = units.Where(owner.SquadManager.IsNotHiddenUnit).ClosestTo(a.CenterPosition) ?? units.ClosestTo(a.CenterPosition);
				}

				if (closestEnemy != null)
				{
					owner.TargetActor = closestEnemy;
					newTarget = true;
				}
				else
				{
					owner.FuzzyStateMachine.ChangeState(owner, new AirFleeStateCA(), true);
					return;
				}
			}

			if (!NearToPosSafely(owner, owner.TargetActor.CenterPosition))
			{
				owner.FuzzyStateMachine.ChangeState(owner, new AirFleeStateCA(), true);
				return;
			}

			var leader = owner.Units.FirstOrDefault();
			var buildableInfo = leader.Info.TraitInfoOrDefault<BuildableInfo>();
			var limitOne = buildableInfo != null && buildableInfo.BuildLimit == 1;
			var canBuildMoreOfAircraft = leader != null ? !limitOne && owner.SquadManager.CanBuildMoreOfAircraft(leader.Info) : false;
			var waitingCount = owner.WaitingUnits.Count();

			var waitingPatience = 99;
			if (waitingCount > 7)
				waitingPatience = 65;
			else if (waitingCount > 3)
				waitingPatience = 85;
			else if (waitingCount > 1)
				waitingPatience = 95;

			var impatience = owner.World.LocalRandom.Next(100);
			var noPatience = impatience > waitingPatience;

			foreach (var a in owner.Units)
			{
				var currentActivity = a.CurrentActivity;
				var activityType = currentActivity.GetType();
				var nextActivity = currentActivity.NextActivity;

				var ammoPools = a.TraitsImplementing<AmmoPool>().ToArray();
				if (!ReloadsAutomatically(ammoPools, a.TraitOrDefault<Rearmable>()))
				{
					if (!HasAmmo(ammoPools) && !owner.RearmingUnits.Contains(a))
					{
						owner.RearmingUnits.Add(a);
						owner.WaitingUnits.Remove(a);
						owner.NewUnits.Remove(a);
						owner.Bot.QueueOrder(new Order("ReturnToBase", a, false));
						continue;
					}

					if (owner.NewUnits.Contains(a))
					{
						owner.WaitingUnits.Add(a);
						owner.NewUnits.Remove(a);
						owner.Bot.QueueOrder(new Order("Move", a, Target.FromCell(owner.World, RandomBuildingLocation(owner)), false));
					}

					if (owner.WaitingUnits.Contains(a))
						continue;

					if (owner.RearmingUnits.Contains(a))
					{
						if (FullAmmo(ammoPools))
						{
							owner.RearmingUnits.Remove(a);
							owner.WaitingUnits.Add(a);
							owner.Bot.QueueOrder(new Order("Move", a, Target.FromCell(owner.World, RandomBuildingLocation(owner)), false));
						}
						else
							owner.Bot.QueueOrder(new Order("ReturnToBase", a, false));

						continue;
					}

					// target switched or not attacking, attack the target
					if ((newTarget || activityType != typeof(FlyAttack)) && CanAttackTarget(a, owner.TargetActor))
					{
						owner.Bot.QueueOrder(new Order("Attack", a, Target.FromActor(owner.TargetActor), false));
						continue;
					}
					else if (activityType == typeof(FlyIdle))
					{
						owner.RearmingUnits.Add(a);
						owner.Bot.QueueOrder(new Order("ReturnToBase", a, false));
					}
				}
				else
					owner.WaitingUnits.Add(a);
			}

			if ((!canBuildMoreOfAircraft || noPatience) && owner.WaitingUnits.Any() && owner.WaitingUnits.Count() >= owner.RearmingUnits.Count())
			{
				foreach (var a in owner.WaitingUnits)
					if (CanAttackTarget(a, owner.TargetActor))
						owner.Bot.QueueOrder(new Order("Attack", a, Target.FromActor(owner.TargetActor), false));

				owner.WaitingUnits.Clear();
			}
		}

		public void Deactivate(SquadCA owner) { }
	}

	class AirFleeStateCA : AirStateBaseCA, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

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

				owner.Bot.QueueOrder(new Order("Move", a, Target.FromCell(owner.World, RandomBuildingLocation(owner)), false));
			}

			owner.FuzzyStateMachine.ChangeState(owner, new AirIdleStateCA(), true);
		}

		public void Deactivate(SquadCA owner) { }
	}
}
