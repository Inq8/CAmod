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
	[Desc("Updates a counter when the actor is created/disposed or changes owner.")]
	public class UpdatesCountInfo : TraitInfo
	{
		[FieldLoader.Require]
		[Desc("Name of the counter to update.")]
		public readonly string Type = null;

		public override object Create(ActorInitializer init) { return new UpdatesCount(init, this); }
	}

	public class UpdatesCount : INotifyCreated, INotifyActorDisposing, INotifyOwnerChanged
	{
		public readonly UpdatesCountInfo Info;
		IEnumerable<ProvidesPrerequisiteOnCount> counters;

		public UpdatesCount(ActorInitializer init, UpdatesCountInfo info)
		{
			Info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			UpdateCounters(self.Owner);

			foreach (var c in counters)
				c.Increment(Info.Type);
		}

		void UpdateCounters(Player owner)
		{
			counters = owner.PlayerActor.TraitsImplementing<ProvidesPrerequisiteOnCount>()
				.Where(c => c.Info.RequiredCounts.ContainsKey(Info.Type) && c.Enabled);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			foreach (var c in counters)
				c.Decrement(Info.Type);

			UpdateCounters(newOwner);

			foreach (var c in counters)
				c.Increment(Info.Type);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			foreach (var c in counters)
				c.Decrement(Info.Type);
		}
	}
}
