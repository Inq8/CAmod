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
	[Desc("The actor gains a timed condition when infiltrated.")]
	class InfiltrateForTimedConditionInfo : TraitInfo
	{
		[GrantedConditionReference]
		[FieldLoader.Require]
		public readonly string Condition = null;

		[Desc("Condition duration. Use zero for infinite duration.")]
		public readonly int Duration = 0;

		[Desc("The `TargetTypes` from `Targetable` that are allowed to enter.")]
		public readonly BitSet<TargetableType> Types = default;

		[NotificationReference("Speech")]
		[Desc("Sound the victim will hear when technology gets stolen.")]
		public readonly string InfiltratedNotification = null;

		[NotificationReference("Speech")]
		[Desc("Sound the perpetrator will hear after successful infiltration.")]
		public readonly string InfiltrationNotification = null;

		[Desc("Experience to grant to the infiltrating player.")]
		public readonly int PlayerExperience = 0;

		public readonly bool ShowSelectionBar = false;
		public readonly Color SelectionBarColor = Color.Red;

		[Desc("If true, will also grant the condition to all actors of the same type owned by the target player.")]
		public readonly bool ApplyToAllOfType = false;

		public override object Create(ActorInitializer init) { return new InfiltrateForTimedCondition(this); }
	}

	class InfiltrateForTimedCondition : INotifyInfiltrated, ITick, ISelectionBar
	{
		public InfiltrateForTimedConditionInfo Info { get; }
		int conditionToken = Actor.InvalidConditionToken;
		int ticks;

		public InfiltrateForTimedCondition(InfiltrateForTimedConditionInfo info)
		{
			Info = info;
		}

		void INotifyInfiltrated.Infiltrated(Actor self, Actor infiltrator, BitSet<TargetableType> types)
		{
			if (!Info.Types.Overlaps(types))
				return;

			if (Info.InfiltratedNotification != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.InfiltratedNotification, self.Owner.Faction.InternalName);

			if (Info.InfiltrationNotification != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, infiltrator.Owner, "Speech", Info.InfiltrationNotification, infiltrator.Owner.Faction.InternalName);

			GrantCondition(self);

			infiltrator.Owner.PlayerActor.TraitOrDefault<PlayerExperience>()?.GiveExperience(Info.PlayerExperience);

			if (Info.ApplyToAllOfType)
			{
				var otherActors = self.World.ActorsWithTrait<InfiltrateForTimedCondition>()
					.Where(a => a.Actor.Info.Name == self.Info.Name
						&& a.Actor.Owner == self.Owner
						&& a.Actor != self
						&& a.Trait.Info.Condition == Info.Condition);

				foreach (var a in otherActors)
					a.Trait?.GrantCondition(a.Actor);
			}
		}

		public void GrantCondition(Actor self)
		{
			ticks = Info.Duration;

			if (conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);

			conditionToken = self.GrantCondition(Info.Condition);
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
			if (!Info.ShowSelectionBar || ticks <= 0)
				return 0f;

			return (float)ticks / Info.Duration;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

		Color ISelectionBar.GetColor() { return Info.SelectionBarColor; }
	}
}
