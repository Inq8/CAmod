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

		public override object Create(ActorInitializer init) { return new GrantConditionOnOrders(init.Self, this); }
	}

	public class GrantConditionOnOrders : PausableConditionalTrait<GrantConditionOnOrdersInfo>, IResolveOrder
	{
		int conditionToken = Actor.InvalidConditionToken;

		public GrantConditionOnOrders(Actor self, GrantConditionOnOrdersInfo info)
			: base(info) { }

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (Info.OrderNames.Contains(order.OrderString))
				GrantCondition(self);
			else
				RevokeCondition(self);
		}

		void GrantCondition(Actor self)
		{
			if (conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(Info.Condition);
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
