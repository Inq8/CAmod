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
using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	public class DeployForGrantedConditionTurreted : Activity
	{
		readonly GrantConditionOnDeployTurreted deploy;
		readonly bool canTurn;
		readonly bool moving;
		readonly IFacing facing;
		readonly Action onComplete;

		public DeployForGrantedConditionTurreted(Actor self, GrantConditionOnDeployTurreted deploy, bool moving = false, Action onComplete = null)
		{
			this.deploy = deploy;
			this.moving = moving;
			canTurn = self.Info.HasTraitInfo<IFacingInfo>();
			facing = self.TraitOrDefault<IFacing>();
			this.onComplete = onComplete;
		}

		protected override void OnFirstRun(Actor self)
		{
			// Turn to the required facing.
			if (deploy.DeployState == DeployState.Undeployed && deploy.Info.ValidFacings.Length > 0 && canTurn && !moving)
			{
				var desiredFacing = deploy.Info.ValidFacings[0];

				// Choose the nearest facing to the current facing.
				if (facing != null)
				{
					var currentFacing = facing.Facing;
					var nearestFacingDiff = int.MaxValue;

					foreach (var validDeployFacing in deploy.Info.ValidFacings)
					{
						int diff1 = Math.Abs(validDeployFacing.Facing - currentFacing.Facing);
						int diff2 = 256 - diff1;
						int diff = Math.Min(diff1, diff2);

						if (diff < nearestFacingDiff)
						{
							desiredFacing = validDeployFacing;
							nearestFacingDiff = diff;
						}
					}
				}

				QueueChild(new Turn(self, desiredFacing));
			}
		}

		protected override void OnLastRun(Actor self)
		{
			onComplete?.Invoke();
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling || (deploy.DeployState != DeployState.Deployed && moving))
				return true;

			QueueChild(new DeployInner(deploy));
			return true;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (NextActivity != null)
				foreach (var n in NextActivity.TargetLineNodes(self))
					yield return n;

			yield break;
		}
	}

	public class DeployInner : Activity
	{
		readonly GrantConditionOnDeployTurreted deployment;
		bool initiated;

		public DeployInner(GrantConditionOnDeployTurreted deployment)
		{
			this.deployment = deployment;

			// Once deployment animation starts, the animation must finish.
			IsInterruptible = false;
		}

		public override bool Tick(Actor self)
		{
			// Wait for deployment
			if (deployment.DeployState == DeployState.Deploying || deployment.DeployState == DeployState.Undeploying)
				return false;

			if (initiated)
				return true;

			if (deployment.DeployState == DeployState.Undeployed)
				deployment.Deploy();
			else
				deployment.Undeploy();

			initiated = true;
			return false;
		}
	}
}
