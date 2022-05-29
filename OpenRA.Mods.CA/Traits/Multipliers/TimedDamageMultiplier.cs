#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public enum TimedDamageMultiplierState
	{
		Ready,
		Draining,
		Charging
	}

	[Desc("On taking damage, gives damage reduction to the actor for a limited time.")]
	public class TimedDamageMultiplierInfo : ConditionalTraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant.")]
		public readonly string ActiveCondition = null;

		[GrantedConditionReference]
		[Desc("The condition to grant.")]
		public readonly string ChargingCondition = null;

		[Desc("Number of ticks to wait before revoking the condition.")]
		public readonly int Duration = 50;

		[Desc("Number of ticks to wait after condition expires before reactivating.")]
		public readonly int ChargeTime = 50;

		[Desc("Percentage damage modifier.")]
		public readonly int Modifier = 50;

		[Desc("Minimum damage to trigger the timed damage modifier.")]
		public readonly int MinimumDamage = 100;

		[Desc("Play a randomly selected sound from this list when deploying.")]
		public readonly string ActivateSound = null;

		[Desc("Play a randomly selected sound from this list when undeploying.")]
		public readonly string DeactivateSound = null;

		[Desc("If true, charging will be reset on taking any damage.")]
		public readonly bool ResetChargingOnDamage = false;

		[Desc("Damage type(s) that trigger and are affected by the damage multiplier.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		public readonly bool GrantConditionWhenReady = false;
		public readonly bool ShowSelectionBar = true;
		public readonly bool ShowSelectionBarWhenReady = false;
		public readonly Color DrainingColor = Color.LightCyan;
		public readonly Color ChargingColor = Color.Cyan;

		public override object Create(ActorInitializer init) { return new TimedDamageMultiplier(this); }
	}

	public class TimedDamageMultiplier : ConditionalTrait<TimedDamageMultiplierInfo>, ITick, IDamageModifier, INotifyDamage, ISelectionBar
	{
		public new readonly TimedDamageMultiplierInfo Info;
		public int Ticks { get; private set; }
		int token = Actor.InvalidConditionToken;
		TimedDamageMultiplierState state;

		public TimedDamageMultiplier(TimedDamageMultiplierInfo info)
			: base(info)
		{
			Info = info;
			state = TimedDamageMultiplierState.Ready;
		}

		int IDamageModifier.GetDamageModifier(Actor attacker, Damage damage)
		{
			if (IsTraitDisabled || state == TimedDamageMultiplierState.Charging || damage.Value < Info.MinimumDamage)
				return 100;

			var validDamageType = Info.DamageTypes.IsEmpty || damage.DamageTypes.Overlaps(Info.DamageTypes);
			return validDamageType ? Info.Modifier : 100;
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled)
				return;

			if (Info.ResetChargingOnDamage && state == TimedDamageMultiplierState.Charging && e.Damage.Value > 0)
				Ticks = Info.ChargeTime;

			if (state != TimedDamageMultiplierState.Ready || e.Damage.Value < Info.MinimumDamage)
				return;

			if (!Info.DamageTypes.IsEmpty && !e.Damage.DamageTypes.Overlaps(Info.DamageTypes))
				return;

			state = TimedDamageMultiplierState.Draining;
			Ticks = Info.Duration;
			GrantActiveCondition(self);

			if (Info.ActivateSound != null)
				Game.Sound.Play(SoundType.World, Info.ActivateSound, self.CenterPosition);
		}

		void GrantActiveCondition(Actor self)
		{
			if (token == Actor.InvalidConditionToken && Info.ActiveCondition != null)
				token = self.GrantCondition(Info.ActiveCondition);
		}

		void GrantChargingCondition(Actor self)
		{
			if (token == Actor.InvalidConditionToken && Info.ChargingCondition != null)
				token = self.GrantCondition(Info.ChargingCondition);
		}

		void RevokeCondition(Actor self)
		{
			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (state == TimedDamageMultiplierState.Ready)
			{
				if (Info.GrantConditionWhenReady)
					GrantActiveCondition(self);

				return;
			}

			if (--Ticks > 0)
				return;

			if (state == TimedDamageMultiplierState.Draining)
			{
				state = TimedDamageMultiplierState.Charging;
				Ticks = Info.ChargeTime;
				RevokeCondition(self);
				GrantChargingCondition(self);

				if (Info.DeactivateSound != null)
					Game.Sound.Play(SoundType.World, Info.DeactivateSound, self.CenterPosition);
			}
			else if (state == TimedDamageMultiplierState.Charging)
			{
				state = TimedDamageMultiplierState.Ready;
				Ticks = Info.Duration;
				RevokeCondition(self);

				if (Info.GrantConditionWhenReady)
					GrantActiveCondition(self);
			}
		}

		protected override void TraitDisabled(Actor self)
		{
			state = TimedDamageMultiplierState.Ready;
			Ticks = Info.Duration;
			RevokeCondition(self);
		}

		float ISelectionBar.GetValue()
		{
			if (!Info.ShowSelectionBar || IsTraitDisabled)
				return 0f;

			if (!Info.ShowSelectionBarWhenReady && state == TimedDamageMultiplierState.Ready)
				return 0f;

			if (state == TimedDamageMultiplierState.Ready)
				return 1f;

			if (state == TimedDamageMultiplierState.Draining)
				return (float)Ticks / Info.Duration;

			if (state == TimedDamageMultiplierState.Charging)
				return (float)(Info.ChargeTime - Ticks) / Info.ChargeTime;

			return 0f;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

		Color ISelectionBar.GetColor() { return state == TimedDamageMultiplierState.Draining ? Info.DrainingColor : Info.ChargingColor; }
	}
}
