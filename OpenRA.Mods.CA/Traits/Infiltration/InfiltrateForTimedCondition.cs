#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("The actor gains a timed condition when infiltrated.")]
	class InfiltrateForTimedConditionInfo : TraitInfo
	{
		[GrantedConditionReference]
		[FieldLoader.Require]
		public readonly string Condition = null;

		[Desc("Condition duration. Use zero for infinite duration.")]
		public readonly int Duration = 0;

		[Desc("The `TargetTypes` from `Targetable` that are allowed to enter.")]
		public readonly BitSet<TargetableType> Types = default(BitSet<TargetableType>);

		[NotificationReference("Speech")]
		[Desc("Sound the victim will hear when technology gets stolen.")]
		public readonly string InfiltratedNotification = null;

		[NotificationReference("Speech")]
		[Desc("Sound the perpetrator will hear after successful infiltration.")]
		public readonly string InfiltrationNotification = null;

		public readonly bool ShowSelectionBar = false;
		public readonly Color SelectionBarColor = Color.Red;

		public override object Create(ActorInitializer init) { return new InfiltrateForTimedCondition(this); }
	}

	class InfiltrateForTimedCondition : INotifyInfiltrated, ITick, ISelectionBar
	{
		readonly InfiltrateForTimedConditionInfo info;
		int conditionToken = Actor.InvalidConditionToken;
		int ticks;

		public InfiltrateForTimedCondition(InfiltrateForTimedConditionInfo info)
		{
			this.info = info;
		}

		void INotifyInfiltrated.Infiltrated(Actor self, Actor infiltrator, BitSet<TargetableType> types)
		{
			if (!info.Types.Overlaps(types))
				return;

			if (info.InfiltratedNotification != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.InfiltratedNotification, self.Owner.Faction.InternalName);

			if (info.InfiltrationNotification != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, infiltrator.Owner, "Speech", info.InfiltrationNotification, infiltrator.Owner.Faction.InternalName);

			ticks = info.Duration;
			conditionToken = self.GrantCondition(info.Condition);
		}

		void ITick.Tick(Actor self)
		{
			if (conditionToken == Actor.InvalidConditionToken)
				return;

			if (--ticks < 0)
				conditionToken = self.RevokeCondition(conditionToken);
		}

		float ISelectionBar.GetValue()
		{
			if (!info.ShowSelectionBar || ticks <= 0)
				return 0f;

			return (float)ticks / info.Duration;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

		Color ISelectionBar.GetColor() { return info.SelectionBarColor; }
	}
}
