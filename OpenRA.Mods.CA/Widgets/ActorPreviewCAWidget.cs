#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets
{
	/// <summary>
	/// Extended ActorPreviewWidget that supports:
	/// - Arbitrary player colors (not tied to map players)
	/// - Preview rendering of overlay traits like WithColoredOverlay, WithPalettedOverlay
	/// - Automatic palette swapping for player-colored sprites to use encyclopedia palette
	/// Uses Render() instead of RenderUI() to get SpriteRenderables which already support IModifyableRenderable
	/// </summary>
	public class ActorPreviewCAWidget : Widget
	{
		public bool Animate = false;
		public Func<float> GetScale = () => 1f;

		readonly ModData modData;
		readonly WorldRenderer worldRenderer;
		readonly WorldViewportSizes viewportSizes;

		IActorPreview[] preview = Array.Empty<IActorPreview>();
		List<IActorPreviewRenderModifier> previewModifiers = new();

		public int2 PreviewOffset { get; private set; }
		public int2 IdealPreviewSize { get; private set; }

		// The color to use for player-colored elements
		Color previewColor = Color.White;

		[ObjectCreator.UseCtor]
		public ActorPreviewCAWidget(ModData modData, WorldRenderer worldRenderer)
		{
			this.modData = modData;
			viewportSizes = modData.Manifest.Get<WorldViewportSizes>();
			this.worldRenderer = worldRenderer;
		}

		protected ActorPreviewCAWidget(ActorPreviewCAWidget other)
			: base(other)
		{
			modData = other.modData;
			preview = other.preview;
			previewModifiers = other.previewModifiers;
			worldRenderer = other.worldRenderer;
			viewportSizes = other.viewportSizes;
			previewColor = other.previewColor;
		}

		public override Widget Clone() { return new ActorPreviewCAWidget(this); }

		/// <summary>
		/// Sets an arbitrary color to use for player-colored elements in the preview.
		/// </summary>
		public void SetPreviewColor(Color color)
		{
			previewColor = color;
		}

		/// <summary>
		/// Sets the preview with a custom color for player-colored elements.
		/// </summary>
		public void SetPreview(ActorInfo actor, TypeDictionary td, Color? color = null)
		{
			if (color.HasValue)
				previewColor = color.Value;

			var init = new ActorPreviewInitializer(actor, worldRenderer, td);

			preview = actor.TraitInfos<IRenderActorPreviewInfo>()
				.SelectMany(rpi => rpi.RenderPreview(init))
				.ToArray();

			// Collect preview-aware render modifiers
			previewModifiers.Clear();
			var modifierInfos = actor.TraitInfos<IActorPreviewRenderModifierInfo>().ToList();
			foreach (var modifierInfo in modifierInfos)
			{
				var modifier = modifierInfo.GetPreviewRenderModifier(worldRenderer, actor, td, previewColor);
				if (modifier != null)
					previewModifiers.Add(modifier);
			}

			// Calculate the preview bounds
			var r = preview.SelectMany(p => p.ScreenBounds(worldRenderer, WPos.Zero));
			var b = r.Union();
			IdealPreviewSize = new int2((int)(b.Width * viewportSizes.DefaultScale), (int)(b.Height * viewportSizes.DefaultScale));
			PreviewOffset = -new int2((int)(b.Left * viewportSizes.DefaultScale), (int)(b.Top * viewportSizes.DefaultScale)) - IdealPreviewSize / 2;
		}

		IFinalizedRenderable[] renderables;
		readonly Dictionary<string, PaletteReference> paletteCache = new();

		public override void PrepareRenderables()
		{
			var origin = RenderOrigin + PreviewOffset + new int2(RenderBounds.Size.Width / 2, RenderBounds.Size.Height / 2);

			// Calculate where WPos.Zero would render on screen in viewport coordinates
			var zeroScreenPos = worldRenderer.ScreenPxPosition(WPos.Zero);
			var viewportZeroPos = worldRenderer.Viewport.WorldToViewPx(zeroScreenPos);

			// Calculate offset from that position to our desired screen position
			// This offset in screen pixels needs to be converted to world units for OffsetBy
			var screenOffset = origin - viewportZeroPos;

			// Convert screen pixel offset to world vector
			// The tile size and scale determine the conversion factor
			var worldOffsetX = screenOffset.X * worldRenderer.TileScale / worldRenderer.TileSize.Width;
			var worldOffsetY = screenOffset.Y * worldRenderer.TileScale / worldRenderer.TileSize.Height;
			var worldOffset = new WVec(worldOffsetX, worldOffsetY, 0);

			// Use Render() at WPos.Zero to get SpriteRenderables which support IModifyableRenderable
			// This allows WithColoredOverlayCA and alpha modifications to work automatically
			var baseRenderables = preview
				.SelectMany(p => p.Render(worldRenderer, WPos.Zero))
				.OrderBy(WorldRenderer.RenderableZPositionComparisonKey)
				.Select(r =>
				{
					// Offset each renderable to the correct screen position
					r = r.OffsetBy(worldOffset);

					// Apply encyclopedia scale to SpriteRenderables
					if (r is SpriteRenderable sr && GetScale() != 1f)
					{
						var scaleFactor = GetScale();
						return new SpriteRenderable(
							sr.GetType().GetField("sprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sr) as Sprite,
							sr.Pos,
							sr.Offset,
							sr.ZOffset,
							sr.Palette,
							(float)sr.GetType().GetField("scale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sr) * scaleFactor,
							sr.Alpha,
							sr.Tint,
							sr.TintModifiers,
							sr.IsDecoration);
					}
					return r;
				})
				.ToList();

			// Calculate scaled bounds for modifiers
			var scaledBounds = new Rectangle(
				origin.X - (int)(IdealPreviewSize.X * GetScale() / 2),
				origin.Y - (int)(IdealPreviewSize.Y * GetScale() / 2),
				(int)(IdealPreviewSize.X * GetScale()),
				(int)(IdealPreviewSize.Y * GetScale()));

			// Apply preview modifiers
			foreach (var modifier in previewModifiers)
				baseRenderables = modifier.ModifyPreviewRender(worldRenderer, baseRenderables, scaledBounds).ToList();

			// Swap player palettes to encyclopedia palettes for proper coloring
			renderables = baseRenderables
				.Select(r => SwapPlayerPalette(r))
				.Select(r => r.PrepareRender(worldRenderer))
				.ToArray();
		}

		/// <summary>
		/// Swaps player-colored palettes to encyclopedia palettes.
		/// e.g., "playerGreece" -> "encyclopedia", "playertdNod" -> "encyclopediatd", "playerscrinScrin" -> "encyclopediascrin"
		/// </summary>
		IRenderable SwapPlayerPalette(IRenderable r)
		{
			if (r is IPalettedRenderable pr && pr.Palette != null)
			{
				var paletteName = pr.Palette.Name;
				if (paletteName != null && paletteName.StartsWith("player", StringComparison.Ordinal))
				{
					// Palette names are like "playerGreece", "playertdNod", "playerscrinScrin"
					// We need to extract just the base palette type (player, playertd, playerscrin)
					// and replace with encyclopedia equivalent
					string newPaletteName;
					if (paletteName.StartsWith("playerscrin", StringComparison.Ordinal))
						newPaletteName = "encyclopediascrin";
					else if (paletteName.StartsWith("playertd", StringComparison.Ordinal))
						newPaletteName = "encyclopediatd";
					else
						newPaletteName = "encyclopedia";

					if (!paletteCache.TryGetValue(newPaletteName, out var newPalette))
					{
						newPalette = worldRenderer.Palette(newPaletteName);
						paletteCache[newPaletteName] = newPalette;
					}

					var ret = (IRenderable)pr.WithPalette(newPalette);

					if (r is IModifyableRenderable mr && ret is IModifyableRenderable retMr)
						ret = retMr.WithAlpha(mr.Alpha).WithTint(mr.Tint, mr.TintModifiers);

					return ret;
				}
			}

			return r;
		}

		public override void Draw()
		{
			Game.Renderer.EnableAntialiasingFilter();
			foreach (var r in renderables)
				r.Render(worldRenderer);
			Game.Renderer.DisableAntialiasingFilter();
		}

		public override void Tick()
		{
			if (Animate && preview.Length > 0)
			{
				foreach (var p in preview)
					p.Tick();

				foreach (var m in previewModifiers)
					m.Tick();
			}
		}
	}
}
