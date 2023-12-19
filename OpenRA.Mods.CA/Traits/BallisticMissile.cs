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
	[Desc("This unit, when ordered to move, will fly in ballistic path then will detonate itself upon reaching target.")]
	public class BallisticMissileInfo : MissileBaseInfo
	{
		public override object Create(ActorInitializer init) { return new BallisticMissile(init, this); }
	}

	public class BallisticMissile : MissileBase
	{
		public BallisticMissile(ActorInitializer init, BallisticMissileInfo info)
			: base(init, info)
		{
			//
		}

		public override void SetTarget(Target target)
		{
			Target = Target.FromPos(target.CenterPosition);
		}

		protected override Activity GetActivity(Actor self, Target target)
		{
			return new BallisticMissileFly(self, target, this);
		}
	}
}
