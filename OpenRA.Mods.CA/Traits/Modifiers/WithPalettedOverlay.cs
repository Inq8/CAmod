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
	[Desc("Overlays a copy of each renderable of the actor using the specified palette.",
		"Will obscure the underlying renderables if the chosen palette has no transparency.")]
	public class WithPalettedOverlayInfo : ConditionalTraitInfo, IActorPreviewRenderModifierInfo
	{
		[PaletteReference]
		[Desc("Palette to use when rendering the overlay")]
		public readonly string Palette = "invuln";

		[Desc("Player relationships that see the overlay.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally | PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		[Desc("Whether the effect is visible through fog.")]
		public readonly bool VisibleThroughFog = true;

		[Desc("Whether to show this overlay in actor previews (encyclopedia, tooltips, etc).")]
		public readonly bool ShowInPreview = false;

		public override object Create(ActorInitializer init) { return new WithPalettedOverlay(init.Self, this); }

		public IActorPreviewRenderModifier GetPreviewRenderModifier(WorldRenderer wr, ActorInfo actorInfo, TypeDictionary inits, Color previewColor)
		{
			// ShowInPreview overrides the condition check - if true, always show in preview
			if (!ShowInPreview)
				return null;

			return new WithPalettedOverlayPreviewModifier(wr, this);
		}
	}

	public class WithPalettedOverlay : ConditionalTrait<WithPalettedOverlayInfo>, IRenderModifier, INotifyOwnerChanged
	{
		bool validRelationship;

		public WithPalettedOverlay(Actor self, WithPalettedOverlayInfo info)
			: base(info)
		{
			Update(self);
		}

		IEnumerable<IRenderable> IRenderModifier.ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			if (IsTraitDisabled)
				return r;

			return ModifiedRender(self, wr, r);
		}

		IEnumerable<IRenderable> ModifiedRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			if (IsTraitDisabled)
				yield break;

			var palette = string.IsNullOrEmpty(Info.Palette) ? null : wr.Palette(Info.Palette);

			foreach (var a in r)
			{
				yield return a;

				if (validRelationship && palette != null && !a.IsDecoration && a is IPalettedRenderable && (Info.VisibleThroughFog || !self.World.FogObscures(self.CenterPosition)))
					yield return ((IPalettedRenderable)a).WithPalette(palette)
						.WithZOffset(a.ZOffset + 1)
						.AsDecoration();
			}
		}

		IEnumerable<Rectangle> IRenderModifier.ModifyScreenBounds(Actor self, WorldRenderer wr, IEnumerable<Rectangle> bounds)
		{
			return bounds;
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			Update(self);
		}

		void Update(Actor self)
		{
			var relationship = self.World.RenderPlayer != null ? self.Owner.RelationshipWith(self.World.RenderPlayer) : PlayerRelationship.None;
			validRelationship = Info.ValidRelationships.HasRelationship(relationship);
		}
	}

	/// <summary>
	/// Preview modifier for WithPalettedOverlay
	/// </summary>
	sealed class WithPalettedOverlayPreviewModifier : IActorPreviewRenderModifier
	{
		readonly PaletteReference palette;

		public WithPalettedOverlayPreviewModifier(WorldRenderer wr, WithPalettedOverlayInfo info)
		{
			palette = string.IsNullOrEmpty(info.Palette) ? null : wr.Palette(info.Palette);
		}

		public IEnumerable<IRenderable> ModifyPreviewRender(WorldRenderer wr, IEnumerable<IRenderable> renderables, Rectangle bounds)
		{
			foreach (var r in renderables)
			{
				yield return r;

				// For preview rendering, apply palette swap to all paletted renderables
				// (unlike in-game where we skip decorations, because the colored overlay used for customising the actor color is a decoration)
				if (palette != null && r is IPalettedRenderable pr)
					yield return pr.WithPalette(palette)
						.WithZOffset(r.ZOffset + 1)
						.AsDecoration();
			}
		}

		public void Tick() { }
	}
}
