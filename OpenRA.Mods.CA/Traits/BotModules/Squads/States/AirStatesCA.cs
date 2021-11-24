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
			var airPriorityChance = owner.World.LocalRandom.Next(0, 100);

			if (airPriorityChance > owner.SquadManager.Info.AirToAirPriority)
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

			if (!owner.IsTargetValid)
			{
				//Game.Debug("target no longer valid, find new target");
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
					//Game.Debug("no targets, flee");
					owner.FuzzyStateMachine.ChangeState(owner, new AirFleeStateCA(), true);
					return;
				}
			}

			if (!NearToPosSafely(owner, owner.TargetActor.CenterPosition))
			{
				// Game.Debug("no neartopossafely, flee");
				owner.FuzzyStateMachine.ChangeState(owner, new AirFleeStateCA(), true);
				return;
			}

			foreach (var a in owner.Units)
			{
				var currentActivity = a.CurrentActivity;
				var activityType = currentActivity.GetType();
				var nextActivity = currentActivity.NextActivity;

				var ammoPools = a.TraitsImplementing<AmmoPool>().ToArray();
				if (!ReloadsAutomatically(ammoPools, a.TraitOrDefault<Rearmable>()))
				{
					if (owner.WaitingUnits.Contains(a))
					{
						//Game.Debug("waiting");
						continue;
					}

					if (!HasAmmo(ammoPools) && !owner.RearmingUnits.Contains(a))
					{
						//Game.Debug("needs to rearm");
						owner.RearmingUnits.Add(a);
						owner.Bot.QueueOrder(new Order("ReturnToBase", a, false));
						continue;
					}

					if (owner.RearmingUnits.Contains(a) || owner.NewUnits.Contains(a))
					{
						if (FullAmmo(ammoPools))
						{
							//Game.Debug("finished rearming, moved to waiting");
							owner.RearmingUnits.Remove(a);
							owner.NewUnits.Remove(a);
							owner.WaitingUnits.Add(a);
							owner.Bot.QueueOrder(new Order("AttackMove", a, Target.FromCell(owner.World, RandomBuildingLocation(owner)), false));
						}

						continue;
					}

					if (CanAttackTarget(a, owner.TargetActor) && (newTarget || activityType != typeof(FlyAttack)))
					{
						owner.Bot.QueueOrder(new Order("Attack", a, Target.FromActor(owner.TargetActor), false));
						continue;
					}
					else if (activityType == typeof(FlyIdle))
						owner.Bot.QueueOrder(new Order("AttackMove", a, Target.FromCell(owner.World, RandomBuildingLocation(owner)), false));
				}
				else
					owner.WaitingUnits.Add(a);
			}

			if (!owner.RearmingUnits.Any() && owner.WaitingUnits.Any())
			{
				//Game.Debug("no rearming units, all waiting, so attack");
				foreach (var a in owner.Units)
				{
					if (CanAttackTarget(a, owner.TargetActor))
						owner.Bot.QueueOrder(new Order("Attack", a, Target.FromActor(owner.TargetActor), false));
				}

				owner.WaitingUnits.Clear();
			}

			//Game.Debug("------ end tick ({0} new, {1} rearming, {2} waiting, {3} total", owner.NewUnits.Count(), owner.RearmingUnits.Count(), owner.WaitingUnits.Count(), owner.Units.Count);
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
