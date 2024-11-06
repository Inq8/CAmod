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
	[Desc("Updates a counter when the actor is created/disposed or changes owner.")]
	public class UpdatesCountInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Name of the counter to update.")]
		public readonly string Type = null;

		public override object Create(ActorInitializer init) { return new UpdatesCount(this); }
	}

	public class UpdatesCount : ConditionalTrait<UpdatesCountInfo>, INotifyCreated, INotifyActorDisposing, INotifyOwnerChanged
	{
		public readonly UpdatesCountInfo info;
		INotifyCountChanged[] counters;

		public UpdatesCount(UpdatesCountInfo info)
			: base(info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			UpdateCounters(self.Owner);

			if (IsTraitDisabled)
				return;

			foreach (var c in counters)
				c.Incremented(info.Type);
		}

		void UpdateCounters(Player owner)
		{
			counters = owner.PlayerActor.TraitsImplementing<INotifyCountChanged>().ToArray();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			foreach (var c in counters)
				c.Decremented(info.Type);

			UpdateCounters(newOwner);

			foreach (var c in counters)
				c.Incremented(info.Type);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			foreach (var c in counters)
				c.Decremented(info.Type);
		}

		protected override void TraitEnabled(Actor self)
		{
			foreach (var c in counters)
				c.Incremented(info.Type);
		}

		protected override void TraitDisabled(Actor self)
		{
			foreach (var c in counters)
				c.Decremented(info.Type);
		}
	}
}
