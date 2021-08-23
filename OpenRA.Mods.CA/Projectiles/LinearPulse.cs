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
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Projectiles
{
	public class LinearPulseInfo : IProjectileInfo
	{
		[Desc("Ticks between pulse impacts.")]
		public readonly int ImpactInterval = 2;

		[Desc("Speed the pulse travels.")]
		public readonly WDist Speed = new WDist(384);

		[Desc("Minimum distance travelled before doing damage.")]
		public readonly WDist MinimumImpactDistance = WDist.Zero;

		[Desc("Maximum distance travelled after which no more damage occurs. Zero falls back to weapon range.")]
		public readonly WDist MaximumImpactDistance = WDist.Zero;

		[Desc("Maximum distance travelled by projectile visual (if present). Zero falls back to weapon range.")]
		public readonly WDist ProjectileVisualRange = WDist.Zero;

		[Desc("Whether to ignore range modifiers, as these can mess up the relationship between ImpactSpacing, Speed and max range.")]
		public readonly bool IgnoreRangeModifiers = true;

		[Desc("The maximum/constant/incremental inaccuracy used in conjunction with the InaccuracyType property.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Controls the way inaccuracy is calculated. Possible values are 'Maximum' - scale from 0 to max with range, 'PerCellIncrement' - scale from 0 with range and 'Absolute' - use set value regardless of range.")]
		public readonly InaccuracyType InaccuracyType = InaccuracyType.Maximum;

		[Desc("Image to display.")]
		public readonly string Image = null;

		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Loop a randomly chosen sequence of Image from this list while this projectile is moving.")]
		public readonly string[] Sequences = { "idle" };

		[Desc("The palette used to draw this projectile.")]
		public readonly string Palette = "effect";

		[Desc("Does this projectile have a shadow?")]
		public readonly bool Shadow = false;

		[Desc("Should the projectile animation repeat?")]
		public readonly bool RepeatAnimation = true;

		[Desc("If true, forces pulse position to start at ground level.")]
		public readonly bool ForceGround = false;

		[PaletteReference]
		[Desc("Palette to use for this projectile's shadow if Shadow is true.")]
		public readonly string ShadowPalette = "shadow";

		public IProjectile Create(ProjectileArgs args) { return new LinearPulse(this, args); }
	}

	public class LinearPulse : IProjectile, ISync
	{
		readonly LinearPulseInfo info;
		readonly ProjectileArgs args;
		readonly WDist speed;
		readonly WAngle facing;
		readonly Animation anim;

		[Sync]
		WPos pos, target, source;
		int ticks;
		int intervalTicks;
		int totalDistanceTravelled;
		int range;
		int projectileRange;

		public Actor SourceActor { get { return args.SourceActor; } }

		public LinearPulse(LinearPulseInfo info, ProjectileArgs args)
		{
			this.info = info;
			this.args = args;
			speed = info.Speed;
			source = args.Source;

			var world = args.SourceActor.World;

			// projectile starts at the source position
			pos = args.Source;

			if (info.ForceGround)
				pos = new WPos(pos.X, pos.Y, 0);

			// initially no distance has been travelled by the pulse
			totalDistanceTravelled = 0;

			// the weapon range (total distance to be travelled)
			range = args.Weapon.Range.Length;
			projectileRange = info.ProjectileVisualRange == WDist.Zero ? range : info.ProjectileVisualRange.Length;

			if (!info.IgnoreRangeModifiers)
				range = OpenRA.Mods.Common.Util.ApplyPercentageModifiers(range, args.RangeModifiers);

			target = args.PassiveTarget;

			// get the offset of the source compared to source actor's center and apply the same offset to the target (so the linear pulse always travels parallel to the source actor's facing)
			var offsetFromCenter = source - args.SourceActor.CenterPosition;
			target = target + offsetFromCenter;

			if (info.Inaccuracy.Length > 0)
			{
				var maxInaccuracyOffset = OpenRA.Mods.Common.Util.GetProjectileInaccuracy(info.Inaccuracy.Length, info.InaccuracyType, args);
				target += WVec.FromPDF(world.SharedRandom, 2) * maxInaccuracyOffset / 1024;
			}

			facing = (target - pos).Yaw;

			if (!string.IsNullOrEmpty(info.Image))
			{
				anim = new Animation(world, info.Image, new Func<WAngle>(GetEffectiveFacing));

				if (info.RepeatAnimation)
					anim.PlayRepeating(info.Sequences.Random(world.SharedRandom));
				else
					anim.Play(info.Sequences.Random(world.SharedRandom));
			}
		}

		public void Tick(World world)
		{
			anim?.Tick();

			var lastPos = pos;
			var convertedVelocity = new WVec(0, -speed.Length, 0);
			var velocity = convertedVelocity.Rotate(WRot.FromYaw(facing));
			pos = pos + velocity;

			totalDistanceTravelled += speed.Length;
			intervalTicks++;

			if (intervalTicks >= info.ImpactInterval)
			{
				intervalTicks = 0;
				if (totalDistanceTravelled >= info.MinimumImpactDistance.Length && (info.MaximumImpactDistance == WDist.Zero || totalDistanceTravelled <= info.MaximumImpactDistance.Length))
					Explode(world);
			}

			if (totalDistanceTravelled >= range)
				world.AddFrameEndTask(w => w.Remove(this));

			ticks++;
		}

		WAngle GetEffectiveFacing()
		{
			var angle = WAngle.Zero;
			var at = (float)ticks / (speed.Length - 1);
			var attitude = angle.Tan() * (1 - 2 * at) / (4 * 1024);

			var u = (facing.Angle % 512) / 512f;
			var scale = 2048 * u * (1 - u);

			var effective = (int)(facing.Angle < 512
				? facing.Angle - scale * attitude
				: facing.Angle + scale * attitude);

			return new WAngle(effective);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (anim == null || totalDistanceTravelled >= projectileRange)
				yield break;

			var world = args.SourceActor.World;
			if (!world.FogObscures(pos))
			{
				if (info.Shadow)
				{
					var dat = world.Map.DistanceAboveTerrain(pos);
					var shadowPos = pos - new WVec(0, 0, dat.Length);
					foreach (var r in anim.Render(shadowPos, wr.Palette(info.ShadowPalette)))
						yield return r;
				}

				var palette = wr.Palette(info.Palette);
				foreach (var r in anim.Render(pos, palette))
					yield return r;
			}
		}

		void Explode(World world)
		{
			args.Weapon.Impact(Target.FromPos(pos), new WarheadArgs(args));
		}
	}
}
