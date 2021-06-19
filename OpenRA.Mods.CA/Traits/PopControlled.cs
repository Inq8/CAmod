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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Population control for an actor type.")]
	public class PopControlledInfo : TraitInfo
	{
		[Desc("Population limit.")]
		public readonly int Limit = 2;

		[Desc("Remove the actor from the world (and destroy it) instead of killing it.")]
		public readonly bool RemoveInstead = false;

		[Desc("Types of damage that this trait causes. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		public override object Create(ActorInitializer init) { return new PopControlled(init, this); }
	}

	public class PopControlled : INotifyCreated
	{
		readonly PopControlledInfo i;
		Actor self;

		public PopControlled(ActorInitializer init, PopControlledInfo info)
		{
			i = info;
			self = init.Self;
		}

		void INotifyCreated.Created(Actor self)
		{
			var instances = self.World.Actors.Where(a => !a.IsDead && a.Owner == self.Owner &&
				a.Info.Name == self.Info.Name).ToList();

			if (instances.Count < i.Limit)
				return;

			var numToRemove = (instances.Count + 1) - i.Limit;

			for (var i = 0; i < numToRemove; i++)
			{
				var instance = instances.FirstOrDefault();
				if (instance != null)
				{
					var popControlled = instance.TraitOrDefault<PopControlled>();
					if (popControlled != null)
						popControlled.Kill(instance);
				}
			}
		}

		void Kill(Actor self)
		{
			if (self.IsDead)
				return;

			if (i.RemoveInstead || !self.Info.HasTraitInfo<IHealthInfo>())
				self.Dispose();
			else
				self.Kill(self, i.DamageTypes);
		}
	}
}
