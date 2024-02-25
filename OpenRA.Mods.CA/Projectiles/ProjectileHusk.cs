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
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.CA.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;
using Util = OpenRA.Mods.Common.Util;

namespace OpenRA.Mods.CA.Projectiles
{
	[Desc("Projectile with customisable acceleration vector, recieve dead actor speed by using range modifier, used as aircraft husk.")]
	public class ProjectileHuskInfo : IProjectileInfo
	{
		public readonly string Image = null;

		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Loop a randomly chosen sequence of Image from this list while falling.")]
		public readonly string[] Sequences = { "idle" };

		[PaletteReference]
		[Desc("The palette used to draw this projectile.")]
		public readonly string Palette = "effect";

		[Desc("Palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		[Desc("Does this projectile have a shadow?")]
		public readonly bool Shadow = false;

		[Desc("Color to draw shadow if Shadow is true.")]
		public readonly Color ShadowColor = Color.FromArgb(140, 0, 0, 0);

		[Desc("Projectile movement vector per tick (forward, right, up), use negative values for opposite directions.")]
		public readonly WVec Velocity = WVec.Zero;

		[Desc("The X of the speed becomes dead actor speed by using range modifier, coop with " + nameof(SpawnHuskEffectOnDeath) + ".")]
		public readonly bool UseRangeModifierAsVelocityX = true;

		[Desc("Movement random factor on Velocity.")]
		public readonly WVec? VelocityRandomFactor = null;

		[Desc("Value added to Velocity every tick when spin is activated.")]
		public readonly WVec AccelerationWhenSpin = new(0, 0, -10);

		[Desc("Value added to Velocity every tickwhen spin is NOT activated.")]
		public readonly WVec Acceleration = new(0, 0, -10);

		[Desc("Chance of Spin. Activate Spin.")]
		public readonly int SpinChance = 100;

		[Desc("Limit the maximum spin (in angle units per tick) that can be achieved.",
			"0 Disables spinning.")]
		public readonly int MaximumSpinSpeed = 0;

		[Desc("Spin acceleration.")]
		public readonly int SpinAcc = 0;

		[Desc("begin spin speed.")]
		public readonly int Spin = 0;

		[Desc("Revert the Y of the speed, and X, Y of acceleration at 50% randomness.")]
		public readonly bool HorizontalRevert = false;

		[Desc("Trail animation.")]
		public readonly string TrailImage = null;

		[SequenceReference(nameof(TrailImage), allowNullImage: true)]
		[Desc("Loop a randomly chosen sequence of TrailImage from this list while this projectile is moving.")]
		public readonly string[] TrailSequences = { "idle" };

		[Desc("Interval in ticks between each spawned Trail animation.")]
		public readonly int TrailInterval = 2;

		[Desc("Delay in ticks until trail animation is spawned.")]
		public readonly int TrailDelay = 0;

		[PaletteReference(nameof(TrailUsePlayerPalette))]
		[Desc("Palette used to render the trail sequence.")]
		public readonly string TrailPalette = "effect";

		[Desc("Use the Player Palette to render the trail sequence.")]
		public readonly bool TrailUsePlayerPalette = false;

		public IProjectile Create(ProjectileArgs args) { return new ProjectileHusk(this, args); }
	}

	public class ProjectileHusk : IProjectile, ISync
	{
		readonly ProjectileHuskInfo info;
		readonly Animation anim;
		readonly ProjectileArgs args;
		readonly string trailPalette;

		readonly float3 shadowColor;
		readonly float shadowAlpha;
		readonly int spinAcc;
		readonly int maxSpin;

		WVec velocity;
		WVec acceleration;
		WAngle facing;
		int spin;
		WDist dat;

		[Sync]
		WPos pos, lastPos;
		int smokeTicks;

		public ProjectileHusk(ProjectileHuskInfo info, ProjectileArgs args)
		{
			this.info = info;
			this.args = args;
			pos = args.Source;
			facing = args.Facing;
			var world = args.SourceActor.World;
			dat = world.Map.DistanceAboveTerrain(pos);

			var vx = info.UseRangeModifierAsVelocityX && args.RangeModifiers.Length > 0 ? args.RangeModifiers[0] : info.Velocity.X;
			var vec = info.VelocityRandomFactor != null ? new WVec(vx + world.SharedRandom.Next(info.VelocityRandomFactor.Value.X), info.Velocity.Y + world.SharedRandom.Next(info.VelocityRandomFactor.Value.Y), info.Velocity.Z + world.SharedRandom.Next(info.VelocityRandomFactor.Value.Z)) : new WVec(vx, info.Velocity.Y, info.Velocity.Z);

			if (info.HorizontalRevert && world.SharedRandom.Next(2) == 0)
			{
				velocity = new WVec(-vec.Y, -vec.X, vec.Z);
				if (info.MaximumSpinSpeed > 0 && world.SharedRandom.Next(1, 101) <= info.SpinChance)
				{
					acceleration = new WVec(-info.AccelerationWhenSpin.Y, info.AccelerationWhenSpin.X, info.AccelerationWhenSpin.Z);
					spin = -info.Spin;
					spinAcc = -info.SpinAcc;
					maxSpin = -info.MaximumSpinSpeed;
				}
				else
					acceleration = new WVec(-info.Acceleration.Y, info.Acceleration.X, info.Acceleration.Z);
			}
			else
			{
				velocity = new WVec(vec.Y, -vec.X, vec.Z);
				if (info.MaximumSpinSpeed > 0 && world.SharedRandom.Next(1, 101) <= info.SpinChance)
				{
					acceleration = new WVec(info.AccelerationWhenSpin.Y, -info.AccelerationWhenSpin.X, info.AccelerationWhenSpin.Z);
					spin = info.Spin;
					spinAcc = info.SpinAcc;
					maxSpin = info.MaximumSpinSpeed;
				}
				else
					acceleration = new WVec(info.Acceleration.Y, -info.Acceleration.X, info.Acceleration.Z);
			}

			velocity = velocity.Rotate(WRot.FromYaw(facing));
			acceleration = acceleration.Rotate(WRot.FromYaw(facing));

			if (!string.IsNullOrEmpty(info.Image))
			{
				anim = new Animation(args.SourceActor.World, info.Image, GetEffectiveFacing);
				anim.PlayRepeating(info.Sequences.Random(args.SourceActor.World.SharedRandom));
			}

			shadowColor = new float3(info.ShadowColor.R, info.ShadowColor.G, info.ShadowColor.B) / 255f;
			shadowAlpha = info.ShadowColor.A / 255f;

			trailPalette = info.TrailPalette;
			if (info.TrailUsePlayerPalette)
				trailPalette += args.SourceActor.Owner.InternalName;
			smokeTicks = info.TrailDelay;
		}

		public void Tick(World world)
		{
			lastPos = pos;
			pos += velocity;
			dat = world.Map.DistanceAboveTerrain(pos);

			if (maxSpin != 0)
			{
				var spinAngle = new WAngle(spin);
				facing += spinAngle;
				acceleration = acceleration.Rotate(WRot.FromYaw(spinAngle));
				spin = Math.Abs(spin) < Math.Abs(maxSpin) ? spin + spinAcc : maxSpin;
			}

			velocity += acceleration;

			// Explodes
			if (dat.Length <= 0)
			{
				pos -= new WVec(0, 0, dat.Length);
				world.AddFrameEndTask(w => w.Remove(this));

				var warheadArgs = new WarheadArgs(args)
				{
					ImpactOrientation = new WRot(WAngle.Zero, Util.GetVerticalAngle(lastPos, pos), args.Facing),
					ImpactPosition = pos,
				};

				args.Weapon.Impact(Target.FromPos(pos), warheadArgs);
			}

			if (!string.IsNullOrEmpty(info.TrailImage) && --smokeTicks < 0)
			{
				world.AddFrameEndTask(w => w.Add(new SpriteEffect(pos, GetEffectiveFacing(), w,
					info.TrailImage, info.TrailSequences.Random(world.SharedRandom), trailPalette)));

				smokeTicks = info.TrailInterval;
			}

			anim?.Tick();
		}

		WAngle GetEffectiveFacing()
		{
			return facing;
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (anim == null)
				yield break;

			var world = args.SourceActor.World;
			if (!world.FogObscures(pos))
			{
				var paletteName = info.Palette;
				if (paletteName != null && info.IsPlayerPalette)
					paletteName += args.SourceActor.Owner.InternalName;

				var palette = wr.Palette(paletteName);

				if (info.Shadow)
				{
					var shadowPos = pos - new WVec(0, 0, dat.Length);
					foreach (var r in anim.Render(shadowPos, palette))
						yield return ((IModifyableRenderable)r)
							.WithTint(shadowColor, ((IModifyableRenderable)r).TintModifiers | TintModifiers.ReplaceColor)
							.WithAlpha(shadowAlpha);
				}

				foreach (var r in anim.Render(pos, palette))
					yield return r;
			}
		}
	}
}
