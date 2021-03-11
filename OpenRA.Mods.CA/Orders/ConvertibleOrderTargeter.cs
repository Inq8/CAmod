#region Copyright & License Information
/*
 * Copyright 2016-2021 The CA Developers (see AUTHORS)
 * This file is part of CA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.CA.Traits.UnitConverter;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Orders
{
	class ConvertibleOrderTargeter : UnitOrderTargeter
	{
		public const string Id = "Convertible";

		private string cursor;

		public ConvertibleOrderTargeter(string cursor)
			: base(Id, 6, cursor, false, true)
		{
			this.cursor = cursor;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			cursor = null;

			if (!target.Info.HasTraitInfo<UnitConverterInfo>())
				return false;

			if (target.Trait<UnitConverter>().IsTraitDisabled)
				return false;

			if (self.Owner.PlayerActor.Trait<PlayerResources>().Cash <= 0)
				return false;

			if (self.Owner != target.Owner)
				return false;

			cursor = this.cursor;
			return true;
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			return false;
		}
	}
}
