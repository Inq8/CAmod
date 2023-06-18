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
	[Desc("Will keep distance from enemies that the unit can't attack.")]
	class KeepsDistanceInfo : ConditionalTraitInfo
	{
		[Desc("Cells to keep distance.")]
		public readonly WDist Distance = WDist.FromCells(7);

		[CursorReference]
		[Desc("Cursor to display when targeting an actor to keep distance from.")]
		public readonly string Cursor = "move";

		public override object Create(ActorInitializer init) { return new KeepsDistance(init, this); }
	}

	class KeepsDistance : ConditionalTrait<KeepsDistanceInfo>, IIssueOrder, IResolveOrder
	{
		readonly IMove move;
		readonly IMoveInfo moveInfo;

		public KeepsDistance(ActorInitializer init, KeepsDistanceInfo info)
			: base(info)
		{
			move = init.Self.TraitOrDefault<IMove>();
			moveInfo = init.Self.Info.TraitInfoOrDefault<IMoveInfo>();
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (IsTraitDisabled)
					yield break;

				yield return new KeepDistanceOrderTargeter(Info.Cursor);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order is KeepDistanceOrderTargeter)
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "KeepDistance")
			{
				self.QueueActivity(order.Queued, move.MoveWithinRange(order.Target, Info.Distance, targetLineColor: moveInfo.GetTargetLineColor()));
				self.ShowTargetLines();
			}
		}

		class KeepDistanceOrderTargeter : IOrderTargeter
		{
			readonly string cursor;

			public KeepDistanceOrderTargeter(string cursor)
			{
				this.cursor = cursor;
			}

			public string OrderID => "KeepDistance";
			public int OrderPriority => 5;
			public bool TargetOverridesSelection(Actor self, in Target target, List<Actor> actorsAt, CPos xy, TargetModifiers modifiers) { return true; }

			bool CanTargetActor(Actor self, in Target target, ref TargetModifiers modifiers, ref string cursor)
			{
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				if (modifiers.HasModifier(TargetModifiers.ForceMove))
					return false;

				cursor = this.cursor;
				var targetOwner = target.Type == TargetType.Actor ? target.Actor.Owner : target.FrozenActor.Owner;
				return self.Owner.RelationshipWith(targetOwner) == PlayerRelationship.Enemy;
			}

			public bool CanTarget(Actor self, in Target target, ref TargetModifiers modifiers, ref string cursor)
			{
				switch (target.Type)
				{
					case TargetType.Actor:
					case TargetType.FrozenActor:
						return CanTargetActor(self, target, ref modifiers, ref cursor);
					case TargetType.Terrain:
						return false;
					default:
						return false;
				}
			}

			public bool IsQueued { get; protected set; }
		}
	}
}
