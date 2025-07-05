#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Orders;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Traits
{
	public class SelectDirectionalTargetWithCircle : SelectDirectionalTarget, IOrderGenerator
	{
		WDist targetCircleRange;
		Color targetCircleColor;
		bool targetCircleUsePlayerColor;

		public SelectDirectionalTargetWithCircle(World world, string order, SupportPowerManager manager, DirectionalSupportPowerInfo info,
			WDist targetCircleRange, Color targetCircleColor, bool targetCircleUsePlayerColor)
			: base(world, order, manager, info)
		{
			this.targetCircleRange = targetCircleRange;
			this.targetCircleColor = targetCircleColor;
			this.targetCircleUsePlayerColor = targetCircleUsePlayerColor;
		}

		IEnumerable<IRenderable> IOrderGenerator.RenderAnnotations(WorldRenderer wr, World world)
		{
			if (targetCircleRange == WDist.Zero)
				yield break;

			var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);

			yield return new RangeCircleAnnotationRenderable(
				world.Map.CenterOfCell(xy),
				targetCircleRange,
				0,
				targetCircleUsePlayerColor ? world.LocalPlayer.Color : targetCircleColor,
				1,
				Color.FromArgb(96, Color.Black),
				3);
		}
	}
}
