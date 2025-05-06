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

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Modifies the interval between seeding resources.")]
	public class SeedsResourceMultiplierInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Percentage modifier to apply.")]
		public readonly int Modifier = 100;

		public override object Create(ActorInitializer init) { return new SeedsResourceMultiplier(this); }
	}

	public class SeedsResourceMultiplier : ConditionalTrait<SeedsResourceMultiplierInfo>, ISeedsResourceIntervalModifier
	{
		public SeedsResourceMultiplier(SeedsResourceMultiplierInfo info)
			: base(info) { }

		int ISeedsResourceIntervalModifier.GetModifier() => IsTraitDisabled ? 100 : Info.Modifier;
	}
}
