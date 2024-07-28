#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ImageWithAlphaWidget : Widget
	{
		public readonly string TooltipTemplate;
		public readonly string TooltipContainer;

		public string ImageCollection = "";
		public string ImageName = "";
		public bool ClickThrough = true;
		public Func<string> GetImageName;
		public Func<string> GetImageCollection;
		public Func<Sprite> GetSprite;
		public Func<float> GetAlpha = () => 1f;

		public string TooltipText;

		readonly Lazy<TooltipContainerWidget> tooltipContainer;
		public Func<string> GetTooltipText;

		readonly CachedTransform<(string, string), Sprite> getImageCache = new(
			((string Collection, string Image) args) => ChromeProvider.GetImage(args.Collection, args.Image));

		public ImageWithAlphaWidget()
		{
			GetImageName = () => ImageName;
			GetImageCollection = () => ImageCollection;
			GetTooltipText = () => TooltipText;
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));

			GetSprite = () => getImageCache.Update((GetImageCollection(), GetImageName()));
		}

		protected ImageWithAlphaWidget(ImageWithAlphaWidget other)
			: base(other)
		{
			ClickThrough = other.ClickThrough;
			ImageName = other.ImageName;
			GetImageName = other.GetImageName;
			ImageCollection = other.ImageCollection;
			GetImageCollection = other.GetImageCollection;

			TooltipTemplate = other.TooltipTemplate;
			TooltipContainer = other.TooltipContainer;
			GetTooltipText = other.GetTooltipText;
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));

			GetSprite = () => getImageCache.Update((GetImageCollection(), GetImageName()));
		}

		public override Widget Clone() { return new ImageWithAlphaWidget(this); }

		public override void Draw()
		{
			//WidgetUtils.DrawSprite(GetSprite(), RenderOrigin);

			//var renderable = new UISpriteRenderable(GetSprite(), WPos.Zero, RenderOrigin, 0, null, 1f, GetAlpha(), 0f);
			//renderable.Render(worldRenderer);

			Game.Renderer.RgbaSpriteRenderer.DrawSprite(GetSprite(), new float3(RenderOrigin.X, RenderOrigin.Y, 0f), 1f, new float3(GetAlpha(), GetAlpha(), GetAlpha()), GetAlpha(), 0f);
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			return !ClickThrough && RenderBounds.Contains(mi.Location);
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null || GetTooltipText == null)
				return;

			tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs() { { "getText", GetTooltipText } });
		}

		public override void MouseExited()
		{
			if (TooltipContainer == null)
				return;

			tooltipContainer.Value.RemoveTooltip();
		}
	}
}
