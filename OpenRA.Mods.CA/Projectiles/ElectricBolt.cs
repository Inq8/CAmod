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
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Projectiles
{
	[Desc("Not a sprite, but an engine effect.")]
	public class ElectricBoltInfo : IProjectileInfo
	{
		[Desc("The width of the zap.")]
		public readonly WDist Width = new WDist(12);

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset = 0;

		[Desc("Beam can be blocked.")]
		public readonly bool Blockable = false;

		[Desc("The maximum duration (in ticks) of the beam's existence.")]
		public readonly int Duration = 5;

		[Desc("The maximum/constant/incremental inaccuracy used in conjunction with the InaccuracyType property.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Controls the way inaccuracy is calculated. Possible values are 'Maximum' - scale from 0 to max with range, 'PerCellIncrement' - scale from 0 with range and 'Absolute' - use set value regardless of range.")]
		public readonly InaccuracyType InaccuracyType = InaccuracyType.Maximum;

		[Desc("Colors of the zaps. The amount of zaps are the amount of colors listed here and PlayerColorZaps.")]
		public readonly Color[] Colors =
		{
			Color.FromArgb(80, 80, 255),
			Color.FromArgb(80, 80, 255),
			Color.FromArgb(255, 255, 255)
		};

		[Desc("Additional zaps colored with the player's color.")]
		public readonly int PlayerColorZaps = 0;

		[Desc("Initial distortion offset.")]
		public readonly int Distortion = 0;

		[Desc("Distortion added per tick for duration of beam.")]
		public readonly int DistortionAnimation = 0;

		[Desc("The maximum angle of the arc of the bolt.")]
		public readonly WAngle Angle = WAngle.FromDegrees(90);

		[Desc("Maximum length per segment.")]
		public readonly WDist SegmentLength = new WDist(320);

		[Desc("Image containing launch effect sequence.")]
		public readonly string LaunchEffectImage = null;

		[Desc("Launch effect sequence to play.")]
		[SequenceReference(nameof(LaunchEffectImage), allowNullImage: true)]
		public readonly string LaunchEffectSequence = null;

		[Desc("Palette to use for launch effect.")]
		[PaletteReference]
		public readonly string LaunchEffectPalette = "effect";

		[Desc("Does the beam follow the target.")]
		public readonly bool TrackTarget = false;

		public IProjectile Create(ProjectileArgs args)
		{
			return new ElectricBolt(this, args);
		}
	}

	public class ElectricBolt : IProjectile, ISync
	{
		readonly ElectricBoltInfo info;
		readonly ProjectileArgs args;
		readonly MersenneTwister random;
		readonly HashSet<(Color Color, WPos[] Positions, WVec[] Distortions)> zaps;
		readonly bool hasLaunchEffect;
		readonly int numSegments;

		int ticks = 0;
		WVec leftVector;
		WVec upVector;
		WVec inaccuracyOffset;

		[Sync]
		WPos target, source, lastTarget, lastSource;

		public ElectricBolt(ElectricBoltInfo info, ProjectileArgs args)
		{
			this.args = args;
			this.info = info;

			var playerColors = args.SourceActor.Owner.Color;
			var colors = info.Colors;
			for (int i = 0; i < info.PlayerColorZaps; i++)
				colors.Append(playerColors);

			source = lastSource = args.Source;
			target = lastTarget = args.PassiveTarget;
			random = args.SourceActor.World.SharedRandom;

			// Apply inaccuracy to target
			if (info.Inaccuracy.Length > 0)
			{
				var maxInaccuracyOffset = OpenRA.Mods.Common.Util.GetProjectileInaccuracy(info.Inaccuracy.Length, info.InaccuracyType, args);
				inaccuracyOffset = WVec.FromPDF(random, 2) * maxInaccuracyOffset / 1024;
				target += inaccuracyOffset;
			}

			var direction = target - source;
			numSegments = (direction.Length - 1) / info.SegmentLength.Length + 1;

			zaps = new HashSet<(Color, WPos[], WVec[])>();
			foreach (var c in colors)
			{
				var numSegments = (direction.Length - 1) / info.SegmentLength.Length + 1;
				var offsets = new WPos[numSegments + 1];
				var distortions = new WVec[numSegments + 1];

				var angle = new WAngle((-info.Angle.Angle / 2) + random.Next(info.Angle.Angle));

				for (var i = 1; i < numSegments; i++)
					offsets[i] = WPos.LerpQuadratic(source, target, angle, i, numSegments);

				zaps.Add((c, offsets, distortions));
			}

			CalculateDistortion(direction);
			CalculateBeam(direction);

			// Do the beam impact (warheads)
			var warheadArgs = new WarheadArgs(args)
			{
				ImpactOrientation = new WRot(WAngle.Zero, Common.Util.GetVerticalAngle(source, target), args.CurrentMuzzleFacing()),
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
			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(args.SourceActor.World, source, target, info.Width, out var blockedPos))
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

			foreach (var zap in zaps)
			{
				var offsets = zap.Positions;
				var distortions = zap.Distortions;
				offsets[0] = source;
				offsets[offsets.Length - 1] = target;

				for (var i = 1; i < offsets.Length - 1; i++)
				{
					// If initialising or source/target have moved set segment base positions
					if (ticks == 0 || lastSource != source || target != lastTarget)
					{
						var segmentStart = direction / numSegments * i;
						offsets[i] = source + segmentStart + distortions[i];
					}

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

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (wr.World.FogObscures(target) &&
				wr.World.FogObscures(source))
				yield break;

			if (ticks < info.Duration)
			{
				foreach (var zap in zaps)
				{
					var offsets = zap.Positions;
					yield return new ElectricBoltRenderable(offsets, info.ZOffset, info.Width, zap.Color);
				}
			}
		}
	}
}
