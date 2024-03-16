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
	[Desc("Attach to a transport to override the unload order.")]
	class PassengerBlockedInfo : ConditionalTraitInfo, Requires<PassengerInfo>
	{
		[CursorReference]
		[Desc("Cursor to display when hovering over the transport.")]
		public readonly string Cursor = "enter-blocked";

		public override object Create(ActorInitializer init) { return new PassengerBlocked(init, this); }
	}

	class PassengerBlocked : ConditionalTrait<PassengerBlockedInfo>, IIssueOrder, IResolveOrder
	{
		public PassengerBlocked(ActorInitializer init, PassengerBlockedInfo info)
			: base(info) { }

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (IsTraitDisabled)
					yield break;

				yield return new PassengerBlockedOrderTargeter(Info.Cursor);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order is PassengerBlockedOrderTargeter)
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "PassengerBlocked")
			{
				// do nothing
			}
		}

		class PassengerBlockedOrderTargeter : IOrderTargeter
		{
			readonly string cursor;

			public PassengerBlockedOrderTargeter(string cursor)
			{
				this.cursor = cursor;
			}

			public string OrderID => "PassengerBlocked";
			public int OrderPriority => 100;
			public bool TargetOverridesSelection(Actor self, in Target target, List<Actor> actorsAt, CPos xy, TargetModifiers modifiers) { return true; }

			bool CanTargetActor(Actor self, in Target target, ref TargetModifiers modifiers, ref string cursor)
			{
				if (modifiers.HasModifier(TargetModifiers.ForceAttack))
					return false;

				if (!target.Actor.Owner.IsAlliedWith(self.Owner))
					return false;

				var cargoBlocked = target.Actor.TraitOrDefault<CargoBlocked>();
				if (cargoBlocked == null || cargoBlocked.IsTraitDisabled)
					return false;

				cursor = this.cursor;
				return true;
			}

			public bool CanTarget(Actor self, in Target target, ref TargetModifiers modifiers, ref string cursor)
			{
				switch (target.Type)
				{
					case TargetType.Actor:
						return CanTargetActor(self, target, ref modifiers, ref cursor);
					default:
						return false;
				}
			}

			public bool IsQueued { get; protected set; }
		}
	}
}
