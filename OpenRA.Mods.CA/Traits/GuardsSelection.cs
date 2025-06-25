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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Attach to support unit so that when ordered as part of a group with combat units it will guard those units.")]
	class GuardsSelectionInfo : ConditionalTraitInfo
	{
		[Desc("Will only guard units with these target types.")]
		public readonly BitSet<TargetableType> ValidTargets = new BitSet<TargetableType>("Ground", "Water");

		[Desc("Will only guard units with these target types.")]
		public readonly HashSet<string> ValidOrders = new HashSet<string>() { "AttackMove", "AssaultMove", "Attack", "ForceAttack", "KeepDistance" };

		[Desc("Maximum number of guard orders to chain together.")]
		public readonly int MaxTargets = 10;

		[Desc("Color to use for the target line.")]
		public readonly Color TargetLineColor = Color.OrangeRed;

		[Desc("Maximum range that guarding actors will maintain.")]
		public readonly WDist Range = WDist.FromCells(2);

		public override object Create(ActorInitializer init) { return new GuardsSelection(init, this); }
	}

	class GuardsSelection : ConditionalTrait<GuardsSelectionInfo>, IResolveOrder, INotifyCreated
	{
		IMove move;

		public GuardsSelection(ActorInitializer init, GuardsSelectionInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			move = self.Trait<IMove>();
			base.Created(self);
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (IsTraitDisabled)
				return;

			if (order.Target.Type == TargetType.Invalid)
				return;

			if (order.Queued)
				return;

			if (!Info.ValidOrders.Contains(order.OrderString))
				return;

			if (self.Owner.IsBot)
				return;

			var world = self.World;

			if (order.Target.Type == TargetType.Actor && (order.Target.Actor.Disposed || order.Target.Actor.Owner == self.Owner || !order.Target.Actor.IsInWorld || order.Target.Actor.IsDead))
				return;

			var guardActors = world.Selection.Actors
				.Where(a => a.Owner == self.Owner
					&& !a.Disposed
					&& !a.IsDead
					&& a.IsInWorld
					&& a != self
					&& IsValidGuardTarget(a))
				.ToArray();

			if (guardActors.Length == 0)
				return;

			var mainGuardActor = guardActors.ClosestTo(order.Target.CenterPosition);
			if (mainGuardActor == null)
				return;

			var mainGuardTarget = Target.FromActor(mainGuardActor);
			world.IssueOrder(new Order("Guard", self, mainGuardTarget, false, null, null));

			var guardTargets = 0;

			foreach (var guardActor in guardActors)
			{
				guardTargets++;
				world.IssueOrder(new Order("Guard", self, Target.FromActor(guardActor), true, null, null));

				if (guardTargets >= Info.MaxTargets)
					break;
			}
		}

		bool IsValidGuardTarget(Actor targetActor)
		{
			if (!Info.ValidTargets.Overlaps(targetActor.GetEnabledTargetTypes()))
				return false;

			if (!targetActor.Info.HasTraitInfo<AttackBaseInfo>() || !targetActor.Info.HasTraitInfo<GuardableInfo>())
				return false;

			var guardsSelection = targetActor.TraitsImplementing<GuardsSelection>();
			if (guardsSelection.Any(t => !t.IsTraitDisabled))
				return false;

			return true;
		}
	}
}
