#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Gives a condition to the actor after a delay.")]
	public class GrantDelayedConditionInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		[Desc("Number of ticks to wait before granting the condition.")]
		public readonly int Delay = 50;

		[Desc("If the trait is disabled, revoke the condition and reset the delay.")]
		public readonly bool RevokeOnDisabled = false;

		[Desc("If the trait is paused, revoke the condition and reset the delay.")]
		public readonly bool RevokeOnPaused = false;

		public override object Create(ActorInitializer init) { return new GrantDelayedCondition(this); }
	}

	public class GrantDelayedCondition : PausableConditionalTrait<GrantDelayedConditionInfo>, ITick
	{
		readonly GrantDelayedConditionInfo info;
		int token = Actor.InvalidConditionToken;
		public int DelayRemaining { get; private set; }

		public GrantDelayedCondition(GrantDelayedConditionInfo info)
			: base(info)
		{
			this.info = info;
			DelayRemaining = info.Delay;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitPaused || IsTraitDisabled)
				return;

			if (--DelayRemaining < 1 && token == Actor.InvalidConditionToken)
				token = self.GrantCondition(info.Condition);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (!info.RevokeOnDisabled)
				return;

			DelayRemaining = info.Delay;

			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}

		protected override void TraitPaused(Actor self)
		{
			if (!info.RevokeOnPaused)
				return;

			DelayRemaining = info.Delay;

			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}
	}
}
