#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Allows unit to leap to a targeted location.")]
	public class TargetedLeapAbilityInfo : TargetedMovementAbilityInfo, Requires<MobileInfo>
	{
		[GrantedConditionReference]
		[Desc("The condition to grant while leaping.")]
		public readonly string LeapCondition = null;

		public override object Create(ActorInitializer init) { return new TargetedLeapAbility(init.Self, this); }
	}

	public class TargetedLeapAbility : TargetedMovementAbility
	{
		public readonly new TargetedLeapAbilityInfo Info;
		readonly Mobile mobile;

		public override string DeployOrderID => "TargetedLeapOrderTargeterDeploy";
		public override string MovementOrderID => "TargetedLeapOrderLeap";

		public TargetedLeapAbility(Actor self, TargetedLeapAbilityInfo info)
			: base(self, info)
		{
			Info = info;
			mobile = self.Trait<Mobile>();
		}

		protected override void QueueMovementActivity(Actor self, Target target)
		{
			if (facing != null)
			{
				var desiredFacing = (target.CenterPosition - self.CenterPosition).Yaw;
				self.QueueActivity(new Turn(self, desiredFacing));
			}

			self.QueueActivity(new TargetedLeap(self, self.World.Map.CellContaining(target.CenterPosition), this, mobile, facing, WAngle.FromDegrees(60)));
		}
	}
}
