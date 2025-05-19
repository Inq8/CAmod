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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc(".")]
	public class RenderLineInfo : ConditionalTraitInfo
	{
		[Desc("Color of the line.")]
		public readonly Color Color = Color.FromArgb(128, Color.White);

		[Desc("Line width.")]
		public readonly float Width = 1;

		[Desc("Line angle.")]
		public readonly WAngle Angle = WAngle.Zero;

		[Desc("Line length.")]
		public readonly WDist Length = WDist.Zero;

		[Desc("Dash length.")]
		public readonly WDist DashLength = WDist.Zero;

		[Desc("Fade duration in ticks.")]
		public readonly int FadeTicks = 0;

		[Desc("If true, fade in as well as out.")]
		public readonly bool FadeIn = true;

		public override object Create(ActorInitializer init) { return new RenderLine(init.Self, this); }
	}

	public class RenderLine : ConditionalTrait<RenderLineInfo>, INotifyCreated, IRenderAnnotations, ITick
	{
		readonly RenderLineInfo info;
		int currentAlpha;
		int ticksUntilFaded;
		int fadePerTick;

		public RenderLine(Actor self, RenderLineInfo info)
			: base(info)
		{
			this.info = info;
			currentAlpha = info.Color.ToAhsv().A;
			ticksUntilFaded = info.FadeTicks;
			fadePerTick = info.FadeTicks > 0 ? currentAlpha / info.FadeTicks : 0;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
		}

		void ITick.Tick(Actor self)
		{
			if (info.FadeTicks == 0)
				return;

			if (--ticksUntilFaded <= 0)
			{
				ticksUntilFaded = info.FadeTicks;

				if (info.FadeIn)
					fadePerTick *= -1;
				else
					currentAlpha = info.Color.ToAhsv().A;
			}

			currentAlpha -= fadePerTick;
		}

		public IEnumerable<IRenderable> LineRenderables(Actor self, WorldRenderer wr)
		{
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				yield break;

			if (IsTraitDisabled)
				yield break;

			var dashLength = info.DashLength == WDist.Zero ? info.Length : info.DashLength;
			var dashVector = new WVec(0, -dashLength.Length, 0);
			dashVector = dashVector.Rotate(WRot.FromYaw(info.Angle));

			var currentDashStartPos = self.CenterPosition;
			var lengthTravelled = WDist.Zero;
			var color = Color.FromArgb(currentAlpha, info.Color);

			while (lengthTravelled.Length < info.Length.Length)
			{
				lengthTravelled = lengthTravelled + (dashLength * 2);
				yield return new LineAnnotationRenderable(currentDashStartPos, currentDashStartPos + dashVector, info.Width, color);
				currentDashStartPos += (dashVector * 2);
			}
		}

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			return LineRenderables(self, wr);
		}

		bool IRenderAnnotations.SpatiallyPartitionable { get { return false; } }
	}
}
