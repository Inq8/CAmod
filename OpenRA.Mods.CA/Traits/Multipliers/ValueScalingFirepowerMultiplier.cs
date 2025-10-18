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
	[Desc("Modifies the firepower of a given actor according to its value.")]
	public class ValueScalingFirepowerMultiplierInfo : ConditionalTraitInfo
	{
		[Desc("Minimum value in the range.")]
		public readonly int MinValue = 1000;

		[Desc("Maximum value in the range.")]
		public readonly int MaxValue = 2000;

		[Desc("Firepower multiplier applied at the minimum value.")]
		public readonly int MinValueModifier = 100;

		[Desc("Firepower multiplier applied at the maximum value.")]
		public readonly int MaxValueModifier = 200;

		public override object Create(ActorInitializer init) { return new ValueScalingFirepowerMultiplier(init.Self, this); }
	}

	public class ValueScalingFirepowerMultiplier : ConditionalTrait<ValueScalingFirepowerMultiplierInfo>, IFirepowerModifier
	{
		readonly int modifier;

		public ValueScalingFirepowerMultiplier(Actor self, ValueScalingFirepowerMultiplierInfo info)
			: base(info)
		{
			modifier = 100;
			var valuedInfo = self.Info.TraitInfoOrDefault<ValuedInfo>();
			if (valuedInfo == null)
				return;

			var value = valuedInfo.Cost;

			if (info.MinValue == info.MaxValue)
			{
				modifier = value == info.MinValue ? info.MinValueModifier : 100;
			}
			else
			{
				var valueRange = info.MaxValue - info.MinValue;
				var modifierRange = info.MaxValueModifier - info.MinValueModifier;
				var valueOffset = value - info.MinValue;

				if (value <= info.MinValue)
					modifier = info.MinValueModifier;
				else if (value >= info.MaxValue)
					modifier = info.MaxValueModifier;
				else
					modifier = info.MinValueModifier + modifierRange * valueOffset / valueRange;
			}
		}

		int IFirepowerModifier.GetFirepowerModifier()
		{
			return IsTraitDisabled ? 100 : modifier;
		}
	}
}
