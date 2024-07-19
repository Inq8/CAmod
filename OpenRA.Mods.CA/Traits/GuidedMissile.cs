#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("This unit, when ordered to move, will fly in direct path then will detonate itself upon reaching target.")]
	public class GuidedMissileInfo : MissileBaseInfo
	{
		[Desc("Missile will not go below this altitude. If zero, the missile will crash if it hits the ground.")]
		public readonly WDist MinAltitude = WDist.Zero;

		[Desc("If a mobile target moves further than this beyond its initial location, the missile will lose tracking. Zero means infinite tracking.")]
		public readonly WDist MaxTargetMovement = WDist.Zero;

		[Desc("Added this value multiplied by the speed (i.e. distance travelled per tick) of the target to MaxTargetMovement.")]
		public readonly int MaxTargetMovementTicks = 0;

		public override object Create(ActorInitializer init) { return new GuidedMissile(init, this); }
	}

	public class GuidedMissile : MissileBase
	{
		public new GuidedMissileInfo Info;
		WPos initialTargetPos;

		public GuidedMissile(ActorInitializer init, GuidedMissileInfo info)
			: base(init, info)
		{
			Info = info;
		}

		public override void SetTarget(Target target)
		{
			Target = target;
			initialTargetPos = target.CenterPosition;
		}

		protected override Activity GetActivity(Actor self, Target target)
		{
			return new GuidedMissileFly(self, target, initialTargetPos, this, CalculateMaxTargetMovement(target));
		}

		private WDist CalculateMaxTargetMovement(Target target)
		{
			var scaledMaxDistance = WDist.Zero;

			if (Info.MaxTargetMovementTicks > 0 && target.Type == TargetType.Actor && !target.Actor.IsDead)
			{
				scaledMaxDistance = GetActorSpeed(target.Actor) * Info.MaxTargetMovementTicks;
			}

			return Info.MaxTargetMovement + scaledMaxDistance;
		}

		private WDist GetActorSpeed(Actor actor)
		{
			var mobileInfo = actor.Info.TraitInfoOrDefault<MobileInfo>();
			if (mobileInfo != null)
			{
				return new WDist(mobileInfo.Speed);
			}

			var aircraftInfo = actor.Info.TraitInfoOrDefault<AircraftInfo>();
			if (aircraftInfo != null)
			{
				return new WDist(aircraftInfo.Speed);
			}

			return WDist.Zero;
		}
	}
}
