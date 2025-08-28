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
		DistanceFromImpact,
		DistanceFromCenterLine,
		DistanceFromSource
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

	public class LinearPulseInfo : IProjectileInfo
	{
		[Desc("Type of impact for the pulse.")]
		public readonly LinearPulseImpactType ImpactType = LinearPulseImpactType.StandardImpact;

		[Desc("Distance between pulse impacts. If zero, defaults to Speed")]
		public readonly WDist ImpactInterval = WDist.Zero;

		[Desc("Speed the pulse travels.")]
		public readonly WDist Speed = WDist.FromCells(6);

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

		[Desc("If true (and not using Warhead impact type) then the same actor can only be impacted once.")]
		public readonly bool SingleHitPerActor = false;

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
	}

	public class LinearPulse : IProjectile, ISync
	{
		readonly LinearPulseInfo info;
		readonly ProjectileArgs args;
		readonly WVec speed;
		readonly WVec visualSpeed;
		readonly WVec directionalSpeed;
		readonly WVec visualDirectionalSpeed;
		readonly WDist impactInterval;
		readonly WDist[] effectiveRange;

		readonly WAngle facing;
		readonly Animation anim;

		[Sync]
		WPos pos, visualPos, target, source;
		int ticks;
		int totalDistanceTravelled;
		int totalVisualDistanceTravelled;
		bool travelComplete;
		bool visualTravelComplete;
		readonly int range;
		readonly int visualRange;
		readonly WPos[] impactPositions;
		readonly HashSet<Actor> impactedActors = new();
		bool finalHitCreated;

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

			// Calculate effective range for falloff (either from explicit Range or from Spread)
			if (info.Range != null && info.Range.Length > 1 && info.Range[1] != new WDist(int.MaxValue))
			{
				// Use explicit Range values
				effectiveRange = info.Range;
			}
			else
			{
				// Calculate ranges from Spread and Falloff array
				effectiveRange = new WDist[info.Falloff.Length];
				effectiveRange[0] = WDist.Zero;
				for (var i = 1; i < info.Falloff.Length; i++)
					effectiveRange[i] = new WDist(info.Spread.Length * i);
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

			travelComplete = totalDistanceTravelled >= range;
			visualTravelComplete = totalVisualDistanceTravelled >= visualRange;

			// Create final hit animation when visual travel completes
			if (visualTravelComplete && !finalHitCreated && !string.IsNullOrEmpty(info.FinalHitAnim))
			{
				finalHitCreated = true;
				var palette = info.FinalHitAnimPalette;
				world.AddFrameEndTask(w => w.Add(new SpriteEffect(pos, facing, w, info.FinalHitAnim, info.FinalHitAnimSequence, palette)));
			}

			if (travelComplete && visualTravelComplete)
				world.AddFrameEndTask(w => w.Remove(this));

			ticks++;
		}

		WAngle GetEffectiveFacing()
		{
			var angle = WAngle.Zero;
			var at = (float)ticks / (speed.Length - 1);
			var attitude = angle.Tan() * (1 - 2 * at) / (4 * 1024);

			var u = facing.Angle % 512 / 512f;
			var scale = 2048 * u * (1 - u);

			var effective = (int)(facing.Angle < 512
				? facing.Angle - scale * attitude
				: facing.Angle + scale * attitude);

			return new WAngle(effective);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
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
			// Create hit animations if specified
			var world = args.SourceActor.World;
			foreach (var impactAnim in info.ImpactAnimations)
			{
				if (!string.IsNullOrEmpty(impactAnim.Image))
				{
					// Pick a random sequence for this impact
					var sequence = impactAnim.Sequences.Random(world.SharedRandom);

					// Create SpriteEffect with facing
					world.AddFrameEndTask(w => w.Add(new SpriteEffect(impactPos, facing, w, impactAnim.Image, sequence, impactAnim.Palette)));
				}
			}

			switch (info.ImpactType)
			{
				case LinearPulseImpactType.StandardImpact:
					args.Weapon.Impact(Target.FromPos(impactPos), new WarheadArgs(args));
					break;
				case LinearPulseImpactType.Rectangle:
					ExplodeRectangle(impactPos);
					break;
				case LinearPulseImpactType.Cone:
					ExplodeCone(impactPos);
					break;
				case LinearPulseImpactType.Trapezoid:
					ExplodeTrapezoid(impactPos);
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

			// Debug visualization
			var debugVis = world.WorldActor.TraitOrDefault<DebugVisualizations>();
			if (debugVis != null && debugVis.CombatGeometry)
			{
				var caDebug = world.WorldActor.TraitOrDefault<WarheadDebugOverlayCA>();
				if (caDebug != null)
				{
					// Create start and end points for the rectangle center line
					var start = impactPos - lengthOffset;
					var end = impactPos + lengthOffset;
					var debugHalfWidth = new WDist(info.RectangleWidth.Length / 2);

					// Use a single range for visualization (could be made configurable)
					var visualRange = new WDist[] { debugHalfWidth };
					caDebug.AddRectangleImpact(start, end, visualRange, debugHalfWidth, Color.Yellow);
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

				// Check if actor intersects with the rectangle
				if (IsActorInRectangle(actor, corner1, corner2, corner3, corner4))
				{
					// Find the closest HitShape to the reference point (like CapsuleDamageWarhead)
					HitShape closestActiveShape = null;
					var closestDistance = int.MaxValue;

					// PERF: Avoid using TraitsImplementing<HitShape> that needs to find the actor in the trait dictionary.
					foreach (var targetPos in actor.EnabledTargetablePositions)
					{
						if (targetPos is HitShape h)
						{
							// For rectangles, calculate distance from HitShape to the center line or impact point
							var referencePoint = GetReferencePoint(actor, impactPos);
							var distance = h.DistanceFromEdge(actor, referencePoint).Length;
							if (distance < closestDistance)
							{
								closestDistance = distance;
								closestActiveShape = h;
							}
						}
					}

					// Cannot be damaged without an active HitShape.
					if (closestActiveShape == null)
						continue;

					// Calculate damage falloff distance based on the selected calculation type
					var falloffDistance = info.DamageCalculationType switch
					{
						DamageCalculationType.HitShape => closestDistance,
						DamageCalculationType.ClosestTargetablePosition => actor.GetTargetablePositions()
							.Where(x => IsPointInRectangle(x, corner1, corner2, corner3, corner4))
							.DefaultIfEmpty(actor.CenterPosition)
							.Min(x => CalculateFalloffDistance(x, impactPos)),
						DamageCalculationType.CenterPosition => CalculateFalloffDistance(actor.CenterPosition, impactPos),
						_ => CalculateFalloffDistance(actor.CenterPosition, impactPos)
					};

					ApplyDamageToActor(actor, impactPos, falloffDistance);

					if (info.SingleHitPerActor)
						impactedActors.Add(actor);
				}
			}
		}

		void ApplyDamageToActor(Actor actor, WPos impactPos, int falloffDistance)
		{
			// The range to target is more than the range the warhead covers
			if (falloffDistance > effectiveRange[^1].Length)
				return;

			var falloffModifier = GetFalloff(falloffDistance);
			var adjustedModifiers = args.DamageModifiers.Append(falloffModifier);

			var impactOrientation = new WRot(WAngle.Zero, WAngle.Zero, facing);

			// If a warhead lands outside the victim's area, calculate impact angles
			if (falloffDistance > 0)
			{
				var referencePoint = GetReferencePoint(actor, impactPos);
				var towardsTargetYaw = (actor.CenterPosition - referencePoint).Yaw;
				impactOrientation = new WRot(WAngle.Zero, WAngle.Zero, towardsTargetYaw);
			}

			var warheadArgs = new WarheadArgs(args)
			{
				ImpactOrientation = impactOrientation,
				ImpactPosition = actor.CenterPosition,
				DamageModifiers = adjustedModifiers.ToArray(),
			};

			args.Weapon.Impact(Target.FromActor(actor), warheadArgs);
		}

		void ExplodeCone(WPos impactPos)
		{
			var world = args.SourceActor.World;

			// Calculate distance from source to impact position
			var distanceFromSource = (impactPos - source).Length;

			// Calculate the radius of the cone at this distance
			var halfConeAngleRadians = info.ConeAngle * Math.PI / 360.0; // Convert degrees to radians and divide by 2

			// Calculate the cone segment boundaries
			var segmentStart = Math.Max(0, distanceFromSource - info.ConeSegmentLength.Length / 2);
			var segmentEnd = distanceFromSource + info.ConeSegmentLength.Length / 2;

			// Calculate cone radii at segment boundaries
			var startRadius = (int)(segmentStart * Math.Tan(halfConeAngleRadians));
			var endRadius = (int)(segmentEnd * Math.Tan(halfConeAngleRadians));
			var maxRadius = Math.Max(startRadius, endRadius);

			// Direction from source to impact
			var direction = impactPos - source;
			if (direction.Length == 0)
				return;

			var forwardDir = direction * 1024 / direction.Length; // Normalized

			// Debug visualization
			var debugVis = world.WorldActor.TraitOrDefault<DebugVisualizations>();
			if (debugVis != null && debugVis.CombatGeometry)
			{
				var caDebug = world.WorldActor.TraitOrDefault<WarheadDebugOverlayCA>();
				if (caDebug != null)
				{
					// Visualize the overall cone in yellow (based on maximum range)
					var maxRange = Math.Max(range, visualRange);
					var maxConeRadius = (int)(maxRange * Math.Tan(halfConeAngleRadians));
					var overallConeAxis = forwardDir * maxRange / 1024;
					var overallRanges = new WDist[] { WDist.Zero, new(maxConeRadius) };
					caDebug.AddConeImpact(source, overallConeAxis, overallRanges, info.ConeAngle, Color.Yellow);

					// Visualize the individual cone segment in red
					// Create a cone from source to the end of this segment
					var segmentEndAxis = forwardDir * segmentEnd / 1024;
					var segmentRanges = new WDist[] { new(startRadius), new(endRadius) };
					caDebug.AddConeImpact(source, segmentEndAxis, segmentRanges, info.ConeAngle, Color.Red);
				}
			}

			// Find all actors within the cone segment
			var searchRadius = new WDist(maxRadius + 512); // Add buffer for safety
			var nearbyActors = world.FindActorsInCircle(impactPos, searchRadius);

			foreach (var actor in nearbyActors)
			{
				// Skip if SingleHitPerActor is enabled and we've already hit this actor
				if (info.SingleHitPerActor && impactedActors.Contains(actor))
					continue;

				// Check if actor is within the cone segment
				if (IsActorInConeSegment(actor, source, forwardDir, segmentStart, segmentEnd, halfConeAngleRadians))
				{
					// Find the closest HitShape to the reference point (like CapsuleDamageWarhead)
					HitShape closestActiveShape = null;
					var closestDistance = int.MaxValue;

					// PERF: Avoid using TraitsImplementing<HitShape> that needs to find the actor in the trait dictionary.
					foreach (var targetPos in actor.EnabledTargetablePositions)
					{
						if (targetPos is HitShape h)
						{
							// For cones, calculate distance from HitShape to the source (apex of cone)
							var referencePoint = GetReferencePoint(actor, impactPos);
							var distance = h.DistanceFromEdge(actor, referencePoint).Length;
							if (distance < closestDistance)
							{
								closestDistance = distance;
								closestActiveShape = h;
							}
						}
					}

					// Cannot be damaged without an active HitShape.
					if (closestActiveShape == null)
						continue;

					// Calculate damage falloff distance based on the selected calculation type
					var falloffDistance = info.DamageCalculationType switch
					{
						DamageCalculationType.HitShape => closestDistance,
						DamageCalculationType.ClosestTargetablePosition => actor.GetTargetablePositions()
							.Min(x => CalculateFalloffDistance(x, impactPos)),
						DamageCalculationType.CenterPosition => CalculateFalloffDistance(actor.CenterPosition, impactPos),
						_ => CalculateFalloffDistance(actor.CenterPosition, impactPos)
					};

					ApplyDamageToActor(actor, impactPos, falloffDistance);

					if (info.SingleHitPerActor)
						impactedActors.Add(actor);
				}
			}
		}

		void ExplodeTrapezoid(WPos impactPos)
		{
			var world = args.SourceActor.World;

			// Calculate distance from source to impact position
			var distanceFromSource = (impactPos - source).Length;

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
					GetTrapezoidCorners(source, forwardDir, 0, range, info.TrapezoidStartWidth.Length, info.TrapezoidEndWidth.Length,
						out var o1, out var o2, out var o3, out var o4);
					caDebug.AddPolygonOutline(new[] { o1, o2, o3, o4 }, Color.Yellow, 1);
				}
			}

			// Compute the single active segment around the current impact distance
			var segStart = Math.Max(0, distanceFromSource - segmentLength / 2);
			var segEnd = Math.Min(range, distanceFromSource + segmentLength / 2);

			// Calculate trapezoid width at start and end of this segment
			var progressStart = (double)segStart / range;
			var progressEnd = (double)segEnd / range;

			var startWidthAtSeg = (int)(info.TrapezoidStartWidth.Length + progressStart * (info.TrapezoidEndWidth.Length - info.TrapezoidStartWidth.Length));
			var endWidthAtSeg = (int)(info.TrapezoidStartWidth.Length + progressEnd * (info.TrapezoidEndWidth.Length - info.TrapezoidStartWidth.Length));

			// Debug visualization for this segment (exact trapezoid outline)
			if (debugVis != null && debugVis.CombatGeometry)
			{
				var caDebug = world.WorldActor.TraitOrDefault<WarheadDebugOverlayCA>();
				if (caDebug != null)
				{
					GetTrapezoidCorners(source, forwardDir, segStart, segEnd, startWidthAtSeg, endWidthAtSeg,
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
				var segCenter = source + normalizedForwardDir * segMid / 1024;
				var searchRadius = new WDist((segEnd - segStart) / 2 + halfMaxWidth + 512);

				foreach (var actor in world.FindActorsInCircle(segCenter, searchRadius))
				{
					if (info.SingleHitPerActor && impactedActors.Contains(actor))
						continue;

					if (IsActorInTrapezoidSegment(actor, source, forwardDir, segStart, segEnd, startWidthAtSeg, endWidthAtSeg))
					{
						// Calculate damage falloff distance similar to rectangle/cone
						var closestDistance = int.MaxValue;
						HitShape closestActiveShape = null;

						foreach (var targetPos in actor.EnabledTargetablePositions)
						{
							if (targetPos is HitShape h && h.IsTraitEnabled())
							{
								var referencePoint = GetReferencePoint(actor, impactPos);
								var distance = h.DistanceFromEdge(actor, referencePoint).Length;
								if (distance < closestDistance)
								{
									closestDistance = distance;
									closestActiveShape = h;
								}
							}
						}

						// Skip actors without active HitShape for HitShape calculation type
						if (info.DamageCalculationType == DamageCalculationType.HitShape && closestActiveShape == null)
							continue;

						// Calculate damage falloff distance based on the selected calculation type
						var falloffDistance = info.DamageCalculationType switch
						{
							DamageCalculationType.HitShape => closestDistance,
							DamageCalculationType.ClosestTargetablePosition => actor.GetTargetablePositions()
								.Where(x => IsPositionInTrapezoidSegment(x, source, forwardDir, segStart, segEnd, startWidthAtSeg, endWidthAtSeg))
								.DefaultIfEmpty(actor.CenterPosition)
								.Min(x => CalculateFalloffDistance(x, impactPos)),
							DamageCalculationType.CenterPosition => CalculateFalloffDistance(actor.CenterPosition, impactPos),
							_ => CalculateFalloffDistance(actor.CenterPosition, impactPos)
						};

						ApplyDamageToActor(actor, impactPos, falloffDistance);

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
			// First check if the actor has any HitShapes
			var hasHitShape = ActorHasHitShape(actor);

			// If no HitShape, fall back to center position check
			if (!hasHitShape)
			{
				return IsPositionInConeSegment(actor.CenterPosition, source, forwardDir, segmentStart, segmentEnd, halfConeAngleRadians);
			}

			// For HitShape-enabled actors, check if any part of their shape intersects with the cone
			// We'll test multiple points around the actor and see if any are within the cone
			var actorCenter = actor.CenterPosition;
			var testPoints = new List<WPos> { actorCenter };

			// Add test points around the actor based on a rough estimate of their size
			// This is a simplified approach - more sophisticated would be to get the actual HitShape bounds
			const int TestRadius = 512; // Test points in a 1-cell radius around the center
			for (var angle = 0; angle < 360; angle += 45) // 8 test points around the center
			{
				var radians = angle * Math.PI / 180.0;
				var offset = new WVec((int)(TestRadius * Math.Cos(radians)), (int)(TestRadius * Math.Sin(radians)), 0);
				testPoints.Add(actorCenter + offset);
			}

			// Check if any test point is within the cone segment
			foreach (var testPoint in testPoints)
			{
				if (IsPositionInConeSegment(testPoint, source, forwardDir, segmentStart, segmentEnd, halfConeAngleRadians))
					return true;
			}

			// Additionally, check distance from HitShape to the cone axis
			foreach (var targetPos in actor.EnabledTargetablePositions)
			{
				if (targetPos is HitShape h)
				{
					// Check distance from HitShape to a point on the cone axis
					var distanceFromSource = (actorCenter - source).Length;

					// Clamp to segment bounds
					var clampedDistance = Math.Max(segmentStart, Math.Min(segmentEnd, distanceFromSource));
					var axisPoint = source + forwardDir * clampedDistance / 1024;

					var distanceToAxis = h.DistanceFromEdge(actor, axisPoint).Length;

					// Calculate the cone radius at this distance
					var coneRadius = clampedDistance * Math.Tan(halfConeAngleRadians);

					// If the HitShape is within the cone radius plus a small tolerance, consider it hit
					if (distanceToAxis <= coneRadius + 256) // Small tolerance for intersection
						return true;
				}
			}

			return false;
		}

		static bool IsPositionInConeSegment(WPos position, WPos source, WVec forwardDir, int segmentStart, int segmentEnd, double halfConeAngleRadians)
		{
			var toPosition = position - source;

			// Check if position is within the distance range of the segment
			var distanceToPosition = toPosition.Length;
			if (distanceToPosition < segmentStart || distanceToPosition > segmentEnd)
				return false;

			// Check if position is within the cone angle
			if (toPosition.Length == 0)
				return true; // Position is at source position

			// Calculate the angle between forward direction and direction to position
			var dotProduct = (long)forwardDir.X * toPosition.X + (long)forwardDir.Y * toPosition.Y; // Use long to prevent overflow
			var cosAngleToPosition = dotProduct / (1024.0 * toPosition.Length); // forwardDir is normalized to 1024

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

			// For HitShape-enabled actors, check if any part of their shape intersects with the rectangle
			// We'll do this by checking the distance from the HitShape to several points on the rectangle
			var rectangleTestPoints = new WPos[]
			{
				corner1, corner2, corner3, corner4, // Corners
				new((corner1.X + corner2.X) / 2, (corner1.Y + corner2.Y) / 2, (corner1.Z + corner2.Z) / 2), // Edge midpoints
				new((corner2.X + corner3.X) / 2, (corner2.Y + corner3.Y) / 2, (corner2.Z + corner3.Z) / 2),
				new((corner3.X + corner4.X) / 2, (corner3.Y + corner4.Y) / 2, (corner3.Z + corner4.Z) / 2),
				new((corner4.X + corner1.X) / 2, (corner4.Y + corner1.Y) / 2, (corner4.Z + corner1.Z) / 2),
				new((corner1.X + corner3.X) / 2, (corner1.Y + corner3.Y) / 2, (corner1.Z + corner3.Z) / 2) // Center
			};

			// Check if the HitShape is close to any of these test points
			foreach (var targetPos in actor.EnabledTargetablePositions)
			{
				if (targetPos is HitShape h)
				{
					foreach (var testPoint in rectangleTestPoints)
					{
						var distance = h.DistanceFromEdge(actor, testPoint).Length;

						// If the HitShape edge is very close to or overlapping with the rectangle, consider it intersecting
						if (distance <= 256) // Small tolerance for intersection (roughly 1/4 cell)
							return true;
					}
				}
			}

			// Also check if the actor's center is inside the rectangle as a fallback
			return IsPointInRectangle(actor.CenterPosition, corner1, corner2, corner3, corner4);
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

		WPos GetReferencePoint(Actor actor, WPos impactPos)
		{
			// The reference point depends on the falloff basis (same logic for both rectangles and cones)
			switch (info.FalloffBasis)
			{
				case FalloffBasis.DistanceFromImpact:
					return impactPos;

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

		int CalculateFalloffDistance(WPos position, WPos impactPos)
		{
			switch (info.FalloffBasis)
			{
				case FalloffBasis.DistanceFromImpact:
					return (position - impactPos).Length;

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

		int GetFalloff(int distance)
		{
			var inner = effectiveRange[0].Length;
			for (var i = 1; i < effectiveRange.Length; i++)
			{
				var outer = effectiveRange[i].Length;
				if (outer > distance)
					return int2.Lerp(info.Falloff[i - 1], info.Falloff[i], distance - inner, outer - inner);

				inner = outer;
			}

			return 0;
		}

		static bool IsActorInTrapezoidSegment(Actor actor, WPos source, WVec forwardDir, int segmentStart, int segmentEnd, int startWidth, int endWidth)
		{
			// First check if the actor has any HitShapes
			var hasHitShape = ActorHasHitShape(actor);

			// If no HitShape, fall back to center position check
			if (!hasHitShape)
				return IsPositionInTrapezoidSegment(actor.CenterPosition, source, forwardDir, segmentStart, segmentEnd, startWidth, endWidth);

			// For HitShape actors, we need to test multiple points
			var actorCenter = actor.CenterPosition;
			const int TestRadius = 512; // Test points in a circle around the actor

			var testPoints = new List<WPos> { actorCenter };

			// Add test points around the actor
			for (var angle = 0; angle < 360; angle += 45)
			{
				var radians = angle * Math.PI / 180.0;
				var offset = new WVec((int)(TestRadius * Math.Cos(radians)), (int)(TestRadius * Math.Sin(radians)), 0);
				testPoints.Add(actorCenter + offset);
			}

			// Check if any test point is within the trapezoid segment
			foreach (var testPoint in testPoints)
			{
				if (IsPositionInTrapezoidSegment(testPoint, source, forwardDir, segmentStart, segmentEnd, startWidth, endWidth))
					return true;
			}

			// Additionally, check distance from HitShape to the trapezoid boundary
			foreach (var targetPos in actor.EnabledTargetablePositions)
			{
				if (targetPos is HitShape h)
				{
					// Calculate the four corners of the trapezoid segment
					GetTrapezoidCorners(source, forwardDir, segmentStart, segmentEnd, startWidth, endWidth,
						out var corner1, out var corner2, out var corner3, out var corner4);

					// Test distance from HitShape to trapezoid corners and edge midpoints
					var trapezoidTestPoints = new WPos[]
					{
						corner1, corner2, corner3, corner4, // Corners
						new((corner1.X + corner2.X) / 2, (corner1.Y + corner2.Y) / 2, (corner1.Z + corner2.Z) / 2), // Edge midpoints
						new((corner2.X + corner3.X) / 2, (corner2.Y + corner3.Y) / 2, (corner2.Z + corner3.Z) / 2),
						new((corner3.X + corner4.X) / 2, (corner3.Y + corner4.Y) / 2, (corner3.Z + corner4.Z) / 2),
						new((corner4.X + corner1.X) / 2, (corner4.Y + corner1.Y) / 2, (corner4.Z + corner1.Z) / 2),
						new((corner1.X + corner3.X) / 2, (corner1.Y + corner3.Y) / 2, (corner1.Z + corner3.Z) / 2) // Center
					};

					foreach (var testPoint in trapezoidTestPoints)
					{
						var distance = h.DistanceFromEdge(actor, testPoint).Length;
						if (distance <= TestRadius) // Small tolerance for intersection
							return true;
					}
				}
			}

			return false;
		}

		static bool IsPositionInTrapezoidSegment(WPos position, WPos source, WVec forwardDir, int segmentStart, int segmentEnd, int startWidth, int endWidth)
		{
			// Calculate the four corners of the trapezoid segment
			GetTrapezoidCorners(source, forwardDir, segmentStart, segmentEnd, startWidth, endWidth,
				out var corner1, out var corner2, out var corner3, out var corner4);

			// Use the existing quadrilateral point-in-polygon test
			return IsPointInsideQuadrilateral(position, corner1, corner2, corner3, corner4);
		}
	}
}
