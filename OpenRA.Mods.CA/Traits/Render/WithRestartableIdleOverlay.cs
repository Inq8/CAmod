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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("Renders a decorative animation on units and buildings that is restarted (and optionally only played once) each time the trait is enabled.")]
	public class WithRestartableIdleOverlayInfo : ConditionalTraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[Desc("Image used for this decoration. Defaults to the actor's type.")]
		public readonly string Image = null;

		[Desc("If true, sequence (and start sequence if set) will only play once on trait being enabled.")]
		public readonly bool PlayOnce = false;

		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Sequence name to use")]
		public readonly string Sequence = "idle-overlay";

		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Animation to play on enabling when actor is first created before playing the main sequence.")]
		public readonly string StartSequence = null;

		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Animation to play on re-enabling before playing the main sequence.")]
		public readonly string RestartSequence = null;

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public readonly bool IsDecoration = false;

		public override object Create(ActorInitializer init) { return new WithRestartableIdleOverlay(init, this); }
	}

	public class WithRestartableIdleOverlay : ConditionalTrait<WithRestartableIdleOverlayInfo>
	{
		readonly Animation overlay;
		readonly WithRestartableIdleOverlayInfo info;
		bool visible;
		bool firstTime;

		public WithRestartableIdleOverlay(ActorInitializer init, WithRestartableIdleOverlayInfo info)
			: base(info)
		{
			this.info = info;
			var rs = init.Self.Trait<RenderSprites>();
			var body = init.Self.Trait<BodyOrientation>();
			firstTime = true;

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
			var startSequence = firstTime ? info.StartSequence : info.RestartSequence;
			firstTime = false;

			if (startSequence != null)
			{
				if (info.PlayOnce)
				{
					overlay.PlayThen(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), startSequence),
						() => overlay.PlayThen(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), info.Sequence), () => visible = false));
				}
				else
				{
					overlay.PlayThen(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), startSequence),
						() => overlay.PlayRepeating(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), info.Sequence)));
				}
			}
			else
			{
				if (info.PlayOnce)
				{
					overlay.PlayThen(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), info.Sequence), () => visible = false);
				}
				else
				{
					overlay.PlayRepeating(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), info.Sequence));
				}
			}
		}

		protected override void TraitDisabled(Actor self)
		{
			if (!info.PlayOnce)
				visible = false;
		}
	}
}
