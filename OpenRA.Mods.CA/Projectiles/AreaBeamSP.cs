#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.SP.Projectiles
{
	public class AreaBeamSPInfo : IProjectileInfo
	{
		[Desc("Projectile speed in WDist / tick, two values indicate a randomly picked velocity per beam.")]
		public readonly WDist[] Speed = { new WDist(128) };

		[Desc("The maximum duration (in ticks) of each beam burst.")]
		public readonly int Duration = 10;

		[Desc("The number of ticks between the beam causing warhead impacts in its area of effect.")]
		public readonly int DamageInterval = 3;

		[Desc("The width of the beam.")]
		public readonly WDist Width = new WDist(512);

		[Desc("The shape of the beam.  Accepts values Cylindrical or Flat.")]
		public readonly BeamRenderableShape Shape = BeamRenderableShape.Cylindrical;

		[Desc("How far beyond the target the projectile keeps on travelling.")]
		public readonly WDist BeyondTargetRange = new WDist(0);

		[Desc("Damage modifier applied at each range step.")]
		public readonly int[] Falloff = { 100, 100 };

		[Desc("Ranges at which each Falloff step is defined.")]
		public readonly WDist[] Range = { WDist.Zero, new WDist(int.MaxValue) };

		[Desc("Can this projectile be blocked when hitting actors with an IBlocksProjectiles trait.")]
		public readonly bool Blockable = false;

		[Desc("Does the beam follow the target.")]
		public readonly bool TrackTarget = false;

		[Desc("Should the beam be visually rendered? False = Beam is invisible.")]
		public readonly bool RenderBeam = true;

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset = 0;

		[Desc("Color of the beam.")]
		public readonly Color Color = Color.Red;

		[Desc("Beam color is the player's color.")]
		public readonly bool UsePlayerColor = false;

		[Desc("Color of the second beam.")]
		public readonly Color SecondColor = Color.Red;

		[Desc("The width of the second beam.")]
		public readonly WDist SecondWidth = new WDist(256);

		[Desc("Image containing hit effect sequence.")]
		public readonly string HitEffectImage = null;

		[SequenceReference(nameof(HitEffectImage), allowNullImage: true)]
		[Desc("Sequence of impact effect to use.")]
		public readonly string HitEffectSequence = "idle";

		[Desc("Impact effect interval.")]
		public readonly int HitEffectInterval = 4;

		[PaletteReference]
		public readonly string HitEffectPalette = "effect";

		[Desc("Image containing launch effect sequence.")]
		public readonly string LaunchEffectImage = null;

		[SequenceReference(nameof(LaunchEffectImage), allowNullImage: true)]
		[Desc("Launch effect sequence to play.")]
		public readonly string LaunchEffectSequence = null;

		[PaletteReference]
		[Desc("Palette to use for launch effect.")]
		public readonly string LaunchEffectPalette = "effect";

		[Desc("Launch effect interval.")]
		public readonly int LaunchEffectInterval = 4;

		public IProjectile Create(ProjectileArgs args)
		{
			var c = UsePlayerColor ? args.SourceActor.Owner.Color : Color;
			return new AreaBeamSP(this, args, c, SecondColor);
		}
	}

	public class AreaBeamSP : IProjectile, ISync
	{
		readonly AreaBeamSPInfo info;
		readonly ProjectileArgs args;
		readonly AttackBase actorAttackBase;
		readonly Color color;
		readonly Color color2;
		readonly WDist speed;
		readonly WDist weaponRange;

		int showHitEffectDelay = -1;
		int showLaunchEffectDelay = -1;

		[Sync]
		WPos headPos;

		[Sync]
		WPos tailPos;

		[Sync]
		WPos target;

		int length;
		WAngle towardsTargetFacing;
		int headTicks;
		int tailTicks;
		bool isHeadTravelling = true;
		bool isTailTravelling;
		bool continueTracking = true;
		Func<WPos> hitPosfunc;
		Func<WPos> launchPosfunc;

		bool IsBeamComplete => !isHeadTravelling && headTicks >= length && !isTailTravelling && tailTicks >= length;

		public AreaBeamSP(AreaBeamSPInfo info, ProjectileArgs args, Color color, Color color2)
		{
			this.info = info;
			this.args = args;
			this.color = color;
			this.color2 = color2;
			actorAttackBase = args.SourceActor.Trait<AttackBase>();

			var world = args.SourceActor.World;
			if (info.Speed.Length > 1)
				speed = new WDist(world.SharedRandom.Next(info.Speed[0].Length, info.Speed[1].Length));
			else
				speed = info.Speed[0];

			// Both the head and tail start at the source actor, but initially only the head is travelling.
			headPos = args.Source;
			tailPos = headPos;

			target = args.PassiveTarget;

			towardsTargetFacing = (target - headPos).Yaw;

			// Update the target position with the range we shoot beyond the target by
			// I.e. we can deliberately overshoot, so aim for that position
			var dir = new WVec(0, -1024, 0).Rotate(WRot.FromYaw(towardsTargetFacing));
			target += dir * info.BeyondTargetRange.Length / 1024;

			length = Math.Max((target - headPos).Length / speed.Length, 1);
			weaponRange = new WDist(Common.Util.ApplyPercentageModifiers(args.Weapon.Range.Length, args.RangeModifiers));

			if (!string.IsNullOrEmpty(info.HitEffectImage))
				showHitEffectDelay = 0;

			if (!string.IsNullOrEmpty(info.LaunchEffectImage))
				showLaunchEffectDelay = 0;

			hitPosfunc = () => headPos;
			launchPosfunc = () => args.Source;
		}

		void TrackTarget()
		{
			if (!continueTracking)
				return;

			if (args.GuidedTarget.IsValidFor(args.SourceActor))
			{
				var guidedTargetPos = args.Weapon.TargetActorCenter ? args.GuidedTarget.CenterPosition : args.GuidedTarget.Positions.PositionClosestTo(args.Source);
				var targetDistance = new WDist((guidedTargetPos - args.Source).Length);

				// Only continue tracking target if it's within weapon range +
				// BeyondTargetRange to avoid edge case stuttering (start firing and immediately stop again).
				if (targetDistance > weaponRange + info.BeyondTargetRange)
					StopTargeting();
				else
				{
					target = guidedTargetPos;
					towardsTargetFacing = (target - args.Source).Yaw;

					// Update the target position with the range we shoot beyond the target by
					// I.e. we can deliberately overshoot, so aim for that position
					var dir = new WVec(0, -1024, 0).Rotate(WRot.FromYaw(towardsTargetFacing));
					target += dir * info.BeyondTargetRange.Length / 1024;
				}
			}
		}

		void StopTargeting()
		{
			continueTracking = false;
			isTailTravelling = true;
		}

		public void Tick(World world)
		{
			if (info.TrackTarget)
				TrackTarget();

			if (++headTicks >= length)
			{
				headPos = target;
				isHeadTravelling = false;
			}
			else if (isHeadTravelling)
				headPos = WPos.LerpQuadratic(args.Source, target, WAngle.Zero, headTicks, length);

			if (tailTicks <= 0 && args.SourceActor.IsInWorld && !args.SourceActor.IsDead)
			{
				args.Source = args.CurrentSource();
				tailPos = args.Source;
			}

			// Allow for leniency to avoid edge case stuttering (start firing and immediately stop again).
			var outOfWeaponRange = weaponRange + info.BeyondTargetRange < new WDist((args.PassiveTarget - args.Source).Length);

			// While the head is travelling, the tail must start to follow Duration ticks later.
			// Alternatively, also stop emitting the beam if source actor dies or is ordered to stop.
			if ((headTicks >= info.Duration && !isTailTravelling) || args.SourceActor.IsDead ||
				!actorAttackBase.IsAiming || outOfWeaponRange)
				StopTargeting();

			if (isTailTravelling)
			{
				if (++tailTicks >= length)
				{
					tailPos = target;
					isTailTravelling = false;
				}
				else
					tailPos = WPos.LerpQuadratic(args.Source, target, WAngle.Zero, tailTicks, length);
			}

			// Check for blocking actors
			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(world, tailPos, headPos, info.Width, out var blockedPos))
			{
				headPos = blockedPos;
				target = headPos;
				length = Math.Min(headTicks, length);
			}

			// Damage is applied to intersected actors every DamageInterval ticks
			if (headTicks % info.DamageInterval == 0)
			{
				var actors = world.FindActorsOnLine(tailPos, headPos, info.Width);
				foreach (var a in actors)
				{
					var adjustedModifiers = args.DamageModifiers.Append(GetFalloff((args.Source - a.CenterPosition).Length));

					var warheadArgs = new WarheadArgs(args)
					{
						ImpactOrientation = new WRot(WAngle.Zero, Common.Util.GetVerticalAngle(args.Source, target), args.CurrentMuzzleFacing()),

						// Calculating an impact position is bogus for line damage.
						// FindActorsOnLine guarantees that the beam touches the target's HitShape,
						// so we just assume a center hit to avoid bogus warhead recalculations.
						ImpactPosition = a.CenterPosition,
						DamageModifiers = adjustedModifiers.ToArray(),
					};

					args.Weapon.Impact(Target.FromActor(a), warheadArgs);
				}
			}

			if (IsBeamComplete)
				world.AddFrameEndTask(w => w.Remove(this));

			if (showHitEffectDelay != -1 && !isHeadTravelling)
			{
				if (showHitEffectDelay == 0)
					world.AddFrameEndTask(w => w.Add(new SpriteEffect(hitPosfunc, () => WAngle.Zero, world, info.HitEffectImage, info.HitEffectSequence, info.HitEffectPalette)));
				showHitEffectDelay = showHitEffectDelay - 1 >= 0 ? showHitEffectDelay - 1 : info.HitEffectInterval;
			}

			if (showLaunchEffectDelay != -1 && !isTailTravelling)
			{
				if (showLaunchEffectDelay == 0)
					world.AddFrameEndTask(w => w.Add(new SpriteEffect(launchPosfunc, () => WAngle.Zero, world, info.LaunchEffectImage, info.LaunchEffectSequence, info.LaunchEffectPalette)));
				showLaunchEffectDelay = showLaunchEffectDelay - 1 >= 0 ? showLaunchEffectDelay - 1 : info.LaunchEffectInterval;
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (!IsBeamComplete && info.RenderBeam && !(wr.World.FogObscures(tailPos) && wr.World.FogObscures(headPos)))
			{
				var beamRender = new BeamRenderable(headPos, info.ZOffset, tailPos - headPos, info.Shape, info.Width, color);
				var beamRender2 = new BeamRenderable(headPos, info.ZOffset, tailPos - headPos, info.Shape, info.SecondWidth, color2);
				yield return beamRender;
				yield return beamRender2;
			}
		}

		int GetFalloff(int distance)
		{
			var inner = info.Range[0].Length;
			for (var i = 1; i < info.Range.Length; i++)
			{
				var outer = info.Range[i].Length;
				if (outer > distance)
					return int2.Lerp(info.Falloff[i - 1], info.Falloff[i], distance - inner, outer - inner);

				inner = outer;
			}

			return 0;
		}
	}
}
