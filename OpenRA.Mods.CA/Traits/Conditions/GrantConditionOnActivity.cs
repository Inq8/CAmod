#region Copyright & License Information
/*
 * By Boolbada of OP Mod
 * Follows OpenRA's license as follows:
 *
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public enum ActivityType { FlyAttack, Fly, ReturnToBase }

	public class GrantConditionOnActivityInfo : ITraitInfo
	{
		[Desc("Activity to grant condition on",
			"Currently valid activities are `Fly`, `FlyAttack` and `ReturnToBase`.")]
		public readonly ActivityType Activity = ActivityType.FlyAttack;

		[GrantedConditionReference]
		[Desc("The condition to grant")]
		public readonly string Condition = null;

		[Desc("Sound to play when Active.")]
		public readonly string ActiveSound = null;

		public object Create(ActorInitializer init) { return new GrantConditionOnActivity(init, this); }
	}

	public class GrantConditionOnActivity : INotifyCreated, ITick
	{
		readonly GrantConditionOnActivityInfo info;

		ConditionManager manager;
		int token = ConditionManager.InvalidConditionToken;

		public GrantConditionOnActivity(ActorInitializer init, GrantConditionOnActivityInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			manager = self.Trait<ConditionManager>();
		}

		void GrantCondition(Actor self, string cond)
		{
			if (manager == null)
				return;

			if (string.IsNullOrEmpty(cond))
				return;

			token = manager.GrantCondition(self, cond);

			if (!string.IsNullOrEmpty(info.ActiveSound))
				Game.Sound.Play(SoundType.World, info.ActiveSound, self.CenterPosition);
		}

		void RevokeCondition(Actor self)
		{
			if (manager == null)
				return;

			if (token == ConditionManager.InvalidConditionToken)
				return;

			token = manager.RevokeCondition(self, token);
		}

		bool IsValidActivity(Actor self)
		{
			if (self.CurrentActivity is Fly && info.Activity == ActivityType.Fly)
				return true;

			if (self.CurrentActivity is FlyAttack && info.Activity == ActivityType.FlyAttack)
				return true;

			if (self.CurrentActivity is ReturnToBase && info.Activity == ActivityType.ReturnToBase)
				return true;

			return false;
		}

		void ITick.Tick(Actor self)
		{
			if (IsValidActivity(self))
			{
				if (token == ConditionManager.InvalidConditionToken)
					GrantCondition(self, info.Condition);
			}
			else
			{
				if (token != ConditionManager.InvalidConditionToken)
					RevokeCondition(self);
			}
		}
	}
}
