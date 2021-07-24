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

		public IProjectile Create(ProjectileArgs args) { return new PlasmaBeam(args, this); }
	}

	public class PlasmaBeam : IProjectile, ISync
	{
		private readonly PlasmaBeamInfo info;
		private readonly Color[] colors;
		private WPos[] offsets;

		readonly ProjectileArgs args;
		private WVec leftVector;
		private WVec upVector;
		readonly MersenneTwister random;
		readonly bool hasLaunchEffect;

		[Sync]
		WPos target;

		[Sync]
		WPos source;

		[Sync]
		WPos lastSource;

		int ticks;

		public PlasmaBeam(ProjectileArgs args, PlasmaBeamInfo info)
		{
			this.args = args;
			this.info = info;

			source = args.Source;
			lastSource = args.Source;

			if (info.ForceVertical)
				target = new WPos(source.X, source.Y, 0);
			else
				target = args.PassiveTarget;

			var world = args.SourceActor.World;

			if (info.Inaccuracy.Length > 0)
			{
				var maxInaccuracyOffset = OpenRA.Mods.Common.Util.GetProjectileInaccuracy(info.Inaccuracy.Length, info.InaccuracyType, args);
				target += WVec.FromPDF(args.SourceActor.World.SharedRandom, 2) * maxInaccuracyOffset / 1024;
			}

			// Check for blocking actors
			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(world, source, target, info.CenterBeamWidth, out var blockedPos))
				target = blockedPos;

			var direction = target - source;
			var rangeBonusAlpha = GetRangeBonusAlpha(direction);

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

			if (info.Distortion != 0 || info.DistortionAnimation != 0)
				random = args.SourceActor.World.SharedRandom;

			CalculateMainBeam(direction);

			if (ticks < 1)
			{
				var warheadArgs = new WarheadArgs(args)
				{
					ImpactOrientation = new WRot(WAngle.Zero, OpenRA.Mods.Common.Util.GetVerticalAngle(source, target), args.CurrentMuzzleFacing()),
					ImpactPosition = target,
				};

				args.Weapon.Impact(Target.FromPos(target), warheadArgs);
			}

			hasLaunchEffect = !string.IsNullOrEmpty(info.LaunchEffectImage) && !string.IsNullOrEmpty(info.LaunchEffectSequence);
		}

		private void CalculateMainBeam(WVec direction)
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

			if (info.SegmentLength == WDist.Zero)
				offsets = new[] { source, target };
			else
			{
				var numSegments = (direction.Length - 1) / info.SegmentLength.Length + 1;
				offsets = new WPos[numSegments + 1];
				offsets[0] = source;
				offsets[offsets.Length - 1] = target;

				for (var i = 1; i < numSegments; i++)
				{
					var segmentStart = direction / numSegments * i;
					offsets[i] = source + segmentStart;

					if (info.Distortion != 0)
					{
						var angle = WAngle.FromDegrees(random.Next(360));
						var distortion = random.Next(info.Distortion);

						var offset = distortion * angle.Cos() * leftVector / (1024 * 1024)
							+ distortion * angle.Sin() * upVector / (1024 * 1024);

						offsets[i] += offset;
					}
				}
			}
		}

		public void Tick(World world)
		{
			source = args.CurrentSource();

			if (hasLaunchEffect && ticks == 0)
				world.AddFrameEndTask(w => w.Add(new SpriteEffect(args.CurrentSource, args.CurrentMuzzleFacing, world,
					info.LaunchEffectImage, info.LaunchEffectSequence, info.LaunchEffectPalette)));

			// Check for blocking actors
			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(world, source, target, info.CenterBeamWidth, out var blockedPos))
				target = blockedPos;

			if (++ticks >= info.Duration)
				world.AddFrameEndTask(w => w.Remove(this));
			else if (info.DistortionAnimation != 0)
			{
				if (source != lastSource)
					CalculateMainBeam(target - source);
				else
				{
					offsets[0] = source;

					for (var i = 1; i < offsets.Length - 1; i++)
					{
						var angle = WAngle.FromDegrees(random.Next(360));
						var distortion = random.Next(info.DistortionAnimation);

						var offset = distortion * angle.Cos() * leftVector / (1024 * 1024)
							+ distortion * angle.Sin() * upVector / (1024 * 1024);

						offsets[i] += offset;
					}
				}
			}

			lastSource = source;
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
