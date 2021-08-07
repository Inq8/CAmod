#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("Copy of RenderDetectionCircle where the range can be specified rather than being based on DetectCloaked.")]
	class WithDetectionCircleInfo : ConditionalTraitInfo
	{
		[Desc("WAngle the Radar update line advances per tick.")]
		public readonly WAngle UpdateLineTick = new WAngle(-1);

		[Desc("Number of trailing Radar update lines.")]
		public readonly int TrailCount = 0;

		[Desc("Color of the circle and scanner update line.")]
		public readonly Color Color = Color.FromArgb(128, Color.LimeGreen);

		[Desc("Range circle line width.")]
		public readonly float Width = 1;

		[Desc("Border color of the circle and scanner update line.")]
		public readonly Color BorderColor = Color.FromArgb(96, Color.Black);

		[Desc("Range circle border width.")]
		public readonly float BorderWidth = 3;

		[Desc("Range of the circle")]
		public readonly WDist Range = WDist.Zero;

		public override object Create(ActorInitializer init) { return new WithDetectionCircle(init.Self, this); }
	}

	class WithDetectionCircle : ConditionalTrait<WithDetectionCircleInfo>, ITick, IRenderAnnotationsWhenSelected
	{
		public new readonly WithDetectionCircleInfo Info;
		WAngle lineAngle;

		public WithDetectionCircle(Actor self, WithDetectionCircleInfo info)
			: base(info)
		{
			Info = info;
		}

		IEnumerable<IRenderable> IRenderAnnotationsWhenSelected.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (IsTraitDisabled)
				yield break;

			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				yield break;

			if (Info.Range == WDist.Zero)
				yield break;

			yield return new DetectionCircleAnnotationRenderable(
				self.CenterPosition,
				Info.Range,
				0,
				Info.TrailCount,
				Info.UpdateLineTick,
				lineAngle,
				Info.Color,
				Info.Width,
				Info.BorderColor,
				Info.BorderWidth);
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable { get { return false; } }

		void ITick.Tick(Actor self)
		{
			lineAngle += Info.UpdateLineTick;
		}
	}
}
