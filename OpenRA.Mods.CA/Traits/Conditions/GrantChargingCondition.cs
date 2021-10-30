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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Gives a condition to the actor that charges when enabled,",
		"drains gradually when paused, and is revoked when fully drained or disabled.")]
	public class GrantChargingConditionInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition type to grant.")]
		public readonly string Condition = null;

		[Desc("Initial charge.")]
		public readonly int InitialCharge = 0;

		[Desc("Maximum charge.")]
		public readonly int MaxCharge = 100;

		[Desc("Charge amount per tick when trait is enabled.")]
		public readonly int ChargeRate = 1;

		[Desc("Charge drained per tick when trait is paused.")]
		public readonly int DischargeRate = 1;

		[Desc("Delay in ticks before charging after being enabled.")]
		public readonly int ChargeDelay = 0;

		[Desc("Minimum charge before reactivating after being disabled.")]
		public readonly int MinReactivationCharge = 0;

		public readonly bool ShowSelectionBar = true;
		public readonly bool ShowSelectionBarWhenFull = true;
		public readonly bool ShowSelectionBarWhenEmpty = true;
		public readonly Color ChargingColor = Color.DarkRed;
		public readonly Color DischargingColor = Color.DarkMagenta;

		public override object Create(ActorInitializer init) { return new GrantChargingCondition(init, this); }
	}

	public class GrantChargingCondition : PausableConditionalTrait<GrantChargingConditionInfo>, INotifyCreated, ITick, ISelectionBar
	{
		int token = Actor.InvalidConditionToken;
		int chargeDelay;
		bool forceCharging = false;

		[Sync]
		int charge;

		public GrantChargingCondition(ActorInitializer init, GrantChargingConditionInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			charge = Info.InitialCharge;
			chargeDelay = Info.ChargeDelay;

			if (charge == Info.MaxCharge)
				GrantCondition(self);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (!IsTraitPaused || (forceCharging && charge < Info.MinReactivationCharge))
			{
				if (charge == Info.MaxCharge)
					return;

				if (chargeDelay > 0 && --chargeDelay > 0)
					return;

				charge += Info.ChargeRate;

				if (charge > Info.MaxCharge)
					charge = Info.MaxCharge;

				if (forceCharging && charge < Info.MinReactivationCharge)
				{
					RevokeCondition(self);
					return;
				}

				GrantCondition(self);
			}
			else
			{
				if (charge == 0)
					return;

				charge -= Info.DischargeRate;

				if (charge <= 0)
				{
					charge = 0;
					RevokeCondition(self);
				}
			}
		}

		void GrantCondition(Actor self)
		{
			forceCharging = false;

			if (token == Actor.InvalidConditionToken)
				token = self.GrantCondition(Info.Condition);
		}

		void RevokeCondition(Actor self)
		{
			forceCharging = true;

			if (token == Actor.InvalidConditionToken)
				return;

			token = self.RevokeCondition(token);
		}

		protected override void TraitDisabled(Actor self)
		{
			charge = 0;
			chargeDelay = Info.ChargeDelay;
			RevokeCondition(self);
		}

		protected override void TraitPaused(Actor self)
		{
			chargeDelay = Info.ChargeDelay;
			forceCharging = true;
		}

		float ISelectionBar.GetValue()
		{
			if (!Info.ShowSelectionBar || IsTraitDisabled)
				return 0f;

			if (!Info.ShowSelectionBarWhenFull && charge == Info.MaxCharge)
				return 0f;

			return (float)charge / Info.MaxCharge;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return charge == Info.MaxCharge ? Info.ShowSelectionBarWhenFull : Info.ShowSelectionBarWhenEmpty; } }

		Color ISelectionBar.GetColor() { return IsTraitPaused ? Info.DischargingColor : Info.ChargingColor; }
	}
}
