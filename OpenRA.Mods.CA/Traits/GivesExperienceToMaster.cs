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
	[Desc("This actor gives experience to any masters with GainsExperience when dealing damage.")]
	class GivesExperienceToMasterInfo : TraitInfo
	{
		[Desc("If -1, use the value of the unit cost.")]
		public readonly int Experience = -1;

		[Desc("Player relationships the attacking player needs to receive the experience.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		[Desc("Percentage of the `Experience` value that is being granted to the killing actor.")]
		public readonly int ActorExperienceModifier = 10000;

		public override object Create(ActorInitializer init) { return new GivesExperienceToMaster(init.Self, this); }
	}

	class GivesExperienceToMaster : INotifyAppliedDamage, INotifyCreated
	{
		readonly GivesExperienceToMasterInfo info;

		IEnumerable<MindControllable> mindControllables;
		IEnumerable<SpawnerSlaveBase> spawnerSlaveBases;

		public GivesExperienceToMaster(Actor self, GivesExperienceToMasterInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			mindControllables = self.TraitsImplementing<MindControllable>();
			spawnerSlaveBases = self.TraitsImplementing<SpawnerSlaveBase>();
		}

		void INotifyAppliedDamage.AppliedDamage(Actor self, Actor damaged, AttackInfo e)
		{
			// Don't notify suicides
			if (damaged == e.Attacker)
				return;

			if (!info.ValidRelationships.HasRelationship(damaged.Owner.RelationshipWith(self.Owner)))
				return;

			var health = damaged.TraitOrDefault<Health>();
			if (health == null)
				return;

			var appliedDamage = Math.Min(e.Damage.Value, health.HP);
			if (appliedDamage == 0)
				return;

			var valued = damaged.Info.TraitInfoOrDefault<ValuedInfo>();
			var exp = info.Experience >= 0 ? info.Experience
				: valued != null ? valued.Cost : 0;

			var experienceModifiers = damaged.TraitsImplementing<IGivesExperienceModifier>().ToArray().Select(m => m.GetGivesExperienceModifier()).Append(info.ActorExperienceModifier);
			experienceModifiers = experienceModifiers.Append((int)(((float)e.Damage.Value / (float)health.MaxHP) * 100));

			foreach (var mindControllable in mindControllables)
				if (mindControllable.Master.HasValue)
					GiveExperience(mindControllable.Master.Value.Actor, exp, experienceModifiers);

			foreach (var SpawnerSlaveBase in spawnerSlaveBases)
				if (SpawnerSlaveBase.Master != null)
					GiveExperience(SpawnerSlaveBase.Master, exp, experienceModifiers);
		}

		void GiveExperience(Actor master, int exp, IEnumerable<int> experienceModifiers)
		{
			if (master.IsDead)
				return;

			var gainsExperience = master.TraitOrDefault<GainsExperience>();
			if (gainsExperience == null)
				return;

			experienceModifiers = experienceModifiers.Concat(master.TraitsImplementing<IGainsExperienceModifier>()
				.Select(x => x.GetGainsExperienceModifier()));

			gainsExperience.GiveExperience(Util.ApplyPercentageModifiers(exp, experienceModifiers));
		}
	}
}
