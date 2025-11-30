#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Enhanced version of WarheadDebugOverlay that supports custom shapes. Attach this to the world actor.")]
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

		readonly WarheadDebugOverlayCAInfo info;
		readonly List<WHImpact> impacts = new();
		readonly List<WHConeImpact> coneImpacts = new();
		readonly List<WHConeSegmentImpact> coneSegmentImpacts = new();
		readonly List<WHPoylineImpact> polylineImpacts = new();

		public WarheadDebugOverlayCA(WarheadDebugOverlayCAInfo info)
		{
			this.info = info;
		}

		public void AddImpact(WPos pos, WDist[] range, Color color)
		{
			impacts.Add(new WHImpact(pos, range, info.DisplayDuration, color));
		}

		sealed class WHPoylineImpact
		{
			public readonly WPos[] Points;
			public readonly int Width;
			public readonly Color Color;
			public int Time;

			public WHPoylineImpact(WPos[] points, int width, int time, Color color)
			{
				Points = points;
				Width = width;
				Time = time;
				Color = color;
			}
		}

		public void AddPolygonOutline(WPos[] points, Color color, int width = 1)
		{
			if (points == null || points.Length < 2)
				return;
			polylineImpacts.Add(new WHPoylineImpact(points, width, info.DisplayDuration, color));
		}

		sealed class WHConeImpact
		{
			public readonly WPos Apex;
			public readonly WVec Axis;
			public readonly WDist[] Range;
			public readonly WDist Length;
			public readonly int ConeAngle;
			public readonly Color Color;
			public int Time;

			public WDist OuterRange => Range[^1];

			public WHConeImpact(WPos apex, WVec axis, WDist[] range, WDist length, int coneAngle, int time, Color color)
			{
				Apex = apex;
				Axis = axis;
				Range = range;
				Length = length;
				ConeAngle = coneAngle;
				Time = time;
				Color = color;
			}
		}

		sealed class WHConeSegmentImpact
		{
			public readonly WPos Apex;
			public readonly WVec Axis;
			public readonly int SegmentStart;
			public readonly int SegmentEnd;
			public readonly int StartRadius;
			public readonly int EndRadius;
			public readonly int ConeAngle;
			public readonly Color Color;
			public int Time;

			public WHConeSegmentImpact(WPos apex, WVec axis, int segmentStart, int segmentEnd, int startRadius, int endRadius, int coneAngle, int time, Color color)
			{
				Apex = apex;
				Axis = axis;
				SegmentStart = segmentStart;
				SegmentEnd = segmentEnd;
				StartRadius = startRadius;
				EndRadius = endRadius;
				ConeAngle = coneAngle;
				Time = time;
				Color = color;
			}
		}

		public void AddConeImpact(WPos apex, WVec axis, WDist[] range, int coneAngle, Color color)
		{
			// Back-compat overload: assume length equals outer range
			coneImpacts.Add(new WHConeImpact(apex, axis, range, range[^1], coneAngle, info.DisplayDuration, color));
		}

		public void AddConeImpact(WPos apex, WVec axis, WDist length, WDist[] range, int coneAngle, Color color)
		{
			coneImpacts.Add(new WHConeImpact(apex, axis, range, length, coneAngle, info.DisplayDuration, color));
		}

		public void AddConeSegmentImpact(WPos apex, WVec axis, int segmentStart, int segmentEnd, int startRadius, int endRadius, int coneAngle, Color color)
		{
			coneSegmentImpacts.Add(new WHConeSegmentImpact(apex, axis, segmentStart, segmentEnd, startRadius, endRadius, coneAngle, info.DisplayDuration, color));
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

			// Render polygon outlines
			foreach (var p in polylineImpacts)
			{
				var alpha = 255.0f * p.Time / info.DisplayDuration;
				var col = Color.FromArgb((int)alpha, p.Color);

				for (var i = 0; i < p.Points.Length; i++)
				{
					var a = p.Points[i];
					var b = p.Points[(i + 1) % p.Points.Length];
					yield return new LineAnnotationRenderable(a, b, p.Width, col);
				}

				if (!wr.World.Paused)
					p.Time--;
			}

			polylineImpacts.RemoveAll(p => p.Time == 0);

			// Render cone impacts
			foreach (var c in coneImpacts)
			{
				var alpha = 255.0f * c.Time / info.DisplayDuration;
				var rangeStep = alpha / c.Range.Length;

				// Draw side rays up to min(length, outer range)
				var maxR = c.OuterRange.Length < c.Length.Length ? c.OuterRange : c.Length;
				foreach (var r in ConeSideLines(c.Apex, c.Axis, c.ConeAngle, maxR, 1, Color.FromArgb((int)alpha, c.Color)))
					yield return r;

				// Draw arc at outer range (clamped by length)
				foreach (var seg in ConeArcSegments(c.Apex, c.Axis, c.ConeAngle, maxR, 1, Color.FromArgb((int)alpha, c.Color)))
					yield return seg;

				// Draw falloff rings as arcs (skip bands beyond length)
				foreach (var r in c.Range)
				{
					if (r.Length > c.Length.Length)
						continue;
					foreach (var seg in ConeArcSegments(c.Apex, c.Axis, c.ConeAngle, r, 1, Color.FromArgb((int)alpha, c.Color)))
						yield return seg;
					alpha -= rangeStep;
				}

				if (!wr.World.Paused)
					c.Time--;
			}

			coneImpacts.RemoveAll(c => c.Time == 0);

			// Render cone segment impacts
			foreach (var cs in coneSegmentImpacts)
			{
				var alpha = 255.0f * cs.Time / info.DisplayDuration;
				var color = Color.FromArgb((int)alpha, cs.Color);

				// Draw cone segment as truncated cone section
				foreach (var r in ConeSegmentLines(cs.Apex, cs.Axis, cs.SegmentStart, cs.SegmentEnd, cs.StartRadius, cs.EndRadius, cs.ConeAngle, 1, color))
					yield return r;

				if (!wr.World.Paused)
					cs.Time--;
			}

			coneSegmentImpacts.RemoveAll(cs => cs.Time == 0);
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;

		static IEnumerable<IRenderable> ConeSideLines(WPos apex, WVec axis, int coneAngleDeg, WDist radius, int width, Color color)
		{
			// Normalize the axis vector
			if (axis.Length == 0)
				yield break;

			var axisNorm = axis * 1024 / axis.Length;
			var halfAngleRad = Math.PI * (coneAngleDeg / 2.0) / 180.0;
			var cos = Math.Cos(halfAngleRad);
			var sin = Math.Sin(halfAngleRad);

			// Create a perpendicular vector in the XY plane
			var perp = new WVec(-axisNorm.Y, axisNorm.X, 0);
			if (perp.Length == 0)
				perp = new WVec(1024, 0, 0); // Fallback if axis is vertical
			else
				perp = perp * 1024 / perp.Length;

			// Calculate the two edge directions by rotating the axis by +/- half angle
			// Using 2D rotation in the plane defined by axis and perp
			var leftVec = new WVec(
				(int)(axisNorm.X * cos - perp.X * sin),
				(int)(axisNorm.Y * cos - perp.Y * sin),
				axisNorm.Z) * radius.Length / 1024;

			var rightVec = new WVec(
				(int)(axisNorm.X * cos + perp.X * sin),
				(int)(axisNorm.Y * cos + perp.Y * sin),
				axisNorm.Z) * radius.Length / 1024;

			yield return new LineAnnotationRenderable(apex, apex + leftVec, width, color);
			yield return new LineAnnotationRenderable(apex, apex + rightVec, width, color);
		}

		static IEnumerable<IRenderable> ConeArcSegments(WPos apex, WVec axis, int coneAngleDeg, WDist radius, int width, Color color)
		{
			const int Segments = 24;

			// Normalize the axis vector
			if (axis.Length == 0)
				yield break;

			var axisNorm = axis * 1024 / axis.Length;
			var halfAngleRad = Math.PI * (coneAngleDeg / 2.0) / 180.0;

			// Create a perpendicular vector in the XY plane
			var perp = new WVec(-axisNorm.Y, axisNorm.X, 0);
			if (perp.Length == 0)
				perp = new WVec(1024, 0, 0); // Fallback if axis is vertical
			else
				perp = perp * 1024 / perp.Length;

			WPos? prev = null;
			for (var i = 0; i <= Segments; i++)
			{
				var t = (double)i / Segments;
				var angle = -halfAngleRad + t * (2 * halfAngleRad);
				var cos = Math.Cos(angle);
				var sin = Math.Sin(angle);

				var vec = new WVec(
					(int)(axisNorm.X * cos - perp.X * sin),
					(int)(axisNorm.Y * cos - perp.Y * sin),
					axisNorm.Z) * radius.Length / 1024;

				var p = apex + vec;
				if (prev.HasValue)
					yield return new LineAnnotationRenderable(prev.Value, p, width, color);
				prev = p;
			}
		}

		static IEnumerable<IRenderable> ConeSegmentLines(WPos apex, WVec axis, int segmentStart, int segmentEnd,
			int startRadius, int endRadius, int coneAngleDeg, int width, Color color)
		{
			// Normalize the axis vector
			if (axis.Length == 0)
				yield break;

			var axisNorm = axis * 1024 / axis.Length;
			var halfAngleRad = Math.PI * (coneAngleDeg / 2.0) / 180.0;

			// Create a perpendicular vector in the XY plane
			var perp = new WVec(-axisNorm.Y, axisNorm.X, 0);
			if (perp.Length == 0)
				perp = new WVec(1024, 0, 0); // Fallback if axis is vertical
			else
				perp = perp * 1024 / perp.Length;

			// Draw the two curved arcs (near and far)
			const int Segments = 24;
			WPos? prevStart = null;
			WPos? prevEnd = null;

			for (var i = 0; i <= Segments; i++)
			{
				var t = (double)i / Segments;
				var angle = -halfAngleRad + t * (2 * halfAngleRad);
				var cos = Math.Cos(angle);
				var sin = Math.Sin(angle);

				var dirVec = new WVec(
					(int)(axisNorm.X * cos - perp.X * sin),
					(int)(axisNorm.Y * cos - perp.Y * sin),
					axisNorm.Z);

				// Calculate points on the start and end arcs (centered at apex)
				var startPoint = apex + dirVec * startRadius / 1024;
				var endPoint = apex + dirVec * endRadius / 1024;

				// Draw arc segments
				if (prevStart.HasValue)
				{
					yield return new LineAnnotationRenderable(prevStart.Value, startPoint, width, color);
					yield return new LineAnnotationRenderable(prevEnd.Value, endPoint, width, color);
				}

				// Draw connecting lines along cone edges (only at the two extremes for clarity)
				if (i == 0 || i == Segments)
				{
					yield return new LineAnnotationRenderable(startPoint, endPoint, width, color);
				}

				prevStart = startPoint;
				prevEnd = endPoint;
			}
		}
	}
}
