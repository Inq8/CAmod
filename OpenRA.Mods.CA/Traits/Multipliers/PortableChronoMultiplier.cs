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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Modifies the range and/or cooldown of PortableChronoCA.")]
	public class PortableChronoMultiplierInfo : ConditionalTraitInfo, Requires<PortableChronoCAInfo>
	{
		[Desc("Percentage modifier to apply.")]
		public readonly int RangeModifier = 100;

		[Desc("Percentage modifier to apply.")]
		public readonly int CooldownModifier = 100;

		public override object Create(ActorInitializer init) { return new PortableChronoMultiplier(this); }
	}

	public class PortableChronoMultiplier : ConditionalTrait<PortableChronoMultiplierInfo>, IPortableChronoModifier
	{
		public PortableChronoMultiplier(PortableChronoMultiplierInfo info)
			: base(info) { }

		int IPortableChronoModifier.GetCooldownModifier() => IsTraitDisabled ? 100 : Info.CooldownModifier;
		int IPortableChronoModifier.GetRangeModifier() => IsTraitDisabled ? 100 : Info.RangeModifier;

		protected override void TraitEnabled(Actor self)
		{
			self.Trait<PortableChronoCA>().UpdateModifiers();
		}

		protected override void TraitDisabled(Actor self)
		{
			self.Trait<PortableChronoCA>().UpdateModifiers();
		}
	}
}
