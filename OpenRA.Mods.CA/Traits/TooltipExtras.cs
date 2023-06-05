#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

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

		[ActorReference]
		[Desc("If set, will use name, price and description of this actor for selection tooltip.")]
		public readonly string FakeActor = null;

		[Desc("If true these tooltip extras are standard (used for the production tooltip).",
			"Otherwise they're only used for selection tooltip for actors where the conditions are met.")]
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
