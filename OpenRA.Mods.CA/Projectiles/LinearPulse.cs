#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Projectiles
{
	public class LinearPulseInfo : IProjectileInfo
	{
		[Desc("Distance between pulse impacts.")]
		public readonly WDist ImpactSpacing = new WDist(768);

		[Desc("Speed the pulse travels.")]
		public readonly WDist Speed = new WDist(384);

		[Desc("Minimum distance travelled before doing damage.")]
		public readonly WDist MinimumImpactDistance = WDist.Zero;

		[Desc("Maximum distance travelled after which no more damage occurs. Zero falls back to weapon range.")]
		public readonly WDist MaximumImpactDistance = WDist.Zero;

		[Desc("Whether to ignore range modifiers, as these can mess up the relationship between ImpactSpacing, Speed and max range.")]
		public readonly bool IgnoreRangeModifiers = true;

		public IProjectile Create(ProjectileArgs args) { return new LinearPulse(this, args); }
	}

	public class LinearPulse : IProjectile, ISync
	{
		readonly LinearPulseInfo info;
		readonly ProjectileArgs args;
		readonly WDist speed;
		readonly WAngle facing;

		[Sync]
		WPos pos, target, source;
		int ticks;
		int intervalDistanceTravelled;
		int totalDistanceTravelled;
		int range;

		public Actor SourceActor { get { return args.SourceActor; } }

		public LinearPulse(LinearPulseInfo info, ProjectileArgs args)
		{
			this.info = info;
			this.args = args;
			speed = info.Speed;
			source = args.Source;

			// projectile starts at the source position
			pos = args.Source;

			// initially no distance has been travelled by the pulse
			totalDistanceTravelled = 0;
			range = args.Weapon.Range.Length;

			if (!info.IgnoreRangeModifiers)
				range = Common.Util.ApplyPercentageModifiers(range, args.RangeModifiers);

			// the weapon range (distance to be travelled in cell units)
			target = args.PassiveTarget;
			facing = (target - pos).Yaw;
		}

		public void Tick(World world)
		{
			var lastPos = pos;
			var convertedVelocity = new WVec(0, -speed.Length, 0);
			var velocity = convertedVelocity.Rotate(WRot.FromYaw(facing));
			pos = pos + velocity;

			if (intervalDistanceTravelled >= info.ImpactSpacing.Length)
			{
				intervalDistanceTravelled = 0;
				if (totalDistanceTravelled >= info.MinimumImpactDistance.Length && (info.MaximumImpactDistance == WDist.Zero || totalDistanceTravelled <= info.MaximumImpactDistance.Length))
					Explode(world);
			}

			totalDistanceTravelled += speed.Length;
			intervalDistanceTravelled += speed.Length;
			ticks++;
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			return Enumerable.Empty<IRenderable>();
		}

		void Explode(World world)
		{
			if (totalDistanceTravelled >= range)
				world.AddFrameEndTask(w => w.Remove(this));
			else
				args.Weapon.Impact(Target.FromPos(pos), new WarheadArgs(args));
		}
	}
}
