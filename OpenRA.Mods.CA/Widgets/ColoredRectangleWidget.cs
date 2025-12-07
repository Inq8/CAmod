#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets
{
	public class ColoredRectangleWidget : Widget
	{
		public Color Color = Color.White;

		public ColoredRectangleWidget() { }

		protected ColoredRectangleWidget(ColoredRectangleWidget other)
			: base(other)
		{
			Color = other.Color;
		}

		public override void Draw()
		{
			var rect = RenderBounds;
			var tl = new float3(rect.Left, rect.Top, 0);
			var br = new float3(rect.Right, rect.Bottom, 0);
			Game.Renderer.RgbaColorRenderer.FillRect(tl, br, Color);
		}

		public override Widget Clone() { return new ColoredRectangleWidget(this); }
	}
}
