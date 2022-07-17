#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Renders a decorative animation on units and buildings that only plays once each time the trait is enabled.")]
	public class WithPlayOnceOnEnabledOverlayInfo : ConditionalTraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[Desc("Image used for this decoration. Defaults to the actor's type.")]
		public readonly string Image = null;

		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Sequence name to use")]
		public readonly string Sequence = "idle-overlay";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public readonly bool IsDecoration = false;

		public override object Create(ActorInitializer init) { return new WithPlayOnceOnEnabledOverlay(init, this); }
	}

	public class WithPlayOnceOnEnabledOverlay : ConditionalTrait<WithPlayOnceOnEnabledOverlayInfo>
	{
		readonly Animation overlay;
		readonly WithPlayOnceOnEnabledOverlayInfo info;
		bool visible;

		public WithPlayOnceOnEnabledOverlay(ActorInitializer init, WithPlayOnceOnEnabledOverlayInfo info)
			: base(info)
		{
			this.info = info;
			var rs = init.Self.Trait<RenderSprites>();
			var body = init.Self.Trait<BodyOrientation>();

			var image = info.Image ?? rs.GetImage(init.Self);
			overlay = new Animation(init.Self.World, image)
			{
				IsDecoration = info.IsDecoration
			};

			var anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(init.Self.Orientation))),
				() => !visible,
				p => RenderUtils.ZOffsetFromCenter(init.Self, p, 1));

			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

		protected override void TraitEnabled(Actor self)
		{
			visible = true;
			overlay.PlayThen(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), info.Sequence), () => visible = false);
		}
	}
}
