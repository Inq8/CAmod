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
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Projectiles
{
	public class LinearPulseInfo : IProjectileInfo
	{
		[Desc("Ticks between pulse impacts.")]
		public readonly WDist ImpactInterval = WDist.Zero;

		[Desc("Speed the pulse travels.")]
		public readonly WDist Speed = new WDist(384);

		[Desc("Visual speed of the projectile. Set to zero to use the same speed as the pulse.")]
		public readonly WDist VisualSpeed = WDist.Zero;

		[Desc("Minimum distance travelled before doing damage.")]
		public readonly WDist MinimumImpactDistance = WDist.Zero;

		[Desc("Maximum distance travelled after which no more damage occurs. Zero falls back to weapon range.")]
		public readonly WDist MaximumImpactDistance = WDist.Zero;

		[Desc("Maximum distance travelled by projectile visual (if present). Zero falls back to weapon range.")]
		public readonly WDist VisualRange = WDist.Zero;

		[Desc("Whether to ignore range modifiers, as these can mess up the relationship between ImpactInterval, Speed and max range.")]
		public readonly bool IgnoreRangeModifiers = true;

		[Desc("The maximum/constant/incremental inaccuracy used in conjunction with the InaccuracyType property.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Controls the way inaccuracy is calculated. Possible values are " +
			"'Maximum' - scale from 0 to max with range, " +
			"'PerCellIncrement' - scale from 0 with range, " +
			"'Absolute' - use set value regardless of range.")]
		public readonly InaccuracyType InaccuracyType = InaccuracyType.Maximum;

		[Desc("Projectile image to display.")]
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

		[Desc("Final impact animation.")]
		public readonly string FinalHitAnim = null;

		[SequenceReference(nameof(FinalHitAnim), allowNullImage: true)]
		[Desc("Sequence of impact animation to use.")]
		public readonly string FinalHitAnimSequence = "idle";

		[PaletteReference]
		public readonly string FinalHitAnimPalette = "effect";

		public IProjectile Create(ProjectileArgs args) { return new LinearPulse(this, args); }
	}

	public class LinearPulse : IProjectile, ISync
	{
		readonly LinearPulseInfo info;
		readonly ProjectileArgs args;
		readonly WVec speed;
		readonly WVec visualSpeed;
		readonly WVec directionalSpeed;
		readonly WVec visualDirectionalSpeed;
		readonly Animation finalHitAnim;
		readonly WDist impactInterval;

		readonly WAngle facing;
		readonly Animation anim;

		[Sync]
		WPos pos, visualPos, target, source;
		int ticks;
		int totalDistanceTravelled;
		int totalVisualDistanceTravelled;
		bool travelComplete;
		bool visualTravelComplete;
		int range;
		int visualRange;
		WPos[] impactPositions;
		bool showFinalHitAnim;
		bool finalHitStarted;

		public Actor SourceActor { get { return args.SourceActor; } }

		public LinearPulse(LinearPulseInfo info, ProjectileArgs args)
		{
			this.info = info;
			this.args = args;

			speed = new WVec(0, -info.Speed.Length, 0);
			visualSpeed = info.VisualSpeed != WDist.Zero && info.VisualSpeed != info.Speed ? new WVec(0, -info.VisualSpeed.Length, 0) : speed;

			impactInterval = info.ImpactInterval > WDist.Zero ? info.ImpactInterval : info.Speed;

			source = args.Source;

			var world = args.SourceActor.World;

			// projectile starts at the source position
			pos = visualPos = args.Source;

			if (info.ForceGround)
				pos = new WPos(pos.X, pos.Y, 0);

			// initially no distance has been travelled by the pulse
			totalDistanceTravelled = 0;
			totalVisualDistanceTravelled = 0;

			// the weapon range (total distance to be travelled)
			range = args.Weapon.Range.Length;
			visualRange = info.VisualRange == WDist.Zero ? range : info.VisualRange.Length;

			if (!info.IgnoreRangeModifiers)
			{
				range = Common.Util.ApplyPercentageModifiers(range, args.RangeModifiers);
				visualRange = Common.Util.ApplyPercentageModifiers(visualRange, args.RangeModifiers);
			}

			target = args.PassiveTarget;

			// get the offset of the source compared to source actor's center and apply the same offset to the target (so the linear pulse always travels parallel to the source actor's facing)
			var offsetFromCenter = source - args.SourceActor.CenterPosition;
			target = target + offsetFromCenter;

			if (info.Inaccuracy.Length > 0)
			{
				var maxInaccuracyOffset = Common.Util.GetProjectileInaccuracy(info.Inaccuracy.Length, info.InaccuracyType, args);
				target += WVec.FromPDF(world.SharedRandom, 2) * maxInaccuracyOffset / 1024;
			}

			facing = (target - pos).Yaw;

			// calculate the vectors for travel
			directionalSpeed = speed.Rotate(WRot.FromYaw(facing));
			visualDirectionalSpeed = info.VisualSpeed != info.Speed ? visualSpeed.Rotate(WRot.FromYaw(facing)) : directionalSpeed;

			// calculate impact positions
			var impactCount = range / impactInterval.Length;
			var impactVector = new WVec(0, -impactInterval.Length, 0).Rotate(WRot.FromYaw(facing));

			impactPositions = Enumerable.Range(0, impactCount)
				.Select(i => pos + impactVector * (i + 1))
				.ToArray();

			if (!string.IsNullOrEmpty(info.Image))
			{
				anim = new Animation(world, info.Image, new Func<WAngle>(GetEffectiveFacing));

				if (info.RepeatAnimation)
					anim.PlayRepeating(info.Sequences.Random(world.SharedRandom));
				else
					anim.Play(info.Sequences.Random(world.SharedRandom));
			}

			if (!string.IsNullOrEmpty(info.FinalHitAnim))
			{
				finalHitAnim = new Animation(world, info.FinalHitAnim);
				showFinalHitAnim = true;
			}
		}

		public void Tick(World world)
		{
			anim?.Tick();

			if (!travelComplete)
				pos += directionalSpeed;

			if (!visualTravelComplete)
				visualPos += visualDirectionalSpeed;

			totalDistanceTravelled += info.Speed.Length;
			totalVisualDistanceTravelled += info.VisualSpeed != WDist.Zero && info.VisualSpeed != info.Speed ? info.VisualSpeed.Length : info.Speed.Length;

			if (!travelComplete)
			{
				for (int idx = 0; idx < impactPositions.Length; idx++)
				{
					var impactDistance = (idx + 1) * impactInterval.Length;

					if (impactDistance < info.MinimumImpactDistance.Length)
						continue;

					if (impactDistance > totalDistanceTravelled || (info.MaximumImpactDistance.Length > 0 && impactDistance > info.MaximumImpactDistance.Length))
						break;

					if (impactDistance > totalDistanceTravelled - speed.Length)
						Explode(impactPositions[idx]);
				}
			}

			travelComplete = totalDistanceTravelled >= range;
			visualTravelComplete = totalVisualDistanceTravelled >= visualRange;

			if (visualTravelComplete && showFinalHitAnim) {

				if (!finalHitStarted)
				{
					finalHitStarted = true;
					finalHitAnim.PlayThen(info.FinalHitAnimSequence, () => showFinalHitAnim = false);
				}

				finalHitAnim.Tick();
			}

			if (travelComplete && visualTravelComplete && !showFinalHitAnim)
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
			if (finalHitStarted && showFinalHitAnim)
				foreach (var r in finalHitAnim.Render(pos, wr.Palette(info.FinalHitAnimPalette)))
					yield return r;

			if (anim == null || totalVisualDistanceTravelled >= visualRange)
				yield break;

			var world = args.SourceActor.World;
			if (!world.FogObscures(visualPos))
			{
				if (info.Shadow)
				{
					var dat = world.Map.DistanceAboveTerrain(visualPos);
					var shadowPos = visualPos - new WVec(0, 0, dat.Length);
					foreach (var r in anim.Render(shadowPos, wr.Palette(info.ShadowPalette)))
						yield return r;
				}

				var palette = wr.Palette(info.Palette);
				foreach (var r in anim.Render(visualPos, palette))
					yield return r;
			}
		}

		void Explode(WPos impactPos)
		{
			args.Weapon.Impact(Target.FromPos(impactPos), new WarheadArgs(args));
		}
	}
}
