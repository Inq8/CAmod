#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Warheads
{
	public enum CapsuleDamageCalculationType { HitShape, ClosestTargetablePosition, CenterPosition }

	[Desc("Stadium/Capsule shaped warhead. Damage radiates from a rectangular area with semi-circular ends.",
		"The impact point is the center of one semi-circle, extending back towards the source.",
		"Damage radius is determined by the Spread and Falloff properties, similar to SpreadDamageWarhead.")]
	public class CapsuleDamageWarhead : DamageWarhead, IRulesetLoaded<WeaponInfo>
	{
		[Desc("Length of the capsule from impact point back towards source.")]
		public readonly WDist Length = new(1024);

		[Desc("Range between falloff steps.")]
		public readonly WDist Spread = new(43);

		[Desc("Damage percentage at each range step")]
		public readonly int[] Falloff = { 100, 37, 14, 5, 0 };

		[Desc("Ranges at which each Falloff step is defined. Overrides Spread.")]
		public readonly WDist[] Range = null;

		[Desc("If true, the length is always the distance from the impact point to the source. Overrides length.")]
		public readonly bool LengthIsImpactToSource = false;

		[Desc("Controls the way damage is calculated. Possible values are 'HitShape', 'ClosestTargetablePosition' and 'CenterPosition'.")]
		public readonly CapsuleDamageCalculationType DamageCalculationType = CapsuleDamageCalculationType.HitShape;

		WDist[] effectiveRange;

		void IRulesetLoaded<WeaponInfo>.RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (Range != null)
			{
				if (Range.Length != 1 && Range.Length != Falloff.Length)
					throw new YamlException("Number of range values must be 1 or equal to the number of Falloff values.");

				for (var i = 0; i < Range.Length - 1; i++)
					if (Range[i] > Range[i + 1])
						throw new YamlException("Range values must be specified in an increasing order.");

				effectiveRange = Range;
			}
			else
				effectiveRange = Exts.MakeArray(Falloff.Length, i => i * Spread);
		}

		protected override void DoImpact(WPos pos, Actor firedBy, WarheadArgs args)
		{
			// Calculate capsule parameters
			var sourcePos = args.Source ?? pos;
			var impactToSource = sourcePos - pos;

			// If we don't have a valid source direction, use the impact orientation
			WVec capsuleDirection;
			if (impactToSource.Length > 0)
			{
				capsuleDirection = impactToSource;
			}
			else if (args.ImpactOrientation != WRot.None)
			{
				// Use the impact orientation to determine the backwards direction
				// The yaw gives us the horizontal direction the projectile was traveling
				var impactYaw = args.ImpactOrientation.Yaw;

				// Create a vector pointing backwards from the impact direction
				capsuleDirection = new WVec(0, Length.Length, 0).Rotate(new WRot(WAngle.Zero, WAngle.Zero, impactYaw + WAngle.FromDegrees(180)));
			}
			else
			{
				// Final fallback - assume north-to-south direction
				capsuleDirection = new WVec(0, Length.Length, 0);
			}

			// Normalize and scale to desired length unless we're using the exact impact->source vector
			if (capsuleDirection.Length > 0)
			{
				// If we know the true source and the rule says to use it, keep its exact length
				if (!(LengthIsImpactToSource && impactToSource.Length > 0))
					capsuleDirection = capsuleDirection * Length.Length / capsuleDirection.Length;
			}

			var capsuleStart = pos; // Impact point (center of first semi-circle)
			var capsuleEnd = pos + capsuleDirection; // Source end (center of second semi-circle)

			// Debug: Ensure we have a valid capsule line
			if ((capsuleEnd - capsuleStart).Length == 0)
			{
				// Fallback to a default direction if the capsule collapsed to a point
				capsuleDirection = new WVec(0, Length.Length, 0);
				capsuleEnd = pos + capsuleDirection;
			}

			// Try to use CA debug visualization for capsule shapes
			var debugVis = firedBy.World.WorldActor.TraitOrDefault<DebugVisualizations>();
			if (debugVis != null && debugVis.CombatGeometry)
			{
				var caDebugOverlay = firedBy.World.WorldActor.TraitOrDefault<WarheadDebugOverlayCA>();
				if (caDebugOverlay != null)
				{
					caDebugOverlay.AddCapsuleImpact(capsuleStart, capsuleEnd, effectiveRange, DebugOverlayColor);
				}
				else
				{
					// Fallback to standard circular visualization
					firedBy.World.WorldActor.Trait<WarheadDebugOverlay>().AddImpact(pos, effectiveRange, DebugOverlayColor);
				}
			}

			// Search area needs to encompass the entire capsule plus maximum falloff range
			var maxRange = effectiveRange[^1];

			// Use a single circle centered at the midpoint of the capsule
			var capsuleCenter = new WPos(
				(capsuleStart.X + capsuleEnd.X) / 2,
				(capsuleStart.Y + capsuleEnd.Y) / 2,
				(capsuleStart.Z + capsuleEnd.Z) / 2);

			// Radius needs to cover from center to either end, plus the maximum damage range
			// Use the actual capsule line length (half) so this works when LengthIsImpactToSource is enabled
			var lineLength = (capsuleEnd - capsuleStart).Length;
			var searchRadius = new WDist(lineLength / 2) + maxRange;

			// Add search radius visualization to debug overlay
			if (debugVis != null && debugVis.CombatGeometry)
			{
				var standardDebugOverlay = firedBy.World.WorldActor.Trait<WarheadDebugOverlay>();

				// Show search area as a light gray circle to distinguish from damage area
				standardDebugOverlay.AddImpact(capsuleCenter, new[] { searchRadius }, Color.FromArgb(64, Color.LightGray));
			}

			foreach (var victim in firedBy.World.FindActorsOnCircle(capsuleCenter, searchRadius))
			{
				if (!IsValidAgainst(victim, firedBy))
					continue;

				// Compute the closest point on the capsule axis for this victim once
				var victimCenter = victim.CenterPosition;
				var closestCapsulePoint = GetClosestPointOnCapsule(victimCenter, capsuleStart, capsuleEnd);

				HitShape closestActiveShape = null;
				var closestDistance = int.MaxValue;

				// PERF: Avoid using TraitsImplementing<HitShape> that needs to find the actor in the trait dictionary.
				foreach (var targetPos in victim.EnabledTargetablePositions)
				{
					if (targetPos is HitShape h)
					{
						// Use the same API as SpreadDamageWarhead: distance from the HitShape edge to the closest point on the capsule axis
						var distance = h.DistanceFromEdge(victim, closestCapsulePoint).Length;
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
				var falloffDistance = DamageCalculationType switch
				{
					CapsuleDamageCalculationType.HitShape => closestDistance,
					CapsuleDamageCalculationType.ClosestTargetablePosition => victim.GetTargetablePositions()
						.Min(x => CalculateDistanceFromCapsule(x, capsuleStart, capsuleEnd)),
					CapsuleDamageCalculationType.CenterPosition => CalculateDistanceFromCapsule(victim.CenterPosition, capsuleStart, capsuleEnd),
					_ => CalculateDistanceFromCapsule(victim.CenterPosition, capsuleStart, capsuleEnd)
				};

				// The range to target is more than the range the warhead covers
				if (falloffDistance > effectiveRange[^1].Length)
					continue;

				var localModifiers = args.DamageModifiers.Append(GetDamageFalloff(falloffDistance));
				var impactOrientation = args.ImpactOrientation;

				// If a warhead lands outside the victim's area, calculate impact angles
				if (falloffDistance > 0)
				{
					var towardsTargetYaw = (victimCenter - closestCapsulePoint).Yaw;
					var impactAngle = Util.GetVerticalAngle(closestCapsulePoint, victimCenter);
					impactOrientation = new WRot(WAngle.Zero, impactAngle, towardsTargetYaw);
				}

				var updatedWarheadArgs = new WarheadArgs(args)
				{
					DamageModifiers = localModifiers.ToArray(),
					ImpactOrientation = impactOrientation,
				};

				InflictDamage(victim, firedBy, closestActiveShape, updatedWarheadArgs);
			}
		}

		static WPos GetClosestPointOnCapsule(WPos point, WPos capsuleStart, WPos capsuleEnd)
		{
			// Convert to 2D for capsule calculation (assuming ground-level combat)
			var p = new int2(point.X, point.Y);
			var a = new int2(capsuleStart.X, capsuleStart.Y);
			var b = new int2(capsuleEnd.X, capsuleEnd.Y);

			var ab = b - a;

			// Use the same fixed-point projection scheme as CapsuleShape: scale by 1024 to avoid overflow
			var abLenSqDiv = ab.LengthSquared / 1024;

			if (abLenSqDiv == 0)
			{
				// Degenerate case: capsule is just a point
				return capsuleStart;
			}

			// Project point onto the line segment: t in [0, 1024]
			var t = int2.Dot(p - a, ab) / abLenSqDiv;
			if (t < 0) t = 0;
			else if (t > 1024) t = 1024;

			var projection = a + new int2(ab.X * t / 1024, ab.Y * t / 1024);

			// Return the 3D position of the closest point on the capsule line
			return new WPos(projection.X, projection.Y, (capsuleStart.Z + capsuleEnd.Z) / 2);
		}

		static int CalculateDistanceFromCapsule(WPos point, WPos capsuleStart, WPos capsuleEnd)
		{
			// Convert to 2D for capsule calculation (assuming ground-level combat)
			var p = new int2(point.X, point.Y);
			var a = new int2(capsuleStart.X, capsuleStart.Y);
			var b = new int2(capsuleEnd.X, capsuleEnd.Y);

			var ab = b - a;

			// Use the same fixed-point projection scheme as CapsuleShape: scale by 1024 to avoid overflow
			var abLenSqDiv = ab.LengthSquared / 1024;

			if (abLenSqDiv == 0)
			{
				// Degenerate case: capsule is just a point, distance is from the point
				return (a - p).Length;
			}

			// Project point onto the line segment: t in [0, 1024]
			var t = int2.Dot(p - a, ab) / abLenSqDiv;
			if (t < 0) t = 0;
			else if (t > 1024) t = 1024;

			var projection = a + new int2(ab.X * t / 1024, ab.Y * t / 1024);

			// Distance from point to the closest point on the line segment
			// This represents the distance from the capsule's central axis
			var distanceFromAxis = (projection - p).Length;

			// For capsule warheads, this distance is what determines damage falloff
			// Points closer to the axis take more damage, further points take less
			return distanceFromAxis;
		}

		int GetDamageFalloff(int distance)
		{
			var inner = effectiveRange[0].Length;
			for (var i = 1; i < effectiveRange.Length; i++)
			{
				var outer = effectiveRange[i].Length;
				if (outer > distance)
					return int2.Lerp(Falloff[i - 1], Falloff[i], distance - inner, outer - inner);

				inner = outer;
			}

			return 0;
		}
	}
}
