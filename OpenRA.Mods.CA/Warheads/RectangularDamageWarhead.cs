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
	[Desc("Rectangle-shaped warhead centered on impact. Damage radiates from the center line; no rounded ends.",
		  "Length defines the rectangle extent along the projectile path. The width is inferred from damage falloff (max range).",
		  "The rectangle's center line is parallel to the source→impact direction (projectile path).")]
	public class RectangularDamageWarhead : DamageWarhead, IRulesetLoaded<WeaponInfo>
	{
		[Desc("Total length of the rectangle along the center line direction.")]
		public readonly WDist Length = new(1024);

		[Desc("Range between falloff steps from the center line.")]
		public readonly WDist Spread = new(43);

		[Desc("Damage percentage at each range step")]
		public readonly int[] Falloff = { 100, 37, 14, 5, 0 };

		[Desc("Ranges (distance from center line) for falloff. Overrides Spread.")]
		public readonly WDist[] Range = null;

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
			var world = firedBy.World;

			// Determine center line orientation: parallel to the source direction (projectile path)
			var sourcePos = args.Source ?? pos;
			var srcToImpact = pos - sourcePos;
			WVec dir;
			if (srcToImpact.Length > 0)
				dir = srcToImpact;
			else if (args.ImpactOrientation != WRot.None)
				dir = new WVec(1024, 0, 0).Rotate(args.ImpactOrientation);
			else
				dir = new WVec(0, 1024, 0);

			// Scale to desired Length
			if (dir.Length > 0)
				dir = dir * Length.Length / dir.Length;

			var start = pos - dir / 2;
			var end = pos + dir / 2;

		// Debug visualization
			var debugVis = world.WorldActor.TraitOrDefault<DebugVisualizations>();
			if (debugVis != null && debugVis.CombatGeometry)
			{
				var caDebug = world.WorldActor.TraitOrDefault<WarheadDebugOverlayCA>();
				if (caDebug != null)
			caDebug.AddRectangleImpact(start, end, effectiveRange, effectiveRange[^1], DebugOverlayColor);
				else
					world.WorldActor.Trait<WarheadDebugOverlay>().AddImpact(pos, effectiveRange, DebugOverlayColor);
			}

		// Find potential victims within a bounding circle: approx half-diagonal + maxRange
		// Use a conservative bound: sqrt(a^2+b^2) <= a + b; half-width is inferred from max falloff
		var halfDiag = new WDist(Length.Length / 2 + effectiveRange[^1].Length);
			var searchRadius = halfDiag + effectiveRange[^1];

			foreach (var victim in world.FindActorsOnCircle(pos, searchRadius))
			{
				if (!IsValidAgainst(victim, firedBy))
					continue;

				// Compute closest point on center line and edge distance
				var victimCenter = victim.CenterPosition;
				// Reject if projection is outside the center-line segment (enforce hard length bounds)
				if (!IsProjectionWithinSegment(victimCenter, start, end))
					continue;
				var closestOnLine = ClosestPointOnSegment(victimCenter, start, end);

				HitShape closestActiveShape = null;
				var closestDistance = int.MaxValue;

				foreach (var targetPos in victim.EnabledTargetablePositions)
				{
					if (targetPos is HitShape h)
					{
						var edgeDist = h.DistanceFromEdge(victim, closestOnLine).Length;
						if (edgeDist < closestDistance)
						{
							closestDistance = edgeDist;
							closestActiveShape = h;
						}
					}
				}

				if (closestActiveShape == null)
					continue;

				int falloffDistance;
				if (DamageCalculationType == CapsuleDamageCalculationType.HitShape)
				{
					// Also ensure the center projection lies within the rectangle length
					falloffDistance = IsProjectionWithinSegment(victimCenter, start, end) ? closestDistance : int.MaxValue;
				}
				else if (DamageCalculationType == CapsuleDamageCalculationType.ClosestTargetablePosition)
				{
					var best = int.MaxValue;
					foreach (var x in victim.GetTargetablePositions())
					{
						if (!IsProjectionWithinSegment(x, start, end))
							continue;
						var d = DistanceFromCenterLine(x, start, end);
						if (d < best)
							best = d;
					}
					falloffDistance = best;
				}
				else // CenterPosition
				{
					falloffDistance = IsProjectionWithinSegment(victimCenter, start, end) ? DistanceFromCenterLine(victimCenter, start, end) : int.MaxValue;
				}

				// Outside physical width (half-width inferred from max falloff)
				if (falloffDistance > effectiveRange[^1].Length)
					continue;

				// Outside max falloff
				if (falloffDistance == int.MaxValue || falloffDistance > effectiveRange[^1].Length)
					continue;

				var localModifiers = args.DamageModifiers.Append(GetDamageFalloff(falloffDistance));

				var impactOrientation = args.ImpactOrientation;
				if (falloffDistance > 0)
				{
					var towardsTargetYaw = (victimCenter - closestOnLine).Yaw;
					var impactAngle = Util.GetVerticalAngle(closestOnLine, victimCenter);
					impactOrientation = new WRot(WAngle.Zero, impactAngle, towardsTargetYaw);
				}

				var updated = new WarheadArgs(args)
				{
					DamageModifiers = localModifiers.ToArray(),
					ImpactOrientation = impactOrientation,
				};

				InflictDamage(victim, firedBy, closestActiveShape, updated);
			}
		}

		static int DistanceFromCenterLine(WPos point, WPos start, WPos end)
		{
			var p = new int2(point.X, point.Y);
			var a = new int2(start.X, start.Y);
			var b = new int2(end.X, end.Y);
			var ab = b - a;
			var abLenSqDiv = ab.LengthSquared / 1024;
			if (abLenSqDiv == 0)
				return (a - p).Length;
			var t = int2.Dot(p - a, ab) / abLenSqDiv;
			if (t < 0) t = 0; else if (t > 1024) t = 1024;
			var proj = a + new int2(ab.X * t / 1024, ab.Y * t / 1024);
			return (proj - p).Length;
		}

		static bool IsProjectionWithinSegment(WPos point, WPos start, WPos end)
		{
			var p = new int2(point.X, point.Y);
			var a = new int2(start.X, start.Y);
			var b = new int2(end.X, end.Y);
			var ab = b - a;
			var ap = p - a;
			long dot = (long)ap.X * ab.X + (long)ap.Y * ab.Y;
			long lenSq = (long)ab.X * ab.X + (long)ab.Y * ab.Y;
			if (lenSq == 0)
				return false;
			return dot >= 0 && dot <= lenSq;
		}

		static WPos ClosestPointOnSegment(WPos point, WPos start, WPos end)
		{
			var p = new int2(point.X, point.Y);
			var a = new int2(start.X, start.Y);
			var b = new int2(end.X, end.Y);
			var ab = b - a;
			var abLenSqDiv = ab.LengthSquared / 1024;
			if (abLenSqDiv == 0)
				return start;
			var t = int2.Dot(p - a, ab) / abLenSqDiv;
			if (t < 0) t = 0; else if (t > 1024) t = 1024;
			var proj = a + new int2(ab.X * t / 1024, ab.Y * t / 1024);
			return new WPos(proj.X, proj.Y, (start.Z + end.Z) / 2);
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
