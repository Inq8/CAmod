#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

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

namespace OpenRA.Mods.CA.Projectiles
{
	[Desc("Not a sprite, but an engine effect.",
		"CA version adds to ZOffset for targets below the source.")]
	public class LaserZapCAInfo : IProjectileInfo
	{
		[Desc("The width of the zap.")]
		public readonly WDist Width = new WDist(86);

		[Desc("The shape of the beam.  Accepts values Cylindrical or Flat.")]
		public readonly BeamRenderableShape Shape = BeamRenderableShape.Cylindrical;

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset = 0;

		[Desc("The maximum duration (in ticks) of the beam's existence.")]
		public readonly int Duration = 10;

		[Desc("Total time-frame in ticks that the beam deals damage every DamageInterval.")]
		public readonly int DamageDuration = 1;

		[Desc("The number of ticks between the beam causing warhead impacts in its area of effect.")]
		public readonly int DamageInterval = 1;

		public readonly bool UsePlayerColor = false;

		[Desc("Color of the beam.")]
		public readonly Color Color = Color.Red;

		[Desc("Beam follows the target.")]
		public readonly bool TrackTarget = true;

		[Desc("The maximum/constant/incremental inaccuracy used in conjunction with the InaccuracyType property.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Controls the way inaccuracy is calculated. Possible values are " +
			"'Maximum' - scale from 0 to max with range, " +
			"'PerCellIncrement' - scale from 0 with range, " +
			"'Absolute' - use set value regardless of range.")]
		public readonly InaccuracyType InaccuracyType = InaccuracyType.Maximum;

		[Desc("Beam can be blocked.")]
		public readonly bool Blockable = false;

		[Desc("Draw a second beam (for 'glow' effect).")]
		public readonly bool SecondaryBeam = false;

		[Desc("The width of the zap.")]
		public readonly WDist SecondaryBeamWidth = new WDist(86);

		[Desc("The shape of the beam.  Accepts values Cylindrical or Flat.")]
		public readonly BeamRenderableShape SecondaryBeamShape = BeamRenderableShape.Cylindrical;

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int SecondaryBeamZOffset = 0;

		public readonly bool SecondaryBeamUsePlayerColor = false;

		[Desc("Color of the secondary beam.")]
		public readonly Color SecondaryBeamColor = Color.Red;

		[Desc("Impact animation.")]
		public readonly string HitAnim = null;

		[SequenceReference(nameof(HitAnim), allowNullImage: true)]
		[Desc("Sequence of impact animation to use.")]
		public readonly string HitAnimSequence = "idle";

		[PaletteReference]
		public readonly string HitAnimPalette = "effect";

		[Desc("Image containing launch effect sequence.")]
		public readonly string LaunchEffectImage = null;

		[SequenceReference(nameof(LaunchEffectImage), allowNullImage: true)]
		[Desc("Launch effect sequence to play.")]
		public readonly string LaunchEffectSequence = null;

		[PaletteReference]
		[Desc("Palette to use for launch effect.")]
		public readonly string LaunchEffectPalette = "effect";

		[Desc("If true, projectile will pass through targets to max range (subject to minimum distance).")]
		public readonly bool PassthroughToMaxRange = false;

		[Desc("Target must be this far away for a full passthrough, otherwise the projectile stops at the target.")]
		public readonly WDist PassthroughMinDistance = WDist.Zero;

		[Desc("If true, full passthroughs will travel parallel to the weapon muzzle offset.")]
		public readonly bool PassthroughParallelToMuzzleOffset = false;

		public IProjectile Create(ProjectileArgs args)
		{
			var c = UsePlayerColor ? args.SourceActor.OwnerColor() : Color;
			return new LaserZapCA(this, args, c);
		}
	}

	public class LaserZapCA : IProjectile, ISync
	{
		readonly ProjectileArgs args;
		readonly LaserZapCAInfo info;
		readonly Animation hitanim;
		readonly Color color;
		readonly Color secondaryColor;
		readonly bool hasLaunchEffect;
		int ticks;
		int interval;
		bool showHitAnim;

		[Sync]
		WPos target;

		[Sync]
		WPos source;

