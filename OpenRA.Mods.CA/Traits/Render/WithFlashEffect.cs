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
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("Flashes the target at a set interval.")]
	class WithFlashEffectInfo : ConditionalTraitInfo
	{
		[Desc("Interval in ticks between flashes.")]
		public readonly int Interval = 25;

		[Desc("Flash color.")]
		public readonly Color Color = Color.FromArgb(80, Color.Red);

		public override object Create(ActorInitializer init) { return new WithFlashEffect(this); }
	}

	class WithFlashEffect : ConditionalTrait<WithFlashEffectInfo>, ITick
	{
		public new readonly WithFlashEffectInfo Info;
		int intervalTicks;

		public WithFlashEffect(WithFlashEffectInfo info)
			: base(info)
		{
			Info = info;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || !self.IsInWorld)
				return;

			if (intervalTicks == 0)
			{
				self.World.AddFrameEndTask(w =>
				{
					w.Add(new FlashTarget(self, Info.Color, 0.5f, 1, 2, 0));
				});
			}

			intervalTicks++;

			if (intervalTicks >= Info.Interval)
				intervalTicks = 0;
		}

		protected override void TraitEnabled(Actor self)
		{
			intervalTicks = 0;
		}

		protected override void TraitDisabled(Actor self)
		{
			intervalTicks = 0;
		}
	}
}
