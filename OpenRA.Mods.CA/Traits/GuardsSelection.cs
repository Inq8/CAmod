#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
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

		[Desc("Maximum number of guard orders to chain together.")]
		public readonly int MaxTargets = 12;

		public override object Create(ActorInitializer init) { return new GuardsSelection(init, this); }
	}

	class GuardsSelection : ConditionalTrait<GuardsSelectionInfo>, IResolveOrder
	{
		public GuardsSelection(ActorInitializer init, GuardsSelectionInfo info)
			: base(info) { }

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (IsTraitDisabled)
				return;

			if (order.Target.Type == TargetType.Invalid)
				return;

			if (order.Queued)
				return;

			var validOrders = new HashSet<string> { "AttackMove", "AssaultMove", "Attack", "ForceAttack", "Move" };

			if (!validOrders.Contains(order.OrderString))
				return;

			if (self.Owner.IsBot)
				return;

			var world = self.World;

			if (order.Target.Type == TargetType.Actor && (order.Target.Actor.Disposed || order.Target.Actor.Owner == world.LocalPlayer || !order.Target.Actor.IsInWorld || order.Target.Actor.IsDead))
				return;

			var guardActors = world.Selection.Actors
				.Where(a => a.Owner == world.LocalPlayer && a.IsInWorld && !a.IsDead && a.Info.HasTraitInfo<AttackBaseInfo>()
					&& IsValidGuardTarget(a))
				.ToArray();

			if (!guardActors.Any())
				return;

			var mainGuardActor = guardActors.ClosestTo(order.Target.CenterPosition);
			if (mainGuardActor == null)
				return;

			var mainGuardTarget = Target.FromActor(mainGuardActor);

			world.IssueOrder(new Order("Guard", self, mainGuardTarget, false, null, null));

			var guardTargets = 0;

			foreach (var guardActor in guardActors)
			{
				if (guardActor.Disposed || guardActor.IsDead || !guardActor.IsInWorld)
					continue;

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

			var guardsSelection = targetActor.Info.HasTraitInfo<GuardsSelectionInfo>();
			if (guardsSelection)
				return false;

			return true;
		}
	}
}
