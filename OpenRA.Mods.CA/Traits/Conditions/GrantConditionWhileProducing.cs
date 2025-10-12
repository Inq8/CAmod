#region Copyright & License Information
/*
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
	[Desc("Grants a condition while the actor is producing something.")]
	public class GrantConditionWhileProducingInfo : ConditionalTraitInfo, Requires<ProductionInfo>
	{
		[FieldLoader.Require]
		[Desc("Production queue type, for actors with multiple queues.")]
		public readonly string ProductionType = null;

		[GrantedConditionReference]
		[Desc("The condition to grant while producing.")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new GrantConditionWhileProducing(init, this); }
	}

	public class GrantConditionWhileProducing : ConditionalTrait<ConditionalTraitInfo>, ITick, INotifyOwnerChanged, INotifyCreated
	{
		readonly GrantConditionWhileProducingInfo info;
		readonly Actor self;

		ProductionQueue queue;
		int conditionToken = Actor.InvalidConditionToken;
		bool wasProducing = false;

		public GrantConditionWhileProducing(ActorInitializer init, GrantConditionWhileProducingInfo info)
			: base(info)
		{
			this.info = info;
			self = init.Self;
		}

		void INotifyCreated.Created(Actor self)
		{
			FindQueue();
		}

		void FindQueue()
		{
			// Per-actor queue
			queue = self.TraitsImplementing<ProductionQueue>()
				.FirstOrDefault(q => info.ProductionType == q.Info.Type);

			// If no queues available - check for classic production queues
			queue ??= self.Owner.PlayerActor.TraitsImplementing<ProductionQueue>()
				.FirstOrDefault(q => info.ProductionType == q.Info.Type);
		}

		bool IsProducing()
		{
			if (queue == null)
				return false;

			var current = queue.AllQueued().Where(i => i.Started).MinByOrDefault(i => i.RemainingTime);
			return current != null;
		}

		void GrantCondition()
		{
			if (string.IsNullOrEmpty(info.Condition) || conditionToken != Actor.InvalidConditionToken)
				return;

			conditionToken = self.GrantCondition(info.Condition);
		}

		void RevokeCondition()
		{
			if (conditionToken == Actor.InvalidConditionToken)
				return;

			conditionToken = self.RevokeCondition(conditionToken);
		}

		void ITick.Tick(Actor self)
		{
			var isProducing = IsProducing();

			if (!IsTraitDisabled || (isProducing && !wasProducing))
				GrantCondition();
			else if (IsTraitDisabled || (!isProducing && wasProducing))
				RevokeCondition();

			wasProducing = isProducing;
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			RevokeCondition();
			wasProducing = false;
			FindQueue();
		}
	}
}
