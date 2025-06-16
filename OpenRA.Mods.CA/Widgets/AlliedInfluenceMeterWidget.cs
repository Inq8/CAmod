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
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets
{
	public class AlliedInfluenceMeterWidget : Widget
	{
		public readonly Color BarColor = Color.Lime;
		public readonly Color InactiveBarColor = Color.FromArgb(128, Color.Gray);
		public readonly Color ThresholdColor = Color.Lime;
		public readonly Color InactiveThresholdColor = Color.FromArgb(128, Color.Silver);

		public readonly string TooltipTemplate;
		public readonly string TooltipContainer;
		protected Lazy<TooltipContainerWidget> tooltipContainer;
		private int2[] polygonPoints;

		public int MaxTicks = 0;
		public int CurrentTicks = 0;
		public int[] Thresholds = { };

		public Func<string> GetTooltipText = () => "";

		[ObjectCreator.UseCtor]
		public AlliedInfluenceMeterWidget()
		{
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));

			polygonPoints = new[]
			{
				new int2(0, 0),
				new int2(14, 14),
				new int2(14, 50),
				new int2(29, 65),
				new int2(17, 65),
				new int2(0, 48),
			};
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null)
				return;

			if (GetTooltipText != null)
				tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs() { { "getText", GetTooltipText } });
		}

		public override void MouseExited()
		{
			// Only try to remove the tooltip if we know it has been created
			// This avoids a crash if the widget (and the container it refers to) are being removed
			if (TooltipContainer != null && tooltipContainer.IsValueCreated)
				tooltipContainer.Value.RemoveTooltip();
		}

		public override void Draw()
		{
			var bounds = RenderBounds;

			var scaledPoints = polygonPoints.Select(p => new int2(
				bounds.Left + p.X,
				bounds.Top + p.Y
			)).ToArray();

			var totalStripes = (bounds.Height + 1) / 2;
			var activeStripes = (int)Math.Round((double)totalStripes * CurrentTicks / MaxTicks);

			// Map each threshold tick to a stripe index
			var thresholdStripes = Thresholds.Select(t => (int)Math.Round((double)t * totalStripes / MaxTicks) - 1).ToArray();

			for (var i = 0; i < totalStripes; i++)
			{
				var y = bounds.Bottom - 1 - (i * 2);
				var lineStart = new int2(bounds.Left, y);
				var lineEnd = new int2(bounds.Right, y);

				 // Find intersections with polygon edges
				var intersections = new List<int>();
				for (var j = 0; j < scaledPoints.Length; j++)
				{
					var p1 = scaledPoints[j];
					var p2 = scaledPoints[(j + 1) % scaledPoints.Length];

					if (Intersects(lineStart, lineEnd, p1, p2, out var intersectX))
						intersections.Add(intersectX);
				}

				// Sort intersections from left to right
				intersections.Sort();

				// Draw line segments between pairs of intersections
				for (var j = 0; j < intersections.Count - 1; j += 2)
				{
					var start = new float2(intersections[j], y);
					var end = new float2(intersections[j + 1], y);

					Color color;

					var isThresholdStripe = thresholdStripes.Contains(i);

					if (i < activeStripes)
					{
						// If this is a threshold stripe, check if we've reached the threshold
						if (isThresholdStripe)
						{
							// Find which threshold this stripe represents
							var thresholdIndex = Array.IndexOf(thresholdStripes, i);
							var actualThreshold = Thresholds[thresholdIndex];

							color = CurrentTicks >= actualThreshold ? ThresholdColor : BarColor;
						}
						else
						{
							color = BarColor;
						}
					}
					else
					{
						color = isThresholdStripe ? InactiveThresholdColor : InactiveBarColor;
					}

					Game.Renderer.RgbaColorRenderer.DrawLine(start, end, 1, color);
				}
			}
		}

		private bool Intersects(int2 line1Start, int2 line1End, int2 line2Start, int2 line2End, out int intersectX)
		{
			intersectX = 0;

			var x1 = line1Start.X;
			var y1 = line1Start.Y;
			var x2 = line1End.X;
			var y2 = line1End.Y;
			var x3 = line2Start.X;
			var y3 = line2Start.Y;
			var x4 = line2End.X;
			var y4 = line2End.Y;

			var denominator = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);
			if (denominator == 0)
				return false;

			var ua = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / (float)denominator;
			var ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / (float)denominator;

			if (ua < 0 || ua > 1 || ub < 0 || ub > 1)
				return false;

			intersectX = (int)(x1 + ua * (x2 - x1));
			return true;
		}
	}
}
