#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("CA version can require the target to be an actor.")]
	public class GrantConditionOnAttackCAInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition type to grant.")]
		public readonly string Condition = null;

		[Desc("Name of the armaments that grant this condition.")]
		public readonly HashSet<string> ArmamentNames = new HashSet<string>() { "primary" };

		[Desc("Shots required to apply an instance of the condition. If there are more instances of the condition granted than values listed,",
			"the last value is used for all following instances beyond the defined range.")]
		public readonly int[] RequiredShotsPerInstance = { 1 };

		[Desc("Maximum instances of the condition to grant.")]
		public readonly int MaximumInstances = 1;

		[Desc("Should all instances reset if the actor passes the final stage?")]
		public readonly bool IsCyclic = false;

		[Desc("Amount of ticks required to pass without firing to revoke an instance.")]
		public readonly int RevokeDelay = 15;

		[Desc("Should an instance be revoked if the actor changes target?")]
		public readonly bool RevokeOnNewTarget = false;

		[Desc("Should all instances be revoked instead of only one?")]
		public readonly bool RevokeAll = false;

		[Desc("Only grant condition if the target is an actor?")]
		public readonly bool RequiresActorTarget = false;

		public override object Create(ActorInitializer init) { return new GrantConditionOnAttackCA(init, this); }
	}

	public class GrantConditionOnAttackCA : PausableConditionalTrait<GrantConditionOnAttackCAInfo>, INotifyCreated, ITick, INotifyAttack
	{
		readonly Stack<int> tokens = new Stack<int>();

		int cooldown = 0;
		int shotsFired = 0;

		// Only tracked when RevokeOnNewTarget is true.
		Target lastTarget = Target.Invalid;

		public GrantConditionOnAttackCA(ActorInitializer init, GrantConditionOnAttackCAInfo info)
			: base(info) { }

		void GrantInstance(Actor self, string cond)
		{
			if (string.IsNullOrEmpty(cond))
				return;

			tokens.Push(self.GrantCondition(cond));
		}

		void RevokeInstance(Actor self, bool revokeAll)
		{
			shotsFired = 0;

			if (tokens.Count == 0)
				return;

			if (!revokeAll)
				self.RevokeCondition(tokens.Pop());
			else
				while (tokens.Count > 0)
					self.RevokeCondition(tokens.Pop());
		}

		void ITick.Tick(Actor self)
		{
			if (tokens.Count > 0 && --cooldown == 0)
			{
				cooldown = Info.RevokeDelay;
				RevokeInstance(self, Info.RevokeAll);
			}
		}

		static bool TargetChanged(in Target lastTarget, in Target target)
		{
			// Invalidate reveal changing the target.
			if (lastTarget.Type == TargetType.FrozenActor &&
				target.Type == TargetType.Actor &&
				lastTarget.FrozenActor.Actor == target.Actor)
				return false;

			if (lastTarget.Type == TargetType.Actor &&
				target.Type == TargetType.FrozenActor &&
				target.FrozenActor.Actor == lastTarget.Actor)
				return false;

			if (lastTarget.Type != target.Type)
				return true;

			// Invalidate attacking different targets with shared target types.
			if (lastTarget.Type == TargetType.Actor &&
				target.Type == TargetType.Actor &&
				lastTarget.Actor != target.Actor)
				return true;

			if (lastTarget.Type == TargetType.FrozenActor &&
				target.Type == TargetType.FrozenActor &&
				lastTarget.FrozenActor != target.FrozenActor)
				return true;

			if (lastTarget.Type == TargetType.Terrain &&
				target.Type == TargetType.Terrain &&
				lastTarget.CenterPosition != target.CenterPosition)
				return true;

			return false;
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (!Info.ArmamentNames.Contains(a.Info.Name))
				return;

			if (Info.RequiresActorTarget && target.Type != TargetType.Actor && target.Type != TargetType.FrozenActor)
				return;

			if (Info.RevokeOnNewTarget)
			{
				if (TargetChanged(lastTarget, target))
					RevokeInstance(self, Info.RevokeAll);

				lastTarget = target;
			}

			cooldown = Info.RevokeDelay;

			if (!Info.IsCyclic && tokens.Count >= Info.MaximumInstances)
				return;

			shotsFired++;
			var requiredShots = tokens.Count < Info.RequiredShotsPerInstance.Length
				? Info.RequiredShotsPerInstance[tokens.Count]
				: Info.RequiredShotsPerInstance[Info.RequiredShotsPerInstance.Length - 1];

			if (shotsFired >= requiredShots)
			{
				if (Info.IsCyclic && tokens.Count == Info.MaximumInstances)
					RevokeInstance(self, true);
				else
					GrantInstance(self, Info.Condition);

				shotsFired = 0;
			}
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

		protected override void TraitDisabled(Actor self)
		{
			RevokeInstance(self, true);
		}
	}
}
