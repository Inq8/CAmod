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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("Displays a custom UI overlay relative to the actor's mouseover bounds. Also renders on actor previews.")]
	public class WithPreviewDecorationInfo : WithDecorationInfo, IActorPreviewRenderModifierInfo
	{
		public override object Create(ActorInitializer init) { return new WithPreviewDecoration(init.Self, this); }

		public IActorPreviewRenderModifier GetPreviewRenderModifier(WorldRenderer wr, ActorInfo actorInfo, TypeDictionary inits, Color previewColor)
		{
			return new WithPreviewDecorationPreviewModifier(wr, actorInfo, this);
		}
	}

	public class WithPreviewDecoration : WithDecoration
	{
		public WithPreviewDecoration(Actor self, WithPreviewDecorationInfo info)
			: base(self, info) { }
	}

	public class WithPreviewDecorationPreviewModifier : IActorPreviewRenderModifier
	{
		readonly WithPreviewDecorationInfo info;
		readonly Animation anim;
		readonly PaletteReference palette;
		readonly BlinkState[] blinkPattern;

		int tick;

		public WithPreviewDecorationPreviewModifier(WorldRenderer wr, ActorInfo actorInfo, WithPreviewDecorationInfo info)
		{
			this.info = info;

			var image = info.Image ?? actorInfo.Name;
			anim = new Animation(wr.World, image);
			anim.PlayRepeating(info.Sequence);

			palette = wr.Palette(info.Palette);

			// For previews, use BlinkPattern if defined, otherwise use first BlinkPatterns entry if any
			if (info.BlinkPattern != null && info.BlinkPattern.Length > 0)
				blinkPattern = info.BlinkPattern;
			else if (info.BlinkPatterns != null && info.BlinkPatterns.Count > 0)
				blinkPattern = info.BlinkPatterns.Values.First();
			else
				blinkPattern = null;
		}

		public void Tick()
		{
			tick++;
			anim.Tick();
		}

		public IEnumerable<IRenderable> ModifyPreviewRender(WorldRenderer wr, IEnumerable<IRenderable> renderables, Rectangle bounds)
		{
			// Collect all renderables
			var allRenderables = renderables.ToList();

			// Yield all existing renderables first
			foreach (var r in allRenderables)
				yield return r;

			if (allRenderables.Count == 0)
				yield break;

			// Handle blinking - check if we should render this tick
			if (blinkPattern != null && blinkPattern.Length > 0)
			{
				var i = tick / info.BlinkInterval % blinkPattern.Length;
				if (blinkPattern[i] != BlinkState.On)
					yield break;
			}

			// Calculate position based on Position setting
			var pos = GetDecorationPosition(bounds, info.Position);
			pos += info.Margin;

			// Render the decoration sprite centered on the calculated position
			var sprite = anim.Image;
			var screenPos = pos - (sprite.Size.XY / 2).ToInt2();

			// Use a high ZOffset to ensure decoration renders on top of the actor
			yield return new UISpriteRenderable(
				sprite,
				WPos.Zero,
				screenPos,
				int.MaxValue,
				palette);
		}

		static int2 GetDecorationPosition(Rectangle bounds, string position)
		{
			return position switch
			{
				"TopLeft" => new int2(bounds.Left, bounds.Top),
				"TopRight" => new int2(bounds.Right, bounds.Top),
				"BottomLeft" => new int2(bounds.Left, bounds.Bottom),
				"BottomRight" => new int2(bounds.Right, bounds.Bottom),
				"Top" => new int2(bounds.Left + bounds.Width / 2, bounds.Top),
				"Bottom" => new int2(bounds.Left + bounds.Width / 2, bounds.Bottom),
				"Left" => new int2(bounds.Left, bounds.Top + bounds.Height / 2),
				"Right" => new int2(bounds.Right, bounds.Top + bounds.Height / 2),
				"Center" => new int2(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2),
				_ => new int2(bounds.Left, bounds.Top)
			};
		}
	}
}
