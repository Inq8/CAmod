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
using OpenRA.Mods.CA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Projectiles
{
	[Desc("Plasma beam projectile.")]
	public class PlasmaBeamInfo : IProjectileInfo
	{
		[Desc("The maximum duration (in ticks) of the beam's existence.")]
		public readonly int Duration = 10;

		[Desc("Beam only fires directly down.")]
		public readonly bool ForceVertical = false;

		[Desc("Color of the beam.")]
		public readonly Color[] Colors = null;

		[Desc("Inner lightness of the distortion beam. 0xff = 255")]
		public readonly byte InnerLightness = 0xff;

		[Desc("Outer lightness of the distortion beam. 0x80 = 128")]
		public readonly byte OuterLightness = 0x80;

		[Desc("The radius of the distortion beam.")]
		public readonly int Radius = 3;

		[Desc("Initial distortion offset.")]
		public readonly int Distortion = 0;

		[Desc("Distortion added per tick for duration of beam.")]
		public readonly int DistortionAnimation = 0;

		[Desc("Maximum length per segment of distortion beam.")]
		public readonly WDist SegmentLength = WDist.Zero;

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset = 0;

		[Desc("Beam can be blocked.")]
		public readonly bool Blockable = false;

		[Desc("The maximum/constant/incremental inaccuracy used in conjunction with the InaccuracyType property.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Controls the way inaccuracy is calculated. Possible values are 'Maximum' - scale from 0 to max with range, 'PerCellIncrement' - scale from 0 with range and 'Absolute' - use set value regardless of range.")]
		public readonly InaccuracyType InaccuracyType = InaccuracyType.Maximum;

		[Desc("Draw primary center beam.")]
		public readonly bool CenterBeam = false;

		[Desc("Draw a second center beam (for 'glow' effect).")]
		public readonly bool SecondaryCenterBeam = false;

		[Desc("The width of the zap.")]
		public readonly WDist CenterBeamWidth = new WDist(86);

		[Desc("The width of the zap.")]
		public readonly WDist SecondaryCenterBeamWidth = new WDist(86);

		[Desc("The shape of the beam. Accepts values Cylindrical or Flat.")]
		public readonly BeamRenderableShape CenterBeamShape = BeamRenderableShape.Cylindrical;

		[Desc("Color of the primary central beam.")]
		public readonly Color CenterBeamColor = Color.Red;

		[Desc("Color of the secondary central beam.")]
		public readonly Color SecondaryCenterBeamColor = Color.Red;

		[Desc("Image containing launch effect sequence.")]
		public readonly string LaunchEffectImage = null;

		[SequenceReference(nameof(LaunchEffectImage), allowNullImage: true)]
		[Desc("Launch effect sequence to play.")]
		public readonly string LaunchEffectSequence = null;

		[PaletteReference]
		[Desc("Palette to use for launch effect.")]
		public readonly string LaunchEffectPalette = "effect";

		[Desc("Does the beam follow the target.")]
		public readonly bool TrackTarget = false;

		public IProjectile Create(ProjectileArgs args) { return new PlasmaBeam(args, this); }
	}

	public class PlasmaBeam : IProjectile, ISync
	{
		readonly PlasmaBeamInfo info;
		readonly ProjectileArgs args;
		readonly MersenneTwister random;
		readonly Color[] colors;
		readonly bool hasLaunchEffect;
		readonly int numSegments;

		int ticks = 0;
		WPos[] offsets;
		WVec[] distortions;
		WVec leftVector;
		WVec upVector;
		WVec inaccuracyOffset;

		[Sync]
		WPos target, source, lastTarget, lastSource;

		public PlasmaBeam(ProjectileArgs args, PlasmaBeamInfo info)
		{
			this.args = args;
			this.info = info;

			// Set the initial source and target positions
			source = lastSource = args.Source;

			if (info.ForceVertical)
				target = lastTarget = new WPos(source.X, source.Y, 0);
			else
				target = lastTarget = args.PassiveTarget;

			random = args.SourceActor.World.SharedRandom;

			// Apply inaccuracy to target
			if (info.Inaccuracy.Length > 0)
			{
				var maxInaccuracyOffset = OpenRA.Mods.Common.Util.GetProjectileInaccuracy(info.Inaccuracy.Length, info.InaccuracyType, args);
				inaccuracyOffset = WVec.FromPDF(random, 2) * maxInaccuracyOffset / 1024;
				target += inaccuracyOffset;
			}

			// Check for blocking actors
			CheckBlocked();

			var direction = target - source;
			numSegments = info.SegmentLength > WDist.Zero ? (direction.Length - 1) / info.SegmentLength.Length + 1 : 1;
			offsets = new WPos[numSegments + 1];
			distortions = new WVec[numSegments + 1];

			var rangeBonusAlpha = GetRangeBonusAlpha(direction);

			// Set colours for beam center to beam edge (from InnerLightness to OuterLightness)
			colors = new Color[info.Radius];
			for (var i = 0; i < info.Radius; i++)
			{
				var color = info.Colors == null ? Color.Red : info.Colors.Random(Game.CosmeticRandom);
				var bw = (float)((info.InnerLightness - info.OuterLightness) * i / (Math.Max(info.Radius - 1, 1)) + info.OuterLightness) / 0xff;
				var alpha = (float)color.A + rangeBonusAlpha;
				var dstR = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float)color.R / 0xff) : 2 * bw * ((float)color.R / 0xff);
				var dstG = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float)color.G / 0xff) : 2 * bw * ((float)color.G / 0xff);
				var dstB = bw > .5 ? 1 - (1 - 2 * (bw - .5)) * (1 - (float)color.B / 0xff) : 2 * bw * ((float)color.B / 0xff);
				colors[i] = Color.FromArgb((int)(alpha), (int)(dstR * 0xff), (int)(dstG * 0xff), (int)(dstB * 0xff));
			}

			CalculateDistortion(direction);
			CalculateBeam(direction);

			// Do the beam impact (warheads)
			var warheadArgs = new WarheadArgs(args)
			{
				ImpactOrientation = new WRot(WAngle.Zero, OpenRA.Mods.Common.Util.GetVerticalAngle(source, target), args.CurrentMuzzleFacing()),
				ImpactPosition = target,
			};

			args.Weapon.Impact(Target.FromPos(target), warheadArgs);

			// Do launch effect
			hasLaunchEffect = !string.IsNullOrEmpty(info.LaunchEffectImage) && !string.IsNullOrEmpty(info.LaunchEffectSequence);
			if (hasLaunchEffect)
			{
				Func<WAngle> getMuzzleFacing = () => args.CurrentMuzzleFacing();
				args.SourceActor.World.AddFrameEndTask(w => w.Add(new SpriteEffect(args.CurrentSource, getMuzzleFacing, args.SourceActor.World,
					info.LaunchEffectImage, info.LaunchEffectSequence, info.LaunchEffectPalette)));
			}
		}

		void CheckBlocked()
		{
			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(args.SourceActor.World, source, target, info.CenterBeamWidth, out var blockedPos))
				target = blockedPos;
		}

		void CalculateDistortion(WVec direction)
		{
			if (info.Distortion != 0 || info.DistortionAnimation != 0)
			{
				leftVector = new WVec(direction.Y, -direction.X, 0);
				if (leftVector.Length != 0)
					leftVector = 1024 * leftVector / leftVector.Length;

				upVector = leftVector.Length != 0
					? new WVec(
					-direction.X * direction.Z,
					-direction.Z * direction.Y,
					direction.X * direction.X + direction.Y * direction.Y)
					: new WVec(direction.Z, direction.Z, 0);
				if (upVector.Length != 0)
					upVector = 1024 * upVector / upVector.Length;
			}
		}

		void TrackTarget()
		{
			if (!info.TrackTarget || ticks == 0)
				return;

			if (!args.GuidedTarget.IsValidFor(args.SourceActor))
				return;

			var guidedTargetPos = args.Weapon.TargetActorCenter ? args.GuidedTarget.CenterPosition : args.GuidedTarget.Positions.PositionClosestTo(args.Source);
			target = guidedTargetPos + inaccuracyOffset;
		}

		void CalculateBeam(WVec direction)
		{
			var shouldDistort = (ticks == 0 && info.Distortion != 0) || (ticks > 0 && info.DistortionAnimation != 0);

			// Always keep the beam starting at source and ending at target
			offsets[0] = source;
			offsets[offsets.Length - 1] = target;

			// For each offset between the start and end, set positions and apply any distortion
			for (var i = 1; i < numSegments; i++)
			{
				// If initialising or source/target have moved set segment base positions
				if (ticks == 0 || lastSource != source || target != lastTarget)
				{
					var segmentStart = direction / numSegments * i;
					offsets[i] = source + segmentStart + distortions[i];
				}

				// Apply distortion to each offset.
				if (shouldDistort)
				{
					var angle = WAngle.FromDegrees(random.Next(360));
					var distortion = random.Next(ticks > 0 ? info.DistortionAnimation : info.Distortion);

					var distOffset = distortion * angle.Cos() * leftVector / (1024 * 1024)
						+ distortion * angle.Sin() * upVector / (1024 * 1024);

					offsets[i] += distOffset;
					distortions[i] += distOffset;
				}
			}
		}

		public void Tick(World world)
		{
			source = args.CurrentSource();
			TrackTarget();
			CheckBlocked();

			if (++ticks >= info.Duration)
				world.AddFrameEndTask(w => w.Remove(this));
			else
				CalculateBeam(target - source);

			lastSource = source;
			lastTarget = target;
		}

		// workaround to stop closer targets resulting in beam with lower alpha
		private int GetRangeBonusAlpha(WVec direction)
		{
			var range = direction.Length;
			var alphaIncreaser = 5120 - range;
			if (alphaIncreaser > 0)
				return alphaIncreaser / 200;

			return 0;
		}

		public IEnumerable<IRenderable> Render(WorldRenderer worldRenderer)
		{
			if (worldRenderer.World.FogObscures(target) &&
				worldRenderer.World.FogObscures(source))
				yield break;

			if (ticks < info.Duration)
			{
				if (info.CenterBeam)
				{
					var rc = Color.FromArgb((info.Duration - ticks) * info.CenterBeamColor.A / info.Duration, info.CenterBeamColor);
					yield return new BeamRenderable(source, info.ZOffset + 2, target - source, info.CenterBeamShape, info.CenterBeamWidth, rc);

					if (info.SecondaryCenterBeam)
					{
						var src = Color.FromArgb((info.Duration - ticks) * info.SecondaryCenterBeamColor.A / info.Duration, info.SecondaryCenterBeamColor);
						yield return new BeamRenderable(source, info.ZOffset + 1, target - source,
							info.CenterBeamShape, info.SecondaryCenterBeamWidth, src);
					}
				}

				for (var i = 0; i < offsets.Length - 1; i++)
					for (var j = 0; j < info.Radius; j++)
						yield return new KKNDLaserRenderable(offsets, info.ZOffset, new WDist(32 + (info.Radius - j - 1) * 64), colors[j]);
			}
		}
	}
}
