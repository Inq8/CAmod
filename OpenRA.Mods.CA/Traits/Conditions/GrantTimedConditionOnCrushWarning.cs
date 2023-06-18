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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Gives a condition to the actor for a limited time when something tries to crush it.")]
	public class GrantTimedConditionOnCrushWarningInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		[Desc("Number of ticks to wait before revoking the condition.")]
		public readonly int Duration = 50;

		public override object Create(ActorInitializer init) { return new GrantTimedConditionOnCrushWarning(this); }
	}

	public class GrantTimedConditionOnCrushWarning : PausableConditionalTrait<GrantTimedConditionOnCrushWarningInfo>, ITick, INotifyCrushed
	{
		readonly GrantTimedConditionOnCrushWarningInfo info;
		int token = Actor.InvalidConditionToken;
		IConditionTimerWatcher[] watchers;
		public int Ticks { get; private set; }

		public GrantTimedConditionOnCrushWarning(GrantTimedConditionOnCrushWarningInfo info)
			: base(info)
		{
			this.info = info;
		}

		protected override void Created(Actor self)
		{
			watchers = self.TraitsImplementing<IConditionTimerWatcher>().Where(Notifies).ToArray();
			base.Created(self);
		}

		void GrantCondition(Actor self, string condition)
		{
			Ticks = info.Duration;

			if (token == Actor.InvalidConditionToken)
				token = self.GrantCondition(condition);
		}

		void RevokeCondition(Actor self)
		{
			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled && token != Actor.InvalidConditionToken)
				RevokeCondition(self);

			if (IsTraitPaused || IsTraitDisabled)
				return;

			foreach (var w in watchers)
				w.Update(info.Duration, Ticks);

			if (token == Actor.InvalidConditionToken)
				return;

			if (--Ticks < 1)
				RevokeCondition(self);
		}

		bool Notifies(IConditionTimerWatcher watcher) { return watcher.Condition == Info.Condition; }

		void INotifyCrushed.OnCrush(Actor self, Actor crusher, BitSet<CrushClass> crushClasses) { }

		void INotifyCrushed.WarnCrush(Actor self, Actor crusher, BitSet<CrushClass> crushClasses)
		{
			GrantCondition(self, info.Condition);
		}
	}
}
