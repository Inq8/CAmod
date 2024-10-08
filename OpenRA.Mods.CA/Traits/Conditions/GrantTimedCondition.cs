﻿#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Gives a condition to the actor for a limited time.")]
	public class GrantTimedConditionInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		[Desc("Number of ticks to wait before revoking the condition.")]
		public readonly int Duration = 50;

		[Desc("If true, condition will last for full duration once enabled, even if trait is subsequently disabled/paused.")]
		public readonly bool ForceFullDuration = false;

		public override object Create(ActorInitializer init) { return new GrantTimedCondition(this); }
	}

	public class GrantTimedCondition : PausableConditionalTrait<GrantTimedConditionInfo>, ITick, ISync, INotifyCreated
	{
		readonly GrantTimedConditionInfo info;
		int token = Actor.InvalidConditionToken;
		IConditionTimerWatcher[] watchers;

		[Sync]
		public int Ticks { get; private set; }

		public GrantTimedCondition(GrantTimedConditionInfo info)
			: base(info)
		{
			this.info = info;
			Ticks = info.Duration;
		}

		protected override void Created(Actor self)
		{
			watchers = self.TraitsImplementing<IConditionTimerWatcher>().Where(Notifies).ToArray();

			base.Created(self);
		}

		void GrantCondition(Actor self, string condition)
		{
			if (token == Actor.InvalidConditionToken)
			{
				Ticks = info.Duration;
				token = self.GrantCondition(condition);
			}
		}

		void RevokeCondition(Actor self)
		{
			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}

		void ITick.Tick(Actor self)
		{
			if (!Info.ForceFullDuration || Ticks <= 0)
			{
				if (IsTraitDisabled && token != Actor.InvalidConditionToken)
					RevokeCondition(self);

				if (IsTraitPaused || IsTraitDisabled)
					return;
			}

			foreach (var w in watchers)
				w.Update(info.Duration, Ticks);

			if (token == Actor.InvalidConditionToken)
				return;

			if (--Ticks < 1)
				RevokeCondition(self);
		}

		protected override void TraitEnabled(Actor self)
		{
			GrantCondition(self, info.Condition);
		}

		bool Notifies(IConditionTimerWatcher watcher) { return watcher.Condition == Info.Condition; }
	}
}
