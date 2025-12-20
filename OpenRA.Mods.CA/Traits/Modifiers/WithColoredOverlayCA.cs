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
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// Preview-aware version of WithColoredOverlay that can show in actor previews.
	/// Overlays a color tint on the actor's renderables.
	/// </summary>
	[Desc("Display a colored overlay when a timed condition is active. Supports preview rendering.")]
	public class WithColoredOverlayCAInfo : ConditionalTraitInfo, IActorPreviewRenderModifierInfo
	{
		[Desc("Color to overlay.")]
		public readonly Color Color = Color.FromArgb(128, 128, 0, 0);

		[Desc("Whether to show this overlay in actor previews (encyclopedia, tooltips, etc).")]
		public readonly bool ShowInPreview = false;

		public override object Create(ActorInitializer init) { return new WithColoredOverlayCA(this); }

		public IActorPreviewRenderModifier GetPreviewRenderModifier(WorldRenderer wr, ActorInfo actorInfo, TypeDictionary inits, Color previewColor)
		{
			// ShowInPreview overrides the condition check - if true, always show in preview
			if (!ShowInPreview)
				return null;

			return new WithColoredOverlayPreviewModifier(this);
		}
	}

	public class WithColoredOverlayCA : ConditionalTrait<WithColoredOverlayCAInfo>, IRenderModifier
	{
		readonly float3 tint;
		readonly float alpha;

		public WithColoredOverlayCA(WithColoredOverlayCAInfo info)
			: base(info)
		{
			tint = new float3(info.Color.R, info.Color.G, info.Color.B) / 255f;
			alpha = info.Color.A / 255f;
		}

		IEnumerable<IRenderable> IRenderModifier.ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			if (IsTraitDisabled)
				return r;

			return ModifiedRender(r);
		}

		IEnumerable<IRenderable> ModifiedRender(IEnumerable<IRenderable> r)
		{
			foreach (var a in r)
			{
				yield return a;

				if (!a.IsDecoration && a is IModifyableRenderable ma)
					yield return ma.WithTint(tint, ma.TintModifiers | TintModifiers.ReplaceColor).WithAlpha(alpha);
			}
		}

		IEnumerable<Rectangle> IRenderModifier.ModifyScreenBounds(Actor self, WorldRenderer wr, IEnumerable<Rectangle> bounds)
		{
			return bounds;
		}
	}

	/// <summary>
	/// Preview modifier for WithColoredOverlayCA
	/// </summary>
	sealed class WithColoredOverlayPreviewModifier : IActorPreviewRenderModifier
	{
		readonly float3 tint;
		readonly float alpha;

		public WithColoredOverlayPreviewModifier(WithColoredOverlayCAInfo info)
		{
			tint = new float3(info.Color.R, info.Color.G, info.Color.B) / 255f;
			alpha = info.Color.A / 255f;
		}

		public IEnumerable<IRenderable> ModifyPreviewRender(WorldRenderer wr, IEnumerable<IRenderable> renderables, Rectangle bounds)
		{
			var result = new List<IRenderable>();

			foreach (var r in renderables)
			{
				result.Add(r);

				// For preview rendering, we apply tint to all modifyable renderables
				// (unlike in-game where we skip decorations)
				if (r is IModifyableRenderable mr)
					result.Add(mr.WithTint(tint, mr.TintModifiers | TintModifiers.ReplaceColor).WithAlpha(alpha));
			}

			return result;
		}

		public void Tick() { }
	}
}
