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
using OpenRA.Activities;
using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public enum CruiseMissileState { Ascending, Cruising, Descending }

	[Desc("This unit, when ordered to move, will fly up to its maximum altitude, fly straight, then descend and detonate itself upon reaching target.")]
	public class CruiseMissileInfo : MissileBaseInfo
	{
		[Desc("Missile will cruise straight at this altitude.")]
		public readonly WDist MaxAltitude = WDist.Zero;

		[Desc("If a mobile target moves further than this beyond its initial location, the missile will lose tracking. Zero means infinite tracking.")]
		public readonly WDist MaxTargetMovement = WDist.Zero;

		[GrantedConditionReference]
		[Desc("The condition to grant when the missile is ascending.")]
		public readonly string AscendingCondition = null;

		[GrantedConditionReference]
		[Desc("The condition to grant when the missile is descending.")]
		public readonly string DescendingCondition = null;

		[Desc("If true, missile will track target.")]
		public readonly bool TrackTarget = false;

		public override object Create(ActorInitializer init) { return new CruiseMissile(init, this); }
	}

	public class CruiseMissile : MissileBase
	{
		private readonly CruiseMissileInfo cruiseMissileInfo;
		int ascendingToken = Actor.InvalidConditionToken;
		int descendingToken = Actor.InvalidConditionToken;
		Actor self;
		WPos initialTargetPos;

		public CruiseMissileState State { get; private set; }

		public CruiseMissile(ActorInitializer init, CruiseMissileInfo info)
			: base(init, info)
		{
			cruiseMissileInfo = info;
			self = init.Self;
		}

		public void SetState(CruiseMissileState newState)
		{
			State = newState;

			if (cruiseMissileInfo.AscendingCondition != null)
			{
				if (State == CruiseMissileState.Ascending && ascendingToken == Actor.InvalidConditionToken)
					ascendingToken = self.GrantCondition(cruiseMissileInfo.AscendingCondition);
				else if (State != CruiseMissileState.Ascending && ascendingToken != Actor.InvalidConditionToken)
					ascendingToken = self.RevokeCondition(ascendingToken);
			}

			if (cruiseMissileInfo.DescendingCondition != null)
			{
				if (State == CruiseMissileState.Descending && descendingToken == Actor.InvalidConditionToken)
					descendingToken = self.GrantCondition(cruiseMissileInfo.DescendingCondition);
				else if (State != CruiseMissileState.Descending && descendingToken != Actor.InvalidConditionToken)
					descendingToken = self.RevokeCondition(descendingToken);
			}
		}

		public override void SetTarget(Target target)
		{
			Target = target;
			initialTargetPos = target.CenterPosition;
		}

		protected override Activity GetActivity(Actor self, Target target)
		{
			return new CruiseMissileFly(self, target, initialTargetPos, this, cruiseMissileInfo.MaxAltitude, cruiseMissileInfo.MaxTargetMovement, cruiseMissileInfo.TrackTarget);
		}
	}
}
