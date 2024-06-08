#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Effects;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Draws an overlay on top of a make animation.")]
	public class WithEnterExitWorldOverlayInfo : TraitInfo
	{
		[Desc("Image containing launch effect sequence.")]
		public readonly string Image = null;

		[Desc("Launch effect sequence to play.")]
		[SequenceReference(nameof(Image))]
		public readonly string EnterSequence = null;

		[Desc("Launch effect sequence to play.")]
		[SequenceReference(nameof(Image))]
		public readonly string ExitSequence = null;

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom palette name.")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName.")]
		public readonly bool IsPlayerPalette = false;

		public override object Create(ActorInitializer init) { return new WithEnterExitWorldOverlay(init.Self, this); }
	}

	public class WithEnterExitWorldOverlay : INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly WithEnterExitWorldOverlayInfo info;

		public WithEnterExitWorldOverlay(Actor self, WithEnterExitWorldOverlayInfo info)
		{
			this.info = info;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(self.CenterPosition, w, info.Image, info.EnterSequence, info.Palette, delay: 0)));
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(self.CenterPosition, w, info.Image, info.ExitSequence, info.Palette, delay: 0)));
		}
	}
}
