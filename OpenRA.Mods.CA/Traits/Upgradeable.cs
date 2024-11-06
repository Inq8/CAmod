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
using OpenRA.Activities;
using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Lists actors this actor may be upgraded to.")]
	public class UpgradeableInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Upgrade type. Upgrade takes effect when a ProvidesUpgrade with the same type is created and enabled.")]
		public readonly string Type = null;

		[Desc("Actor to transform into.")]
		public readonly string Actor = null;

		[GrantedConditionReference]
		[Desc("Condition to apply when upgrade is complete.")]
		public readonly string Condition = null;

		[Desc("Cost (-1 indicates to use the difference between source and target actor cost, or zero if no target actor).")]
		public readonly int Cost = -1;

		[Desc("Base build time in frames (-1 indicates to use the unit's Value).")]
		public readonly int BuildDuration = -1;

		[Desc("Percentage modifier to apply to the build duration.")]
		public readonly int BuildDurationModifier = 60;

		[Desc("If true, skips make animation.")]
		public readonly bool SkipMakeAnims = true;

		[GrantedConditionReference]
		[Desc("Condition to apply while upgrading.")]
		public readonly string UpgradingCondition = null;

		[Desc("Voice to use on upgrade completion.")]
		public readonly string UpgradeCompleteSpeechNotification = "UpgradeComplete";

		[Desc("Sound to play on upgrade completion.")]
		public readonly string UpgradeSound = "voveupgr.aud";

		[ActorReference]
		[Desc("If set, must upgrade near one of these actors.")]
		public readonly HashSet<string> UpgradeAtActors = new HashSet<string> { };

		[Desc("If UpgradeAtActors are set, defines the max distance to upgrade.")]
		public readonly WDist UpgradeAtRange = WDist.FromCells(3);

		[Desc("Color to use for the target line.")]
		public readonly Color TargetLineColor = Color.Cyan;

		[CursorReference]
		[Desc("Cursor to display when able to be upgraded near target actor.")]
		public readonly string UpgradeCursor = "upgrade";

		[CursorReference]
		[Desc("Cursor to display when unable to be upgraded near target actor.")]
		public readonly string UpgradeBlockedCursor = "upgrade-blocked";

		[VoiceReference]
		public readonly string Voice = "Action";

		public readonly bool ShowSelectionBar = true;
		public readonly bool ShowSelectionBarWhenEmpty = false;
		public readonly Color SelectionBarColor = Color.Cyan;

		public override object Create(ActorInitializer init) { return new Upgradeable(init.Self, this); }
	}

	public class Upgradeable : ConditionalTrait<UpgradeableInfo>, IResolveOrder, INotifyCreated, INotifyOwnerChanged, ISelectionBar, IOrderVoice, IIssueOrder
	{
		public readonly new UpgradeableInfo Info;
		readonly PlayerResources playerResources;
		readonly Actor self;
		UpgradesManager upgradesManager;
		Upgrade currentUpgrade;

		public UpgradeInfo UpgradeInfo { get; private set; }

		bool unlocked;
		bool upgraded;
		int upgradeTicksRemaining;

		public bool CanUpgrade
		{
			get { return unlocked && !upgraded && !IsTraitDisabled; }
		}

		public bool CanCancelUpgrade
		{
			get { return currentUpgrade != null && currentUpgrade.State == ActivityState.Active; }
		}

		public Upgradeable(Actor self, UpgradeableInfo info)
			: base(info)
		{
			Info = info;
			this.self = self;
			upgradesManager = self.Owner.PlayerActor.Trait<UpgradesManager>();
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			unlocked = upgraded = false;
			UpgradeInfo = new UpgradeInfo();
		}

		protected override void Created(Actor self)
		{
			var upgradeInfo = upgradesManager.UpgradeableActorCreated(this, Info.Type, self.Info.Name, Info.Actor, Info.Cost, Info.BuildDuration, Info.BuildDurationModifier);
			UpgradeInfo.BuildDuration = upgradeInfo.BuildDuration;
			UpgradeInfo.Cost = upgradeInfo.Cost;
			UpgradeInfo.ActorName = upgradeInfo.ActorName;

			if (upgradesManager.IsUnlocked(Info.Type))
				Unlock();

			base.Created(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			upgradesManager = newOwner.PlayerActor.Trait<UpgradesManager>();
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			// Ensure we're only interacting with this specific Upgradeable trait
			if (order.TargetString != Info.Type)
				return;

			if (order.OrderString == "Upgrade" && CanUpgrade)
			{
				currentUpgrade = new Upgrade(self, order.Target, this, playerResources, (int ticks) => upgradeTicksRemaining = ticks, Info.TargetLineColor);

				if (!order.Queued)
					currentUpgrade.NextActivity?.Cancel(self);

				self.QueueActivity(order.Queued, currentUpgrade);
				self.ShowTargetLines();
			}
			else if (order.OrderString == "CancelUpgrade" && CanCancelUpgrade)
				CancelUpgrade(self);
		}

		public IEnumerable<Actor> GetValidHosts()
		{
			return self.World.Actors
				.Where(a => !a.IsDead
					&& a.IsInWorld
					&& a.Owner.IsAlliedWith(self.Owner)
					&& Info.UpgradeAtActors.Contains(a.Info.Name));
		}

		public void Unlock()
		{
			unlocked = true;
		}

		public void Complete()
		{
			upgraded = true;
			Game.Sound.Play(SoundType.World, Info.UpgradeSound, self.CenterPosition);
		}

		public void UpdateManager()
		{
			upgradesManager.Update();
		}

		void CancelUpgrade(Actor self)
		{
			if (CanCancelUpgrade)
				self.CurrentActivity.Cancel(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			CancelUpgrade(self);
		}

		float ISelectionBar.GetValue()
		{
			if (!Info.ShowSelectionBar || !CanCancelUpgrade)
				return 0f;

			return ((float)(UpgradeInfo.BuildDuration - upgradeTicksRemaining) / UpgradeInfo.BuildDuration).Clamp(0f, 1f);
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return Info.ShowSelectionBarWhenEmpty; } }

		Color ISelectionBar.GetColor() { return Info.SelectionBarColor; }

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString == "Upgrade")
				return Info.Voice;

			return null;
		}

		bool CanUpgradeAt(Actor target, TargetModifiers modifiers)
		{
			return CanUpgradeAt(target);
		}

		bool CanUpgradeAt(Actor target)
		{
			return Info.UpgradeAtActors.Contains(target.Info.Name) && CanUpgrade;
		}

		/* // TODO: get this to work nicely with multiple possible upgrade paths
		public bool IsTooltipVisible(Player forPlayer)
		{
			if (!IsTraitDisabled && self.World.OrderGenerator is UpgradeOrderGenerator && CanUpgrade)
				return forPlayer == self.Owner;
			return false;
		}

		public string TooltipText => $"Upgrade Cost: ${UpgradeInfo.Cost}";
		*/

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				yield return new EnterAlliedActorTargeter<BuildingInfo>(
					"Upgrade",
					5,
					Info.UpgradeCursor,
					Info.UpgradeBlockedCursor,
					CanUpgradeAt,
					_ => CanUpgrade);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "Upgrade")
				return new Order(order.OrderID, self, target, queued) { TargetString = Info.Type };

			return null;
		}
	}
}
