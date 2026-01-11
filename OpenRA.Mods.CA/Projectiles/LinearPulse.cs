#region Copyright & License Information
/*
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
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Projectiles
{
	public enum LinearPulseImpactType
	{
		StandardImpact,
		Rectangle,
		Cone,
		Trapezoid
	}

	public enum FalloffBasis
	{
		DistanceFromCenter,
		DistanceFromCenterLine,
		DistanceFromSource
	}

	public enum LinearPulseForceGroundType
	{
		None,
		DamageOnly,
		DamageAndVisual
	}

	public class ProjectileAnimation
	{
		[Desc("Projectile animation image.")]
		public readonly string Image = null;

		[Desc("Sequences of projectile animation to use, one will be picked randomly for each shot.")]
		[SequenceReference(nameof(Image), allowNullImage: true)]
		public readonly string[] Sequences = { "idle" };

		[PaletteReference]
		[Desc("Palette to use for the projectile animation.")]
		public readonly string Palette = "effect";

		[Desc("Visual speed of this projectile animation. Set to zero to use the same speed as the pulse.")]
		public readonly WDist Speed = WDist.Zero;

		[Desc("Maximum distance travelled by this projectile animation. Zero falls back to weapon range.")]
		public readonly WDist Range = WDist.Zero;

		[Desc("Delay before this projectile animation starts.")]
		public readonly int[] Delay = { 0 };

		[Desc("Does this projectile animation have a shadow?")]
		public readonly bool Shadow = false;

		[Desc("Should this projectile animation repeat?")]
		public readonly bool RepeatAnimation = true;

		[PaletteReference]
		[Desc("Palette to use for this projectile animation's shadow if Shadow is true.")]
		public readonly string ShadowPalette = "shadow";

		[Desc("Offset of the projectile target.")]
		public readonly WVec TargetOffset = WVec.Zero;

		[Desc("The maximum/constant/incremental inaccuracy used in conjunction with the InaccuracyType property.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Controls the way inaccuracy is calculated. Possible values are " +
			"'Maximum' - scale from 0 to max with range, " +
			"'PerCellIncrement' - scale from 0 with range, " +
			"'Absolute' - use set value regardless of range.")]
		public readonly InaccuracyType InaccuracyType = InaccuracyType.Maximum;

		[Desc("When set, display a line behind the actor. Length is measured in ticks after appearing.")]
		public readonly int ContrailLength = 0;

		[Desc("Time (in ticks) after which the line should appear. Controls the distance to the actor.")]
		public readonly int ContrailDelay = 1;

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ContrailZOffset = 2047;

		[Desc("Thickness of the emitted line at the start of the contrail.")]
		public readonly WDist ContrailStartWidth = new(64);

		[Desc("Thickness of the emitted line at the end of the contrail. Will default to " + nameof(ContrailStartWidth) + " if left undefined")]
		public readonly WDist? ContrailEndWidth = null;

		[Desc("RGB color at the contrail start.")]
		public readonly Color ContrailStartColor = Color.White;

		[Desc("Use player remap color instead of a custom color at the contrail the start.")]
		public readonly bool ContrailStartColorUsePlayerColor = false;

		[Desc("The alpha value [from 0 to 255] of color at the contrail the start.")]
		public readonly int ContrailStartColorAlpha = 255;

		[Desc("RGB color at the contrail end. Set to start color if undefined")]
		public readonly Color? ContrailEndColor;

		[Desc("Use player remap color instead of a custom color at the contrail end.")]
		public readonly bool ContrailEndColorUsePlayerColor = false;

		[Desc("The alpha value [from 0 to 255] of color at the contrail end.")]
		public readonly int ContrailEndColorAlpha = 0;

		[Desc("Final impact animation.")]
		public readonly string HitAnim = null;

		[SequenceReference(nameof(HitAnim), allowNullImage: true)]
		[Desc("Sequence of impact animation to use.")]
		public readonly string HitAnimSequence = "idle";

		[PaletteReference]
		public readonly string HitAnimPalette = "effect";

		/// <summary>
		/// This constructor is used solely for documentation generation.
		/// </summary>
		public ProjectileAnimation() { }

		public ProjectileAnimation(MiniYaml content)
		{
			FieldLoader.Load(this, content);
		}
	}

	public class ImpactAnimation
	{
		[Desc("Impact animation image.")]
		public readonly string Image = null;

		[Desc("Sequences of impact animation to use, one will be picked randomly for each impact.")]
		[SequenceReference(nameof(Image), allowNullImage: true)]
		public readonly string[] Sequences = { "idle" };

		[PaletteReference]
		[Desc("Palette to use for the impact animation.")]
		public readonly string Palette = "effect";

		/// <summary>
		/// This constructor is used solely for documentation generation.
		/// </summary>
		public ImpactAnimation() { }

		public ImpactAnimation(MiniYaml content)
		{
			FieldLoader.Load(this, content);
		}
	}

	public class DamageFalloff
	{
		[Desc("Damage modifier applied at each range step for Rectangle, Cone, and Trapezoid impact types.")]
		public readonly int[] Falloff = { 100, 100 };

		[Desc("Range between falloff steps for Rectangle, Cone, and Trapezoid impact types.")]
		public readonly WDist Spread = new(43);

		[Desc("Ranges at which each Falloff step is defined for Rectangle and Cone impact types. Overrides Spread.")]
		public readonly WDist[] Range = null;

		[Desc("Determines what distance is used for damage falloff calculation for Rectangle and Cone impact types.")]
		public readonly FalloffBasis FalloffBasis = FalloffBasis.DistanceFromCenterLine;

		[Desc("Controls the way damage is calculated. Possible values are 'HitShape', 'ClosestTargetablePosition' and 'CenterPosition'.")]
		public readonly DamageCalculationType DamageCalculationType = DamageCalculationType.HitShape;

		/// <summary>
		/// This constructor is used solely for documentation generation.
		/// </summary>
		public DamageFalloff() { }

		public DamageFalloff(MiniYaml content)
		{
			FieldLoader.Load(this, content);
		}
	}

	public class LinearPulseInfo : IProjectileInfo
	{
		[Desc("Type of impact for the pulse.")]
		public readonly LinearPulseImpactType ImpactType = LinearPulseImpactType.StandardImpact;

		[Desc("Rectangle impact area length (parallel to the projectile path). Used with Rectangle impact type.")]
		public readonly WDist RectangleLength = new(1024);

		[Desc("Rectangle impact area width (perpendicular to the projectile path). Used with Rectangle impact type.")]
		public readonly WDist RectangleWidth = new(1024);

		[Desc("Cone angle in degrees for cone impact type. Used with Cone impact type.")]
		public readonly int ConeAngle = 90;

		[Desc("Length of each cone segment for cone impact type. Used with Cone impact type.")]
		public readonly WDist ConeSegmentLength = new(512);

		[Desc("Starting width of trapezoid at source position. Used with Trapezoid impact type.")]
		public readonly WDist TrapezoidStartWidth = new(512);

		[Desc("Ending width of trapezoid at maximum range. Used with Trapezoid impact type.")]
		public readonly WDist TrapezoidEndWidth = new(2048);

		[Desc("Length of each trapezoid segment. Used with Trapezoid impact type.")]
		public readonly WDist TrapezoidSegmentLength = new(512);

		[Desc("Distance between pulse impacts. If zero, defaults to Speed")]
		public readonly WDist ImpactInterval = WDist.Zero;

		[Desc("Speed the pulse travels.")]
		public readonly WDist Speed = WDist.FromCells(6);

		[Desc("Minimum distance travelled before doing damage.")]
		public readonly WDist MinimumImpactDistance = WDist.Zero;

		[Desc("Maximum distance travelled after which no more damage occurs. Zero falls back to weapon range.")]
		public readonly WDist MaximumImpactDistance = WDist.Zero;

		[Desc("Whether to ignore range modifiers, as these can mess up the relationship between ImpactInterval, Speed and max range.")]
		public readonly bool IgnoreRangeModifiers = true;

		[Desc("The maximum/constant/incremental inaccuracy used in conjunction with the InaccuracyType property.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Controls the way inaccuracy is calculated. Possible values are " +
			"'Maximum' - scale from 0 to max with range, " +
			"'PerCellIncrement' - scale from 0 with range, " +
			"'Absolute' - use set value regardless of range.")]
		public readonly InaccuracyType InaccuracyType = InaccuracyType.Maximum;

		[Desc("Whether to force the pulse to ground level. Possible values are " +
			"'None' - neither damage nor visuals are forced to ground level, " +
			"'DamageOnly' - damage is forced to ground level, but visuals are not, " +
			"'DamageAndVisual' - the damage and visual effects (projectile and impact animations) are forced to ground level.")]
		public readonly LinearPulseForceGroundType ForceGround = LinearPulseForceGroundType.None;

		[Desc("If true (and not using Warhead impact type) then the same actor can only be impacted once.")]
		public readonly bool SingleHitPerActor = false;

		[Desc("The minimum range at which friendly actors can be impacted (relative to source).")]
		public readonly WDist MinimumFriendlyFireRange = WDist.Zero;

		[Desc("Is this blocked by actors with BlocksProjectiles trait.")]
		public readonly bool Blockable = false;

		[Desc("Width of projectile (used for finding blocking actors).")]
		public readonly WDist BlockableWidth = new(1);

		[Desc("Size of the area. A smudge will be created in each tile.", "Provide 2 values for a ring effect (outer/inner).")]
		public readonly int[] SmudgeSize = { 0, 0 };

		[Desc("Type of smudge to apply to terrain.")]
		public readonly HashSet<string> SmudgeType = new();

		[Desc("Percentage chance the smudge is created.")]
		public readonly int SmudgeChance = 100;

		[FieldLoader.LoadUsing(nameof(LoadImpactAnimations))]
		public readonly List<ImpactAnimation> ImpactAnimations = new();

		public IProjectile Create(ProjectileArgs args) { return new LinearPulse(this, args); }

		static object LoadImpactAnimations(MiniYaml yaml)
		{
			var retList = new List<ImpactAnimation>();
			foreach (var node in yaml.Nodes.Where(n => n.Key.StartsWith("ImpactAnimation", StringComparison.Ordinal)))
			{
				var impactAnim = new ImpactAnimation(node.Value);
				retList.Add(impactAnim);
			}

			return retList;
		}

		[FieldLoader.LoadUsing(nameof(LoadProjectileAnimations))]
		public readonly List<ProjectileAnimation> ProjectileAnimations = new();

		static object LoadProjectileAnimations(MiniYaml yaml)
		{
			var retList = new List<ProjectileAnimation>();
			foreach (var node in yaml.Nodes.Where(n => n.Key.StartsWith("ProjectileAnimation", StringComparison.Ordinal)))
			{
				var projectileAnim = new ProjectileAnimation(node.Value);
				retList.Add(projectileAnim);
			}

			return retList;
		}

		[FieldLoader.LoadUsing(nameof(LoadDamageFalloffs))]
		public readonly List<DamageFalloff> DamageFalloffs = new();

		static object LoadDamageFalloffs(MiniYaml yaml)
		{
			var retList = new List<DamageFalloff>();
			foreach (var node in yaml.Nodes.Where(n => n.Key.StartsWith("DamageFalloff", StringComparison.Ordinal)))
			{
				var falloff = new DamageFalloff(node.Value);
				retList.Add(falloff);
			}

			return retList;
		}

		// To suppress errors
		public readonly ImpactAnimation ImpactAnimation = null;
		public readonly DamageFalloff DamageFalloff = null;
		public readonly ProjectileAnimation ProjectileAnimation = null;
	}

	public class ProjectileAnimationState
	{
		public readonly ProjectileAnimation Config;
		public readonly Animation Animation;
		public readonly WVec DirectionalSpeed;
		public readonly int Range;
		public readonly ContrailRenderable Contrail;
		public WPos Position;
		public int TotalDistanceTravelled;
		public bool TravelComplete;
		public int RemainingDelay;
		public bool HitAnimationCreated;

		public ProjectileAnimationState(ProjectileAnimation config, Animation animation, WVec directionalSpeed, int range, WPos startPos, ContrailRenderable contrail = null)
		{
			Config = config;
			Animation = animation;
			DirectionalSpeed = directionalSpeed;
			Range = range;
			Position = startPos;
			Contrail = contrail;
			TotalDistanceTravelled = 0;
			TravelComplete = false;
			RemainingDelay = 0;
			HitAnimationCreated = false;
		}

		public void UpdatePosition(bool blocked)
		{
			if (RemainingDelay > 0)
			{
				RemainingDelay--;
				return;
			}

			if (!TravelComplete && !blocked)
			{
				var remainingRange = Range - TotalDistanceTravelled;
				var speedLength = DirectionalSpeed.Length;

				if (remainingRange <= 0)
				{
					TravelComplete = true;
				}
				else if (speedLength <= remainingRange)
				{
					Position += DirectionalSpeed;
					TotalDistanceTravelled += speedLength;
				}
				else
				{
					var partialMove = DirectionalSpeed * remainingRange / speedLength;
					Position += partialMove;
					TotalDistanceTravelled = Range;
					TravelComplete = true;
				}
			}

			TravelComplete = TravelComplete || TotalDistanceTravelled >= Range || blocked;
		}

		public void SetBlockedPosition(WPos blockedPos, WPos source)
		{
			Position = blockedPos;
			TotalDistanceTravelled = (blockedPos - source).Length;
		}
	}

	public class LinearPulse : IProjectile, ISync
	{
		readonly LinearPulseInfo info;
		readonly ProjectileArgs args;
		readonly WVec speed;
		readonly WVec directionalSpeed;
		readonly WDist impactInterval;

		readonly WAngle facing;
		readonly List<ProjectileAnimationState> projectileAnimations = new();

		[Sync]
		WPos pos, target, source;
		int ticks;
		int totalDistanceTravelled;
		bool travelComplete;
		bool blocked;
		readonly int range;
		readonly WPos[] impactPositions;
		readonly HashSet<Actor> impactedActors = new();

		public Actor SourceActor { get { return args.SourceActor; } }

		public LinearPulse(LinearPulseInfo info, ProjectileArgs args)
		{
			this.info = info;
			this.args = args;

			speed = new WVec(0, -info.Speed.Length, 0);

			impactInterval = info.ImpactInterval > WDist.Zero ? info.ImpactInterval : info.Speed;

			source = args.Source;

			var world = args.SourceActor.World;

			// projectile starts at the source position
			pos = args.Source;

			// initially no distance has been travelled by the pulse
			totalDistanceTravelled = 0;

			// the weapon range (total distance to be travelled)
			range = info.MaximumImpactDistance.Length > args.Weapon.Range.Length ? info.MaximumImpactDistance.Length : args.Weapon.Range.Length;

			if (!info.IgnoreRangeModifiers)
			{
				range = Common.Util.ApplyPercentageModifiers(range, args.RangeModifiers);
			}

			target = args.PassiveTarget;

			// get the offset of the source compared to source actor's center and apply the same offset to the target (so the linear pulse always travels parallel to the source actor's facing)
			var offsetFromCenter = source - args.SourceActor.CenterPosition;
			target += offsetFromCenter;

			if (info.Inaccuracy.Length > 0)
			{
				var maxInaccuracyOffset = Common.Util.GetProjectileInaccuracy(info.Inaccuracy.Length, info.InaccuracyType, args);
				target += WVec.FromPDF(world.SharedRandom, 2) * maxInaccuracyOffset / 1024;
			}

			facing = (target - pos).Yaw;

			// calculate the vectors for travel
			directionalSpeed = speed.Rotate(WRot.FromYaw(facing));

			// calculate impact positions
			var impactCount = range / impactInterval.Length;
			var impactVector = new WVec(0, -impactInterval.Length, 0).Rotate(WRot.FromYaw(facing));

			impactPositions = Enumerable.Range(0, impactCount)
				.Select(i => pos + impactVector * (i + 1))
				.ToArray();

			foreach (var projectileAnim in info.ProjectileAnimations)
			{
				if (!string.IsNullOrEmpty(projectileAnim.Image))
				{
					var projectileRange = projectileAnim.Range == WDist.Zero ? range : projectileAnim.Range.Length;

					// calculate target with offset and inaccuracy for this animation
					// convert TargetOffset to forward, right, up format like Armament does
					var convertedOffset = new WVec(projectileAnim.TargetOffset.Y, -projectileAnim.TargetOffset.X, projectileAnim.TargetOffset.Z);
					var rotatedOffset = convertedOffset.Rotate(WRot.FromYaw(facing));

					// create a target at maximum range to ensure consistent offset spacing
					var maxRangeTarget = pos + new WVec(0, -projectileRange, 0).Rotate(WRot.FromYaw(facing));
					var animTarget = maxRangeTarget + rotatedOffset;
					var distanceToAnimTarget = (animTarget - pos).Length;

					if (projectileAnim.Inaccuracy.Length > 0)
					{
						var maxInaccuracyOffset = Common.Util.GetProjectileInaccuracy(projectileAnim.Inaccuracy.Length, projectileAnim.InaccuracyType, args);
						animTarget += WVec.FromPDF(world.SharedRandom, 2) * maxInaccuracyOffset / 1024;
					}

					var animFacing = (animTarget - pos).Yaw;

					var animation = new Animation(world, projectileAnim.Image, new Func<WAngle>(() => GetEffectiveFacingForAnimation(animFacing)));

					if (projectileAnim.RepeatAnimation)
						animation.PlayRepeating(projectileAnim.Sequences.Random(world.SharedRandom));
					else
						animation.Play(projectileAnim.Sequences.Random(world.SharedRandom));

					var projectileSpeed = projectileAnim.Speed != WDist.Zero ? new WVec(0, -projectileAnim.Speed.Length, 0) : speed;
					var projectileDirectionalSpeed = projectileSpeed.Rotate(WRot.FromYaw(animFacing));

					var animRange = distanceToAnimTarget;
					if (!info.IgnoreRangeModifiers)
					{
						animRange = Common.Util.ApplyPercentageModifiers(animRange, args.RangeModifiers);
					}

					ContrailRenderable contrail = null;
					if (projectileAnim.ContrailLength > 0)
					{
						var startcolor = projectileAnim.ContrailStartColorUsePlayerColor
							? Color.FromArgb(projectileAnim.ContrailStartColorAlpha, args.SourceActor.OwnerColor())
							: Color.FromArgb(projectileAnim.ContrailStartColorAlpha, projectileAnim.ContrailStartColor);

						var endcolor = projectileAnim.ContrailEndColorUsePlayerColor
							? Color.FromArgb(projectileAnim.ContrailEndColorAlpha, args.SourceActor.OwnerColor())
							: Color.FromArgb(projectileAnim.ContrailEndColorAlpha, projectileAnim.ContrailEndColor ?? startcolor);

						contrail = new ContrailRenderable(world, args.SourceActor,
							startcolor, projectileAnim.ContrailStartColorUsePlayerColor,
							endcolor, projectileAnim.ContrailEndColor == null ? projectileAnim.ContrailStartColorUsePlayerColor : projectileAnim.ContrailEndColorUsePlayerColor,
							projectileAnim.ContrailStartWidth,
							projectileAnim.ContrailEndWidth ?? projectileAnim.ContrailStartWidth,
							projectileAnim.ContrailLength, projectileAnim.ContrailDelay, projectileAnim.ContrailZOffset);
					}

					var visualGrounded = info.ForceGround == LinearPulseForceGroundType.DamageAndVisual;
					var initialPosition = visualGrounded ? new WPos(args.Source.X, args.Source.Y, 0) : args.Source;

					var animState = new ProjectileAnimationState(projectileAnim, animation, projectileDirectionalSpeed, animRange, initialPosition, contrail);

					if (projectileAnim.Delay.Length == 1)
						animState.RemainingDelay = projectileAnim.Delay[0];
					else if (projectileAnim.Delay.Length >= 2)
						animState.RemainingDelay = world.SharedRandom.Next(projectileAnim.Delay[0], projectileAnim.Delay[1] + 1);

					projectileAnimations.Add(animState);
				}
			}
		}

		public void Tick(World world)
		{
			foreach (var animState in projectileAnimations)
			{
				if (animState.RemainingDelay == 0)
					animState.Animation?.Tick();

				if (animState.Contrail != null && animState.RemainingDelay == 0)
					animState.Contrail.Update(animState.Position);
			}

			var lastPos = pos;

			if (!travelComplete && !blocked)
				pos += directionalSpeed;

			foreach (var animState in projectileAnimations)
				animState.UpdatePosition(blocked);

			if (info.Blockable && !blocked && BlocksProjectiles.AnyBlockingActorsBetween(world, args.SourceActor.Owner, lastPos, pos, info.BlockableWidth, out var blockedPos))
			{
				pos = blockedPos;
				blocked = true;
				totalDistanceTravelled = (blockedPos - source).Length;

				foreach (var animState in projectileAnimations)
					animState.SetBlockedPosition(blockedPos, source);
			}
			else
			{
				totalDistanceTravelled += info.Speed.Length;
			}

			if (!travelComplete)
			{
				for (var idx = 0; idx < impactPositions.Length; idx++)
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

			travelComplete = totalDistanceTravelled >= range || blocked;

			// Create hit animations for individual projectiles when they complete
			foreach (var animState in projectileAnimations)
			{
				if (animState.TravelComplete && !animState.HitAnimationCreated && !string.IsNullOrEmpty(animState.Config.HitAnim))
				{
					animState.HitAnimationCreated = true;
					var palette = animState.Config.HitAnimPalette;
					var animFacing = animState.DirectionalSpeed.Yaw;
					world.AddFrameEndTask(w => w.Add(new SpriteEffect(animState.Position, w, animState.Config.HitAnim, animState.Config.HitAnimSequence, palette)));
				}
			}

			// Check if all projectile animations are complete
			var allProjectileTravelComplete = projectileAnimations.Count == 0 || projectileAnimations.All(a => a.TravelComplete);

			if (travelComplete && allProjectileTravelComplete)
			{
				// Add contrail faders for all animations that have contrails
				foreach (var animState in projectileAnimations)
				{
					if (animState.Contrail != null)
						world.AddFrameEndTask(w => w.Add(new ContrailFader(animState.Position, animState.Contrail)));
				}

				world.AddFrameEndTask(w => w.Remove(this));
			}

			ticks++;
		}

		WAngle GetEffectiveFacingForAnimation(WAngle animFacing)
		{
			var angle = WAngle.Zero;
			var at = (float)ticks / (speed.Length - 1);
			var attitude = angle.Tan() * (1 - 2 * at) / (4 * 1024);

			var u = animFacing.Angle % 512 / 512f;
			var scale = 2048 * u * (1 - u);

			var effective = (int)(animFacing.Angle < 512
				? animFacing.Angle - scale * attitude
				: animFacing.Angle + scale * attitude);

			return new WAngle(effective);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			var world = args.SourceActor.World;

			foreach (var animState in projectileAnimations)
			{
				// Don't render animations that are still delayed
				if (animState.RemainingDelay > 0)
					continue;

				// Render contrail first if this animation has one
				if (animState.Contrail != null)
					yield return animState.Contrail;

				if (animState.Animation == null || animState.TravelComplete)
					continue;

				if (!world.FogObscures(animState.Position))
				{
					// Find the corresponding ProjectileAnimation config for this animation state
					var projectileAnim = animState.Config;
					if (animState.Config != null)
					{
						if (projectileAnim.Shadow)
						{
							var dat = world.Map.DistanceAboveTerrain(animState.Position);
							var shadowPos = animState.Position - new WVec(0, 0, dat.Length);
							foreach (var r in animState.Animation.Render(shadowPos, wr.Palette(projectileAnim.ShadowPalette)))
								yield return r;
						}

						var palette = wr.Palette(projectileAnim.Palette);
						foreach (var r in animState.Animation.Render(animState.Position, palette))
							yield return r;
					}
				}
			}
		}

		void Explode(WPos impactPos)
		{
			// Create hit animations if specified
			var world = args.SourceActor.World;
			var damageGrounded = info.ForceGround != LinearPulseForceGroundType.None;
			var visualGrounded = info.ForceGround == LinearPulseForceGroundType.DamageAndVisual;
			var impactPosForDamage = damageGrounded ? new WPos(impactPos.X, impactPos.Y, 0) : impactPos;
			var impactPosForVisual = visualGrounded ? new WPos(impactPos.X, impactPos.Y, 0) : impactPos;

			foreach (var impactAnim in info.ImpactAnimations)
			{
				if (!string.IsNullOrEmpty(impactAnim.Image))
				{
					// Pick a random sequence for this impact
					var sequence = impactAnim.Sequences.Random(world.SharedRandom);

					// Create SpriteEffect with facing
					world.AddFrameEndTask(w => w.Add(new SpriteEffect(impactPosForVisual, facing, w, impactAnim.Image, sequence, impactAnim.Palette)));
				}
			}

			if (info.SmudgeType.Any())
			{
				DoSmudge(Target.FromPos(impactPosForDamage), new WarheadArgs(args));
			}

			switch (info.ImpactType)
			{
				case LinearPulseImpactType.StandardImpact:
					args.Weapon.Impact(Target.FromPos(impactPosForDamage), new WarheadArgs(args));
					break;
				case LinearPulseImpactType.Rectangle:
					ExplodeRectangle(impactPosForDamage);
					break;
				case LinearPulseImpactType.Cone:
					ExplodeCone(impactPosForDamage);
					break;
				case LinearPulseImpactType.Trapezoid:
					ExplodeTrapezoid(impactPosForDamage);
					break;
			}
		}

		void ExplodeRectangle(WPos impactPos)
		{
			var world = args.SourceActor.World;

			// Calculate rectangle dimensions and orientation
			var halfLength = info.RectangleLength.Length / 2;
			var halfWidth = info.RectangleWidth.Length / 2;

			// Rectangle is oriented along the projectile's path
			var direction = target - source;
			if (direction.Length == 0)
				return;

			var forwardDir = direction * 1024 / direction.Length; // Normalized forward direction
			var rightDir = new WVec(-forwardDir.Y, forwardDir.X, 0); // Perpendicular direction

			// Calculate rectangle corners centered on impact position
			var lengthOffset = forwardDir * halfLength / 1024;
			var widthOffset = rightDir * halfWidth / 1024;

			var corner1 = impactPos - lengthOffset - widthOffset;
			var corner2 = impactPos - lengthOffset + widthOffset;
			var corner3 = impactPos + lengthOffset + widthOffset;
			var corner4 = impactPos + lengthOffset - widthOffset;

			// Debug visualization - use the actual rectangle corners for grounded visualization
			var debugVis = world.WorldActor.TraitOrDefault<DebugVisualizations>();
			if (debugVis != null && debugVis.CombatGeometry)
			{
				var caDebug = world.WorldActor.TraitOrDefault<WarheadDebugOverlayCA>();
				if (caDebug != null)
				{
					// Use grounded corners if ForceGround is set for damage
					WPos[] vizCorners;
					if (info.ForceGround != LinearPulseForceGroundType.None)
					{
						// Ground the corners for visualization to match damage area
						vizCorners = new WPos[]
						{
							new(corner1.X, corner1.Y, 0),
							new(corner2.X, corner2.Y, 0),
							new(corner3.X, corner3.Y, 0),
							new(corner4.X, corner4.Y, 0)
						};
					}
					else
					{
						vizCorners = new WPos[] { corner1, corner2, corner3, corner4 };
					}

					caDebug.AddPolygonOutline(vizCorners, Color.Red);
				}
			}

			// Find all actors intersecting with the rectangle
			var searchRadius = new WDist(Math.Max(halfLength, halfWidth) + 512); // Add buffer for safety
			var nearbyActors = world.FindActorsInCircle(impactPos, searchRadius);

			foreach (var actor in nearbyActors)
			{
				// Skip if SingleHitPerActor is enabled and we've already hit this actor
				if (info.SingleHitPerActor && impactedActors.Contains(actor))
					continue;

				// Skip friendly actors if they are too close to the source
				if (info.MinimumFriendlyFireRange > WDist.Zero)
				{
					var actorDistanceFromSource = (actor.CenterPosition - source).Length;
					if (actorDistanceFromSource < info.MinimumFriendlyFireRange.Length &&
						args.SourceActor.Owner.RelationshipWith(actor.Owner).HasRelationship(PlayerRelationship.Ally))
						continue;
				}

				// Check if actor intersects with the rectangle
				if (IsActorInRectangle(actor, corner1, corner2, corner3, corner4))
				{
					// Calculate falloff data for all configured DamageFalloff settings
					var falloffData = new List<(DamageFalloff Falloff, int FalloffDistance)>();

					// If no DamageFalloffs are configured, apply damage without any falloff
					if (info.DamageFalloffs.Count == 0)
					{
						ApplyDamageToActor(actor, impactPos, falloffData);
					}
					else
					{
						foreach (var damageFalloff in info.DamageFalloffs)
						{
							// Find the closest HitShape to the reference point for this falloff
							HitShape closestActiveShape = null;
							var closestDistance = int.MaxValue;

							// PERF: Avoid using TraitsImplementing<HitShape> that needs to find the actor in the trait dictionary.
							foreach (var targetPos in actor.EnabledTargetablePositions)
							{
								if (targetPos is HitShape h)
								{
									var referencePoint = GetReferencePoint(actor, impactPos, damageFalloff);
									var distance = h.DistanceFromEdge(actor, referencePoint).Length;
									if (distance < closestDistance)
									{
										closestDistance = distance;
										closestActiveShape = h;
									}
								}
							}

							// Cannot be damaged without an active HitShape for HitShape calculation type
							if (damageFalloff.DamageCalculationType == DamageCalculationType.HitShape && closestActiveShape == null)
								continue;

							// Calculate damage falloff distance based on the selected calculation type
							var falloffDistance = damageFalloff.DamageCalculationType switch
							{
								DamageCalculationType.HitShape => closestDistance,
								DamageCalculationType.ClosestTargetablePosition => actor.GetTargetablePositions()
									.Where(x => IsPointInRectangle(x, corner1, corner2, corner3, corner4))
									.DefaultIfEmpty(actor.CenterPosition)
									.Min(x => CalculateFalloffDistance(x, impactPos, damageFalloff)),
								DamageCalculationType.CenterPosition => CalculateFalloffDistance(actor.CenterPosition, impactPos, damageFalloff),
								_ => CalculateFalloffDistance(actor.CenterPosition, impactPos, damageFalloff)
							};

							falloffData.Add((damageFalloff, falloffDistance));
						}

						// If we have falloff data, apply damage
						if (falloffData.Count > 0)
						{
							ApplyDamageToActor(actor, impactPos, falloffData);
						}
					}

					if (info.SingleHitPerActor)
						impactedActors.Add(actor);
				}
			}
		}

		void ApplyDamageToActor(Actor actor, WPos impactPos, List<(DamageFalloff Falloff, int FalloffDistance)> falloffData)
		{
			// Calculate all damage modifiers from the falloffs
			var damageModifiers = new List<int>(args.DamageModifiers);

			foreach (var (falloff, distance) in falloffData)
			{
				var modifier = GetFalloffModifier(falloff, distance);
				damageModifiers.Add(modifier);
			}

			// If the sum of all modifiers is zero, we can skip applying damage
			if (damageModifiers.Sum() == 0)
				return;

			var impactOrientation = new WRot(WAngle.Zero, WAngle.Zero, facing);

			// If a warhead lands outside the victim's area, calculate impact angles
			// Use the first falloff's distance for impact angle calculation
			if (falloffData.Count > 0 && falloffData[0].FalloffDistance > 0)
			{
				var referencePoint = GetReferencePoint(actor, impactPos, falloffData[0].Falloff);
				var towardsTargetYaw = (actor.CenterPosition - referencePoint).Yaw;
				impactOrientation = new WRot(WAngle.Zero, WAngle.Zero, towardsTargetYaw);
			}

			var warheadArgs = new WarheadArgs(args)
			{
				ImpactOrientation = impactOrientation,
				ImpactPosition = actor.CenterPosition,
				DamageModifiers = damageModifiers.ToArray(),
			};

			args.Weapon.Impact(Target.FromActor(actor), warheadArgs);
		}

		void ExplodeCone(WPos impactPos)
		{
			var world = args.SourceActor.World;

			// Calculate distance from source to impact position (respecting damage grounding)
			var distanceFromSource = (impactPos - source).Length; // Grounded distance calculation

			// Calculate the radius of the cone at this distance
			var halfConeAngleRadians = info.ConeAngle * Math.PI / 360.0; // Convert degrees to radians and divide by 2

			// Calculate the cone segment boundaries, clamped to [0, range]
			var segmentHalf = info.ConeSegmentLength.Length / 2;
			var segmentStart = Math.Max(0, distanceFromSource - segmentHalf);
			var segmentEnd = Math.Min(range, distanceFromSource + segmentHalf);

			// In 2D top-down, the cone is a wedge from the apex: arcs are centered at the apex with radius equal to distance from source
			var startRadius = segmentStart;
			var endRadius = segmentEnd;
			var maxRadius = Math.Max(startRadius, endRadius);

			// Use the fixed projectile direction, not the direction to current impact
			var forwardDir = directionalSpeed;
			if (forwardDir.Length == 0)
				return;

			var normalizedForwardDir = forwardDir * 1024 / forwardDir.Length;

			// Debug visualization
			var debugVis = world.WorldActor.TraitOrDefault<DebugVisualizations>();
			if (debugVis != null && debugVis.CombatGeometry)
			{
				var caDebug = world.WorldActor.TraitOrDefault<WarheadDebugOverlayCA>();
				if (caDebug != null)
				{
					// When ForceGround is true, visualize the cone at ground level
					var vizSource = info.ForceGround != LinearPulseForceGroundType.None ? new WPos(source.X, source.Y, 0) : source;

					// Visualize the overall cone as a single large segment from 0..range
					var maxRange = range;
					var maxConeRadius = maxRange;
					caDebug.AddConeSegmentImpact(vizSource, normalizedForwardDir, 0, maxRange, 0, maxConeRadius, info.ConeAngle, Color.Yellow);

					// Visualize the individual cone segment in red
					// Create a cone segment showing the actual truncated section
					caDebug.AddConeSegmentImpact(vizSource, normalizedForwardDir, segmentStart, segmentEnd, startRadius, endRadius, info.ConeAngle, Color.Red);
				}
			}

			// Find all actors within the cone segment
			var searchRadius = new WDist(maxRadius + 512); // Add buffer for safety
			var segMid = (segmentStart + segmentEnd) / 2;
			var segCenter = source + normalizedForwardDir * segMid / 1024;
			var nearbyActors = world.FindActorsInCircle(segCenter, searchRadius);

			foreach (var actor in nearbyActors)
			{
				// Skip if SingleHitPerActor is enabled and we've already hit this actor
				if (info.SingleHitPerActor && impactedActors.Contains(actor))
					continue;

				// Skip friendly actors if they are too close to the source
				if (info.MinimumFriendlyFireRange > WDist.Zero)
				{
					var actorDistanceFromSource = (actor.CenterPosition - source).Length;
					if (actorDistanceFromSource < info.MinimumFriendlyFireRange.Length &&
						args.SourceActor.Owner.RelationshipWith(actor.Owner).HasRelationship(PlayerRelationship.Ally))
						continue;
				}

				// Check if actor is within the cone segment using the fixed forward direction
				if (IsActorInConeSegment(actor, source, normalizedForwardDir, segmentStart, segmentEnd, halfConeAngleRadians))
				{
					// Calculate falloff data for all configured DamageFalloff settings
					var falloffData = new List<(DamageFalloff Falloff, int FalloffDistance)>();

					// If no DamageFalloffs are configured, apply damage without any falloff
					if (info.DamageFalloffs.Count == 0)
					{
						ApplyDamageToActor(actor, impactPos, falloffData);
					}
					else
					{
						foreach (var damageFalloff in info.DamageFalloffs)
						{
							// Find the closest HitShape to the reference point for this falloff
							HitShape closestActiveShape = null;
							var closestDistance = int.MaxValue;

							// PERF: Avoid using TraitsImplementing<HitShape> that needs to find the actor in the trait dictionary.
							foreach (var targetPos in actor.EnabledTargetablePositions)
							{
								if (targetPos is HitShape h)
								{
									var referencePoint = GetReferencePoint(actor, impactPos, damageFalloff);
									var distance = h.DistanceFromEdge(actor, referencePoint).Length;
									if (distance < closestDistance)
									{
										closestDistance = distance;
										closestActiveShape = h;
									}
								}
							}

							// Cannot be damaged without an active HitShape for HitShape calculation type
							if (damageFalloff.DamageCalculationType == DamageCalculationType.HitShape && closestActiveShape == null)
								continue;

							// Calculate damage falloff distance based on the selected calculation type
							var falloffDistance = damageFalloff.DamageCalculationType switch
							{
								DamageCalculationType.HitShape => closestDistance,
								DamageCalculationType.ClosestTargetablePosition => actor.GetTargetablePositions()
									.Min(x => CalculateFalloffDistance(x, impactPos, damageFalloff)),
								DamageCalculationType.CenterPosition => CalculateFalloffDistance(actor.CenterPosition, impactPos, damageFalloff),
								_ => CalculateFalloffDistance(actor.CenterPosition, impactPos, damageFalloff)
							};

							falloffData.Add((damageFalloff, falloffDistance));
						}

						// If we have falloff data, apply damage
						if (falloffData.Count > 0)
						{
							ApplyDamageToActor(actor, impactPos, falloffData);
						}
					}

					if (info.SingleHitPerActor)
						impactedActors.Add(actor);
				}
			}
		}

		void ExplodeTrapezoid(WPos impactPos)
		{
			var world = args.SourceActor.World;

			// When ForceGround is enabled, use grounded source for all calculations to match visualization
			var effectiveSource = info.ForceGround != LinearPulseForceGroundType.None ? new WPos(source.X, source.Y, 0) : source;

			// Calculate distance from source to impact position using XY only to avoid Z-component errors
			var distanceFromSource = (impactPos - effectiveSource).Length;

			// Use the trapezoid segment length to determine how many segments we need
			var segmentLength = info.TrapezoidSegmentLength.Length;

			// Calculate the forward direction vector
			var forwardDir = directionalSpeed;
			if (forwardDir.Length == 0)
			{
				// Fallback: use the direction from source to impact
				forwardDir = impactPos - source;
			}

			// Debug visualization (overall trapezoid outline)
			var debugVis = world.WorldActor.TraitOrDefault<DebugVisualizations>();
			if (debugVis != null && debugVis.CombatGeometry)
			{
				var caDebug = world.WorldActor.TraitOrDefault<WarheadDebugOverlayCA>();
				if (caDebug != null)
				{
					// Use exact overall trapezoid from start (0) to full range with configured widths
					GetTrapezoidCorners(effectiveSource, forwardDir, 0, range, info.TrapezoidStartWidth.Length, info.TrapezoidEndWidth.Length,
						out var o1, out var o2, out var o3, out var o4);
					caDebug.AddPolygonOutline(new[] { o1, o2, o3, o4 }, Color.Yellow, 1);
				}
			}

			// Compute the single active segment around the current impact distance
			var segStart = Math.Max(0, distanceFromSource - segmentLength / 2);
			var segEnd = Math.Min(range, distanceFromSource + segmentLength / 2);

			// Calculate trapezoid width at start and end of this segment using integer math
			// to ensure consistent results at segment boundaries
			var startWidthAtSeg = GetTrapezoidWidthAtDistance(segStart);
			var endWidthAtSeg = GetTrapezoidWidthAtDistance(segEnd);

			// Debug visualization for this segment (exact trapezoid outline)
			if (debugVis != null && debugVis.CombatGeometry)
			{
				var caDebug = world.WorldActor.TraitOrDefault<WarheadDebugOverlayCA>();
				if (caDebug != null)
				{
					GetTrapezoidCorners(effectiveSource, forwardDir, segStart, segEnd, startWidthAtSeg, endWidthAtSeg,
						out var corner1, out var corner2, out var corner3, out var corner4);
					caDebug.AddPolygonOutline(new[] { corner1, corner2, corner3, corner4 }, Color.Red, 1);
				}
			}

			// Find actors in this trapezoid segment
			{
				var maxWidth = Math.Max(startWidthAtSeg, endWidthAtSeg);
				var halfMaxWidth = maxWidth / 2;
				var segMid = (segStart + segEnd) / 2;
				var normalizedForwardDir = Normalize1024OrDefault(forwardDir);
				var segCenter = effectiveSource + normalizedForwardDir * segMid / 1024;
				var searchRadius = new WDist((segEnd - segStart) / 2 + halfMaxWidth + 512);

				foreach (var actor in world.FindActorsInCircle(segCenter, searchRadius))
				{
					if (info.SingleHitPerActor && impactedActors.Contains(actor))
						continue;

					// Skip friendly actors if they are too close to the source
					if (info.MinimumFriendlyFireRange > WDist.Zero)
					{
						var actorDistanceFromSource = (actor.CenterPosition - source).Length;
						if (actorDistanceFromSource < info.MinimumFriendlyFireRange.Length &&
							args.SourceActor.Owner.RelationshipWith(actor.Owner).HasRelationship(PlayerRelationship.Ally))
							continue;
					}

					if (IsActorInTrapezoidSegment(actor, effectiveSource, forwardDir, segStart, segEnd, startWidthAtSeg, endWidthAtSeg))
					{
						// Calculate falloff data for all configured DamageFalloff settings
						var falloffData = new List<(DamageFalloff Falloff, int FalloffDistance)>();

						// If no DamageFalloffs are configured, apply damage without any falloff
						if (info.DamageFalloffs.Count == 0)
						{
							ApplyDamageToActor(actor, impactPos, falloffData);
						}
						else
						{
							foreach (var damageFalloff in info.DamageFalloffs)
							{
								// Calculate damage falloff distance similar to rectangle/cone
								var closestDistance = int.MaxValue;
								HitShape closestActiveShape = null;

								foreach (var targetPos in actor.EnabledTargetablePositions)
								{
									if (targetPos is HitShape h && h.IsTraitEnabled())
									{
										var referencePoint = GetReferencePoint(actor, impactPos, damageFalloff);
										var distance = h.DistanceFromEdge(actor, referencePoint).Length;
										if (distance < closestDistance)
										{
											closestDistance = distance;
											closestActiveShape = h;
										}
									}
								}

								// Skip actors without active HitShape for HitShape calculation type
								if (damageFalloff.DamageCalculationType == DamageCalculationType.HitShape && closestActiveShape == null)
									continue;

								// Calculate damage falloff distance based on the selected calculation type
								var falloffDistance = damageFalloff.DamageCalculationType switch
								{
									DamageCalculationType.HitShape => closestDistance,
									DamageCalculationType.ClosestTargetablePosition => actor.GetTargetablePositions()
										.Where(x => IsPositionInTrapezoidSegment(x, effectiveSource, forwardDir, segStart, segEnd, startWidthAtSeg, endWidthAtSeg))
										.DefaultIfEmpty(actor.CenterPosition)
										.Min(x => CalculateFalloffDistance(x, impactPos, damageFalloff)),
									DamageCalculationType.CenterPosition => CalculateFalloffDistance(actor.CenterPosition, impactPos, damageFalloff),
									_ => CalculateFalloffDistance(actor.CenterPosition, impactPos, damageFalloff)
								};

								falloffData.Add((damageFalloff, falloffDistance));
							}

							// If we have falloff data, apply damage
							if (falloffData.Count > 0)
							{
								ApplyDamageToActor(actor, impactPos, falloffData);
							}
						}

						if (info.SingleHitPerActor)
							impactedActors.Add(actor);
					}
				}
			}
		}

		static bool ActorHasHitShape(Actor actor)
		{
			foreach (var targetPos in actor.EnabledTargetablePositions)
			{
				if (targetPos is HitShape)
					return true;
			}

			return false;
		}

		static bool IsActorInConeSegment(Actor actor, WPos source, WVec forwardDir, int segmentStart, int segmentEnd, double halfConeAngleRadians)
		{
			// Check if cone segment intersects with actor's HitShape
			if (ActorHasHitShape(actor))
			{
				// For HitShape-enabled actors, check both intersection and containment
				foreach (var targetPos in actor.EnabledTargetablePositions)
				{
					if (targetPos is HitShape h)
					{
						// First check: Is the HitShape fully within the segment?
						// Test if the actor's center is within the segment - if so, the HitShape might be fully contained
						if (IsPositionInConeSegment(actor.CenterPosition, source, forwardDir, segmentStart, segmentEnd, halfConeAngleRadians))
						{
							// Check if all edges of the segment are outside or on the HitShape boundary
							// If the center is in and no segment edges penetrate the HitShape, then HitShape is fully contained
							var closestPoint = FindClosestPointOnConeSegmentOutline(actor.CenterPosition, source, forwardDir, segmentStart, segmentEnd, halfConeAngleRadians);
							var distanceToOutline = h.DistanceFromEdge(actor, closestPoint);

							// If center is in segment and closest outline point is outside HitShape, then HitShape is fully within segment
							if (distanceToOutline.Length > 0)
								return true;
						}

						// Second check: Does the segment outline intersect with the HitShape?
						var closestPoint2 = FindClosestPointOnConeSegmentOutline(actor.CenterPosition, source, forwardDir, segmentStart, segmentEnd, halfConeAngleRadians);
						var distance = h.DistanceFromEdge(actor, closestPoint2);

						// Check if the closest point is inside the HitShape OR if HitShape is very close (intersecting)
						if (distance == WDist.Zero || distance.Length <= 64) // Small tolerance for edge intersection
							return true;
					}
				}

				return false;
			}

			// Fallback: center position only for actors without HitShapes
			return IsPositionInConeSegment(actor.CenterPosition, source, forwardDir, segmentStart, segmentEnd, halfConeAngleRadians);
		}

		static bool IsPositionInConeSegment(WPos position, WPos source, WVec forwardDir, int segmentStart, int segmentEnd, double halfConeAngleRadians)
		{
			// Use XY-only vector from source to position
			var toPosition = new WVec(position.X - source.X, position.Y - source.Y, 0);

			// Check if position is within the distance range of the segment
			var distanceToPosition = toPosition.Length;
			if (distanceToPosition < segmentStart || distanceToPosition > segmentEnd)
				return false;

			// Check if position is within the cone angle
			if (toPosition.Length == 0)
				return true; // Position is at source position

			// Calculate the angle between forward direction and direction to position
			var dotProduct = (long)forwardDir.X * toPosition.X + (long)forwardDir.Y * toPosition.Y; // Use long to prevent overflow
			var cosAngleToPosition = dotProduct / (1024.0 * toPosition.Length); // forwardDir is normalized to 1024 and toPosition is XY-only

			// Clamp to avoid floating point errors in acos
			cosAngleToPosition = Math.Max(-1.0, Math.Min(1.0, cosAngleToPosition));
			var angleToPosition = Math.Acos(cosAngleToPosition);

			return angleToPosition <= halfConeAngleRadians;
		}

		static bool IsActorInRectangle(Actor actor, WPos corner1, WPos corner2, WPos corner3, WPos corner4)
		{
			// First check if the actor has any HitShapes
			var hasHitShape = ActorHasHitShape(actor);

			// If no HitShape, fall back to center position check
			if (!hasHitShape)
			{
				var actorPos = actor.CenterPosition;
				return IsPointInRectangle(actorPos, corner1, corner2, corner3, corner4);
			}

			// For HitShape-enabled actors, check both intersection and containment
			foreach (var targetPos in actor.EnabledTargetablePositions)
			{
				if (targetPos is HitShape h)
				{
					// First check: Is the HitShape fully within the rectangle?
					// Test if the actor's center is within the rectangle - if so, the HitShape might be fully contained
					if (IsPointInRectangle(actor.CenterPosition, corner1, corner2, corner3, corner4))
					{
						// Check if all edges of the rectangle are outside or on the HitShape boundary
						// If the center is in and no rectangle edges penetrate the HitShape, then HitShape is fully contained
						var closestPoint = FindClosestPointOnRectangleOutline(actor.CenterPosition, corner1, corner2, corner3, corner4);
						var distanceToOutline = h.DistanceFromEdge(actor, closestPoint);

						// If center is in rectangle and closest outline point is outside HitShape, then HitShape is fully within rectangle
						if (distanceToOutline.Length > 0)
							return true;
					}

					// Second check: Does the rectangle outline intersect with the HitShape?
					var closestPoint2 = FindClosestPointOnRectangleOutline(actor.CenterPosition, corner1, corner2, corner3, corner4);
					var distance = h.DistanceFromEdge(actor, closestPoint2);

					// Check if closest point is inside the HitShape OR if HitShape is very close (intersecting)
					if (distance == WDist.Zero || distance.Length <= 64) // Small tolerance for edge intersection
						return true;
				}
			}

			return false;
		}

		static bool IsPointInRectangle(WPos point, WPos corner1, WPos corner2, WPos corner3, WPos corner4)
		{
			// Use the cross product method to determine if point is inside rectangle
			// Rectangle corners should be in order (counter-clockwise or clockwise)
			return IsPointInsideQuadrilateral(point, corner1, corner2, corner3, corner4);
		}

		static bool IsPointInsideQuadrilateral(WPos point, WPos p1, WPos p2, WPos p3, WPos p4)
		{
			// Check if point is on the same side of each edge
			var d1 = CrossProduct2D(p2 - p1, point - p1);
			var d2 = CrossProduct2D(p3 - p2, point - p2);
			var d3 = CrossProduct2D(p4 - p3, point - p3);
			var d4 = CrossProduct2D(p1 - p4, point - p4);

			// All cross products should have the same sign (or be zero)
			var hasNeg = d1 < 0 || d2 < 0 || d3 < 0 || d4 < 0;
			var hasPos = d1 > 0 || d2 > 0 || d3 > 0 || d4 > 0;

			return !(hasNeg && hasPos);
		}

		static long CrossProduct2D(WVec a, WVec b)
		{
			return (long)a.X * b.Y - (long)a.Y * b.X;
		}

		/// <summary>
		/// Calculates the trapezoid width at a given distance from the source using integer arithmetic
		/// to ensure consistent results at segment boundaries.
		/// </summary>
		int GetTrapezoidWidthAtDistance(int distance)
		{
			var startWidth = info.TrapezoidStartWidth.Length;
			var endWidth = info.TrapezoidEndWidth.Length;
			var widthDelta = endWidth - startWidth;

			// Use integer arithmetic with proper rounding: startWidth + (distance * widthDelta + range/2) / range
			// The + range/2 ensures we round to nearest rather than truncate
			return startWidth + (int)(((long)distance * widthDelta + range / 2) / range);
		}

		static WVec Normalize1024OrDefault(WVec v)
		{
			return v.Length > 0 ? v * 1024 / v.Length : new WVec(1024, 0, 0);
		}

		static WVec PerpendicularXY(WVec v)
		{
			return new WVec(-v.Y, v.X, 0);
		}

		static void GetTrapezoidCorners(WPos source, WVec forwardDir, int segmentStart, int segmentEnd, int startWidth, int endWidth,
			out WPos corner1, out WPos corner2, out WPos corner3, out WPos corner4)
		{
			var n = Normalize1024OrDefault(forwardDir);
			var perp = PerpendicularXY(n);

			var startCenter = source + n * segmentStart / 1024;
			var endCenter = source + n * segmentEnd / 1024;

			var startHalfWidth = startWidth / 2;
			var endHalfWidth = endWidth / 2;

			corner1 = startCenter - perp * startHalfWidth / 1024; // Start left
			corner2 = startCenter + perp * startHalfWidth / 1024; // Start right
			corner3 = endCenter + perp * endHalfWidth / 1024;     // End right
			corner4 = endCenter - perp * endHalfWidth / 1024;     // End left
		}

		// Shared helper: project an XY position onto the pulse center line from source to (source + range * dir)
		// Returns true on success, false when directionalSpeed/line is degenerate. Keeps the Z of the input.
		bool TryProjectOntoCenterLine(WPos input, out WPos projection)
		{
			var centerLineDirection = directionalSpeed;
			if (centerLineDirection.Length == 0)
			{
				projection = input;
				return false;
			}

			var normalizedDirection = centerLineDirection * 1024 / centerLineDirection.Length;
			var centerLineEnd = source + normalizedDirection * range / 1024;

			var p = new int2(input.X, input.Y);
			var a = new int2(source.X, source.Y);
			var b = new int2(centerLineEnd.X, centerLineEnd.Y);

			var ab = b - a;
			var abLenSqDiv = ab.LengthSquared / 1024;
			if (abLenSqDiv == 0)
			{
				projection = input;
				return false;
			}

			var t = int2.Dot(p - a, ab) / abLenSqDiv;
			if (t < 0) t = 0;
			else if (t > 1024) t = 1024;

			var proj2D = a + new int2(ab.X * t / 1024, ab.Y * t / 1024);
			projection = new WPos(proj2D.X, proj2D.Y, input.Z);
			return true;
		}

		WPos GetReferencePoint(Actor actor, WPos impactPos, DamageFalloff falloff)
		{
			// The reference point depends on the falloff basis
			switch (falloff.FalloffBasis)
			{
				case FalloffBasis.DistanceFromCenter:
					var finalImpactPos = source + directionalSpeed * range / directionalSpeed.Length;
					return new WPos((source.X + finalImpactPos.X) / 2, (source.Y + finalImpactPos.Y) / 2, (source.Z + finalImpactPos.Z) / 2);

				case FalloffBasis.DistanceFromCenterLine:
					return TryProjectOntoCenterLine(actor.CenterPosition, out var proj)
						? proj
						: impactPos;

				case FalloffBasis.DistanceFromSource:
					return source;

				default:
					return impactPos;
			}
		}

		int CalculateFalloffDistance(WPos position, WPos impactPos, DamageFalloff falloff)
		{
			switch (falloff.FalloffBasis)
			{
				case FalloffBasis.DistanceFromCenter:
					var finalImpactPos = source + directionalSpeed * range / directionalSpeed.Length;
					var centerPoint = new WPos((source.X + finalImpactPos.X) / 2, (source.Y + finalImpactPos.Y) / 2, (source.Z + finalImpactPos.Z) / 2);
					return (position - centerPoint).Length;

				case FalloffBasis.DistanceFromCenterLine:
					return TryProjectOntoCenterLine(position, out var proj2)
						? (proj2 - position).Length
						: (position - impactPos).Length;

				case FalloffBasis.DistanceFromSource:
					return (position - source).Length;

				default:
					return (position - impactPos).Length;
			}
		}

		static int GetFalloffModifier(DamageFalloff falloff, int distance)
		{
			// Calculate effective range for this falloff (either from explicit Range or from Spread)
			WDist[] effectiveRange;
			if (falloff.Range != null && falloff.Range.Length > 1 && falloff.Range[1] != new WDist(int.MaxValue))
			{
				// Use explicit Range values
				effectiveRange = falloff.Range;
			}
			else
			{
				// Calculate ranges from Spread and Falloff array
				effectiveRange = new WDist[falloff.Falloff.Length];
				effectiveRange[0] = WDist.Zero;
				for (var i = 1; i < falloff.Falloff.Length; i++)
					effectiveRange[i] = new WDist(falloff.Spread.Length * i);
			}

			// The range to target is more than the range this falloff covers
			if (distance > effectiveRange[^1].Length)
				return 0;

			var inner = effectiveRange[0].Length;
			for (var i = 1; i < effectiveRange.Length; i++)
			{
				var outer = effectiveRange[i].Length;
				if (outer > distance)
					return int2.Lerp(falloff.Falloff[i - 1], falloff.Falloff[i], distance - inner, outer - inner);

				inner = outer;
			}

			return 0;
		}

		static bool IsActorInTrapezoidSegment(Actor actor, WPos source, WVec forwardDir, int segmentStart, int segmentEnd, int startWidth, int endWidth)
		{
			// Check if trapezoid segment intersects with actor's HitShape
			if (ActorHasHitShape(actor))
			{
				// For HitShape-enabled actors, check both intersection and containment
				foreach (var targetPos in actor.EnabledTargetablePositions)
				{
					if (targetPos is HitShape h)
					{
						// First check: Is the HitShape fully within the trapezoid segment?
						// Test if the actor's center is within the segment - if so, the HitShape might be fully contained
						if (IsPositionInTrapezoidSegment(actor.CenterPosition, source, forwardDir, segmentStart, segmentEnd, startWidth, endWidth))
						{
							// Check if all edges of the trapezoid are outside or on the HitShape boundary
							// If the center is in and no trapezoid edges penetrate the HitShape, then HitShape is fully contained
							var closestPoint = FindClosestPointOnTrapezoidOutline(actor.CenterPosition, source, forwardDir, segmentStart, segmentEnd, startWidth, endWidth);
							var distanceToOutline = h.DistanceFromEdge(actor, closestPoint);

							// If center is in segment and closest outline point is outside HitShape, then HitShape is fully within segment
							if (distanceToOutline.Length > 0)
								return true;
						}

						// Second check: Does the trapezoid outline intersect with the HitShape?
						var closestPoint2 = FindClosestPointOnTrapezoidOutline(actor.CenterPosition, source, forwardDir, segmentStart, segmentEnd, startWidth, endWidth);
						var distance = h.DistanceFromEdge(actor, closestPoint2);

						// Check if closest point is inside the HitShape OR if HitShape is very close (intersecting)
						if (distance == WDist.Zero || distance.Length <= 64) // Small tolerance for edge intersection
							return true;
					}
				}

				return false;
			}

			// Fallback: center position only for actors without HitShapes
			return IsPositionInTrapezoidSegment(actor.CenterPosition, source, forwardDir, segmentStart, segmentEnd, startWidth, endWidth);
		}

		static bool IsPositionInTrapezoidSegment(WPos position, WPos source, WVec forwardDir, int segmentStart, int segmentEnd, int startWidth, int endWidth)
		{
			// Calculate the four corners of the trapezoid segment
			GetTrapezoidCorners(source, forwardDir, segmentStart, segmentEnd, startWidth, endWidth,
				out var corner1, out var corner2, out var corner3, out var corner4);

			// Use the existing quadrilateral point-in-polygon test
			return IsPointInsideQuadrilateral(position, corner1, corner2, corner3, corner4);
		}

		/// <summary>
		/// Finds the closest point on a rectangle's outline to a given position.
		/// </summary>
		static WPos FindClosestPointOnRectangleOutline(WPos position, WPos corner1, WPos corner2, WPos corner3, WPos corner4)
		{
			var closestPoint = corner1;
			var closestDistance = (position - corner1).LengthSquared;

			// Check all four edges of the rectangle
			var edges = new (WPos Start, WPos End)[]
			{
				(corner1, corner2),
				(corner2, corner3),
				(corner3, corner4),
				(corner4, corner1)
			};

			foreach (var (start, end) in edges)
			{
				var pointOnEdge = FindClosestPointOnLineSegment(position, start, end);
				var distanceSquared = (position - pointOnEdge).LengthSquared;

				if (distanceSquared < closestDistance)
				{
					closestDistance = distanceSquared;
					closestPoint = pointOnEdge;
				}
			}

			return closestPoint;
		}

		/// <summary>
		/// Finds the closest point on a cone segment's outline to a given position.
		/// </summary>
		static WPos FindClosestPointOnConeSegmentOutline(WPos position, WPos source, WVec forwardDir, int segmentStart, int segmentEnd, double halfConeAngleRadians)
		{
			var normalizedForwardDir = Normalize1024OrDefault(forwardDir);
			var perpDir = PerpendicularXY(normalizedForwardDir);

			var closestPoint = source;
			var closestDistance = (position - source).LengthSquared;

			// Sample points along the cone segment outline (both edges and the arc)
			const int SampleCount = 8; // Number of samples along each arc

			// Left arc from segmentStart to segmentEnd
			for (var i = 0; i <= SampleCount; i++)
			{
				var t = (double)i / SampleCount;
				var distance = (int)(segmentStart + t * (segmentEnd - segmentStart));
				var cos = Math.Cos(-halfConeAngleRadians);
				var sin = Math.Sin(-halfConeAngleRadians);

				var direction = new WVec(
					(int)(normalizedForwardDir.X * cos - perpDir.X * sin),
					(int)(normalizedForwardDir.Y * cos - perpDir.Y * sin),
					0);

				var samplePoint = source + direction * distance / 1024;
				var distanceSquared = (position - samplePoint).LengthSquared;

				if (distanceSquared < closestDistance)
				{
					closestDistance = distanceSquared;
					closestPoint = samplePoint;
				}
			}

			// Right arc from segmentStart to segmentEnd
			for (var i = 0; i <= SampleCount; i++)
			{
				var t = (double)i / SampleCount;
				var distance = (int)(segmentStart + t * (segmentEnd - segmentStart));
				var cos = Math.Cos(halfConeAngleRadians);
				var sin = Math.Sin(halfConeAngleRadians);

				var direction = new WVec(
					(int)(normalizedForwardDir.X * cos - perpDir.X * sin),
					(int)(normalizedForwardDir.Y * cos - perpDir.Y * sin),
					0);

				var samplePoint = source + direction * distance / 1024;
				var distanceSquared = (position - samplePoint).LengthSquared;

				if (distanceSquared < closestDistance)
				{
					closestDistance = distanceSquared;
					closestPoint = samplePoint;
				}
			}

			// Inner arc at segmentStart (if not at source)
			if (segmentStart > 0)
			{
				for (var i = 0; i <= SampleCount; i++)
				{
					var t = (double)i / SampleCount;
					var angle = -halfConeAngleRadians + t * (2 * halfConeAngleRadians);
					var cos = Math.Cos(angle);
					var sin = Math.Sin(angle);

					var direction = new WVec(
						(int)(normalizedForwardDir.X * cos - perpDir.X * sin),
						(int)(normalizedForwardDir.Y * cos - perpDir.Y * sin),
						0);

					var samplePoint = source + direction * segmentStart / 1024;
					var distanceSquared = (position - samplePoint).LengthSquared;

					if (distanceSquared < closestDistance)
					{
						closestDistance = distanceSquared;
						closestPoint = samplePoint;
					}
				}
			}

			// Outer arc at segmentEnd
			for (var i = 0; i <= SampleCount; i++)
			{
				var t = (double)i / SampleCount;
				var angle = -halfConeAngleRadians + t * (2 * halfConeAngleRadians);
				var cos = Math.Cos(angle);
				var sin = Math.Sin(angle);

				var direction = new WVec(
					(int)(normalizedForwardDir.X * cos - perpDir.X * sin),
					(int)(normalizedForwardDir.Y * cos - perpDir.Y * sin),
					0);

				var samplePoint = source + direction * segmentEnd / 1024;
				var distanceSquared = (position - samplePoint).LengthSquared;

				if (distanceSquared < closestDistance)
				{
					closestDistance = distanceSquared;
					closestPoint = samplePoint;
				}
			}

			return closestPoint;
		}

		/// <summary>
		/// Finds the closest point on a trapezoid's outline to a given position.
		/// </summary>
		static WPos FindClosestPointOnTrapezoidOutline(WPos position, WPos source, WVec forwardDir, int segmentStart, int segmentEnd, int startWidth, int endWidth)
		{
			GetTrapezoidCorners(source, forwardDir, segmentStart, segmentEnd, startWidth, endWidth,
				out var corner1, out var corner2, out var corner3, out var corner4);

			// Use the same logic as rectangle - find closest point on the trapezoid's edges
			return FindClosestPointOnRectangleOutline(position, corner1, corner2, corner3, corner4);
		}

		/// <summary>
		/// Finds the closest point on a line segment to a given position.
		/// </summary>
		static WPos FindClosestPointOnLineSegment(WPos position, WPos lineStart, WPos lineEnd)
		{
			var line = lineEnd - lineStart;
			var lineLength = line.Length;

			if (lineLength == 0)
				return lineStart; // Degenerate line segment

			var toPosition = position - lineStart;
			var projectionLength = WVec.Dot(toPosition, line) / lineLength;

			// Clamp to line segment bounds
			if (projectionLength <= 0)
				return lineStart;
			if (projectionLength >= lineLength)
				return lineEnd;

			// Calculate the point on the line segment
			var projectionVector = line * projectionLength / lineLength;
			return lineStart + projectionVector;
		}

		void DoSmudge(in Target target, WarheadArgs args)
		{
			if (target.Type == TargetType.Invalid)
				return;

			var firedBy = args.SourceActor;
			var world = firedBy.World;

			if (info.SmudgeChance < world.SharedRandom.Next(100))
				return;

			var pos = target.CenterPosition;
			var targetTile = world.Map.CellContaining(pos);
			var smudgeLayers = world.WorldActor.TraitsImplementing<SmudgeLayer>().ToDictionary(x => x.Info.Type);

			var minRange = (info.SmudgeSize.Length > 1 && info.SmudgeSize[1] > 0) ? info.SmudgeSize[1] : 0;
			var allCells = world.Map.FindTilesInAnnulus(targetTile, minRange, info.SmudgeSize[0]);

			// Draw the smudges:
			foreach (var sc in allCells)
			{
				var smudgeType = world.Map.GetTerrainInfo(sc).AcceptsSmudgeType.FirstOrDefault(info.SmudgeType.Contains);
				if (smudgeType == null)
					continue;

				if (!smudgeLayers.TryGetValue(smudgeType, out var smudgeLayer))
					throw new NotImplementedException($"Unknown smudge type `{smudgeType}`");

				smudgeLayer.AddSmudge(sc);
			}
		}
	}
}
