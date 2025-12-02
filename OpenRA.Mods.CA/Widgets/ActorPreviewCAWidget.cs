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
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets
{
	/// <summary>
	/// Extended ActorPreviewWidget that supports:
	/// - Arbitrary player colors (not tied to map players)
	/// - Preview rendering of overlay traits like WithColoredOverlay, WithPalettedOverlay
	/// - Automatic palette swapping for player-colored sprites to use encyclopedia palette
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
			var scale = GetScale() * viewportSizes.DefaultScale;
			var origin = RenderOrigin + PreviewOffset + new int2(RenderBounds.Size.Width / 2, RenderBounds.Size.Height / 2);

			IEnumerable<IRenderable> baseRenderables = preview
				.SelectMany(p => p.RenderUI(worldRenderer, origin, scale));

			// Calculate scaled bounds for modifiers
			// The bounds are centered around origin, scaled appropriately
			var scaledBounds = new Rectangle(
				origin.X - (int)(IdealPreviewSize.X * GetScale() / 2),
				origin.Y - (int)(IdealPreviewSize.Y * GetScale() / 2),
				(int)(IdealPreviewSize.X * GetScale()),
				(int)(IdealPreviewSize.Y * GetScale()));

			// Apply preview modifiers
			foreach (var modifier in previewModifiers)
				baseRenderables = modifier.ModifyPreviewRender(worldRenderer, baseRenderables, scaledBounds);

			// Swap player palettes to encyclopedia palettes for proper coloring
			baseRenderables = baseRenderables.Select(r => SwapPlayerPalette(r));

			renderables = baseRenderables
				.OrderBy(WorldRenderer.RenderableZPositionComparisonKey)
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

					return pr.WithPalette(newPalette);
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
