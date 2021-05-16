#region Copyright & License Information
/*
 * Copyright 2016-2021 The CA Developers (see AUTHORS)
 * This file is part of CA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Allow convertible units to enter and spawn a new actor or actors.")]
	public class ReflectsDamageInfo : ConditionalTraitInfo
	{
		[Desc("Relationships required by actors that damage will be reflected to.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		[Desc("Actor types that damage should be reflected to.")]
		public readonly HashSet<string> ValidActors = new HashSet<string>();

		[Desc("Actor types that damage should not be reflected to.")]
		public readonly HashSet<string> InvalidActors = new HashSet<string>();

		[Desc("Actor types whose damage should not be reflected.")]
		public readonly HashSet<string> InvalidAttackerActors = new HashSet<string>();

		[Desc("Percentage of damage that should be reflected.")]
		public readonly int DamagePercentage = 100;

		[Desc("If true, reflect damage back at the attacker, otherwise use range to find units to reflect to.")]
		public readonly bool ReflectToAttacker = false;

		[Desc("The range to search for actors.")]
		public readonly WDist Range = WDist.FromCells(3);

		[Desc("Maximum number of units damage will be reflected to.")]
		public readonly int MaxUnits = 1;

		[Desc("Split damage equally amongst actors the damage is reflected to? Otherwise full DamagePercentage will be applied to all.")]
		public readonly bool SplitDamage = false;

		[Desc("If true, negative damage (repairs/heals) is also reflected.")]
		public readonly bool ReflectsHealing = false;

		public override object Create(ActorInitializer init) { return new ReflectsDamage(init, this); }
	}

	public class ReflectsDamage : ConditionalTrait<ReflectsDamageInfo>, INotifyDamage
	{
		public readonly Player Player;

		public ReflectsDamage(ActorInitializer init, ReflectsDamageInfo info)
			: base(info)
		{
			Player = init.Self.Owner;
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled || e.Damage.Value == 0)
				return;

			if (!Info.ReflectsHealing && e.Damage.Value < 0)
				return;

			if (Info.InvalidAttackerActors.Any() && Info.InvalidAttackerActors.Contains(e.Attacker.Info.Name))
				return;

			if (self == e.Attacker)
				return;

			var units = new List<Actor>();

			if (Info.ReflectToAttacker)
			{
				if (IsValidUnit(e.Attacker))
					units.Add(e.Attacker);
			}
			else
			{
				units = self.World.FindActorsInCircle(self.CenterPosition, Info.Range)
					.Where(a => a != self && IsValidUnit(a))
					.Take(Info.MaxUnits)
					.ToList();
			}

			if (!units.Any())
				return;

			var totalDamage = e.Damage.Value;
			var damageTypes = e.Damage.DamageTypes;

			var percentageAsList = new List<int>();
			percentageAsList.Add(Info.DamagePercentage);
			var damageAmt = Util.ApplyPercentageModifiers(totalDamage, percentageAsList);

			if (Info.SplitDamage)
				damageAmt /= units.Count;

			var damage = new Damage(damageAmt, damageTypes);

			foreach (var unit in units)
			{
				var health = unit.TraitOrDefault<IHealth>();

				if (health != null)
					health.InflictDamage(unit, unit, damage, true);
			}
		}

		public bool IsValidUnit(Actor a)
		{
			if (a == null || a.IsDead)
				return false;

			if (!Info.ValidRelationships.HasStance(a.Owner.RelationshipWith(Player)))
				return false;

			if (Info.ValidActors.Any() && !Info.ValidActors.Contains(a.Info.Name))
				return false;

			if (Info.InvalidActors.Any() && Info.InvalidActors.Contains(a.Info.Name))
				return false;

			return true;
		}
	}
}
