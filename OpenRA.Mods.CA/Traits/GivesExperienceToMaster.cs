#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
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
	[Desc("This actor gives experience to a GainsExperience actor when they are killed.")]
	class GivesExperienceToMasterInfo : TraitInfo
	{
		[Desc("If -1, use the value of the unit cost.")]
		public readonly int Experience = -1;

		[Desc("Player relationships the attacking player needs to receive the experience.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		[Desc("Percentage of the `Experience` value that is being granted to the killing actor.")]
		public readonly int ActorExperienceModifier = 10000;

		public override object Create(ActorInitializer init) { return new GivesExperienceToMaster(this); }
	}

	class GivesExperienceToMaster : INotifyKilled, INotifyCreated
	{
		readonly GivesExperienceToMasterInfo info;

		int exp;
		IEnumerable<int> experienceModifiers;

		public GivesExperienceToMaster(GivesExperienceToMasterInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();
			exp = info.Experience >= 0 ? info.Experience
				: valued != null ? valued.Cost : 0;

			experienceModifiers = self.TraitsImplementing<IGivesExperienceModifier>().ToArray().Select(m => m.GetGivesExperienceModifier());
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (exp == 0 || e.Attacker == null || e.Attacker.Disposed)
				return;

			if (!info.ValidRelationships.HasRelationship(e.Attacker.Owner.RelationshipWith(self.Owner)))
				return;

			exp = Util.ApplyPercentageModifiers(exp, experienceModifiers);

			var killer = e.Attacker;
			if (killer != null)
			{
				var mindControllables = killer.TraitsImplementing<MindControllable>();
				foreach (var mindControllable in mindControllables)
					if (mindControllable.Master != null)
						GiveExperience(mindControllable.Master, exp);

				var baseSpawnerSlaves = killer.TraitsImplementing<BaseSpawnerSlave>();
				foreach (var baseSpawnerSlave in baseSpawnerSlaves)
					if (baseSpawnerSlave.Master != null)
						GiveExperience(baseSpawnerSlave.Master, exp);
			}
		}

		void GiveExperience(Actor master, int exp)
		{
			if (master.IsDead)
				return;

			var gainsExperience = master.TraitOrDefault<GainsExperience>();
			if (gainsExperience == null)
				return;

			var experienceModifier = master.TraitsImplementing<IGainsExperienceModifier>()
				.Select(x => x.GetGainsExperienceModifier()).Append(info.ActorExperienceModifier);
			gainsExperience.GiveExperience(Util.ApplyPercentageModifiers(exp, experienceModifier));
		}
	}
}
