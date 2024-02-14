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
using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Use on an actor to make it attachable to other actors with the AttachableTo trait.")]
	public class AttachableInfo : TraitInfo, Requires<IPositionableInfo>
	{
		[Desc("The `TargetTypes` from `Targetable` that can be attached to.")]
		public readonly BitSet<TargetableType> Types = default;

		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Color to use for the target line.")]
		public readonly Color TargetLineColor = Color.Yellow;

		[Desc("Player relationships the owner of the infiltration target needs.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		[CursorReference]
		[Desc("Cursor to display when able to infiltrate the target actor.")]
		public readonly string EnterCursor = "enter";

		[CursorReference]
		[Desc("Cursor to display when unable to infiltrate the target actor.")]
		public readonly string BlockedCursor = "enter-blocked";

		[Desc("Sounds played on being attached.")]
		public readonly string AttachSound = null;

		[GrantedConditionReference]
		[Desc("The condition to grant when attached.")]
		public readonly string AttachedCondition = null;

		[GrantedConditionReference]
		[Desc("The condition to grant when detached.")]
		public readonly string DetachedCondition = null;

		[Desc("Attachable type to use to check for limits.")]
		public readonly string AttachableType = null;

		[Desc("On attaching, transform into this actor.")]
		public readonly string OnAttachTransformInto = null;

		[Desc("On detaching, transform into this actor.")]
		public readonly string OnDetachTransformInto = null;

		[Desc("The range at which the actor can attach.")]
		public readonly WDist MinAttachDistance = WDist.Zero;

		public override object Create(ActorInitializer init) { return new Attachable(init, this); }
	}

	public class Attachable : INotifyCreated, INotifyKilled, INotifyActorDisposing, INotifyOwnerChanged, ITick, INotifyAiming,
		IIssueOrder, IResolveOrder, IOrderVoice
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

		public Attachable(ActorInitializer init, AttachableInfo info)
		{
			self = init.Self;
			Info = info;
			positionable = self.Trait<IPositionable>();
			attachedConditionToken = Actor.InvalidConditionToken;
			detachedConditionToken = Actor.InvalidConditionToken;
		}

		void INotifyCreated.Created(Actor self)
		{
			autoTarget = self.TraitOrDefault<AutoTarget>();
			attackBases = self.TraitsImplementing<AttackBase>().ToArray();
		}

		public bool IsValid { get { return self != null && !self.IsDead; } }

		/** Called from AttachableTo.Attach() */
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

		public void HostLost()
		{
			self.Dispose();
		}

		public void HostEnteredCargo()
		{
			if (!IsValid || !self.IsInWorld)
				return;

			self.World.AddFrameEndTask(w =>
			{
				w.Remove(self);
			});
		}

		public void HostExitedCargo()
		{
			if (!IsValid || self.IsInWorld)
				return;

			self.World.AddFrameEndTask(w =>
			{
				SetPosition(attachedTo.CenterPosition);
				w.Add(self);
			});
		}

		public void HostTransformed(Actor newActor)
		{
			var newAttachableTo = newActor.TraitOrDefault<AttachableTo>();
			if (newAttachableTo != null && newAttachableTo.Attach(this))
				return;

			if (Info.OnDetachTransformInto != null)
			{
				var faction = self.Owner.Faction.InternalName;
				var transform = new InstantTransform(self, Info.OnDetachTransformInto)
				{
					ForceHealthPercentage = 0,
					Faction = faction,
					SkipMakeAnims = true,
					Offset = new CVec(0, 1),
					OnComplete = a => Detach()
				};

				self.QueueActivity(transform);
			}
			else
			{
				Detach();
				self.Dispose();
			}
		}

		void ITick.Tick(Actor self)
		{
			if (!IsValid || attachedTo == null)
				return;

			if (!self.IsInWorld)
				return;

			SetPosition(attachedTo.CenterPosition);
		}

		void SetPosition(WPos pos)
		{
			if (attachedTo.CenterPosition.X == self.CenterPosition.X && attachedTo.CenterPosition.Y == self.CenterPosition.Y)
				return;

			var newPosition = new WPos(pos.X, pos.Y, self.CenterPosition.Z);
			positionable.SetPosition(self, newPosition);
			positionable.SetCenterPosition(self, newPosition);
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

		/** Updates AttachedTo and updates conditions. */
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

		void INotifyAiming.StartedAiming(Actor self, AttackBase attack) { }
		void INotifyAiming.StoppedAiming(Actor self, AttackBase attack)
		{
			Stop();
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new AttachOrderTargeter(this);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID != "Attach")
				return null;

			return new Order(order.OrderID, self, target, queued);
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "Attach" && CanAttachToTarget(self, order.Target)
				? Info.Voice : null;
		}

		public bool CanAttachToTarget(Actor self, in Target target)
		{
			switch (target.Type)
			{
				case TargetType.Actor:
					return CanAttachToActor(self, target);
				case TargetType.FrozenActor:
					return CanAttachToFrozenActor(self, target);
				default:
					return false;
			}
		}

		bool CanAttachToActor(Actor self, in Target target)
		{
			return Info.Types.Overlaps(target.Actor.GetEnabledTargetTypes())
				&& Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(target.Actor.Owner))
				&& target.Actor.TraitsImplementing<AttachableTo>().Any(x => x.CanAttach(this));
		}

		bool CanAttachToFrozenActor(Actor self, in Target target)
		{
			return target.FrozenActor.IsValid
				&& Info.Types.Overlaps(target.FrozenActor.TargetTypes)
				&& Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(target.FrozenActor.Owner))
				&& target.FrozenActor.Actor.TraitsImplementing<AttachableTo>().Any(x => x.CanAttach(this));
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "Attach")
				return;

			if (!CanAttachToTarget(self, order.Target))
				return;

			self.QueueActivity(order.Queued, new Attach(self, order.Target, this, Info.TargetLineColor));
			self.ShowTargetLines();
		}
	}

	sealed class AttachOrderTargeter : UnitOrderTargeter
	{
		readonly Attachable attachable;

		public AttachOrderTargeter(Attachable attachable)
			: base("Attach", 7, attachable.Info.EnterCursor, true, true)
		{
			this.attachable = attachable;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			var stance = self.Owner.RelationshipWith(target.Owner);
			if (!attachable.Info.ValidRelationships.HasRelationship(stance))
				return false;

			if (!attachable.Info.Types.Overlaps(target.GetEnabledTargetTypes()))
				return false;

			cursor = target.TraitsImplementing<AttachableTo>().Any(x => x.CanAttach(attachable)) ? attachable.Info.EnterCursor : attachable.Info.BlockedCursor;
			return true;
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			var stance = self.Owner.RelationshipWith(target.Owner);
			if (!attachable.Info.ValidRelationships.HasRelationship(stance))
				return false;

			if (!attachable.Info.Types.Overlaps(target.Info.GetAllTargetTypes()))
				return false;

			cursor = target.Actor.TraitsImplementing<AttachableTo>().Any(x => x.CanAttach(attachable)) ? attachable.Info.EnterCursor : attachable.Info.BlockedCursor;
			return true;
		}
	}
}
