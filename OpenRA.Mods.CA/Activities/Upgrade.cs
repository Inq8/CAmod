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
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	[Desc("Activity whereby an actor searches for a location it can upgrade,",
		"moves to that location, and receives the upgrade.")]
	public class Upgrade : Activity
	{
		readonly PlayerResources playerResources;
		readonly Upgradeable upgradeable;

		int upgradeTicksRemaining;
		int upgradeCostRemaining;
		int upgradingConditionToken;

		Target host;
		INotifyResupply[] notifyResupplies;
		IMove move;
		IMoveInfo moveInfo;
		readonly Color targetLineColor;
		Action<int> updateTicksRemaining;

		bool upgradeInProgress = false;

		public Upgrade(Actor self, in Target target, Upgradeable upgradeable, PlayerResources playerResources, Action<int> updateTicksRemaining, Color? targetLineColor)
		{
			host = target;
			this.upgradeable = upgradeable;
			this.playerResources = playerResources;
			this.updateTicksRemaining = updateTicksRemaining;
			upgradeTicksRemaining = upgradeable.UpgradeInfo.BuildDuration;
			upgradeCostRemaining = upgradeable.UpgradeInfo.Cost;
			upgradingConditionToken = Actor.InvalidConditionToken;
			move = self.TraitOrDefault<IMove>();
			moveInfo = self.Info.TraitInfoOrDefault<IMoveInfo>();
			this.targetLineColor = targetLineColor ?? moveInfo.GetTargetLineColor();
		}

		protected override void OnFirstRun(Actor self)
		{
			// Host might be self if order issued via hotkey or upgrade button
			var hostActor = host.Type != TargetType.Actor || host.Actor == self ? FindNearestHost(self) : host.Actor;

			if (hostActor != null)
			{
				host = Target.FromActor(hostActor);
				notifyResupplies = host.Actor.TraitsImplementing<INotifyResupply>().ToArray();
			}
		}

		/* Return true to complete. */
		public override bool Tick(Actor self)
		{
			if (IsCanceling || !upgradeable.CanUpgrade)
			{
				CancelUpgrade(self);
				return true;
			}

			updateTicksRemaining(upgradeTicksRemaining);
			var isHostInvalid = upgradeable.Info.UpgradeAtActors.Any() && (host.Actor == null || host.Type != TargetType.Actor || !host.Actor.IsInWorld || host.Actor == self);

			if (isHostInvalid)
			{
				// This ensures transports are also cancelled when the host becomes invalid
				Cancel(self, true);
				return true;
			}

			bool isCloseEnough;

			// Negative means there's no distance limit.
			// If RepairableNear, use TargetablePositions instead of CenterPosition
			// to ensure the actor moves close enough to the host.
			// Otherwise check against host CenterPosition.
			if (upgradeable.Info.UpgradeAtRange < WDist.Zero)
				isCloseEnough = true;
			else
				isCloseEnough = (host.CenterPosition - self.CenterPosition).HorizontalLengthSquared <= upgradeable.Info.UpgradeAtRange.LengthSquared;

			if (!isCloseEnough)
			{
				QueueChild(move.MoveWithinRange(host, upgradeable.Info.UpgradeAtRange - WDist.FromCells(1), targetLineColor: targetLineColor));
				return false;
			}

			UpgradeInProgressTick(self);

			if (!upgradeInProgress)
				return true;

			return false;
		}

		Actor FindNearestHost(Actor self)
		{
			if (!upgradeable.Info.UpgradeAtActors.Any())
				return null;

			var upgradeAtActor = upgradeable.GetValidHosts()
				.OrderBy(a => a.Owner == self.Owner ? 0 : 1)
				.ThenBy(p => (self.Location - p.Location).LengthSquared);

			return upgradeAtActor.FirstOrDefault();
		}

		void UpgradeInProgressTick(Actor self)
		{
			if (!upgradeInProgress)
			{
				upgradeInProgress = true;
				upgradeable.UpdateManager();
			}

			var expectedRemainingCost = upgradeTicksRemaining == 1 ? 0 : upgradeable.UpgradeInfo.Cost * upgradeTicksRemaining / Math.Max(1, upgradeable.UpgradeInfo.BuildDuration);
			var costThisFrame = upgradeCostRemaining - expectedRemainingCost;

			/* Insufficient funds. */
			if (costThisFrame != 0 && !playerResources.TakeCash(costThisFrame, true))
				return;

			upgradeCostRemaining -= costThisFrame;

			foreach (var notifyResupply in notifyResupplies)
				notifyResupply.ResupplyTick(host.Actor, self, ResupplyType.Rearm);

			if (--upgradeTicksRemaining > 0)
				return;

			ApplyUpgrade(self);
		}

		void ApplyUpgrade(Actor self)
		{
			if (self.IsDead || !self.IsInWorld)
				return;

			var faction = self.Owner.Faction.InternalName;

			var cargo = self.TraitOrDefault<Cargo>();
			if (cargo != null && !cargo.IsEmpty())
			{
				if (cargo.CanUnload())
					QueueChild(new UnloadCargo(self, cargo.Info.LoadRange));
				else
				{
					CancelUpgrade(self);
					return;
				}
			}

			if (upgradeable.Info.Actor != null)
				Transform(self, faction);
			else if (upgradeable.Info.Condition != null)
			{
				self.GrantCondition(upgradeable.Info.Condition);
				CompleteUpgrade(self, faction);
			}
		}

		void Transform(Actor self, string faction)
		{
			var transform = new InstantTransform(self, upgradeable.Info.Actor)
			{
				ForceHealthPercentage = 0,
				Faction = faction,
				OnComplete = (Actor a) => { CompleteUpgrade(self, faction); },
				SkipMakeAnims = upgradeable.Info.SkipMakeAnims
			};
			QueueChild(transform);
		}

		void CompleteUpgrade(Actor self, string faction)
		{
			if (self.IsDead || !self.IsInWorld)
				return;

			upgradeInProgress = false;
			upgradeable.Complete();

			if (upgradingConditionToken != Actor.InvalidConditionToken)
				upgradingConditionToken = self.RevokeCondition(upgradingConditionToken);

			if (upgradeable.Info.UpgradeCompleteSpeechNotification != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", upgradeable.Info.UpgradeCompleteSpeechNotification, faction);

			Cancel(self, true);
		}

		void CancelUpgrade(Actor self)
		{
			if (!upgradeInProgress)
				return;

			if (upgradeTicksRemaining > 0)
				playerResources.GiveCash(upgradeable.UpgradeInfo.Cost - upgradeCostRemaining);

			upgradeTicksRemaining = upgradeable.UpgradeInfo.BuildDuration;
			upgradeCostRemaining = upgradeable.UpgradeInfo.Cost;
			upgradeInProgress = false;
			upgradeable.UpdateManager();

			if (upgradingConditionToken != Actor.InvalidConditionToken)
				upgradingConditionToken = self.RevokeCondition(upgradingConditionToken);
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (ChildActivity == null)
			{
				yield return new TargetLineNode(host, targetLineColor);
			}
			else
			{
				var current = ChildActivity;
				while (current != null)
				{
					foreach (var n in current.TargetLineNodes(self))
						yield return n;

					current = current.NextActivity;
				}
			}
		}
	}
}
