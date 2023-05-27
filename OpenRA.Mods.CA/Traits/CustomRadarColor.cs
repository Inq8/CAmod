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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Define a custom radar color used regardless of owner.")]
	public class CustomRadarColorInfo : TraitInfo
	{
		[Desc("Color to use")]
		public readonly Color Color = Color.White;

		public override object Create(ActorInitializer init) { return new CustomRadarColor(init, this); }
	}

	public class CustomRadarColor : IRadarColorModifier
	{
		public CustomRadarColorInfo Info { get; set; }

		public CustomRadarColor(ActorInitializer init, CustomRadarColorInfo info)
		{
			Info = info;
		}

		Color IRadarColorModifier.RadarColorOverride(Actor self, Color color)
		{
			return Info.Color;
		}
	}
}
