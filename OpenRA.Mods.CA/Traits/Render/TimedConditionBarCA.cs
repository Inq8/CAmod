#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Visualizes the remaining time for a condition. CA version is conditional and allows relationship to be specified.")]
	sealed class TimedConditionBarCAInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Condition that this bar corresponds to")]
		public readonly string Condition = null;

		public readonly Color Color = Color.Red;

		[Desc("Relationships that can see the bar.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		public override object Create(ActorInitializer init) { return new TimedConditionBarCA(init.Self, this); }
	}

	sealed class TimedConditionBarCA : ConditionalTrait<TimedConditionBarCAInfo>, ISelectionBar, IConditionTimerWatcher
	{
		readonly TimedConditionBarCAInfo info;
		readonly Actor self;
		float value;

		public TimedConditionBarCA(Actor self, TimedConditionBarCAInfo info)
			: base(info)
		{
			this.self = self;
			this.info = info;
		}

		void IConditionTimerWatcher.Update(int duration, int remaining)
		{
			value = duration > 0 ? remaining * 1f / duration : 0;
		}

		string IConditionTimerWatcher.Condition => info.Condition;

		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled)
				return 0;

			if (Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(self.World.RenderPlayer)))
				return 0;

			return value;
		}

		Color ISelectionBar.GetColor() { return info.Color; }
		bool ISelectionBar.DisplayWhenEmpty => false;
	}
}
