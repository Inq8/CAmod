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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	abstract class StateBaseCA
	{
		protected static void GoToRandomOwnBuilding(SquadCA squad)
		{
			var loc = RandomBuildingLocation(squad);
			foreach (var a in squad.Units)
				squad.Bot.QueueOrder(new Order("Move", a, Target.FromCell(squad.World, loc), false));
		}

		protected static CPos RandomBuildingLocation(SquadCA squad)
		{
			var location = squad.SquadManager.GetRandomBaseCenter();
			var buildings = squad.World.ActorsHavingTrait<Building>()
				.Where(a => a.Owner == squad.Bot.Player).ToList();
			if (buildings.Count > 0)
				location = buildings.Random(squad.Random).Location;

			return location;
		}

		protected static bool BusyAttack(Actor a)
		{
			if (a.IsIdle)
				return false;

			var activity = a.CurrentActivity;
			var type = activity.GetType();
			if (type == typeof(Attack) || type == typeof(FlyAttack))
				return true;

			var next = activity.NextActivity;
			if (next == null)
				return false;

			var nextType = next.GetType();
			if (nextType == typeof(Attack) || nextType == typeof(FlyAttack))
				return true;

			return false;
		}

		protected static bool CanAttackTarget(Actor a, Actor target)
		{
			if (!a.Info.HasTraitInfo<AttackBaseInfo>())
				return false;

			var targetTypes = target.GetEnabledTargetTypes();
			if (targetTypes.IsEmpty)
				return false;

			var arms = a.TraitsImplementing<Armament>();
			foreach (var arm in arms)
			{
				if (arm.IsTraitDisabled)
					continue;

				if (arm.Weapon.IsValidTarget(targetTypes))
					return true;
			}

			return false;
		}

		protected virtual bool ShouldFlee(SquadCA squad, Func<IEnumerable<Actor>, bool> flee)
		{
			if (!squad.IsValid)
				return false;

			var randomSquadUnit = squad.Units.Random(squad.Random);
			var dangerRadius = squad.SquadManager.Info.DangerScanRadius;
			var units = squad.World.FindActorsInCircle(randomSquadUnit.CenterPosition, WDist.FromCells(dangerRadius)).ToList();

			// If there are any own buildings within the DangerRadius, don't flee
			// PERF: Avoid LINQ
			foreach (var u in units)
				if ((u.Owner == squad.Bot.Player && u.Info.HasTraitInfo<BuildingInfo>()))
					return false;

			var enemyAroundUnit = units.Where(unit => squad.SquadManager.IsPreferredEnemyUnit(unit) && unit.Info.HasTraitInfo<AttackBaseInfo>());
			if (!enemyAroundUnit.Any())
				return false;

			return flee(enemyAroundUnit);
		}

		protected static bool IsRearming(Actor a)
		{
			if (a.IsIdle)
				return false;

			var activity = a.CurrentActivity;
			var activityType = activity.GetType();
			if (activityType == typeof(Resupply) || activityType == typeof(ReturnToBase))
				return true;

			var next = activity.NextActivity;
			if (next == null)
				return false;

			var nextType = next.GetType();

			if (nextType == typeof(Resupply) || nextType == typeof(ReturnToBase))
				return true;

			return false;
		}

		protected static bool FullAmmo(IEnumerable<AmmoPool> ammoPools)
		{
			foreach (var ap in ammoPools)
				if (!ap.HasFullAmmo)
					return false;

			return true;
		}

		protected static bool HasAmmo(IEnumerable<AmmoPool> ammoPools)
		{
			foreach (var ap in ammoPools)
				if (ap.HasAmmo)
					return true;

			return false;
		}

		protected static bool ReloadsAutomatically(IEnumerable<AmmoPool> ammoPools, Rearmable rearmable)
		{
			if (rearmable == null)
				return true;

			foreach (var ap in ammoPools)
				if (rearmable.Info.AmmoPools.Contains(ap.Info.Name))
					return false;

			return true;
		}
	}
}
