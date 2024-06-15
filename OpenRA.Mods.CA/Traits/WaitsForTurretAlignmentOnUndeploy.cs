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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc(".")]
	public class WaitsForTurretAlignmentOnUndeployInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new WaitsForTurretAlignmentOnUndeploy(init, this); }
	}

	public class WaitsForTurretAlignmentOnUndeploy : INotifyDeployTriggered, ITick
	{
		public readonly WaitsForTurretAlignmentOnUndeployInfo Info;
		readonly IEnumerable<Turreted> turrets;
		readonly IEnumerable<INotifyDeployComplete> notify;
		bool deployAligning;
		bool undeployAligning;

		public WaitsForTurretAlignmentOnUndeploy(ActorInitializer init, WaitsForTurretAlignmentOnUndeployInfo info)
		{
			Info = info;
			turrets = init.Self.TraitsImplementing<Turreted>();
			notify = init.Self.TraitsImplementing<INotifyDeployComplete>();
		}

		bool AllTurretsAligned()
		{
			return turrets.All(t => t.LocalOrientation.Yaw == t.Info.InitialFacing);
		}

		void ITick.Tick(Actor self)
		{
			if (!deployAligning && !undeployAligning)
				return;

			if (AllTurretsAligned())
			{
				foreach (var n in notify)
				{
					if (deployAligning)
						n.FinishedDeploy(self);

					if (undeployAligning)
						n.FinishedUndeploy(self);
				}

				deployAligning = undeployAligning = false;
			}
		}

		void INotifyDeployTriggered.Deploy(Actor self, bool skipMakeAnim)
		{
			deployAligning = true;

		}

		void INotifyDeployTriggered.Undeploy(Actor self, bool skipMakeAnim)
		{
			undeployAligning = true;
		}
	}
}
