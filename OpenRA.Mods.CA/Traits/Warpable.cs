﻿#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("This actor can be affected by temporal warheads.")]
	public class WarpableInfo : TraitInfo, Requires<IHealthInfo>
	{
		[GrantedConditionReference]
		[Desc("The condition type to grant when the actor is affected.")]
		public readonly string Condition = null;

		[Desc("Amount of ticks required to pass without being damaged to revoke effects of the temporal weapon.")]
		public readonly int RevokeDelay = 1;

		[Desc("Amount of warp damage revoked each tick after RevokeDelay has passed. Use -1 for all damage to be revoked.",
			"Zero means damage will never be revoked.")]
		public readonly int RevokeRate = -1;

		[Desc("Amount required to be taken for the unit to be killed.",
			"Use -1 to be calculated from the actor health.")]
		public readonly int EraseDamage = -1;

		[Desc("Types of damage the unit is erased with. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		[Desc("If set, the required value to warp will be scaled with the health.")]
		public readonly bool ScaleWithCurrentHealthPercentage = false;

		public readonly bool ShowSelectionBar = true;
		public readonly Color SelectionBarColor = Color.FromArgb(128, 200, 255);

		public override object Create(ActorInitializer init) { return new Warpable(init, this); }
	}

	public class Warpable : ISync, ITick, ISelectionBar
	{
		readonly Actor self;
		readonly WarpableInfo info;
		readonly Health health;
		readonly int requiredDamage;
		int ScalingRequiredDamage { get { return Math.Max(100 * health.HP / health.MaxHP * requiredDamage / 100, 1); } }

		int token = Actor.InvalidConditionToken;

		[Sync]
		int receivedDamage;

		[Sync]
		int tick;

		public Warpable(ActorInitializer init, WarpableInfo info)
		{
			this.info = info;
			self = init.Self;
			health = self.Trait<Health>();
			requiredDamage = info.EraseDamage >= 0 ? info.EraseDamage : health.MaxHP;
		}

		public void AddDamage(int damage, Actor damager)
		{
			receivedDamage = receivedDamage + damage;
			tick = info.RevokeDelay;

			if (info.ScaleWithCurrentHealthPercentage)
				if (receivedDamage >= ScalingRequiredDamage)
					self.Kill(damager, info.DamageTypes);
			else
				if (receivedDamage >= requiredDamage)
					self.Kill(damager, info.DamageTypes);

			if (!string.IsNullOrEmpty(info.Condition) && token == Actor.InvalidConditionToken)
				token = self.GrantCondition(info.Condition);
		}

		void ITick.Tick(Actor self)
		{
			if (receivedDamage > 0 && --tick < 0)
			{
				if (info.RevokeRate == -1)
					receivedDamage = 0;
				else
					receivedDamage -= info.RevokeRate;

				if (receivedDamage < 0)
					receivedDamage = 0;

				if (receivedDamage == 0 && token != Actor.InvalidConditionToken)
					token = self.RevokeCondition(token);
			}
		}

		float ISelectionBar.GetValue()
		{
			if (!info.ShowSelectionBar)
				return 0;

			if (info.ScaleWithCurrentHealthPercentage)
				return (float)receivedDamage / ScalingRequiredDamage;

			return (float)receivedDamage / requiredDamage;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

		Color ISelectionBar.GetColor() { return info.SelectionBarColor; }
	}
}
