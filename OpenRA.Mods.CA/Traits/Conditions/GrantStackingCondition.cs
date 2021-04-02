#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class GrantStackingConditionInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition type to grant.")]
		public readonly string Condition = null;

		[Desc("Number of ticks between applying instances.")]
		public readonly int DelayPerInstance = 25;

		[Desc("Maximum instances of the condition to grant.")]
		public readonly int MaximumInstances = 1;

		[Desc("Amount of ticks required to pass while trait is disabled before removing instances. Use -1 to make condition permanent.")]
		public readonly int RevokeDelay = 25;

		public override object Create(ActorInitializer init) { return new GrantStackingCondition(init, this); }
	}

	public class GrantStackingCondition : PausableConditionalTrait<GrantStackingConditionInfo>, INotifyCreated, ITick
	{
		readonly Stack<int> tokens = new Stack<int>();

		int delayTicks = 0;
		int revokeDelayTicks = 0;

		public GrantStackingCondition(ActorInitializer init, GrantStackingConditionInfo info)
			: base(info) { }

		void GrantInstance(Actor self, string cond)
		{
			if (string.IsNullOrEmpty(cond))
				return;

			tokens.Push(self.GrantCondition(cond));
		}

		void Clear(Actor self)
		{
			delayTicks = 0;

			if (tokens.Count == 0)
				return;

			while (tokens.Count > 0)
				self.RevokeCondition(tokens.Pop());
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused)
			{
				if (tokens.Count == 0 || Info.RevokeDelay < 0)
					return;

				if (++revokeDelayTicks >= Info.RevokeDelay)
				{
					revokeDelayTicks = 0;
					Clear(self);
				}

				return;
			}

			if (tokens.Count >= Info.MaximumInstances)
				return;

			delayTicks++;

			if (delayTicks >= Info.DelayPerInstance)
			{
				GrantInstance(self, Info.Condition);
				delayTicks = 0;
			}
		}
	}
}
