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

namespace OpenRA.Mods.CA.Graphics
{
	public sealed class SelectionBoxAnnotationRenderableCA : IRenderable, IFinalizedRenderable
	{
		readonly Rectangle decorationBounds;
		readonly Color color;
		readonly float thickness;

		public SelectionBoxAnnotationRenderableCA(Actor actor, Rectangle decorationBounds, Color color, float thickness)
			: this(actor.CenterPosition, decorationBounds, color, thickness) { }

		public SelectionBoxAnnotationRenderableCA(WPos pos, Rectangle decorationBounds, Color color, float thickness)
		{
			Pos = pos;
			this.decorationBounds = decorationBounds;
			this.color = color;
			this.thickness = thickness;
		}

		public WPos Pos { get; }
		public PaletteReference Palette => null;
		public int ZOffset => 0;
		public bool IsDecoration => true;

		public IRenderable WithPalette(PaletteReference newPalette) { return this; }
		public IRenderable WithZOffset(int newOffset) { return this; }
		public IRenderable OffsetBy(in WVec vec)
		{
			return new SelectionBoxAnnotationRenderableCA(Pos + vec, decorationBounds, color, thickness);
		}

		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			var tl = wr.Viewport.WorldToViewPx(new float2(decorationBounds.Left, decorationBounds.Top)).ToFloat2();
			var br = wr.Viewport.WorldToViewPx(new float2(decorationBounds.Right, decorationBounds.Bottom)).ToFloat2();
			var tr = new float2(br.X, tl.Y);
			var bl = new float2(tl.X, br.Y);
			var u = new float2(1 + thickness * 3, 0);
			var v = new float2(0, 1 + thickness * 3);

			var cr = Game.Renderer.RgbaColorRenderer;
			cr.DrawLine(new float3[] { tl + u, tl, tl + v }, thickness, color, true);
			cr.DrawLine(new float3[] { tr - u, tr, tr + v }, thickness, color, true);
			cr.DrawLine(new float3[] { br - u, br, br - v }, thickness, color, true);
			cr.DrawLine(new float3[] { bl + u, bl, bl - v }, thickness, color, true);
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}