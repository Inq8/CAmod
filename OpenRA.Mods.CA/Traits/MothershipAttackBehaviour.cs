#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Attack replaced by force attack if actor is not mobile. Stop on ammo empty.")]
	class MothershipAttackBehaviourInfo : ConditionalTraitInfo
	{
		[Desc("Issue stop command if ammo is empty.")]
		public readonly bool StopOnEmpty = true;

		public override object Create(ActorInitializer init) { return new MothershipAttackBehaviour(init, this); }
	}

	class MothershipAttackBehaviour : ConditionalTrait<MothershipAttackBehaviourInfo>, IResolveOrder, ITick, INotifyAttack
	{
		bool attackStarted;
		bool ammoActive;
		bool issueStopOrder;

		public MothershipAttackBehaviour(ActorInitializer init, MothershipAttackBehaviourInfo info)
			: base(info)
		{
			attackStarted = false;
			ammoActive = false;
			issueStopOrder = false;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (IsTraitDisabled)
				return;

			if ((order.OrderString == "Attack" || order.OrderString == "ForceAttack") && order.Target.Type == TargetType.Actor)
			{
				if (order.Target.Actor.TraitOrDefault<Mobile>() != null)
					return;

				var target = Target.FromPos(order.Target.Actor.CenterPosition);
				self.World.IssueOrder(new Order("ForceAttack", self, target, false, null, null));
			}
		}

		void ITick.Tick(Actor self)
		{
			if (issueStopOrder)
			{
				var rejectsOrders = false;
				foreach (var r in self.TraitsImplementing<RejectsOrders>())
				{
					if (!r.IsTraitDisabled)
					{
						rejectsOrders = true;
						break;
					}
				}

				if (!rejectsOrders)
				{
					self.World.IssueOrder(new Order("Stop", self, false));
					issueStopOrder = false;
				}
			}

			if (!Info.StopOnEmpty || !attackStarted)
				return;

			var ammoPools = self.TraitsImplementing<AmmoPool>();
			var ammoCount = 0;
			foreach (var ammoPool in ammoPools)
				ammoCount += ammoPool.CurrentAmmoCount;

			if (ammoActive && ammoCount == 0 && attackStarted)
			{
				issueStopOrder = true;
				attackStarted = false;
				ammoActive = false;
			}
			else if (ammoCount > 0)
				ammoActive = true;
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			attackStarted = true;
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }
	}
}
