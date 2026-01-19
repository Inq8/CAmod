#region Copyright & License Information
/*
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
	public sealed class UIModifyableSpriteRenderable : IPalettedRenderable, IModifyableRenderable, IFinalizedRenderable
	{
		readonly Sprite sprite;
		readonly int2 screenPos;
		readonly float scale;
		readonly float rotation;
		readonly Size tileSize;
		readonly int tileScale;

		public UIModifyableSpriteRenderable(
			Sprite sprite,
			WPos effectiveWorldPos,
			int2 screenPos,
			int zOffset,
			PaletteReference palette,
			float scale,
			float alpha,
			in float3 tint,
			TintModifiers tintModifiers,
			bool isDecoration,
			float rotation,
			Size tileSize,
			int tileScale)
		{
			this.sprite = sprite;
			Pos = effectiveWorldPos;
			this.screenPos = screenPos;
			ZOffset = zOffset;
			Palette = palette;
			this.scale = scale;
			Alpha = alpha;
			Tint = tint;
			TintModifiers = tintModifiers;
			IsDecoration = isDecoration;
			this.rotation = rotation;
			this.tileSize = tileSize;
			this.tileScale = tileScale;

			// PERF: Remove useless palette assignments for RGBA sprites
			// HACK: This is working around the fact that palettes are defined on traits rather than sequences
			// and can be removed once this has been fixed
			if (sprite.Channel == TextureChannel.RGBA && !(palette?.HasColorShift ?? false))
				Palette = null;
		}

		public WPos Pos { get; }
		public WVec Offset => WVec.Zero;
		public bool IsDecoration { get; }

		public PaletteReference Palette { get; }
		public int ZOffset { get; }

		public float Alpha { get; }
		public float3 Tint { get; }
		public TintModifiers TintModifiers { get; }

		public IPalettedRenderable WithPalette(PaletteReference newPalette)
		{
			return new UIModifyableSpriteRenderable(
				sprite, Pos, screenPos, ZOffset, newPalette, scale,
				Alpha, Tint, TintModifiers, IsDecoration, rotation,
				tileSize, tileScale);
		}

		public IRenderable WithZOffset(int newOffset)
		{
			return new UIModifyableSpriteRenderable(
				sprite, Pos, screenPos, newOffset, Palette, scale,
				Alpha, Tint, TintModifiers, IsDecoration, rotation,
				tileSize, tileScale);
		}

		public IRenderable OffsetBy(in WVec vec)
		{
			// Convert world offset to screen pixel offset.
			// This mirrors WorldRenderer.ScreenPxOffset but avoids needing a WorldRenderer reference here.
			var dx = (int)System.Math.Round((float)tileSize.Width * vec.X / tileScale);
			var dy = (int)System.Math.Round((float)tileSize.Height * (vec.Y - vec.Z) / tileScale);
			return new UIModifyableSpriteRenderable(
				sprite, Pos + vec, screenPos + new int2(dx, dy), ZOffset, Palette, scale,
				Alpha, Tint, TintModifiers, IsDecoration, rotation,
				tileSize, tileScale);
		}

		public IRenderable AsDecoration()
		{
			return new UIModifyableSpriteRenderable(
				sprite, Pos, screenPos, ZOffset, Palette, scale,
				Alpha, Tint, TintModifiers, true, rotation,
				tileSize, tileScale);
		}

		public IModifyableRenderable WithAlpha(float newAlpha)
		{
			return new UIModifyableSpriteRenderable(
				sprite, Pos, screenPos, ZOffset, Palette, scale,
				newAlpha, Tint, TintModifiers, IsDecoration, rotation,
				tileSize, tileScale);
		}

		public IModifyableRenderable WithTint(in float3 newTint, TintModifiers newTintModifiers)
		{
			return new UIModifyableSpriteRenderable(
				sprite, Pos, screenPos, ZOffset, Palette, scale,
				Alpha, newTint, newTintModifiers, IsDecoration, rotation,
				tileSize, tileScale);
		}

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }

		public void Render(WorldRenderer wr)
		{
			var t = Alpha * Tint;
			var a = Alpha;
			if ((TintModifiers & TintModifiers.ReplaceColor) != 0)
				a *= -1;

			Game.Renderer.SpriteRenderer.DrawSprite(sprite, Palette, screenPos, scale, t, a, rotation);
		}

		public void RenderDebugGeometry(WorldRenderer wr)
		{
			var offset = screenPos + sprite.Offset.XY;
			if (rotation == 0f)
				Game.Renderer.RgbaColorRenderer.DrawRect(offset, offset + sprite.Size.XY, 1, Color.Red);
			else
				Game.Renderer.RgbaColorRenderer.DrawPolygon(Util.RotateQuad(offset, sprite.Size, rotation), 1, Color.Red);
		}

		public Rectangle ScreenBounds(WorldRenderer wr)
		{
			var offset = screenPos + sprite.Offset;
			return Util.BoundingRectangle(offset, sprite.Size, rotation);
		}
	}
}
