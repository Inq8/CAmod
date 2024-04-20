#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Toggles a condition on and off when a specified order type is received.")]
	public class GrantConditionOnOrdersInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("Order name that toggles the condition.")]
		public readonly HashSet<string> OrderNames = new HashSet<string> { };

		[Desc("Only grant condition if the target is an actor?")]
		public readonly bool RequiresActorTarget = false;

		[Desc("Sound to play when condition is granted.")]
		public readonly string ActiveSound = null;

		[Desc("Valid relationships of the attacker for triggering the condition.")]
		public readonly PlayerRelationship ValidTargetRelationships = PlayerRelationship.Ally | PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		public override object Create(ActorInitializer init) { return new GrantConditionOnOrders(init.Self, this); }
	}

	public class GrantConditionOnOrders : PausableConditionalTrait<GrantConditionOnOrdersInfo>, IResolveOrder, INotifyBecomingIdle
	{
		int conditionToken = Actor.InvalidConditionToken;

		public GrantConditionOnOrders(Actor self, GrantConditionOnOrdersInfo info)
			: base(info) { }

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (!Info.OrderNames.Contains(order.OrderString) && !order.Queued)
				RevokeCondition(self);

			if (Info.RequiresActorTarget && order.Target.Type != TargetType.Actor && order.Target.Type != TargetType.FrozenActor)
				return;

			if (Info.OrderNames.Contains(order.OrderString))
				GrantCondition(self);

			Actor targetActor = null;
			if (order.Target.Type == TargetType.Actor)
				targetActor = order.Target.Actor;
			else if (order.Target.Type == TargetType.FrozenActor)
				targetActor = order.Target.FrozenActor.Actor;

			if (targetActor != null && !Info.ValidTargetRelationships.HasRelationship(targetActor.Owner.RelationshipWith(self.Owner)))
				return;
		}

		void INotifyBecomingIdle.OnBecomingIdle(Actor self)
		{
			RevokeCondition(self);
		}

		void GrantCondition(Actor self)
		{
			if (conditionToken == Actor.InvalidConditionToken)
			{
				conditionToken = self.GrantCondition(Info.Condition);

				if (!string.IsNullOrEmpty(Info.ActiveSound))
					Game.Sound.Play(SoundType.World, Info.ActiveSound, self.CenterPosition);
			}
		}

		void RevokeCondition(Actor self)
		{
			if (conditionToken == Actor.InvalidConditionToken)
				return;

			conditionToken = self.RevokeCondition(conditionToken);
		}

		protected override void TraitDisabled(Actor self)
		{
			RevokeCondition(self);
		}

		protected override void TraitPaused(Actor self)
		{
			RevokeCondition(self);
		}
	}
}
