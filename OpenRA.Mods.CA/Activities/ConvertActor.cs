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
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	public class ConvertActor : Enter
	{
		public ConvertActor(Actor self, Target target, Color targetLineColor)
			: base(self, target, targetLineColor)
		{
		}

		protected override void OnEnterComplete(Actor self, Actor targetActor)
		{
			targetActor.Trait<UnitConverter>().Enter(self, targetActor);
			self.Dispose();
		}
	}
}
