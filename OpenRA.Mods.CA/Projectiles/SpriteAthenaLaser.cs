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
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Projectiles
{
	[Desc("Generate laser connect by image with different height offset, and trigger warheads on the ground until expires.")]
	class SpriteAthenaLaserInfo : IProjectileInfo
	{
		[FieldLoader.Require]
		[Desc("Laser Image to display.")]
		public readonly string Image = null;

		[SequenceReference(nameof(Image), allowNullImage: false)]
		[Desc("Laser sprite Sequence of Image from this list while this projectile is moving.")]
		public readonly string Sequence = "idle";

		[Desc("Number of the laser sprite to form the beam.")]
		public readonly int SpriteNumber = 8;

		[Desc("Offset of laser sprite to form the beam.")]
		public readonly int HeightOffset = 1024;

		[Desc("Laser ring image to display.")]
		public readonly string RingImage = null;

		[SequenceReference(nameof(RingImage), allowNullImage: true)]
		[Desc("Sequence of laser ring image from this list while this projectile is moving.")]
		public readonly string RingSequence = "idle";

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("The palette used to draw this projectile.")]
		public readonly string Palette = "effect";

		[Desc("Palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		[Desc("Projectile speed in WDist / tick.")]
		public readonly WDist Speed = new(90);

		[Desc("Rotation speed around the target.")]
		public readonly WAngle RotSpeed = WAngle.Zero;

		[Desc("Rotation speed slowly add to max.")]
		public readonly bool RotStartFromZero = true;

		[Desc("How many ticks will pass between explosions.")]
		public readonly int ExplosionInterval = 3;

		[Desc("How many ticks will the projectile pierce even after reach the target location.")]
		public readonly int PierceTicks = 0;

		[Desc("How many ticks will the projectile stay after motion.")]
		public readonly int StayTicks = 8;

		public IProjectile Create(ProjectileArgs args) { return new SpriteAthenaLaser(this, args); }
	}

	class SpriteAthenaLaser : IProjectile
	{
		readonly int explosionInterval;
		readonly ProjectileArgs args;
		readonly WarheadArgs warheadArgs;
		readonly Animation[] animations;
		readonly Animation ringAnim;
		readonly int heightoffset;
		readonly int rotAcc;
		readonly int speed;
		readonly World world;
		readonly string paletteName;
		readonly int length, flightticks, maxticks;
		readonly WPos target;
		readonly WPos source;

		WPos projectilepos;
		WAngle rot;
		int ticks;
		int rotSpeed;

		protected bool FlightLengthReached => ticks > flightticks;

		protected bool LifeExpired => ticks > maxticks;

		public SpriteAthenaLaser(SpriteAthenaLaserInfo info, ProjectileArgs args)
		{
			this.args = args;
			warheadArgs = new WarheadArgs(args);
			speed = info.Speed.Length;

			world = args.SourceActor.World;
			source = new WPos(args.Source.X, args.Source.Y, 0);
			target = new WPos(args.PassiveTarget.X, args.PassiveTarget.Y, 0);
			length = Math.Max((target - source).Length / Math.Max(speed, 1), 1);
			rotSpeed = info.RotStartFromZero ? 0 : info.RotSpeed.Angle * 1000;
			rotAcc = info.RotStartFromZero ? info.RotSpeed.Angle * 1000 / length : 0;

			projectilepos = source - new WVec(0, 0, world.Map.DistanceAboveTerrain(source).Length);
			flightticks = length + info.PierceTicks;
			maxticks = length + info.PierceTicks + info.StayTicks;
			explosionInterval = Math.Max(info.ExplosionInterval, 1);
			heightoffset = info.HeightOffset;

			paletteName = info.Palette;
			if (paletteName != null && info.IsPlayerPalette)
				paletteName += args.SourceActor.Owner.InternalName;

			if (!string.IsNullOrEmpty(info.Image))
			{
				animations = new Animation[info.SpriteNumber];

				for (var i = 0; i < animations.Length; i++)
				{
					animations[i] = new Animation(world, info.Image);
					animations[i].PlayRepeating(info.Sequence);
				}
			}

			if (!string.IsNullOrEmpty(info.RingImage))
			{
				ringAnim = new Animation(world, info.RingImage);
				ringAnim.PlayRepeating(info.RingSequence);
			}
		}

		IEnumerable<IRenderable> IEffect.Render(WorldRenderer wr)
		{
			if (LifeExpired)
				yield break;

			foreach (var r in RenderAnimation(wr))
				yield return r;
		}

		void IEffect.Tick(World world)
		{
			ticks++;

			if (ticks % explosionInterval == 0)
			{
				warheadArgs.ImpactPosition = projectilepos;
				args.Weapon.Impact(Target.FromPos(projectilepos), warheadArgs);
			}

			ringAnim?.Tick();
			for (var i = 0; i < animations.Length; i++)
				animations[i].Tick();

			rotSpeed += rotAcc;
			if (!FlightLengthReached)
			{
				var pos = projectilepos;
				if (speed != 0)
					pos = WPos.Lerp(source, target, ticks, length);

				if (rotSpeed != 0)
				{
					rot += new WAngle(rotSpeed / 1000);
					pos = target + (pos - target).Rotate(WRot.FromYaw(rot));
				}

				projectilepos = pos - new WVec(0, 0, world.Map.DistanceAboveTerrain(pos).Length);
			}

			if (LifeExpired)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		protected IEnumerable<IRenderable> RenderAnimation(WorldRenderer wr)
		{
			var renderpos = projectilepos;
			var palette = wr.Palette(paletteName);

			if (world.FogObscures(projectilepos))
				yield break;

			if (ringAnim != null)
			{
				foreach (var r in ringAnim.Render(renderpos, palette))
					yield return r;
			}

			for (var i = 0; i < animations.Length; i++)
			{
				foreach (var r in animations[i].Render(renderpos, palette))
					yield return r;

				renderpos += new WVec(0, 0, heightoffset);
			}
		}
	}
}
