﻿#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("This trait interacts with and provides a container for Attach/DetachDelayedWeaponWarheads.")]
	public class DelayedWeaponAttachableInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Type of DelayedWeapons that can be attached to this trait.")]
		public readonly string Type = "bomb";

		[Desc("Defines the maximum of DelayedWeapons which can be attached at any given time.")]
		public readonly int AttachLimit = 1;

		[Desc("Show a bar indicating the progress until triggering the DelayedWeapon with the smallest remaining time.")]
		public readonly bool ShowProgressBar = true;

		[GrantedConditionReference]
		[Desc("The condition to grant while any DelayedWeapon is attached.")]
		public readonly string Condition = null;

		public readonly Color ProgressBarColor = Color.DarkRed;

		public override object Create(ActorInitializer init) { return new DelayedWeaponAttachable(init.Self, this); }
	}

	public class DelayedWeaponAttachable : ConditionalTrait<DelayedWeaponAttachableInfo>, ITick, INotifyKilled, ISelectionBar, INotifyTransform, INotifyRemovedFromWorld
	{
		public HashSet<DelayedWeaponTrigger> Container { get; private set; }

		readonly Actor self;
		readonly HashSet<Actor> detectors = new HashSet<Actor>();
		readonly Stack<int> tokens = new Stack<int>();

		bool IsEnabled { get { return tokens.Count > 0; } }

		public DelayedWeaponAttachable(Actor self, DelayedWeaponAttachableInfo info)
			: base(info)
		{
			this.self = self;
			Container = new HashSet<DelayedWeaponTrigger>();
		}

		void ITick.Tick(Actor self)
		{
			if (!IsTraitDisabled)
			{
				foreach (var trigger in Container)
					trigger.Tick(self);

				Container.RemoveWhere(p => !p.IsValid);

				while (tokens.Count > Container.Count)
					self.RevokeCondition(tokens.Pop());
			}
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (!IsTraitDisabled)
			{
				foreach (var trigger in Container)
				{
					if (!trigger.DeathTypes.IsEmpty && !e.Damage.DamageTypes.Overlaps(trigger.DeathTypes))
						continue;

					trigger.Activate(self);
				}

				Container.RemoveWhere(p => !p.IsValid);
			}
		}

		public bool CanAttach(string type)
		{
			return !IsTraitDisabled && Info.Type == type && Container.Count < Info.AttachLimit;
		}

		public void Attach(DelayedWeaponTrigger trigger)
		{
			tokens.Push(self.GrantCondition(Info.Condition));
			Container.Add(trigger);
		}

		public void AddDetector(Actor detector)
		{
			detectors.Add(detector);
		}

		public void RemoveDetector(Actor detector)
		{
			if (detectors.Contains(detector))
				detectors.Remove(detector);
		}

		protected override void TraitDisabled(Actor self)
		{
			Container.Clear();
			while (tokens.Count > Container.Count)
				self.RevokeCondition(tokens.Pop());
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

		float ISelectionBar.GetValue()
		{
			var value = 0f;

			if (!Info.ShowProgressBar || Container.Count == 0)
				return value;

			var smallestTrigger = Container.Where(b => b.AttachedBy.Owner.IsAlliedWith(self.World.LocalPlayer) || detectors.Any(d => d.Owner.IsAlliedWith(self.World.LocalPlayer)))
				.MinByOrDefault(t => t.RemainingTime);
			if (smallestTrigger == null)
				return value;

			return smallestTrigger.RemainingTime * 1.0f / smallestTrigger.TriggerTime;
		}

		Color ISelectionBar.GetColor()
		{
			return Info.ProgressBarColor;
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			Container.Clear();
			while (tokens.Count > Container.Count)
				self.RevokeCondition(tokens.Pop());
		}

		void INotifyTransform.BeforeTransform(Actor self)
		{
			if (!IsTraitDisabled)
			{
				foreach (var trigger in Container)
					trigger.Activate(self);

				Container.RemoveWhere(p => !p.IsValid);

				while (tokens.Count > 0 && Container.Count == 0)
					self.RevokeCondition(tokens.Pop());
			}
		}

		void INotifyTransform.OnTransform(Actor self) { }

		void INotifyTransform.AfterTransform(Actor toActor) { }
	}
}
