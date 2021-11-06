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
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Extra tooltip items.")]
	public class TooltipExtrasInfo : TraitInfo
	{
		[Desc("Strengths.")]
		public readonly string Strengths = "";

		[Desc("Weaknesses.")]
		public readonly string Weaknesses = "";

		[Desc("Attributes.")]
		public readonly string Attributes = "";

		public override object Create(ActorInitializer init) { return new TooltipExtras(init, this); }
	}

	public class TooltipExtras
	{
		public readonly TooltipExtrasInfo Info;

		public TooltipExtras(ActorInitializer init, TooltipExtrasInfo info)
		{
			Info = info;
		}
	}
}
