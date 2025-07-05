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
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Projectiles
{
	[Desc("Copy of TeslaZap. CA version adds to ZOffset for targets below the source.")]
	public class TeslaZapCAInfo : IProjectileInfo
	{
		public readonly string Image = "litning";

		[SequenceReference(nameof(Image))]
		public readonly string BrightSequence = "bright";

		[SequenceReference(nameof(Image))]
		public readonly string DimSequence = "dim";

		[PaletteReference]
		public readonly string Palette = "effect";

		public readonly int BrightZaps = 1;
		public readonly int DimZaps = 2;

		public readonly int Duration = 2;

		public readonly int DamageDuration = 1;

		public readonly bool TrackTarget = true;

		public IProjectile Create(ProjectileArgs args) { return new TeslaZapCA(this, args); }
	}

	public class TeslaZapCA : IProjectile, ISync
	{
		readonly ProjectileArgs args;
		readonly TeslaZapCAInfo info;
		TeslaZapRenderableCA zap;
		int ticksUntilRemove;
		int damageDuration;

		[Sync]
		WPos target;

		public TeslaZapCA(TeslaZapCAInfo info, ProjectileArgs args)
		{
			this.args = args;
			this.info = info;
			ticksUntilRemove = info.Duration;
			damageDuration = info.DamageDuration > info.Duration ? info.Duration : info.DamageDuration;
			target = args.PassiveTarget;
		}

		public void Tick(World world)
		{
			if (ticksUntilRemove-- <= 0)
				world.AddFrameEndTask(w => w.Remove(this));

			// Zap tracks target
			if (info.TrackTarget && args.GuidedTarget.IsValidFor(args.SourceActor))
				target = args.Weapon.TargetActorCenter ? args.GuidedTarget.CenterPosition : args.GuidedTarget.Positions.ClosestToIgnoringPath(args.Source);

			if (damageDuration-- > 0)
				args.Weapon.Impact(Target.FromPos(target), new WarheadArgs(args));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			var zOffset = 0;
			var verticalDiff = target.Y - args.Source.Y;
			if (verticalDiff > 0)
				zOffset += verticalDiff;

			zap = new TeslaZapRenderableCA(args.Source, zOffset, target - args.Source,
				info.Image, info.BrightSequence, info.BrightZaps, info.DimSequence, info.DimZaps, info.Palette);

			yield return zap;
		}
	}
}
