﻿#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Orders
{
	public class ShootableBallisticMissileMoveOrderTargeter : IOrderTargeter
	{
		public string OrderID { get; protected set; }
		public int OrderPriority { get; protected set; }
		public bool TargetOverridesSelection(Actor self, in Target target, List<Actor> actorsAt, CPos xy, TargetModifiers modifiers)
		{
			return modifiers.HasModifier(TargetModifiers.ForceMove);
		}

		public ShootableBallisticMissileMoveOrderTargeter(BallisticMissileInfo info)
		{
			OrderID = "Move";
			OrderPriority = 4;
		}

		public virtual bool CanTarget(Actor self, in Target target, ref TargetModifiers modifiers, ref string cursor)
		{
			// BMs can always move
			return true;
		}

		public bool IsQueued { get; protected set; }
	}
}
