#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class UndeployOnStopInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new UndeployOnStop(init, this); }
	}

	public class UndeployOnStop : ConditionalTrait<UndeployOnStopInfo>, IResolveOrder
	{
		private readonly GrantConditionOnDeploy trait;
		private readonly GrantConditionOnDeployTurreted turretedTrait;

		public UndeployOnStop(ActorInitializer init, UndeployOnStopInfo info)
			: base(info)
		{
			trait = init.Self.TraitOrDefault<GrantConditionOnDeploy>();
			turretedTrait = init.Self.TraitOrDefault<GrantConditionOnDeployTurreted>();
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (IsTraitDisabled)
				return;

			if (order.OrderString != "Stop")
				return;

			if (trait != null && trait.DeployState == DeployState.Deployed)
			{
				trait.Undeploy();
				return;
			}

			if (turretedTrait != null && turretedTrait.DeployState == DeployState.Deployed)
			{
				turretedTrait.Undeploy();
				return;
			}
		}
	}
}
