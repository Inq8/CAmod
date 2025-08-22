#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Enhanced version of WarheadDebugOverlay that supports capsule-shaped warheads. Attach this to the world actor.")]
	public class WarheadDebugOverlayCAInfo : TraitInfo
	{
		public readonly int DisplayDuration = 25;

		public override object Create(ActorInitializer init) { return new WarheadDebugOverlayCA(this); }
	}

	public class WarheadDebugOverlayCA : IRenderAnnotations
	{
		sealed class WHImpact
		{
			public readonly WPos CenterPosition;
			public readonly WDist[] Range;
			public readonly Color Color;
			public int Time;

			public WDist OuterRange => Range[^1];

			public WHImpact(WPos pos, WDist[] range, int time, Color color)
			{
				CenterPosition = pos;
				Range = range;
				Color = color;
				Time = time;
			}
		}

		sealed class WHCapsuleImpact
		{
			public readonly WPos CapsuleStart;
			public readonly WPos CapsuleEnd;
			public readonly WDist[] Range;
			public readonly Color Color;
			public int Time;

			public WDist OuterRange => Range[^1];

			public WHCapsuleImpact(WPos capsuleStart, WPos capsuleEnd, WDist[] range, int time, Color color)
			{
				CapsuleStart = capsuleStart;
				CapsuleEnd = capsuleEnd;
				Range = range;
				Color = color;
				Time = time;
			}
		}

		readonly WarheadDebugOverlayCAInfo info;
		readonly List<WHImpact> impacts = new();
		readonly List<WHCapsuleImpact> capsuleImpacts = new();
		readonly List<WHRectangleImpact> rectangleImpacts = new();

		public WarheadDebugOverlayCA(WarheadDebugOverlayCAInfo info)
		{
			this.info = info;
		}

		public void AddImpact(WPos pos, WDist[] range, Color color)
		{
			impacts.Add(new WHImpact(pos, range, info.DisplayDuration, color));
		}

		public void AddCapsuleImpact(WPos capsuleStart, WPos capsuleEnd, WDist[] range, Color color)
		{
			capsuleImpacts.Add(new WHCapsuleImpact(capsuleStart, capsuleEnd, range, info.DisplayDuration, color));
		}

		sealed class WHRectangleImpact
		{
			public readonly WPos Start; // One end of the rectangle center line
			public readonly WPos End;   // The other end of the rectangle center line
			public readonly WDist[] Range; // Falloff distances from the center line
			public readonly WDist HalfWidth; // Physical half-width cap
			public readonly Color Color;
			public int Time;

			public WHRectangleImpact(WPos start, WPos end, WDist[] range, WDist halfWidth, int time, Color color)
			{
				Start = start;
				End = end;
				Range = range;
				HalfWidth = halfWidth;
				Color = color;
				Time = time;
			}
		}

		public void AddRectangleImpact(WPos start, WPos end, WDist[] range, WDist halfWidth, Color color)
		{
			rectangleImpacts.Add(new WHRectangleImpact(start, end, range, halfWidth, info.DisplayDuration, color));
		}
		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			// Render standard circular impacts
			foreach (var i in impacts)
			{
				var alpha = 255.0f * i.Time / info.DisplayDuration;
				var rangeStep = alpha / i.Range.Length;

				yield return new CircleAnnotationRenderable(i.CenterPosition, i.OuterRange, 1, Color.FromArgb((int)alpha, i.Color));

				foreach (var r in i.Range)
				{
					yield return new CircleAnnotationRenderable(i.CenterPosition, r, 1, Color.FromArgb((int)alpha, i.Color), true);
					alpha -= rangeStep;
				}

				if (!wr.World.Paused)
					i.Time--;
			}

			impacts.RemoveAll(i => i.Time == 0);

			// Render capsule impacts
			foreach (var c in capsuleImpacts)
			{
				var alpha = 255.0f * c.Time / info.DisplayDuration;
				var rangeStep = alpha / c.Range.Length;

				// Render the outer capsule boundary
				foreach (var renderable in CapsuleAnnotationRenderable.Create(c.CapsuleStart, c.CapsuleEnd, c.OuterRange, 1, Color.FromArgb((int)alpha, c.Color)))
					yield return renderable;

				// Render each falloff range as a capsule
				foreach (var r in c.Range)
				{
					foreach (var renderable in CapsuleAnnotationRenderable.Create(c.CapsuleStart, c.CapsuleEnd, r, 1, Color.FromArgb((int)alpha, c.Color)))
						yield return renderable;
					alpha -= rangeStep;
				}

				if (!wr.World.Paused)
					c.Time--;
			}

			capsuleImpacts.RemoveAll(c => c.Time == 0);

			// Render rectangle impacts (center-line aligned)
			foreach (var r in rectangleImpacts)
			{
				var alpha = 255.0f * r.Time / info.DisplayDuration;
				var rangeStep = alpha / r.Range.Length;

				// Outer physical boundary using halfWidth
				foreach (var poly in RectanglePolygons(r.Start, r.End, r.HalfWidth, Color.FromArgb((int)alpha, r.Color)))
					yield return poly;

				// Falloff rectangles with widths capped at halfWidth
				foreach (var band in r.Range)
				{
					var hw = band.Length < r.HalfWidth.Length ? band : r.HalfWidth;
					foreach (var poly in RectanglePolygons(r.Start, r.End, hw, Color.FromArgb((int)alpha, r.Color)))
						yield return poly;
					alpha -= rangeStep;
				}

				if (!wr.World.Paused)
					r.Time--;
			}

			rectangleImpacts.RemoveAll(r => r.Time == 0);
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;

		static IEnumerable<IRenderable> RectanglePolygons(WPos start, WPos end, WDist halfWidth, Color color)
		{
			// Build rectangle corners around the center line from start to end
			var ab = end - start;
			var perp = new WVec(-ab.Y, ab.X, 0);
			if (perp.Length == 0)
				yield break;

			var rvec = perp * halfWidth.Length / perp.Length;
			var a = start + rvec;
			var b = end + rvec;
			var c = end - rvec;
			var d = start - rvec;

			var center = new WPos((start.X + end.X) / 2, (start.Y + end.Y) / 2, (start.Z + end.Z) / 2);
			yield return new PolygonAnnotationRenderable(new[] { a, b, c, d }, center, 1, color);
		}
	}
}
