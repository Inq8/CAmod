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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Unit must face the target and charge up to fire. ",
		"Note: All armaments will share the charge, so its best suited for units with a single weapon.")]
	public class AttackFrontalChargedInfo : AttackFrontalInfo, Requires<IFacingInfo>
	{
		[Desc("Amount of charge required to attack. Use two numbers to represent a random number in a range (lower value included, upper excluded).")]
		public readonly int[] ChargeLevel = { 25 };

		[Desc("Amount to increase the charge level each tick with a valid target.")]
		public readonly int ChargeRate = 1;

		[Desc("Amount to decrease the charge level each tick without a valid target.")]
		public readonly int DischargeRate = 1;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while the charge level is greater than zero.")]
		public readonly string ChargingCondition = null;

		[Desc("Array of charge levels on which to add a stack of ChargingCondition.")]
		public readonly int[] ConditionChargeLevels = null;

		[Desc("Number of shots that can be fired after charging.")]
		public readonly int ShotsPerCharge = 1;

		[Desc("Armaments that count towards shots per charge. Leave empty for all.")]
		public readonly string[] ChargeConsumingArmaments = default;

		[Desc("If true, charge will be lost while turning.")]
		public readonly bool LosesChargeWhileTurning = false;

		public readonly bool ShowSelectionBar = false;
		public readonly Color SelectionBarColor = Color.FromArgb(128, 200, 255);

		public override object Create(ActorInitializer init) { return new AttackFrontalCharged(init.Self, this); }
	}

	public class AttackFrontalCharged : AttackFrontal, INotifyAttack, INotifySold, ISelectionBar
	{
		public new readonly AttackFrontalChargedInfo Info;
		readonly IMove movement;
		readonly Stack<int> chargingTokens = new Stack<int>();

		bool charging;
		int shotsFired;
		int requiredChargeLevel;

		public int ChargeLevel { get; private set; }

		int StackCount
		{
			get
			{
				if (Info.ConditionChargeLevels == null)
					return ChargeLevel > 0 ? 1 : 0;

				var stackCount = 0;

				foreach (var level in Info.ConditionChargeLevels)
				{
					if (ChargeLevel >= level)
						stackCount++;
				}

				return stackCount;
			}
		}

		public bool IsCharged
		{
			get
			{
				return ChargeLevel >= requiredChargeLevel;
			}
		}

		public bool IsTurning
		{
			get
			{
				return movement != null && (movement.CurrentMovementTypes & MovementType.Turn) != 0;
			}
		}

		public AttackFrontalCharged(Actor self, AttackFrontalChargedInfo info)
			: base(self, info)
		{
			Info = info;
			shotsFired = 0;
			movement = self.TraitOrDefault<IMove>();
			requiredChargeLevel = Common.Util.RandomInRange(self.World.SharedRandom, info.ChargeLevel);
		}

		protected override void TraitEnabled(Actor self)
		{
			if (self.CurrentActivity is Attack && !(self.CurrentActivity is AttackCharged))
				self.CurrentActivity.Cancel(self);
		}

		protected override void Tick(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (!IsAiming && ChargeLevel == 0)
				return;

			var reloading = false;
			foreach (var armament in Armaments)
			{
				if (!armament.IsTraitDisabled && armament.IsReloading && armament.Burst == armament.Weapon.Burst)
				{
					reloading = true;
					break;
				}
			}

			// Stop charging when we lose our target
			bool isTurning = IsTurning;

			charging =
				(self.CurrentActivity is AttackCharged || self.CurrentActivity is AttackMoveActivity || self.CurrentActivity is Hunt || self.CurrentActivity is HuntCA)
				&& !reloading
				&& IsAiming
				&& (!isTurning || (isTurning && ChargeLevel > 0 && !Info.LosesChargeWhileTurning));

			var delta = charging ? Info.ChargeRate : -Info.DischargeRate;
			ChargeLevel = (ChargeLevel + delta).Clamp(0, requiredChargeLevel);

			if (!charging)
				shotsFired = 0;

			UpdateConditionInstances(self);

			base.Tick(self);
		}

		void UpdateConditionInstances(Actor self)
		{
			if (string.IsNullOrEmpty(Info.ChargingCondition))
				return;

			while (chargingTokens.Count > StackCount)
				self.RevokeCondition(chargingTokens.Pop());

			while (chargingTokens.Count < StackCount)
				chargingTokens.Push(self.GrantCondition(Info.ChargingCondition));
		}

		public override Activity GetAttackActivity(Actor self, AttackSource source, in Target newTarget, bool allowMove, bool forceAttack, Color? targetLineColor = null)
		{
			return new AttackCharged(self, newTarget, allowMove, forceAttack, targetLineColor);
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (Info.ChargeConsumingArmaments != null && !Info.ChargeConsumingArmaments.Contains(a.Info.Name))
				return;

			shotsFired++;
			if (shotsFired >= Info.ShotsPerCharge)
			{
				shotsFired = 0;
				ChargeLevel = 0;
				requiredChargeLevel = Common.Util.RandomInRange(self.World.SharedRandom, Info.ChargeLevel);
			}
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }
		void INotifySold.Selling(Actor self) { ChargeLevel = 0; }
		void INotifySold.Sold(Actor self) { }

		float ISelectionBar.GetValue()
		{
			if (!Info.ShowSelectionBar || ChargeLevel == requiredChargeLevel)
				return 0;

			return (float)ChargeLevel / requiredChargeLevel;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

		Color ISelectionBar.GetColor() { return Info.SelectionBarColor; }
	}
}
