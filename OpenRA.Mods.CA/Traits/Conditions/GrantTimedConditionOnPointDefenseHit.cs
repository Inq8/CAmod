#region Copyright & License Information
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
	[Desc("Gives a condition to the actor for a limited time when point defense destroys an incoming projectile.")]
	public class GrantTimedConditionOnPointDefenseHitInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		[Desc("The amount of damage required to add 1 tick of charging time.")]
		public readonly int ScaleChargeTimeWithDamageAmount = 10;

		public override object Create(ActorInitializer init) { return new GrantTimedConditionOnPointDefenseHit(this, init); }
	}

	public class GrantTimedConditionOnPointDefenseHit : PausableConditionalTrait<GrantTimedConditionOnPointDefenseHitInfo>, ITick, INotifyPointDefenseHit
	{
		readonly GrantTimedConditionOnPointDefenseHitInfo info;
		int token = Actor.InvalidConditionToken;
		IConditionTimerWatcher[] watchers;
		public int Ticks { get; private set; }
		int maxTicks;
		Actor self;

		public GrantTimedConditionOnPointDefenseHit(GrantTimedConditionOnPointDefenseHitInfo info, ActorInitializer init)
			: base(info)
		{
			this.info = info;
			maxTicks = 0;
			self = init.Self;
		}

		protected override void Created(Actor self)
		{
			watchers = self.TraitsImplementing<IConditionTimerWatcher>().Where(Notifies).ToArray();
			base.Created(self);
		}

		void GrantCondition(string condition, int damageAvoided)
		{
			maxTicks = damageAvoided / info.ScaleChargeTimeWithDamageAmount;
			Ticks = maxTicks;

			if (token == Actor.InvalidConditionToken)
				token = self.GrantCondition(condition);
		}

		void RevokeCondition()
		{
			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled && token != Actor.InvalidConditionToken)
				RevokeCondition();

			if (IsTraitPaused || IsTraitDisabled)
				return;

			foreach (var w in watchers)
				w.Update(maxTicks, Ticks);

			if (token == Actor.InvalidConditionToken)
				return;

			if (--Ticks < 1)
				RevokeCondition();
		}

		bool Notifies(IConditionTimerWatcher watcher) { return watcher.Condition == Info.Condition; }

		void INotifyPointDefenseHit.Hit(int damagePrevented)
		{
			GrantCondition(info.Condition, damagePrevented);
		}
	}
}
