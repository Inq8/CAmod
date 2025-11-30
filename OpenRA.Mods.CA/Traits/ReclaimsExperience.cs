#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("When killed, gives to a reclaimable pool. When created takes from that pool.")]
	public class ReclaimsExperienceInfo : TraitInfo, Requires<GainsExperienceInfo>
	{
		[Desc("Pool to use. Uses actor name if not set.")]
		public readonly string Type = null;

		public override object Create(ActorInitializer init) { return new ReclaimsExperience(init, this); }
	}

	public class ReclaimsExperience : INotifyCreated, INotifyKilled
	{
		public readonly ReclaimsExperienceInfo Info;
		GainsExperience gainsExperienceTrait;

		public ReclaimsExperience(ActorInitializer init, ReclaimsExperienceInfo info)
		{
			Info = info;
			gainsExperienceTrait = init.Self.TraitsImplementing<GainsExperience>().First();
		}

		void INotifyCreated.Created(Actor self)
		{
			var pool = self.Owner.PlayerActor.TraitsImplementing<ReclaimableExperiencePool>().SingleOrDefault();

			if (pool != null)
				gainsExperienceTrait.GiveExperience(pool.TakeXpFromPool(Info.Type ?? self.Info.Name));
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			var pool = self.Owner.PlayerActor.TraitsImplementing<ReclaimableExperiencePool>().SingleOrDefault();
			if (pool != null)
				pool.AddXpToPool(Info.Type ?? self.Info.Name, gainsExperienceTrait.Experience);
		}
	}
}
