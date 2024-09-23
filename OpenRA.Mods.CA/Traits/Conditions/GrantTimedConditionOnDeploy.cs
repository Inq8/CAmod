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
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class GrantTimedConditionOnDeployInfo : PausableConditionalTraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition granted during deploying.")]
		public readonly string DeployingCondition = null;

		[GrantedConditionReference]
		[FieldLoader.Require]
		[Desc("The condition granted after deploying.")]
		public readonly string DeployedCondition = null;

		[GrantedConditionReference]
		[Desc("The condition granted after deploying.")]
		public readonly string ChargingCondition = null;

		[Desc("Cooldown in ticks until the unit can deploy.")]
		public readonly int CooldownTicks;

		[Desc("The deployed state's length in ticks.")]
		public readonly int DeployedTicks;

		[Desc("Cursor to display when able to (un)deploy the actor.")]
		public readonly string DeployCursor = "deploy";

		[Desc("Cursor to display when unable to (un)deploy the actor.")]
		public readonly string DeployBlockedCursor = "deploy-blocked";

		[SequenceReference]
		[Desc("Animation to play for deploying.")]
		public readonly string DeployAnimation = null;

		[SequenceReference]
		[Desc("Animation to play for undeploying.")]
		public readonly string UndeployAnimation = null;

		[Desc("Apply (un)deploy animations to sprite bodies with these names.")]
		public readonly string[] BodyNames = { "body" };

		[Desc("Facing that the actor must face before deploying. Set to -1 to deploy regardless of facing.")]
		public readonly int Facing = -1;

		[Desc("Sound to play when deploying.")]
		public readonly string DeploySound = null;

		[Desc("Sound to play when undeploying.")]
		public readonly string UndeploySound = null;

		[Desc("If true, deploy/undeploy sounds will be audible through fog of war.")]
		public readonly bool AudibleThroughFog = true;

		public readonly bool StartsFullyCharged = false;

		[Desc("If true, the condition will be granted instantly (not delayed by animations/activities).")]
		public readonly bool Instant = false;

		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("If true, after deploying the condition will only start to drain after the actor attacks.")]
		public readonly bool DischargeOnAttack = false;

		[Desc("If DischargeOnAttack is true, maximum ticks before the condition will begin to drain regardless.")]
		public readonly int MaxTicksBeforeDischarge = 0;

		public readonly bool ShowSelectionBar = true;
		public readonly bool ShowSelectionBarWhenFull = true;
		public readonly bool ShowSelectionBarWhenEmpty = true;
		public readonly Color ChargingColor = Color.DarkRed;
		public readonly Color DischargingColor = Color.DarkMagenta;

		public override object Create(ActorInitializer init) { return new GrantTimedConditionOnDeploy(init.Self, this); }
	}

	public enum TimedDeployState { Charging, Ready, Active, Deploying, Undeploying }

	public class GrantTimedConditionOnDeploy : PausableConditionalTrait<GrantTimedConditionOnDeployInfo>,
		IResolveOrder, IIssueOrder, ISelectionBar, IOrderVoice, ISync, ITick, IIssueDeployOrder, INotifyAttack
	{
		readonly Actor self;
		readonly bool canTurn;
		int deployedToken = Actor.InvalidConditionToken;
		int deployingToken = Actor.InvalidConditionToken;
		int chargingToken = Actor.InvalidConditionToken;
		bool attacked;

		WithSpriteBody[] wsbs;

		[Sync]
		int ticks;
		int ticksUntilForcedDischarge;

		TimedDeployState deployState;

		public TimedDeployState DeployState { get { return deployState; } }

		public GrantTimedConditionOnDeploy(Actor self, GrantTimedConditionOnDeployInfo info)
			: base(info)
		{
			this.self = self;
			canTurn = self.Info.HasTraitInfo<IFacingInfo>();
			attacked = false;
		}

		protected override void Created(Actor self)
		{
			wsbs = self.TraitsImplementing<WithSpriteBody>().Where(w => Info.BodyNames.Contains(w.Info.Name)).ToArray();

			if (Info.StartsFullyCharged)
			{
				ticks = Info.DeployedTicks;
				deployState = TimedDeployState.Ready;
			}
			else
			{
				ticks = Info.CooldownTicks;
				deployState = TimedDeployState.Charging;
				chargingToken = self.GrantCondition(Info.ChargingCondition);
			}

			ticksUntilForcedDischarge = Info.MaxTicksBeforeDischarge;
			base.Created(self);
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			return new Order("GrantTimedConditionOnDeploy", self, queued);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) { return !IsTraitPaused && !IsTraitDisabled; }

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (!IsTraitDisabled)
					yield return new DeployOrderTargeter("GrantTimedConditionOnDeploy", 5,
						() => IsCursorBlocked() ? Info.DeployBlockedCursor : Info.DeployCursor);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "GrantTimedConditionOnDeploy")
				return new Order(order.OrderID, self, queued);

			return null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "GrantTimedConditionOnDeploy" || deployState != TimedDeployState.Ready)
				return;

			if (!order.Queued)
			{
				if (Info.Instant)
				{
					Deploy();
					return;
				}
				else
					self.CancelActivity();
			}

			// Turn to the required facing.
			if (Info.Facing != -1 && canTurn)
				self.QueueActivity(new Turn(self, WAngle.FromFacing(Info.Facing)));

			self.QueueActivity(new CallFunc(Deploy));
		}

		bool IsCursorBlocked()
		{
			return deployState != TimedDeployState.Ready && !IsTraitPaused;
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "GrantTimedConditionOnDeploy" && deployState == TimedDeployState.Ready ? Info.Voice : null;
		}

		public void Deploy()
		{
			// Something went wrong, most likely due to deploy order spam and the fact that this is a delayed action.
			if (deployState != TimedDeployState.Ready)
				return;

			deployState = TimedDeployState.Deploying;

			if (!string.IsNullOrEmpty(Info.DeploySound) && (Info.AudibleThroughFog || !self.World.FogObscures(self.CenterPosition)))
				Game.Sound.Play(SoundType.World, Info.DeploySound, self.CenterPosition);

			var wsb = wsbs.FirstEnabledTraitOrDefault();
			var hasDeployAnimation = wsb != null && !string.IsNullOrEmpty(Info.DeployAnimation);

			// If there is no animation to play just grant the upgrades that are used while deployed.
			// Alternatively, play the deploy animation and then grant the upgrades.
			if (Info.Instant || !hasDeployAnimation)
			{
				OnDeployCompleted();

				if (hasDeployAnimation)
					wsb.PlayCustomAnimation(self, Info.DeployAnimation);
			}
			else
			{
				if (deployingToken == Actor.InvalidConditionToken)
					deployingToken = self.GrantCondition(Info.DeployingCondition);
				wsb.PlayCustomAnimation(self, Info.DeployAnimation, OnDeployCompleted);
			}
		}

		void OnDeployCompleted()
		{
			if (deployedToken == Actor.InvalidConditionToken)
				deployedToken = self.GrantCondition(Info.DeployedCondition);

			RevokeDeployingCondition();

			deployState = TimedDeployState.Active;
			ticksUntilForcedDischarge = Info.MaxTicksBeforeDischarge;
		}

		void RevokeDeploy()
		{
			deployState = TimedDeployState.Undeploying;

			if (!string.IsNullOrEmpty(Info.UndeploySound) && (Info.AudibleThroughFog || !self.World.FogObscures(self.CenterPosition)))
				Game.Sound.Play(SoundType.World, Info.UndeploySound, self.CenterPosition);

			var wsb = wsbs.FirstEnabledTraitOrDefault();

			if (string.IsNullOrEmpty(Info.UndeployAnimation) || wsb == null)
				OnUndeployCompleted();
			else
			{
				if (deployingToken == Actor.InvalidConditionToken)
					deployingToken = self.GrantCondition(Info.DeployingCondition);
				wsb.PlayCustomAnimation(self, Info.UndeployAnimation, OnUndeployCompleted);
			}
		}

		void RevokeDeployingCondition()
		{
			attacked = false;
			if (deployingToken != Actor.InvalidConditionToken)
				deployingToken = self.RevokeCondition(deployingToken);
		}

		void RevokeDeployedCondition()
		{
			if (deployedToken != Actor.InvalidConditionToken)
				deployedToken = self.RevokeCondition(deployedToken);
		}

		void RevokeChargingCondition()
		{
			if (chargingToken != Actor.InvalidConditionToken)
				chargingToken = self.RevokeCondition(chargingToken);
		}

		void OnUndeployCompleted()
		{
			RevokeDeployingCondition();
			RevokeDeployedCondition();

			deployState = TimedDeployState.Charging;
			ticks = Info.CooldownTicks;

			if (chargingToken == Actor.InvalidConditionToken)
				chargingToken = self.GrantCondition(Info.ChargingCondition);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitPaused || IsTraitDisabled)
				return;

			if (deployState == TimedDeployState.Ready || deployState == TimedDeployState.Deploying || deployState == TimedDeployState.Undeploying)
				return;

			if (deployState == TimedDeployState.Active && Info.DischargeOnAttack && !attacked && (Info.MaxTicksBeforeDischarge == 0 || --ticksUntilForcedDischarge > 0))
				return;

			if (--ticks < 0)
			{
				if (deployState == TimedDeployState.Charging)
				{
					ticks = Info.DeployedTicks;
					deployState = TimedDeployState.Ready;
					RevokeChargingCondition();
				}
				else
					RevokeDeploy();
			}
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			attacked = true;
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled || !Info.ShowSelectionBar || deployState == TimedDeployState.Undeploying)
				return 0f;

			if (!Info.ShowSelectionBarWhenFull && deployState == TimedDeployState.Ready)
				return 0f;

			if (deployState == TimedDeployState.Deploying || deployState == TimedDeployState.Ready)
				return 1f;

			return deployState == TimedDeployState.Charging
				? (float)(Info.CooldownTicks - ticks) / Info.CooldownTicks
				: (float)ticks / Info.DeployedTicks;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return Info.ShowSelectionBar ? (deployState == TimedDeployState.Ready ? Info.ShowSelectionBarWhenFull : Info.ShowSelectionBarWhenEmpty) : false; } }

		Color ISelectionBar.GetColor() { return deployState == TimedDeployState.Charging ? Info.ChargingColor : Info.DischargingColor; }
	}
}
