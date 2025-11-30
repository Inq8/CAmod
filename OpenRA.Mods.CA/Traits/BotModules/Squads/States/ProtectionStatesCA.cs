#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Linq;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	class UnitsForProtectionIdleState : GroundStateBaseCA, IState
	{
		public void Activate(SquadCA owner) { }
		public void Tick(SquadCA owner) { owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionAttackState(), true); }
		public void Deactivate(SquadCA owner) { }
	}

	class UnitsForProtectionAttackState : GroundStateBaseCA, IState
	{
		public const int BackoffTicks = 4;
		internal int Backoff = BackoffTicks;

		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			var leader = Leader(owner);
			if (!owner.IsTargetValid(leader))
			{
				var target = owner.SquadManager.FindClosestEnemy(leader, WDist.FromCells(owner.SquadManager.Info.ProtectionScanRadius));
				owner.SetActorToTarget(target);
				if (owner.TargetActor == null)
				{
					owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionFleeState(), true);
					return;
				}
			}

			if (!owner.IsTargetVisible)
			{
				if (Backoff < 0)
				{
					owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionFleeState(), true);
					Backoff = BackoffTicks;
					return;
				}

				Backoff--;
			}
			else
				owner.Bot.QueueOrder(new Order("AttackMove", null, owner.Target, false, groupedActors: owner.Units.ToArray()));
		}

		public void Deactivate(SquadCA owner) { }
	}

	class UnitsForProtectionFleeState : GroundStateBaseCA, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			GoToRandomOwnBuilding(owner);
			owner.FuzzyStateMachine.ChangeState(owner, new UnitsForProtectionIdleState(), true);
		}

		public void Deactivate(SquadCA owner) { owner.Units.Clear(); }
	}
}
