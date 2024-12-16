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
using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class DeployOnAttackInfo : PausableConditionalTraitInfo
	{
		[Desc("Name of the armaments that trigger deployment.")]
		public readonly HashSet<string> ArmamentNames = new() { "primary" };

		public override object Create(ActorInitializer init) { return new DeployOnAttack(init, this); }
	}

	public class DeployOnAttack : PausableConditionalTrait<DeployOnAttackInfo>, INotifyAttack
	{
		private readonly GrantConditionOnDeploy trait;
		private readonly GrantConditionOnDeployTurreted turretedTrait;
		private readonly GrantTimedConditionOnDeploy timedTrait;

		public DeployOnAttack(ActorInitializer init, DeployOnAttackInfo info)
			: base(info)
		{
			trait = init.Self.TraitOrDefault<GrantConditionOnDeploy>();
			turretedTrait = init.Self.TraitOrDefault<GrantConditionOnDeployTurreted>();
			timedTrait = init.Self.TraitOrDefault<GrantTimedConditionOnDeploy>();
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (!Info.ArmamentNames.Contains(a.Info.Name))
				return;

			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (trait != null && trait.DeployState == DeployState.Undeployed)
			{
				if (self.CurrentActivity == null || !self.CurrentActivity.ChildHasPriority)
					self.QueueActivity(false, new DeployForGrantedCondition(self, trait));
				else
					self.CurrentActivity.QueueChild(new DeployForGrantedCondition(self, trait));
				return;
			}

			if (turretedTrait != null && turretedTrait.DeployState == DeployState.Undeployed)
			{
				if (self.CurrentActivity == null || !self.CurrentActivity.ChildHasPriority)
					self.QueueActivity(false, new DeployForGrantedConditionTurreted(self, turretedTrait));
				else
					self.CurrentActivity.QueueChild(new DeployForGrantedConditionTurreted(self, turretedTrait));
				return;
			}

			if (timedTrait != null && timedTrait.DeployState == TimedDeployState.Ready)
			{
				timedTrait.Deploy();
				return;
			}
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }
	}
}
