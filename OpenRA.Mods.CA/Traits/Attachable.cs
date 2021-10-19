#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
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

	public class Attachable : INotifyCreated, INotifyKilled, INotifyActorDisposing, INotifyOwnerChanged, ITick, INotifyBlockingMove
	{
		public readonly AttachableInfo Info;
		AttachableTo attachedTo;
		AutoTarget autoTarget;
		AttackBase[] attackBases;
		int attachedConditionToken;
		int detachedConditionToken;
		Target lastTarget;
		readonly IPositionable positionable;
		readonly Actor self;
		bool beingCarried;

		public Attachable(ActorInitializer init, AttachableInfo info)
		{
			self = init.Self;
			Info = info;
			positionable = self.Trait<IPositionable>();
			attachedConditionToken = Actor.InvalidConditionToken;
			detachedConditionToken = Actor.InvalidConditionToken;
			beingCarried = false;
		}

		void INotifyCreated.Created(Actor self)
		{
			autoTarget = self.TraitOrDefault<AutoTarget>();
			attackBases = self.TraitsImplementing<AttackBase>().ToArray();
		}

		public bool IsValid { get { return self != null && !self.IsDead; } }

		void INotifyBlockingMove.OnNotifyBlockingMove(Actor self, Actor blocking)
		{
			var carryall = blocking.TraitOrDefault<Carryall>();
			beingCarried = true;
			if (carryall != null)
				ParentEnteredCargo();
		}

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

		void ITick.Tick(Actor self)
		{
			if (!IsValid || attachedTo == null)
				return;

			if (!self.IsInWorld && attachedTo.IsInWorld && beingCarried && !attachedTo.Carryable.Reserved)
			{
				beingCarried = false;
				ParentExitedCargo();
				return;
			}

			if (!self.IsInWorld)
				return;

			SetPosition(attachedTo.CenterPosition);
		}

		public void SetPosition(WPos pos)
		{
			if (attachedTo.CenterPosition == self.CenterPosition)
				return;

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

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
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

		public void Stop()
		{
			if (attackBases.Count() == 0)
				return;

			self.CancelActivity();
			self.World.IssueOrder(new Order("Stop", self, false));
		}

		public void Attack(Target target, bool force)
		{
			if (attackBases.Count() == 0)
				return;

			if (!TargetSwitched(lastTarget, target))
				return;

			lastTarget = target;
			self.World.AddFrameEndTask(w =>
			{
				var orderString = force ? "ForceAttack" : "Attack";
				self.World.IssueOrder(new Order(orderString, self, target, false, null, null));
			});
		}

		public void SetStance(UnitStance value)
		{
			if (autoTarget != null)
				autoTarget.SetStance(self, value);
		}

		bool TargetSwitched(Target lastTarget, Target newTarget)
		{
			if (newTarget.Type != lastTarget.Type)
				return true;

			if (newTarget.Type == TargetType.Terrain)
				return newTarget.CenterPosition != lastTarget.CenterPosition;

			if (newTarget.Type == TargetType.Actor)
				return lastTarget.Actor != newTarget.Actor;

			return false;
		}

		public void ParentEnteredCargo()
		{
			self.World.AddFrameEndTask(w =>
			{
				w.Remove(self);
			});
		}

		public void ParentExitedCargo()
		{
			self.World.AddFrameEndTask(w =>
			{
				SetPosition(attachedTo.CenterPosition);
				w.Add(self);
			});
		}
	}
}
