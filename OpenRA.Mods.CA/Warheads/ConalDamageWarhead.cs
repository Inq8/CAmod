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
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Warheads
{
	[Desc("Cone-shaped warhead with radial falloff from the source.",
		"Cone angle is configurable; size is defined by falloff ranges (Spread/Range/Falloff).",
		"Apex is at the source; axis points from source toward the impact position.")]
	public class ConalDamageWarhead : DamageWarhead, IRulesetLoaded<WeaponInfo>
	{
		[Desc("Full cone angle in degrees (e.g., 90 means 45° to either side of the axis).")]
		public readonly int ConeAngle = 90;

		[Desc("Range between falloff steps (radial from source).")]
		public readonly WDist Spread = new(43);

		[Desc("Damage percentage at each range step")]
		public readonly int[] Falloff = { 100, 37, 14, 5, 0 };

		[Desc("Ranges at which each Falloff step is defined. Overrides Spread.")]
		public readonly WDist[] Range = null;

		[Desc("Source position offset.")]
		public readonly WVec SourceOffset = new WVec(0, 0, 0);

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

			// Apply source offset to the source position
			var sourcePos = args.Source ?? pos;
			if (SourceOffset != WVec.Zero)
			{
				// Apply offset relative to the impact orientation or source-to-impact direction
				WRot offsetRotation;
				if (args.ImpactOrientation != WRot.None)
				{
					offsetRotation = args.ImpactOrientation;
				}
				else if (args.Source.HasValue)
				{
					var sourceToImpact = pos - args.Source.Value;
					if (sourceToImpact.Length > 0)
						offsetRotation = new WRot(WAngle.Zero, WAngle.Zero, sourceToImpact.Yaw);
					else
						offsetRotation = WRot.None;
				}
				else
				{
					offsetRotation = WRot.None;
				}

				var rotatedOffset = SourceOffset.Rotate(offsetRotation);
				sourcePos += rotatedOffset;
			}

			// Apex at adjusted source; axis from source towards impact (or orientation fallback)
			var apex = sourcePos;
			var axis = pos - apex;
			if (axis.Length == 0)
			{
				if (args.ImpactOrientation != WRot.None)
					axis = new WVec(1024, 0, 0).Rotate(args.ImpactOrientation);
				else
					axis = new WVec(0, 1024, 0);
			}

			// Cone length is determined by the maximum falloff range
			var coneLength = effectiveRange[^1].Length;

			// Precompute angle threshold
			var halfAngleRad = Math.PI * (ConeAngle / 2.0) / 180.0;
			var cosThreshold = Math.Cos(halfAngleRad);

			// Debug visualization
			var debugVis = world.WorldActor.TraitOrDefault<DebugVisualizations>();
			if (debugVis != null && debugVis.CombatGeometry)
			{
				var caOverlay = world.WorldActor.TraitOrDefault<WarheadDebugOverlayCA>();
				if (caOverlay != null)
					caOverlay.AddConeImpact(apex, axis, effectiveRange, ConeAngle, DebugOverlayColor);
				else
					world.WorldActor.Trait<WarheadDebugOverlay>().AddImpact(apex, effectiveRange, DebugOverlayColor);
			}

			var maxRange = effectiveRange[^1];

			foreach (var victim in world.FindActorsOnCircle(apex, maxRange))
			{
				if (!IsValidAgainst(victim, firedBy))
					continue;

				var center = victim.CenterPosition;
				if (!IsInsideFiniteCone(center, apex, axis, cosThreshold, coneLength))
					continue;

				HitShape closestActiveShape = null;
				var closestDistance = int.MaxValue;

				// As in SpreadDamageWarhead, measure edge distance from the apex for HitShape mode
				foreach (var targetPos in victim.EnabledTargetablePositions)
				{
					if (targetPos is HitShape h)
					{
						var distance = h.DistanceFromEdge(victim, apex).Length;
						if (distance < closestDistance)
						{
							closestDistance = distance;
							closestActiveShape = h;
						}
					}
				}

				if (closestActiveShape == null)
					continue;

				int falloffDistance;
				if (DamageCalculationType == CapsuleDamageCalculationType.HitShape)
				{
					falloffDistance = closestDistance;
				}
				else if (DamageCalculationType == CapsuleDamageCalculationType.ClosestTargetablePosition)
				{
					var best = int.MaxValue;
					foreach (var x in victim.GetTargetablePositions())
					{
						if (!IsInsideFiniteCone(x, apex, axis, cosThreshold, coneLength))
							continue;
						var d = (x - apex).Length;
						if (d < best)
							best = d;
					}

					falloffDistance = best;
				}
				else // CenterPosition
				{
					falloffDistance = (center - apex).Length;
				}

				if (falloffDistance == int.MaxValue || falloffDistance > maxRange.Length)
					continue;

				var localModifiers = args.DamageModifiers.Append(GetDamageFalloff(falloffDistance));

				var impactOrientation = args.ImpactOrientation;
				if (falloffDistance > 0)
				{
					var towardsTargetYaw = (center - apex).Yaw;
					var impactAngle = Util.GetVerticalAngle(apex, center);
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

		static bool IsInsideCone(WPos point, WPos apex, WVec axis, double cosThreshold)
		{
			var vx = (double)(point.X - apex.X);
			var vy = (double)(point.Y - apex.Y);
			var vlen = Math.Sqrt(vx * vx + vy * vy);
			if (vlen == 0)
				return true; // Apex

			var dx = (double)axis.X;
			var dy = (double)axis.Y;
			var dlen = Math.Sqrt(dx * dx + dy * dy);
			if (dlen == 0)
				return false;

			var dot = vx * dx + vy * dy;
			if (dot <= 0) // Behind apex
				return false;

			var cosTheta = dot / (vlen * dlen);
			return cosTheta >= cosThreshold;
		}

		static bool IsInsideFiniteCone(WPos point, WPos apex, WVec axis, double cosThreshold, int coneLength)
		{
			if (!IsInsideCone(point, apex, axis, cosThreshold))
				return false;

			// Also enforce finite length: projection of (point - apex) onto axis within [0, coneLength]
			var vx = (long)(point.X - apex.X);
			var vy = (long)(point.Y - apex.Y);
			var dx = (long)axis.X;
			var dy = (long)axis.Y;
			var axisLen = axis.Length;
			if (axisLen == 0)
				return false;
			var dot = vx * dx + vy * dy;
			if (dot <= 0)
				return false;

			// Compare along-axis distance without sqrt by scaling
			// t = dot / |axis|; require t <= coneLength
			// Use 64-bit to avoid overflow
			return dot <= (long)axisLen * coneLength;
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
