#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Updates a counter when the actor is created/disposed.")]
	public class UpdatesCountInfo : TraitInfo
	{
		[FieldLoader.Require]
		[Desc("Name of the counter to update.")]
		public readonly string Name = null;

		public override object Create(ActorInitializer init) { return new UpdatesCount(init, this); }
	}

	public class UpdatesCount : INotifyCreated, INotifyActorDisposing
	{
		public readonly UpdatesCountInfo Info;
		IEnumerable<ProvidesPrerequisiteOnCount> counters;

		public UpdatesCount(ActorInitializer init, UpdatesCountInfo info)
		{
			Info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			counters = self.Owner.PlayerActor.TraitsImplementing<ProvidesPrerequisiteOnCount>()
				.Where(c => c.Name == Info.Name
					&& (c.Factions.Length == 0 || c.Factions.Contains(self.Owner.Faction.InternalName)));

			foreach (var c in counters)
				c.Increment();
		}

		void INotifyActorDisposing.Disposing(OpenRA.Actor self)
		{
			foreach (var c in counters)
				c.Decrement();
		}
	}
}
