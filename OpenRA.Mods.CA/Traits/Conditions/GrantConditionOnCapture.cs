#region Copyright & License Information
/*
 * Copyright 2015-2022 OpenRA.Mods.CA Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Grants a condition when this actor is captured.")]
	class GrantConditionOnCaptureInfo : ConditionalTraitInfo
	{
		public readonly BitSet<TargetableType> Types = default;

		[FieldLoader.Require]
		[GrantedConditionReference]
		public readonly string Condition = null;

		[Desc("Use `TimedConditionBar` for visualization.")]
		public readonly int Duration = 0;

		[Desc("Grant condition only if the capturer's CaptureTypes overlap with these types. Leave empty to allow all types.")]
		public readonly BitSet<CaptureType> CaptureTypes = default(BitSet<CaptureType>);

		public override object Create(ActorInitializer init) { return new GrantConditionOnCapture(this); }
	}

	class GrantConditionOnCapture : ConditionalTrait<GrantConditionOnCaptureInfo>, INotifyCapture, INotifyCreated, ITick
	{
		int conditionToken = Actor.InvalidConditionToken;
		int duration;
		IConditionTimerWatcher[] watchers;

		public GrantConditionOnCapture(GrantConditionOnCaptureInfo info)
			: base(info) { }

		void INotifyCapture.OnCapture(Actor self, Actor infiltrator, Player oldOwner, Player newOwner, BitSet<CaptureType> captureTypes)
		{
			if (IsTraitDisabled)
				return;

			if (!Info.CaptureTypes.IsEmpty && !Info.CaptureTypes.Overlaps(captureTypes))
				return;

			duration = Info.Duration;

			if (conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(Info.Condition);
		}

		bool Notifies(IConditionTimerWatcher watcher) { return watcher.Condition == Info.Condition; }

		protected override void Created(Actor self)
		{
			watchers = self.TraitsImplementing<IConditionTimerWatcher>().Where(Notifies).ToArray();

			base.Created(self);
		}

		void ITick.Tick(Actor self)
		{
			if (conditionToken != Actor.InvalidConditionToken && Info.Duration > 0)
			{
				if (--duration < 0)
				{
					conditionToken = self.RevokeCondition(conditionToken);
					foreach (var w in watchers)
						w.Update(0, 0);
				}
				else
					foreach (var w in watchers)
						w.Update(Info.Duration, duration);
			}
		}
	}
}
