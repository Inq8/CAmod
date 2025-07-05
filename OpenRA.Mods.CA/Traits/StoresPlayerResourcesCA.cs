#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Adds capacity to a player's harvested resource limit.")]
	public class StoresPlayerResourcesCAInfo : TraitInfo
	{
		[FieldLoader.Require]
		public readonly int Capacity = 0;

		public readonly bool DisableTransferFromBotOwner = false;

		public override object Create(ActorInitializer init) { return new StoresPlayerResourcesCA(init.Self, this); }
	}

	public class StoresPlayerResourcesCA : INotifyOwnerChanged, INotifyCapture, INotifyKilled, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly StoresPlayerResourcesCAInfo info;
		PlayerResources player;
		int storedBeforeOwnerChange;

		public int Stored => player.ResourceCapacity == 0 ? 0 : (int)((long)info.Capacity * player.Resources / player.ResourceCapacity);

		public StoresPlayerResourcesCA(Actor self, StoresPlayerResourcesCAInfo info)
		{
			this.info = info;
			player = self.Owner.PlayerActor.Trait<PlayerResources>();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			storedBeforeOwnerChange = Stored;
			player = newOwner.PlayerActor.Trait<PlayerResources>();
		}

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner, BitSet<CaptureType> captureTypes)
		{
			if (info.DisableTransferFromBotOwner && oldOwner.IsBot)
				return;

			var resources = storedBeforeOwnerChange;
			oldOwner.PlayerActor.Trait<PlayerResources>().TakeResources(resources);
			newOwner.PlayerActor.Trait<PlayerResources>().GiveResources(resources);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			// Lose the stored resources.
			player.TakeResources(Stored);
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			player.AddStorageCapacity(info.Capacity);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			player.RemoveStorageCapacity(info.Capacity);
		}
	}
}
