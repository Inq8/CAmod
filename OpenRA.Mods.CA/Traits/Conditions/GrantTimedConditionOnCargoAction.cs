#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Flags]
	public enum CargoActionType
	{
		Load = 1,
		Unload = 2,
	}

	[Desc("Gives a condition to the actor for a limited time if a passenger enters/exits.")]
	public class GrantTimedConditionOnCargoActionInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		[Desc("Number of ticks to wait before revoking the condition.")]
		public readonly int Duration = 50;

		[Desc("Events leading to the condition being granted. Possible values are: Load and Unload.")]
		public readonly CargoActionType Actions = CargoActionType.Load | CargoActionType.Unload;

		public override object Create(ActorInitializer init) { return new GrantTimedConditionOnCargoAction(this); }
	}

	public class GrantTimedConditionOnCargoAction : PausableConditionalTrait<GrantTimedConditionOnCargoActionInfo>, ITick, ISync, INotifyCreated, INotifyPassengerEntered, INotifyPassengerExited
	{
		readonly GrantTimedConditionOnCargoActionInfo info;
		int token = Actor.InvalidConditionToken;
		IConditionTimerWatcher[] watchers;

		[Sync]
		public int Ticks { get; private set; }

		public GrantTimedConditionOnCargoAction(GrantTimedConditionOnCargoActionInfo info)
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

		void INotifyPassengerEntered.OnPassengerEntered(Actor self, Actor passenger)
		{
			if (!IsTraitDisabled && Info.Actions.HasFlag(CargoActionType.Load))
				GrantCondition(self, info.Condition);
		}

		void INotifyPassengerExited.OnPassengerExited(Actor self, Actor passenger)
		{
			if (!IsTraitDisabled && Info.Actions.HasFlag(CargoActionType.Unload))
				GrantCondition(self, info.Condition);
		}

		bool Notifies(IConditionTimerWatcher watcher) { return watcher.Condition == Info.Condition; }
	}
}
