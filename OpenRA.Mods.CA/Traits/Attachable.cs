#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Use on an actor to make it attachable to other actors with the AttachableTo trait.")]
	public class AttachableInfo : TraitInfo, Requires<IPositionableInfo>
	{
		[GrantedConditionReference]
		[Desc("The condition to grant when attached.")]
		public readonly string AttachedCondition = null;

		[GrantedConditionReference]
		[Desc("The condition to grant when detached.")]
		public readonly string DetachedCondition = null;

		[Desc("Attachable type to use to check for limits.")]
		public readonly string AttachableType = null;

		public override object Create(ActorInitializer init) { return new Attachable(init, this); }
	}

	public class Attachable : INotifyKilled, INotifyActorDisposing
	{
		public readonly AttachableInfo Info;
		AttachableTo attachedTo;
		int attachedConditionToken;
		int detachedConditionToken;
		readonly IPositionable positionable;
		readonly Actor self;

		public Attachable(ActorInitializer init, AttachableInfo info)
		{
			self = init.Self;
			Info = info;
			positionable = self.TraitOrDefault<IPositionable>();
			attachedConditionToken = Actor.InvalidConditionToken;
			detachedConditionToken = Actor.InvalidConditionToken;
		}

		public bool IsValid { get { return self != null && !self.IsDead; } }

		public void AttachTo(AttachableTo attachableTo, WPos pos)
		{
			Detach();
			attachedTo = attachableTo;
			SetPosition(pos);

			if (Info.AttachedCondition != null && attachedConditionToken == Actor.InvalidConditionToken)
				attachedConditionToken = self.GrantCondition(Info.AttachedCondition);

			if (detachedConditionToken != Actor.InvalidConditionToken)
				detachedConditionToken = self.RevokeCondition(detachedConditionToken);
		}

		public void AttachedToLost()
		{
			self.Dispose();
		}

		public void SetPosition(WPos pos)
		{
			positionable.SetPosition(self, pos);
			positionable.SetVisualPosition(self, pos);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			Detach();
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			Detach();
		}

		void Detach()
		{
			if (attachedTo != null)
			{
				attachedTo.Detach(this);
				attachedTo = null;
			}

			if (attachedConditionToken != Actor.InvalidConditionToken)
				attachedConditionToken = self.RevokeCondition(attachedConditionToken);

			if (Info.DetachedCondition != null && detachedConditionToken == Actor.InvalidConditionToken)
				detachedConditionToken = self.GrantCondition(Info.DetachedCondition);
		}
	}
}
