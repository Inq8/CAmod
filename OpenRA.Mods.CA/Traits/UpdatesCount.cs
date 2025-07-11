#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Flags]
	public enum UpdateOnType
	{
		Created = 1,
		Disposed = 2,
		Killed = 4,
	}

	[Desc("Updates a counter when the actor is created/disposed or changes owner.")]
	public class UpdatesCountInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Name of the counter to update.")]
		public readonly string Type = null;

		[Desc("What triggers an update.")]
		public readonly UpdateOnType UpdateOn = UpdateOnType.Created | UpdateOnType.Disposed;

		public override object Create(ActorInitializer init) { return new UpdatesCount(this); }
	}

	public class UpdatesCount : ConditionalTrait<UpdatesCountInfo>, INotifyCreated, INotifyActorDisposing, INotifyOwnerChanged, INotifyKilled
	{
		public readonly UpdatesCountInfo info;
		CountManager countManager;

		public UpdatesCount(UpdatesCountInfo info)
			: base(info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			UpdateCounter(self.Owner);

			if (!info.UpdateOn.HasFlag(UpdateOnType.Created))
				return;

			if (IsTraitDisabled)
				return;

			countManager.Increment(info.Type);
		}

		void UpdateCounter(Player owner)
		{
			countManager = owner.PlayerActor.Trait<CountManager>();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			countManager.Decrement(info.Type);
			UpdateCounter(newOwner);
			countManager.Increment(info.Type);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (!info.UpdateOn.HasFlag(UpdateOnType.Disposed))
				return;

			countManager.Decrement(info.Type);
		}

		protected override void TraitEnabled(Actor self)
		{
			if (!info.UpdateOn.HasFlag(UpdateOnType.Created))
				return;

			countManager.Increment(info.Type);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (!info.UpdateOn.HasFlag(UpdateOnType.Created))
				return;

			countManager.Decrement(info.Type);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (!info.UpdateOn.HasFlag(UpdateOnType.Killed))
				return;

			if (self.Owner.WinState != WinState.Undefined)
				return;

			var attackingPlayer = e.Attacker.Owner;

			if (attackingPlayer.RelationshipWith(self.Owner) != PlayerRelationship.Enemy)
				return;

			var attackerCounter = attackingPlayer.PlayerActor.Trait<CountManager>();
			attackerCounter.Increment(info.Type);
		}
	}
}
