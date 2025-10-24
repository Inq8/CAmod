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
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets
{
	public class CroppableImageWidget : ImageWidget
	{
		public enum CropDirection
		{
			TopDown,
			BottomUp,
			LeftRight,
			RightLeft
		}

		public CropDirection Direction = CropDirection.BottomUp;
		public float CropPercentage = 0f;
		public Func<float> GetCropPercentage;

		public CroppableImageWidget()
		{
			GetCropPercentage = () => CropPercentage;
		}

		protected CroppableImageWidget(CroppableImageWidget other)
			: base(other)
		{
			Direction = other.Direction;
			CropPercentage = other.CropPercentage;
			GetCropPercentage = other.GetCropPercentage;
		}

		public override Widget Clone()
		{
			return new CroppableImageWidget(this);
		}

		public override void Draw()
		{
			var sprite = GetSprite();
			if (sprite == null)
				return;

			var renderBounds = RenderBounds;
			var cropPercentage = Math.Clamp(GetCropPercentage(), 0f, 1f);

			if (cropPercentage <= 0f)
				return;

			// Calculate the scissor rectangle based on crop direction and percentage
			Rectangle scissorRect;
			switch (Direction)
			{
				case CropDirection.BottomUp:
					var visibleHeight = (int)(renderBounds.Height * cropPercentage);
					scissorRect = new Rectangle(
						renderBounds.Left,
						renderBounds.Bottom - visibleHeight,
						renderBounds.Width,
						visibleHeight);
					break;

				case CropDirection.TopDown:
					var topVisibleHeight = (int)(renderBounds.Height * cropPercentage);
					scissorRect = new Rectangle(
						renderBounds.Left,
						renderBounds.Top,
						renderBounds.Width,
						topVisibleHeight);
					break;

				case CropDirection.LeftRight:
					var leftVisibleWidth = (int)(renderBounds.Width * cropPercentage);
					scissorRect = new Rectangle(
						renderBounds.Left,
						renderBounds.Top,
						leftVisibleWidth,
						renderBounds.Height);
					break;

				case CropDirection.RightLeft:
					var rightVisibleWidth = (int)(renderBounds.Width * cropPercentage);
					scissorRect = new Rectangle(
						renderBounds.Right - rightVisibleWidth,
						renderBounds.Top,
						rightVisibleWidth,
						renderBounds.Height);
					break;

				default:
					scissorRect = renderBounds;
					break;
			}

			// Enable scissor test and draw the sprite
			Game.Renderer.EnableScissor(scissorRect);
			base.Draw();
			Game.Renderer.DisableScissor();
		}
	}
}