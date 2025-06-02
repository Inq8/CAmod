#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Population control for an actor type.")]
	public class PopControlledInfo : TraitInfo
	{
		[Desc("The type of pop control (defaults to the actor name).")]
		public readonly string Type = null;

		[Desc("Remove the actor from the world (and destroy it) instead of killing it.")]
		public readonly bool RemoveInstead = false;

		[Desc("Types of damage that this trait causes. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> DamageTypes = default;

		public override object Create(ActorInitializer init) { return new PopControlled(this); }
	}

	public class PopControlled : INotifyCreated, INotifyOwnerChanged
	{
		readonly PopControlledInfo info;
		PopController controller;

		public PopControlledInfo Info => info;

		public PopControlled(PopControlledInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			UpdateController(self.Owner);
			controller.Update(self, info.Type);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			UpdateController(newOwner);
			controller.Update(self, info.Type);
		}

		void UpdateController(Player owner)
		{
			controller = owner.PlayerActor.Trait<PopController>();
		}
	}
}
