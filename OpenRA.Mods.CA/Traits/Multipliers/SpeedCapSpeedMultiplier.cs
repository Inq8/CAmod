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
	[Desc("Modifies the speed of an actor to emulate specified maximum speed.")]
	public class SpeedCapSpeedMultiplierInfo : ConditionalTraitInfo
	{
		[Desc("Maximum speed to emulate.")]
		public readonly int MaxSpeed = 100;

		public override object Create(ActorInitializer init) { return new SpeedCapSpeedMultiplier(init.Self, this); }
	}

	public class SpeedCapSpeedMultiplier : ConditionalTrait<SpeedCapSpeedMultiplierInfo>, ISpeedModifier
	{
		readonly int modifier;

		public SpeedCapSpeedMultiplier(Actor self, SpeedCapSpeedMultiplierInfo info)
			: base(info)
		{
			modifier = 100;

			var aircraftInfo = self.Info.TraitInfoOrDefault<AircraftInfo>();
			if (aircraftInfo != null)
			{
				modifier = CalculateModifier(aircraftInfo.Speed, info.MaxSpeed);
			}

			var mobileInfo = self.Info.TraitInfoOrDefault<MobileInfo>();
			if (mobileInfo != null)
			{
				modifier = CalculateModifier(mobileInfo.Speed, info.MaxSpeed);
			}
		}

		int CalculateModifier(int speed, int maxSpeed)
		{
			if (speed > maxSpeed)
				return (int)(maxSpeed / (float)speed * 100);

			return 100;
		}

		int ISpeedModifier.GetSpeedModifier()
		{
			return IsTraitDisabled ? 100 : modifier;
		}
	}
}