		public LaserZapCA(LaserZapCAInfo info, ProjectileArgs args, Color color)
		{
			this.args = args;
			this.info = info;
			this.color = color;
			secondaryColor = info.SecondaryBeamUsePlayerColor ? args.SourceActor.OwnerColor() : info.SecondaryBeamColor;
			target = args.PassiveTarget;
			source = args.Source;

			if (info.Inaccuracy.Length > 0)
			{
				var maxInaccuracyOffset = Common.Util.GetProjectileInaccuracy(info.Inaccuracy.Length, info.InaccuracyType, args);
				target += WVec.FromPDF(args.SourceActor.World.SharedRandom, 2) * maxInaccuracyOffset / 1024;
			}

			if (!string.IsNullOrEmpty(info.HitAnim))
			{
				hitanim = new Animation(args.SourceActor.World, info.HitAnim);
				showHitAnim = true;
			}

			hasLaunchEffect = !string.IsNullOrEmpty(info.LaunchEffectImage) && !string.IsNullOrEmpty(info.LaunchEffectSequence);

			if (info.PassthroughToMaxRange && (info.PassthroughMinDistance.Length == 0 || (target - args.Source).Length >= info.PassthroughMinDistance.Length))
			{
				var rangeModifiers = args.SourceActor.TraitsImplementing<IRangeModifier>().ToArray().Select(m => m.GetRangeModifier());
			 	var weaponRange = new WDist(Common.Util.ApplyPercentageModifiers(args.Weapon.Range.Length, rangeModifiers));
				var speed = new WVec(0, -weaponRange.Length, 0);

				if (info.PassthroughParallelToMuzzleOffset)
				{
					var offsetFromCenter = args.Source - args.SourceActor.CenterPosition;
					target += offsetFromCenter;
				}

				var fullRangeVector = speed.Rotate(WRot.FromYaw((target - args.Source).Yaw));
				target = args.Source + fullRangeVector;
			}
		}

		public void Tick(World world)
		{
			source = args.CurrentSource();

			if (hasLaunchEffect && ticks == 0)
				world.AddFrameEndTask(w => w.Add(new SpriteEffect(args.CurrentSource, args.CurrentMuzzleFacing, world,
					info.LaunchEffectImage, info.LaunchEffectSequence, info.LaunchEffectPalette)));

			// Beam tracks target
			if (info.TrackTarget && args.GuidedTarget.IsValidFor(args.SourceActor))
				target = args.Weapon.TargetActorCenter ? args.GuidedTarget.CenterPosition : args.GuidedTarget.Positions.ClosestToIgnoringPath(source);

			// Check for blocking actors
			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(world, args.SourceActor.Owner, source, target, info.Width, out var blockedPos))
			{
				target = blockedPos;
			}

			if (ticks < info.DamageDuration && --interval <= 0)
			{
				var warheadArgs = new WarheadArgs(args)
				{
					ImpactOrientation = new WRot(WAngle.Zero, OpenRA.Mods.Common.Util.GetVerticalAngle(source, target), args.CurrentMuzzleFacing()),
					ImpactPosition = target,
				};

				args.Weapon.Impact(Target.FromPos(target), warheadArgs);
				interval = info.DamageInterval;
			}

			if (showHitAnim)
			{
				if (ticks == 0)
					hitanim.PlayThen(info.HitAnimSequence, () => showHitAnim = false);

				hitanim.Tick();
			}

			if (++ticks >= info.Duration && !showHitAnim)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (wr.World.FogObscures(target) &&
				wr.World.FogObscures(source))
				yield break;

			if (ticks < info.Duration)
			{
				var zOffset = info.ZOffset;
				var secondaryBeamZOffset = info.SecondaryBeamZOffset;
				var verticalDiff = target.Y - args.Source.Y;
				if (verticalDiff > 0)
				{
					zOffset += verticalDiff;
					secondaryBeamZOffset += verticalDiff;
				}

				var rc = Color.FromArgb((info.Duration - ticks) * color.A / info.Duration, color);
				yield return new BeamRenderable(source, zOffset, target - source, info.Shape, info.Width, rc);

				if (info.SecondaryBeam)
				{
					var src = Color.FromArgb((info.Duration - ticks) * secondaryColor.A / info.Duration, secondaryColor);
					yield return new BeamRenderable(source, secondaryBeamZOffset, target - source,
						info.SecondaryBeamShape, info.SecondaryBeamWidth, src);
				}
			}

			if (showHitAnim)
				foreach (var r in hitanim.Render(target, wr.Palette(info.HitAnimPalette)))
					yield return r;
		}
	}
}
