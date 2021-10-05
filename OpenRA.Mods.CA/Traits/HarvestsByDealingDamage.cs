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

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Allows harvester tp gain resources by inflicting damage.")]
	public class HarvestsByDealingDamageInfo : ConditionalTraitInfo, Requires<HarvesterInfo>
	{
		[Desc("Divide damage by this number to get units.")]
		public readonly int DivideDamageBy = 1000;

		public override object Create(ActorInitializer init) { return new HarvestsByDealingDamage(init, this); }
	}

	public class HarvestsByDealingDamage : ConditionalTrait<HarvestsByDealingDamageInfo>, INotifyAppliedDamage
	{
		readonly Harvester harv;
		readonly INotifyHarvesterAction[] notifyHarvesterActions;

		public HarvestsByDealingDamage(ActorInitializer init, HarvestsByDealingDamageInfo info)
			: base(info)
		{
			harv = init.Self.Trait<Harvester>();
			notifyHarvesterActions = init.Self.TraitsImplementing<INotifyHarvesterAction>().ToArray();
		}

		void INotifyAppliedDamage.AppliedDamage(Actor self, Actor damaged, AttackInfo e)
		{
			if (IsTraitDisabled)
				return;

			if (e.Damage.Value <= 0 || damaged == self)
				return;

			if (harv.IsFull)
				return;

			var units = e.Damage.Value / Info.DivideDamageBy;
			units = units > 1 ? units : 1;
			var resources = self.World.WorldActor.TraitsImplementing<ResourceType>().ToList();

			if (!resources.Any())
				return;

			var resource = resources.First();

			for (var i = 0; i < units; i++)
			{
				if (!harv.IsFull)
					harv.AcceptResource(self, resource);
			}

			foreach (var t in notifyHarvesterActions)
				t.Harvested(self, resource);
		}
	}
}
