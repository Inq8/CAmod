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
	// Extends the base SupportPowerInstance to allow modifying the remaining ticks.
	public class SupportPowerInstanceCA : SupportPowerInstance
	{
		public SupportPowerInstanceCA(string key, SupportPowerInfo info, SupportPowerManager manager)
			: base(key, info, manager)
		{

		}

		public void SetRemainingTicks(int ticks)
		{
			remainingSubTicks = (ticks * 100).Clamp(0, TotalTicks * 100);
		}

		public void AddToRemainingTicks(int ticks)
		{
			remainingSubTicks = (remainingSubTicks + ticks * 100).Clamp(0, TotalTicks * 100);
		}

		public void SubtractFromRemainingTicks(int ticks)
		{
			remainingSubTicks = (remainingSubTicks - ticks * 100).Clamp(0, TotalTicks * 100);
		}
	}
}
