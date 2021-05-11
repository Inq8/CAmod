#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Primitives;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Attach to support unit so that when ordered as part of a group with combat units it will guard those units.")]
	class GuardsSelectionInfo : ConditionalTraitInfo
	{
		[Desc("Will only guard units with these target types.")]
		public readonly BitSet<TargetableType> ValidTargets = new BitSet<TargetableType>("Ground", "Water");

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

			if (order.OrderString != "Attack" && order.OrderString != "ForceAttack" && order.OrderString != "Move")
				return;

			if (self.Owner.IsBot)
				return;

			var at = self.TraitOrDefault<AutoTarget>();
			if (at != null && at.Stance == UnitStance.AttackAnything)
				return;

			var world = self.World;

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

			foreach (var guardActor in guardActors)
				world.IssueOrder(new Order("Guard", self, Target.FromActor(guardActor), true, null, null));
		}

		bool IsValidGuardTarget(Actor targetActor)
		{
			if (!Info.ValidTargets.Overlaps(targetActor.GetEnabledTargetTypes()))
				return false;

			var guardsSelection = targetActor.Info.HasTraitInfo<GuardsSelectionInfo>();

			foreach (var t in targetActor.TraitsImplementing<GuardsSelection>())
			{
  				if (!t.IsTraitDisabled)
  					return false;
  			}

			return true;
		}
	}
}
