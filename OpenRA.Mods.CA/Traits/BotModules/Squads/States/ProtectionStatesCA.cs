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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	abstract class ProtectionStateBaseCA : GroundStateBaseCA
	{
	}

	class UnitsForProtectionIdleState : ProtectionStateBaseCA, IState
	{
		public void Activate(SquadCA owner) { }
		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid)
			{
				owner.TargetActor = owner.SquadManager.FindClosestEnemy(owner.CenterPosition, WDist.FromCells(owner.SquadManager.Info.ProtectionScanRadius));

				if (owner.TargetActor == null)
				{
					Retreat(owner, flee: false, rearm: true, repair: true);
					return;
				}
			}

			owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionAttackStateCA(), false);
		}

		public void Deactivate(SquadCA owner) { }
	}

	class UnitsForProtectionAttackStateCA : ProtectionStateBaseCA, IState
	{
		public const int BackoffTicks = 4;
		internal int Backoff = BackoffTicks;

		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid)
			{
				owner.TargetActor = owner.SquadManager.FindClosestEnemy(owner.CenterPosition, WDist.FromCells(owner.SquadManager.Info.ProtectionScanRadius));

				if (owner.TargetActor == null)
				{
					owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionFleeStateCA(), false);
					return;
				}
			}

			// rescan target to prevent being ambushed and die without fight
			// return to AttackMove state for formation
			var leader = owner.Units.ClosestTo(owner.TargetActor.CenterPosition);
			if (leader == null)
				return;
			var protectionScanRadius = WDist.FromCells(owner.SquadManager.Info.ProtectionScanRadius);
			var targetActor = owner.SquadManager.FindClosestEnemy(leader.CenterPosition, protectionScanRadius);
			var cannotRetaliate = false;
			List<Actor> resupplyingUnits = new List<Actor>();
			List<Actor> followingUnits = new List<Actor>();
			List<Actor> attackingUnits = new List<Actor>();

			if (targetActor != null)
				owner.TargetActor = targetActor;

			if (!owner.IsTargetVisible)
			{
				if (Backoff < 0)
				{
					owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionFleeStateCA(), false);
					Backoff = BackoffTicks;
					return;
				}

				Backoff--;
			}
			else
			{
				cannotRetaliate = true;

				foreach (var a in owner.Units)
				{
					// Air units control:
					var ammoPools = a.TraitsImplementing<AmmoPool>().ToArray();
					if (a.Info.HasTraitInfo<AircraftInfo>() && ammoPools.Any())
					{
						if (BusyAttack(a))
						{
							cannotRetaliate = false;
							continue;
						}

						if (!ReloadsAutomatically(ammoPools, a.TraitOrDefault<Rearmable>()))
						{
							if (IsRearming(a))
								continue;

							if (!HasAmmo(ammoPools))
							{
								resupplyingUnits.Add(a);
								continue;
							}
						}

						if (CanAttackTarget(a, owner.TargetActor))
						{
							attackingUnits.Add(a);
							cannotRetaliate = false;
						}
						else
							followingUnits.Add(a);
					}

					// Ground/naval units control:
					else
					{
						if (CanAttackTarget(a, owner.TargetActor))
						{
							attackingUnits.Add(a);
							cannotRetaliate = false;
						}
						else
							followingUnits.Add(a);
					}
				}
			}

			if (cannotRetaliate)
			{
				owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionFleeStateCA(), false);
				return;
			}

			owner.Bot.QueueOrder(new Order("ReturnToBase", null, false, groupedActors: resupplyingUnits.ToArray()));
			owner.Bot.QueueOrder(new Order("AttackMove", null, Target.FromCell(owner.World, leader.Location), false, groupedActors: followingUnits.ToArray()));
			owner.Bot.QueueOrder(new Order("Attack", null, Target.FromActor(owner.TargetActor), false, groupedActors: attackingUnits.ToArray()));
		}

		public void Deactivate(SquadCA owner) { }
	}

	class UnitsForProtectionFleeStateCA : ProtectionStateBaseCA, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			Retreat(owner, flee: true, rearm: true, repair: true);
			owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionIdleState(), false);
		}

		public void Deactivate(SquadCA owner) { owner.Units.Clear(); }
	}
}
