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
		public readonly WDist Spacing = new WDist(768);

		[Desc("Speed the pulse travels.")]
		public readonly WDist Speed = new WDist(384);

		public IProjectile Create(ProjectileArgs args) { return new LinearPulse(this, args); }
	}

	public class LinearPulse : IProjectile, ISync
	{
		readonly LinearPulseInfo info;
		readonly ProjectileArgs args;
		readonly WDist speed;
		readonly int facing;

		[Sync]
		WPos pos, target, source;
		int length;
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

			// the weapon range (distance to be travelled in cell units)
			range = Common.Util.ApplyPercentageModifiers(args.Weapon.Range.Length, args.RangeModifiers);
			target = args.PassiveTarget;
			facing = (target - pos).Yaw.Facing;
			length = Math.Max((target - pos).Length / speed.Length, 1);
		}

		public void Tick(World world)
		{
			var lastPos = pos;
			pos = WPos.LerpQuadratic(source, target, WAngle.Zero, ticks, length);

			if (intervalDistanceTravelled >= info.Spacing.Length)
			{
				intervalDistanceTravelled = 0;
				if (totalDistanceTravelled > 512)
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
			{
				world.AddFrameEndTask(w => w.Remove(this));
			}
			else
			{
				args.Weapon.Impact(Target.FromPos(pos), new WarheadArgs(args));
			}
		}
	}
}
