#region Copyright & License Information
/*
 * By Boolbada of OP Mod
 * Follows OpenRA's license as follows:
 *
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class HarvesterBalancerInfo : ConditionalTraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant")]
		public readonly string Condition = null;

		[Desc("Harvester must be within this distance from the refinery to be granted the condition.")]
		public readonly WDist MaxDistanceFromRefinery = WDist.FromCells(5);

		[Desc("Condition is disabled for this many ticks after harvester takes damage.")]
		public readonly int DisableOnDamageDuration = 125;

		[Desc("Ticks between checking whether the condition should be applied.")]
		public readonly int CheckInterval = 10;

		[Desc("Ticks to apply boost on creation when not linked to refinery (i.e. produced from factory).")]
		public readonly int UnlinkedDuration = 150;

		public override object Create(ActorInitializer init) { return new HarvesterBalancer(this); }
	}

	public class HarvesterBalancer : ConditionalTrait<HarvesterBalancerInfo>, INotifyCreated, ITick, INotifyHarvesterAction, INotifyDamage
	{
		int conditionToken = Actor.InvalidConditionToken;
		Actor destinationRefinery;
		bool movingToRefinery = false;
		bool movingToResources = false;
		int damageWindowTicks = 0;
		int ticksUntilCheck = 0;
		int unlinkedBuffTicks = 0;

		public HarvesterBalancer(HarvesterBalancerInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			movingToResources = true;
			unlinkedBuffTicks = Info.UnlinkedDuration;
		}

		void GrantCondition(Actor self, string cond)
		{
			if (string.IsNullOrEmpty(cond))
				return;

			conditionToken = self.GrantCondition(Info.Condition);
		}

		void RevokeCondition(Actor self)
		{
			if (conditionToken == Actor.InvalidConditionToken)
				return;

			conditionToken = self.RevokeCondition(conditionToken);
		}

		bool SpeedBuffNeeded(Actor self)
		{
			if (IsTraitDisabled)
				return false;

			if (damageWindowTicks > 0)
				return false;

			if (!movingToResources && !movingToRefinery)
				return false;

			var facing = self.Trait<IFacing>().Facing;
			var facingUp = (facing.Angle >= 0 && facing.Angle < 256) || facing.Angle > 768;
			var facingDown = facing.Angle > 256 && facing.Angle < 768;
			var isCloseToRefinery = false;

			if (destinationRefinery != null)
			{
				var pos = self.CenterPosition;
				var refineryTarget = Target.FromActor(destinationRefinery);
				isCloseToRefinery = refineryTarget.IsInRange(pos, Info.MaxDistanceFromRefinery);
			}

			if ((isCloseToRefinery || unlinkedBuffTicks > 0) && movingToResources && facingUp)
				return true;

			if (isCloseToRefinery && movingToRefinery && facingDown)
				return true;

			return false;
		}

		void ITick.Tick(Actor self)
		{
			if (ticksUntilCheck > 0)
			{
				ticksUntilCheck--;
			}
			else
			{
				if (SpeedBuffNeeded(self))
				{
					if (conditionToken == Actor.InvalidConditionToken)
						GrantCondition(self, Info.Condition);
				}
				else
				{
					if (conditionToken != Actor.InvalidConditionToken)
						RevokeCondition(self);
				}

				ticksUntilCheck = Info.CheckInterval;
			}

			if (damageWindowTicks > 0)
				damageWindowTicks--;

			if (unlinkedBuffTicks > 0)
				unlinkedBuffTicks--;
		}

		public void MovingToResources(Actor self, CPos targetCell)
		{
		}

		public void MovingToRefinery(Actor self, Actor refineryActor)
		{
			movingToResources = false;
			movingToRefinery = true;
			destinationRefinery = refineryActor;
		}

		public void MovementCancelled(Actor self)
		{
			movingToRefinery = false;
			movingToResources = false;
		}

		public void Harvested(Actor self, ResourceType resource)
		{
			movingToRefinery = false;
			movingToResources = false;
		}

		public void Docked() { }
		public void Undocked()
		{
			movingToRefinery = false;
			movingToResources = true;
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			damageWindowTicks = Info.DisableOnDamageDuration;
		}

		protected override void TraitEnabled(Actor self)
		{
			ticksUntilCheck = 0;
		}

		protected override void TraitDisabled(Actor self)
		{
			ticksUntilCheck = 0;
		}
	}
}
