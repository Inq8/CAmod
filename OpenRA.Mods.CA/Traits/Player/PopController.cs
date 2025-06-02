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
using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Works with PopControlled trait on actors to cull excess instances.")]
	public class PopControllerInfo : TraitInfo
	{
		[Desc("Limits by type.")]
		public readonly Dictionary<string, int> Limits = new();

		public override object Create(ActorInitializer init) { return new PopController(init.Self, this); }
	}

	public class PopController
	{
		readonly Player player;
		readonly World world;
		HashSet<string> pendingCulls;

		public PopControllerInfo Info { get; private set; }

		public PopController(Actor self, PopControllerInfo info)
		{
			player = self.Owner;
			world = self.World;
			pendingCulls = new HashSet<string>();
			Info = info;
		}

		public void Update(Actor a, string limitType)
		{
			if (limitType == null)
				limitType = a.Info.Name.ToLowerInvariant();

			Cull(limitType);
		}

		void Cull(string limitType)
		{
			if (pendingCulls.Contains(limitType))
				return;

			pendingCulls.Add(limitType);

			world.AddFrameEndTask(w => {
				var instances = world.ActorsWithTrait<PopControlled>().Where(a => !a.Actor.IsDead
					&& a.Actor.Owner == player
					&& (a.Actor.IsInWorld || a.Actor.TraitOrDefault<Passenger>().Transport != null)
					&& (a.Trait.Info.Type == limitType || (a.Trait.Info.Type == null && a.Actor.Info.Name == limitType))
				).OrderBy(a => a.Actor.ActorID).ToList();

				var limit = (Info.Limits == null || !Info.Limits.ContainsKey(limitType)) ? 1 : Info.Limits[limitType];

				if (instances.Count > limit) {
					var numToRemove = instances.Count - limit;

					for (var i = 0; i < numToRemove; i++)
					{
						var instance = instances.FirstOrDefault();

						if (instance.Trait.Info.RemoveInstead || !instance.Actor.Info.HasTraitInfo<IHealthInfo>())
							instance.Actor.Dispose();
						else
							instance.Actor.Kill(instance.Actor, instance.Trait.Info.DamageTypes);

						instances.Remove(instance);
					}
				}

				pendingCulls.Remove(limitType);
			});
		}
	}
}
