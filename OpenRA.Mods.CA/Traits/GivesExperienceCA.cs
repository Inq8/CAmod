#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("This actor gives experience to a GainsExperience actor when they are killed or damaged.")]
	sealed class GivesExperienceCAInfo : TraitInfo
	{
		[Desc("If -1, use the value of the unit cost.")]
		public readonly int Experience = -1;

		[Desc("Player relationships the attacking player needs to receive the experience.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		[Desc("Percentage of the `Experience` value that is being granted to the killing actor.")]
		public readonly int ActorExperienceModifier = 10000;

		[Desc("Percentage of the `Experience` value that is being granted to the player owning the killing actor.")]
		public readonly int PlayerExperienceModifier = 0;

		[Desc("If true, gives experience on damage, otherwise gives experience when killed.")]
		public readonly bool ActorExperienceOnDamage = false;

		public override object Create(ActorInitializer init) { return new GivesExperienceCA(this); }
	}

	sealed class GivesExperienceCA : INotifyKilled, INotifyCreated, INotifyDamage
	{
		readonly GivesExperienceCAInfo info;

		int baseXp;
		Health health;
		IEnumerable<int> experienceModifiers;

		public GivesExperienceCA(GivesExperienceCAInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			health = self.TraitOrDefault<Health>();
			var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();

			baseXp = info.Experience >= 0 ? info.Experience
				: valued != null ? valued.Cost : 0;

			experienceModifiers = self.TraitsImplementing<IGivesExperienceModifier>().ToArray().Select(m => m.GetGivesExperienceModifier());
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			GiveExperience(self, e, true);
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (!info.ActorExperienceOnDamage)
				return;

			GiveExperience(self, e, false);
		}

		void GiveExperience(Actor self, AttackInfo e, bool fromKill)
		{
			if (baseXp == 0 || e.Attacker == null || e.Attacker.Disposed)
				return;

			if (!info.ValidRelationships.HasRelationship(e.Attacker.Owner.RelationshipWith(self.Owner)))
				return;

			var exp = Util.ApplyPercentageModifiers(baseXp, experienceModifiers);

			if (fromKill)
				e.Attacker.Owner.PlayerActor.TraitOrDefault<PlayerExperience>()
					?.GiveExperience(Util.ApplyPercentageModifiers(exp, new[] { info.PlayerExperienceModifier }));

			if (info.ActorExperienceOnDamage && (fromKill || health == null))
				return;

			var attacker = e.Attacker.TraitOrDefault<GainsExperience>();
			if (attacker == null)
				return;

			var killerExperienceModifiers = e.Attacker.TraitsImplementing<IGainsExperienceModifier>()
				.Select(x => x.GetGainsExperienceModifier()).Append(info.ActorExperienceModifier);

			// If applying based on damage, calculate the percentage of the total HP that the attack inflicted, and get that same percentage of the xp
			if (info.ActorExperienceOnDamage)
			{
				var hpBefore = Math.Min(health.HP + e.Damage.Value, health.MaxHP);
				var appliedDamage = Math.Min(e.Damage.Value, hpBefore);

				if (appliedDamage <= 0)
					return;

				var damageRatio = (float)appliedDamage / (float)health.MaxHP;
				killerExperienceModifiers = killerExperienceModifiers.Append((int)(damageRatio * 100));
			}

			exp = Util.ApplyPercentageModifiers(exp, killerExperienceModifiers);
			attacker.GiveExperience(Math.Max(exp, 250)); // if less than 1% of target HP is lost, give a token amount of xp
		}
	}
}
