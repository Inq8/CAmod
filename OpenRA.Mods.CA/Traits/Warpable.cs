#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

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

		int token = Actor.InvalidConditionToken;

		[Sync]
		int recievedDamage;

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
			recievedDamage = recievedDamage + damage;
			tick = info.RevokeDelay;

			if (info.ScaleWithCurrentHealthPercentage)
			{
				var currentRequiredDamage = 100 * health.HP / health.MaxHP * requiredDamage / 100;

				if (recievedDamage >= currentRequiredDamage)
					self.Kill(damager, info.DamageTypes);
			}
			else
				if (recievedDamage >= requiredDamage)
				self.Kill(damager, info.DamageTypes);

			if (!string.IsNullOrEmpty(info.Condition) &&
				token == Actor.InvalidConditionToken)
				token = self.GrantCondition(info.Condition);
		}

		void ITick.Tick(Actor self)
		{
			if (--tick < 0)
			{
				recievedDamage = 0;

				if (token != Actor.InvalidConditionToken)
					token = self.RevokeCondition(token);
			}
		}

		float ISelectionBar.GetValue()
		{
			if (!info.ShowSelectionBar)
				return 0;

			return (float)recievedDamage / requiredDamage;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

		Color ISelectionBar.GetColor() { return info.SelectionBarColor; }
	}
}
