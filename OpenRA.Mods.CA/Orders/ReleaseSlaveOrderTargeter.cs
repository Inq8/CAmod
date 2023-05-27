#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Orders
{
	public class ReleaseSlaveOrderTargeter : UnitOrderTargeter
	{
		readonly string releaseCursor;
		readonly Func<Actor, TargetModifiers, bool> canTarget;

		public ReleaseSlaveOrderTargeter(string order, int priority, string releaseCursor,
			Func<Actor, TargetModifiers, bool> canTarget)
			: base(order, priority, releaseCursor, false, true)
		{
			this.releaseCursor = releaseCursor;
			this.canTarget = canTarget;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			if (!self.Owner.IsAlliedWith(target.Owner) || !target.Info.HasTraitInfo<MindControllableInfo>() || !canTarget(target, modifiers))
				return false;

			cursor = releaseCursor;
			return true;
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			// Allied actors are never frozen
			return false;
		}
	}
}
