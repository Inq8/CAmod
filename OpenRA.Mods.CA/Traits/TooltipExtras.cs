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

using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Extra tooltip items.")]
	public class TooltipExtrasInfo : ConditionalTraitInfo
	{
		[Desc("Description.")]
		public readonly string Description = "";

		[Desc("Strengths.")]
		public readonly string Strengths = "";

		[Desc("Weaknesses.")]
		public readonly string Weaknesses = "";

		[Desc("Attributes.")]
		public readonly string Attributes = "";

		[Desc("If true these tooltip extras are standard (used for the production tooltip).",
			"False implies conditional (only used for selection tooltip).")]
		public readonly bool IsStandard = true;

		public override object Create(ActorInitializer init) { return new TooltipExtras(init, this); }
	}

	public class TooltipExtras : ConditionalTrait<TooltipExtrasInfo>
	{
		public new readonly TooltipExtrasInfo Info;

		public TooltipExtras(ActorInitializer init, TooltipExtrasInfo info)
			: base(info)
		{
			Info = info;
		}
	}
}
