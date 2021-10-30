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

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class DeployOnAttackInfo : PausableConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new DeployOnAttack(init, this); }
	}

	public class DeployOnAttack : PausableConditionalTrait<DeployOnAttackInfo>, INotifyAttack
	{
		public DeployOnAttack(ActorInitializer init, DeployOnAttackInfo info)
			: base(info) { }

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			var trait = self.TraitOrDefault<GrantConditionOnDeploy>();
			if (trait != null && trait.DeployState == DeployState.Undeployed)
			{
				trait.Deploy();
				return;
			}

			var timedTrait = self.TraitOrDefault<GrantTimedConditionOnDeploy>();
			if (timedTrait != null && timedTrait.DeployState == TimedDeployState.Ready)
			{
				timedTrait.Deploy();
				return;
			}
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }
	}
}
