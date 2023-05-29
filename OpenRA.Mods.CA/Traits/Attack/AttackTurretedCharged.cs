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
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Actor has a visual turret used to attack and must charge before firing. ",
		"Note: All armaments will share the charge, so its best suited for units with a single weapon.")]
	public class AttackTurretedChargedInfo : AttackTurretedInfo, Requires<TurretedInfo>
	{
		[Desc("Amount of charge required to attack.")]
		public readonly int ChargeLevel = 25;

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

		[Desc("Charging sounds.")]
		public readonly string[] ChargingSounds = null;

		[Desc("Charging sound audible through fog.")]
		public readonly bool ChargeAudibleThroughFog = true;

		[Desc("If true, will charge while turret is turning to face the target.")]
		public readonly bool ChargeWhileTurning = false;

		public readonly bool ShowSelectionBar = false;
		public readonly Color SelectionBarColor = Color.FromArgb(128, 200, 255);

		public override object Create(ActorInitializer init) { return new AttackTurretedCharged(init.Self, this); }
	}

	public class AttackTurretedCharged : AttackTurreted, INotifyAttack, INotifySold, ISelectionBar
	{
		public new readonly AttackTurretedChargedInfo Info;

		readonly Stack<int> chargingTokens = new Stack<int>();

		bool turretReady;
		bool charging;
		int shotsFired;

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
				return ChargeLevel >= Info.ChargeLevel;
			}
		}

		public AttackTurretedCharged(Actor self, AttackTurretedChargedInfo info)
			: base(self, info)
		{
			Info = info;
			shotsFired = 0;
			turretReady = false;
		}

		protected override void Tick(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			var reloading = false;
			foreach (var armament in Armaments)
			{
				if (armament.IsTraitEnabled() && armament.FireDelay > 0 && armament.Burst == armament.Weapon.Burst)
				{
					reloading = true;
					break;
				}
			}

			// Stop charging when we lose our target
			charging = !reloading && IsAiming && (Info.ChargeWhileTurning || turretReady);

			if (charging && ChargeLevel == 0)
				ChargeSound(self);

			var delta = charging ? Info.ChargeRate : -Info.DischargeRate;
			ChargeLevel = (ChargeLevel + delta).Clamp(0, Info.ChargeLevel);

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

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			shotsFired++;
			if (shotsFired >= Info.ShotsPerCharge)
			{
				shotsFired = 0;
				ChargeLevel = 0;
			}
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }
		void INotifySold.Selling(Actor self) { ChargeLevel = 0; }
		void INotifySold.Sold(Actor self) { }

		float ISelectionBar.GetValue()
		{
			if (!Info.ShowSelectionBar || ChargeLevel == Info.ChargeLevel)
				return 0;

			return (float)ChargeLevel / Info.ChargeLevel;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

		Color ISelectionBar.GetColor() { return Info.SelectionBarColor; }

		// Main change with AttackTurreted, also checks if charged here
		protected override bool CanAttack(Actor self, in Target target)
		{
			if (target.Type == TargetType.Invalid)
				return false;

			// Don't break early from this loop - we want to bring all turrets to bear!
			turretReady = false;
			foreach (var t in turrets)
				if (t.FaceTarget(self, target))
					turretReady = true;

			return turretReady && base.CanAttack(self, target) && IsCharged;
		}

		void ChargeSound(Actor self)
		{
			if (Info.ChargingSounds == null)
				return;

			var sound = Info.ChargingSounds.RandomOrDefault(Game.CosmeticRandom);
			var shouldStart = Info.ChargeAudibleThroughFog || (!self.World.ShroudObscures(self.CenterPosition) && !self.World.FogObscures(self.CenterPosition));

			if (!shouldStart)
				return;

			Game.Sound.Play(SoundType.World, sound, self.CenterPosition);
		}
	}
}
