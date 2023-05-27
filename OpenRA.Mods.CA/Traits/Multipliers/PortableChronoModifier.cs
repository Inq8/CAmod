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
	[Desc("Modifies the range and/or cooldown of PortableChronoCA.")]
	public class PortableChronoModifierInfo : ConditionalTraitInfo, Requires<PortableChronoCAInfo>
	{
		[Desc("Percentage modifier to apply.")]
		public readonly int RangeModifier = 100;

		[Desc("Percentage modifier to apply.")]
		public readonly int CooldownModifier = 100;

		[Desc("Number of extra charges to grant.")]
		public readonly int ExtraCharges = 0;

		public override object Create(ActorInitializer init) { return new PortableChronoModifier(this); }
	}

	public class PortableChronoModifier : ConditionalTrait<PortableChronoModifierInfo>, IPortableChronoModifier
	{
		public PortableChronoModifier(PortableChronoModifierInfo info)
			: base(info) { }

		int IPortableChronoModifier.GetCooldownModifier() => IsTraitDisabled ? 100 : Info.CooldownModifier;
		int IPortableChronoModifier.GetRangeModifier() => IsTraitDisabled ? 100 : Info.RangeModifier;
		int IPortableChronoModifier.GetExtraCharges() => IsTraitDisabled ? 0 : Info.ExtraCharges;

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
