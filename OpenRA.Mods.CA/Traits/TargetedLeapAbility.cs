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
using OpenRA.Graphics;
using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Allows unit to leap to a targeted location.")]
	public class TargetedLeapAbilityInfo : PausableConditionalTraitInfo, Requires<MobileInfo>
	{
		[Desc("Cooldown in ticks until the unit can teleport.")]
		public readonly int ChargeDelay = 25;

		[Desc("The maximum distance in cells this unit can teleport (only used if HasDistanceLimit = true).")]
		public readonly int MaxDistance = 10;

		[Desc("Leap speed (in WDist units/tick).")]
		public readonly int Speed = 150;

		[Desc("Possible sounds to play when taking off.")]
		public readonly string[] TakeOffSounds = null;

		[Desc("Possible sounds to play when landing.")]
		public readonly string[] LandingSounds = null;

		[CursorReference]
		[Desc("Cursor to display when able to deploy the actor.")]
		public readonly string DeployCursor = "deploy";

		[CursorReference]
		[Desc("Cursor to display when unable to deploy the actor.")]
		public readonly string DeployBlockedCursor = "deploy-blocked";

		[CursorReference]
		[Desc("Cursor to display when targeting a teleport location.")]
		public readonly string TargetCursor = "ability";

		[CursorReference]
		[Desc("Cursor to display when the targeted location is blocked.")]
		public readonly string TargetBlockedCursor = "move-blocked";

		[GrantedConditionReference]
		[Desc("The condition to grant while leaping.")]
		public readonly string LeapCondition = null;

		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Range circle color.")]
		public readonly Color CircleColor = Color.FromArgb(128, Color.LawnGreen);

		[Desc("Range circle line width.")]
		public readonly float CircleWidth = 1;

		[Desc("Range circle border color.")]
		public readonly Color CircleBorderColor = Color.FromArgb(96, Color.Black);

		[Desc("Range circle border width.")]
		public readonly float CircleBorderWidth = 3;

		[Desc("Color to use for the target line.")]
		public readonly Color TargetLineColor = Color.LawnGreen;

		[Desc("Number of charges.")]
		public readonly int Charges = 1;

		[Desc("If true, gain max charges after recharging.")]
		public readonly bool RechargeToMax = false;

		[Desc("If true recharge will reset any ongoing recharge on teleport.")]
		public readonly bool ResetRechargeOnUse = true;

		[Desc("Cooldown between jumps (irrespective of charge).")]
		public readonly int Cooldown = 0;

		public readonly bool ShowSelectionBar = true;
		public readonly bool ShowSelectionBarWhenFull = true;

		[Desc("Selection bar color.")]
		public readonly Color SelectionBarColor = Color.Magenta;

		public readonly bool ShowCooldownSelectionBar = false;

		[Desc("Cooldown selection bar color.")]
		public readonly Color CooldownSelectionBarColor = Color.Silver;

		[CursorReference]
		[Desc("Cursor to display when targeting a teleport location with modifier key held.")]
		public readonly string TargetModifiedCursor = null;

		public override object Create(ActorInitializer init) { return new TargetedLeapAbility(init.Self, this); }
	}

	public class TargetedLeapAbility : PausableConditionalTrait<TargetedLeapAbilityInfo>, IIssueOrder, IResolveOrder, ITick, ISelectionBar, IOrderVoice, ISync, IIssueDeployOrder
	{
		public readonly new TargetedLeapAbilityInfo Info;
		readonly Mobile mobile;
		IFacing facing;

		[Sync]
		int chargeTick = 0;

		[Sync]
		int cooldownTicks = 0;

		public int ChargeDelay { get; private set; }
		public int MaxDistance { get; private set; }
		public int MaxCharges { get; private set; }
		public int Charges { get; private set; }

		public TargetedLeapAbility(Actor self, TargetedLeapAbilityInfo info)
			: base(info)
		{
			Info = info;
			mobile = self.Trait<Mobile>();
			ChargeDelay = Info.ChargeDelay;
			MaxDistance = Info.MaxDistance;
			MaxCharges = Info.Charges;
			Charges = Info.Charges;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			facing = self.TraitOrDefault<IFacing>();
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (cooldownTicks > 0)
				cooldownTicks--;

			if (chargeTick > 0)
			{
				chargeTick--;

				if (chargeTick == 0)
				{
					if (Info.RechargeToMax)
					{
						Charges = MaxCharges;
					}
					else
					{
						if (Charges < MaxCharges)
							Charges++;

						if (Charges < MaxCharges)
							ResetChargeTime();
					}
				}
			}
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			// HACK: Switch the global order generator instead of actually issuing an order
			if (CanLeap)
				self.World.OrderGenerator = new TargetableLeapOrderTargeterOrderGenerator(self, this, mobile);

			// HACK: We need to issue a fake order to stop the game complaining about the bodge above
			return new Order("TargetableLeapOrderTargeterDeploy", self, Target.Invalid, queued);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) { return !IsTraitPaused && !IsTraitDisabled; }

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (IsTraitDisabled)
					yield break;

				yield return new TargetableLeapOrderTargeterOrderTargeter(Info.TargetCursor);
				yield return new DeployOrderTargeter("TargetableLeapOrderTargeterDeploy", 5,
					() => CanLeap ? Info.DeployCursor : Info.DeployBlockedCursor);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "TargetableLeapOrderTargeterDeploy")
			{
				// HACK: Switch the global order generator instead of actually issuing an order
				if (CanLeap)
					self.World.OrderGenerator = new TargetableLeapOrderTargeterOrderGenerator(self, this, mobile);

				// HACK: We need to issue a fake order to stop the game complaining about the bodge above
				return new Order(order.OrderID, self, Target.Invalid, queued);
			}

			if (order.OrderID == "TargetableLeapOrderLeap")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "TargetableLeapOrderLeap" && CanLeap && order.Target.Type != TargetType.Invalid)
			{
				if (!order.Queued)
					self.CancelActivity();

				var cell = self.World.Map.CellContaining(order.Target.CenterPosition);

				self.QueueActivity(mobile.MoveWithinRange(order.Target, WDist.FromCells(Info.MaxDistance), targetLineColor: Info.TargetLineColor));

				if (facing != null)
				{
					var desiredFacing = (order.Target.CenterPosition - self.CenterPosition).Yaw;
					self.QueueActivity(new Turn(self, desiredFacing));
				}

				self.QueueActivity(new TargetedLeap(self, cell, this, mobile, facing, WAngle.FromDegrees(60)));

				//self.QueueActivity(mobile.MoveTo(cell, 5, targetLineColor: Info.TargetLineColor));
				self.ShowTargetLines();
			}
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "TargetableLeapOrderLeap" && CanLeap ? Info.Voice : null;
		}

		public void ConsumeCharge()
		{
			cooldownTicks = Info.Cooldown;

			Charges--;

			if (Info.ResetRechargeOnUse || chargeTick == 0)
				ResetChargeTime();
		}

		public void ResetChargeTime()
		{
			chargeTick = ChargeDelay;
		}

		public bool CanLeap => !IsTraitDisabled && !IsTraitPaused && Charges > 0 && cooldownTicks == 0;

		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled)
				return 0f;

			if (Info.ShowCooldownSelectionBar && cooldownTicks > 0 && Charges > 0)
				return (float)(Info.Cooldown - cooldownTicks) / Info.Cooldown;

			if (!Info.ShowSelectionBar || chargeTick == ChargeDelay)
				return 0f;

			if (!Info.ShowSelectionBarWhenFull && chargeTick == 0)
				return 0f;

			return (float)(ChargeDelay - chargeTick) / ChargeDelay;
		}

		Color ISelectionBar.GetColor() { return Info.ShowCooldownSelectionBar && cooldownTicks > 0 && Charges > 0 ? Info.CooldownSelectionBarColor : Info.SelectionBarColor; }
		bool ISelectionBar.DisplayWhenEmpty => false;
	}

	class TargetableLeapOrderTargeterOrderTargeter : IOrderTargeter
	{
		readonly string targetCursor;

		public TargetableLeapOrderTargeterOrderTargeter(string targetCursor)
		{
			this.targetCursor = targetCursor;
		}

		public string OrderID => "TargetableLeapOrderLeap";
		public int OrderPriority => 5;
		public bool IsQueued { get; protected set; }
		public bool TargetOverridesSelection(Actor self, in Target target, List<Actor> actorsAt, CPos xy, TargetModifiers modifiers) { return true; }

		public bool CanTarget(Actor self, in Target target, ref TargetModifiers modifiers, ref string cursor)
		{
			if (modifiers.HasModifier(TargetModifiers.ForceMove))
			{
				var xy = self.World.Map.CellContaining(target.CenterPosition);

				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				if (self.IsInWorld && self.Owner.Shroud.IsExplored(xy))
				{
					cursor = targetCursor;
					return true;
				}

				return false;
			}

			return false;
		}
	}

	class TargetableLeapOrderTargeterOrderGenerator : OrderGenerator
	{
		readonly Actor self;
		readonly Mobile mobile;
		readonly TargetedLeapAbility TargetableLeapOrderTargeter;
		readonly TargetedLeapAbilityInfo info;
		readonly IEnumerable<TraitPair<TargetedLeapAbility>> selectedWithAbility;

		public TargetableLeapOrderTargeterOrderGenerator(Actor self, TargetedLeapAbility TargetableLeapOrderTargeter, Mobile mobile)
		{
			this.self = self;
			this.mobile = mobile;
			this.TargetableLeapOrderTargeter = TargetableLeapOrderTargeter;
			info = TargetableLeapOrderTargeter.Info;

			selectedWithAbility = self.World.Selection.Actors
				.Where(a => a.Info.HasTraitInfo<TargetedLeapAbilityInfo>() && a.Owner == self.Owner && !a.IsDead)
				.Select(a => new TraitPair<TargetedLeapAbility>(a, a.Trait<TargetedLeapAbility>()));
		}

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
			{
				world.CancelInputMode();
				yield break;
			}

			if (self.IsInWorld
				&& self.Location != cell
				&& self.Trait<TargetedLeapAbility>().CanLeap
				&& self.Owner.Shroud.IsExplored(cell)
				&& mobile.CanEnterCell(cell)
			) {
				world.CancelInputMode();
				var targetCell = Target.FromCell(world, cell);

				var selectedOrderedByDistance = selectedWithAbility
					.Where(a => !a.Actor.IsDead
						&& a.Actor.Owner == self.Owner
						&& a.Actor.IsInWorld
						&& a.Trait.CanLeap)
					.OrderBy(a => (a.Actor.CenterPosition - targetCell.CenterPosition).Length);

				if (mi.Modifiers.HasModifier(Modifiers.Ctrl))
				{
					var closest = selectedOrderedByDistance.First();
					yield return new Order("TargetableLeapOrderLeap", closest.Actor, targetCell, mi.Modifiers.HasModifier(Modifiers.Shift));
				}
				else
				{
					foreach (var s in selectedOrderedByDistance)
						yield return new Order("TargetableLeapOrderLeap", s.Actor, targetCell, mi.Modifiers.HasModifier(Modifiers.Shift));
				}
			}
		}

		protected override void SelectionChanged(World world, IEnumerable<Actor> selected)
		{
			if (!selected.Contains(self))
				world.CancelInputMode();
		}

		protected override void Tick(World world)
		{
			if (TargetableLeapOrderTargeter.IsTraitDisabled || TargetableLeapOrderTargeter.IsTraitPaused)
			{
				world.CancelInputMode();
				return;
			}
		}

		protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }

		protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }

		protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
		{
			if (!self.IsInWorld || self.Owner != self.World.LocalPlayer)
				yield break;

			if (info.CircleWidth > 0)
			{
				foreach (var s in selectedWithAbility)
				{
					if (s.Actor.IsInWorld && s.Trait.CanLeap && self.Owner == self.World.LocalPlayer)
					{
						yield return new RangeCircleAnnotationRenderable(
							s.Actor.CenterPosition + new WVec(0, s.Actor.CenterPosition.Z, 0),
							WDist.FromCells(s.Trait.Info.MaxDistance),
							0,
							s.Trait.Info.CircleColor,
							s.Trait.Info.CircleWidth,
							s.Trait.Info.CircleBorderColor,
							s.Trait.Info.CircleBorderWidth);
					}
				}
			}
		}

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (self.IsInWorld
				&& self.Location != cell
				&& TargetableLeapOrderTargeter.CanLeap
				&& self.Owner.Shroud.IsExplored(cell)
				&& mobile.CanEnterCell(cell))
				return info.TargetModifiedCursor != null && mi.Modifiers.HasModifier(Modifiers.Ctrl) ? info.TargetModifiedCursor : info.TargetCursor;
			else
				return info.TargetBlockedCursor;
		}
	}
}
