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
	[Desc("This unit, when ordered to move, will fly up to its maximum altitude, fly straight, then descend and detonate itself upon reaching target.")]
	public class CruiseMissileInfo : MissileBaseInfo
	{
		[Desc("Missile will cruise straight at this altitude.")]
		public readonly WDist MaxAltitude = WDist.Zero;

		[Desc("If a mobile target moves further than this beyond its initial location, the missile will lose tracking.")]
		public readonly WDist MaxTargetMovement = WDist.Zero;

		public override object Create(ActorInitializer init) { return new CruiseMissile(init, this); }
	}

	public class CruiseMissile : MissileBase
	{
		private readonly CruiseMissileInfo cruiseMissileInfo;

		public CruiseMissile(ActorInitializer init, CruiseMissileInfo info)
			: base(init, info)
		{
			cruiseMissileInfo = info;
		}

		public override void SetTarget(Target target)
		{
			Target = target;
		}

		protected override Activity GetActivity(Actor self, Target target)
		{
			return new CruiseMissileFly(self, target, this, cruiseMissileInfo.MaxAltitude, cruiseMissileInfo.MaxTargetMovement);
		}
	}
}
