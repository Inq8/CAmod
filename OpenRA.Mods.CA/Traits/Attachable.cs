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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public enum OnDetachBehavior
	{
		Dispose,
		None,
		Transform
	}

	[Desc("Use on an actor to make it attachable to other actors with the AttachableTo trait.",
		"An actor should only have one Attachable trait.")]
	public class AttachableInfo : TraitInfo, Requires<IPositionableInfo>
	{
		[Desc("The attachment type (matches that of the `" + nameof(AttachableTo) + "` trait).")]
		[FieldLoader.Require]
		public readonly string Type = null;

		[Desc("The `TargetTypes` from `Targetable` that can be attached to.")]
		public readonly BitSet<TargetableType> TargetTypes = default;

		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Color to use for the target line.")]
		public readonly Color TargetLineColor = Color.Yellow;

		[Desc("Player relationships the owner of the attachment target needs.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		[CursorReference]
		[Desc("Cursor to display when able to attach to target actor.")]
		public readonly string EnterCursor = "enter";

		[CursorReference]
		[Desc("Cursor to display when able to attach to multiple actors.")]
		public readonly string EnterMultiCursor = "enter-multi";

		[CursorReference]
		[Desc("Cursor to display when unable to attach the target actor.")]
		public readonly string BlockedCursor = "enter-blocked";

		[Desc("Sounds played on being attached.")]
		public readonly string AttachSound = null;

		[GrantedConditionReference]
		[Desc("The condition to grant when attached.")]
		public readonly string AttachedCondition = null;

		[GrantedConditionReference]
		[Desc("The condition to grant when detached.")]
		public readonly string DetachedCondition = null;

		[Desc("On attaching, transform into this actor.")]
		public readonly string OnAttachTransformInto = null;

		[Desc("On detaching, transform into this actor.")]
		public readonly string OnDetachTransformInto = null;

		[Desc("If true, detatch on host being killed/captured, otherwise dispose.")]
		public readonly OnDetachBehavior OnDetachBehavior = OnDetachBehavior.Dispose;

		[Desc("The range at which the actor can attach.")]
		public readonly WDist MinAttachDistance = WDist.Zero;

		public override object Create(ActorInitializer init) { return new Attachable(init, this); }
	}

	public class Attachable : INotifyCreated, INotifyKilled, INotifyActorDisposing, INotifyOwnerChanged, INotifyAiming,
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
			CompleteDetach();
			attachedTo = attachableTo;
			SetPosition(pos);

			if (Info.AttachedCondition != null && attachedConditionToken == Actor.InvalidConditionToken)
				attachedConditionToken = self.GrantCondition(Info.AttachedCondition);

			if (detachedConditionToken != Actor.InvalidConditionToken)
				detachedConditionToken = self.RevokeCondition(detachedConditionToken);
		}

		public void HostLost()
		{
			InitDetach();
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
			var newAttachableTo = newActor.TraitsImplementing<AttachableTo>().FirstOrDefault(a => a.CanAttach(this));
			if (newAttachableTo != null && newAttachableTo.Attach(this))
				return;

			InitDetach();
		}

		public void HostPositionChanged()
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
			CompleteDetach();
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			CompleteDetach();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			InitDetach();
		}

		void InitDetach()
		{
			switch (Info.OnDetachBehavior)
			{
				case OnDetachBehavior.Dispose:
					CompleteDetach();
					self.Dispose();
					break;

				case OnDetachBehavior.None:
					CompleteDetach();
					break;

				case OnDetachBehavior.Transform:
					Transform();
					break;
			}
		}

		void Transform()
		{
			if (Info.OnDetachTransformInto == null)
				throw new InvalidOperationException($"No actor defined for {self.Info.Name} to transform into on detaching.");

			var faction = self.Owner.Faction.InternalName;
			var transform = new InstantTransform(self, Info.OnDetachTransformInto)
			{
				ForceHealthPercentage = 0,
				Faction = faction,
				SkipMakeAnims = true,
				Offset = new CVec(0, 1),
				OnComplete = a => CompleteDetach()
			};

			self.QueueActivity(false, transform);
		}

		/** Updates AttachedTo and updates conditions. */
		void CompleteDetach()
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
			if (attachedTo != null)
				Stop();
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new AttachOrderTargeter(this, true);
				yield return new AttachOrderTargeter(this);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID != "Attach" && order.OrderID != "MassAttach")
				return null;

			return new Order(order.OrderID, self, target, queued);
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "Attach" || order.OrderString != "MassAttach") && CanAttachToTarget(self, order.Target)
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
			return (!Info.TargetTypes.Any() || Info.TargetTypes.Overlaps(target.Actor.GetEnabledTargetTypes()))
				&& Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(target.Actor.Owner))
				&& target.Actor.TraitsImplementing<AttachableTo>().Any(x => x.CanAttach(this));
		}

		bool CanAttachToFrozenActor(Actor self, in Target target)
		{
			return target.FrozenActor.IsValid
				&& (!Info.TargetTypes.Any() || Info.TargetTypes.Overlaps(target.FrozenActor.TargetTypes))
				&& Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(target.FrozenActor.Owner))
				&& target.FrozenActor.Actor.TraitsImplementing<AttachableTo>().Any(x => x.CanAttach(this));
		}

		public void ResolveOrder(Actor self, Order order)
		{
			switch (order.OrderString)
			{
				case "Attach":
					if (!CanAttachToTarget(self, order.Target))
						return;

					self.QueueActivity(order.Queued, new Attach(self, order.Target, this, Info.TargetLineColor));
					break;

				case "MassAttach":
					ResolveMassAttachOrder(self, order);
					break;
			}

			self.ShowTargetLines();
		}

		void ResolveMassAttachOrder(Actor self, Order order)
		{
			// Enter orders are only valid for own/allied actors,
			// which are guaranteed to never be frozen.
			if (order.Target.Type != TargetType.Actor)
				return;

			var targetActor = order.Target.Actor;

			var selectedWithTrait = self.World.Selection.Actors
				.Where(a => a.Info.TraitInfos<AttachableInfo>().Any(ai => ai.Type == Info.Type)
					&& a.Owner == self.Owner
					&& !a.IsDead)
				.OrderBy(a => (a.CenterPosition - targetActor.CenterPosition).LengthSquared)
				.Select(a => new TraitPair<Attachable>(a, a.TraitsImplementing<Attachable>().FirstOrDefault(a => a.Info.Type == Info.Type)));

			// Find the closest actor to the target transport
			var closestActor = selectedWithTrait.FirstOrDefault();

			// Only perform allocation if the current actor is the closest actor
			if (closestActor.Actor != self)
				return;

			// Create a list of available transports
			var availableTargets = self.World.Actors
				.Where(a => a.Info.TraitInfos<AttachableToInfo>().Any(ai => ai.Type == Info.Type)
					&& a.Info.Name == targetActor.Info.Name
					&& a.Owner == targetActor.Owner
					&& !a.IsDead
					&& (a.CenterPosition - targetActor.CenterPosition).HorizontalLengthSquared <= WDist.FromCells(10).LengthSquared)
				.Select(a => new TraitPair<AttachableTo>(a, a.TraitsImplementing<AttachableTo>().FirstOrDefault(a => a.Info.Type == Info.Type)))
				.Where(t => closestActor.Trait.CanAttachToActor(closestActor.Actor, Target.FromActor(t.Actor)))
				.ToList();

			// Allocate passengers to the closest available transport
			foreach (var pair in selectedWithTrait)
			{
				if (availableTargets.Count == 0)
					break;

				var closestTarget = availableTargets
					.OrderBy(t => (t.Actor.CenterPosition - pair.Actor.CenterPosition).LengthSquared)
					.FirstOrDefault();

				// can't queue an activity here because the selected units aren't known by all clients so causes a de-sync
				// pair.Actor.QueueActivity(false, new Attach(pair.Actor, Target.FromActor(closestTarget.Actor), pair.Trait, Info.TargetLineColor));
				self.World.IssueOrder(new Order("Attach", pair.Actor, Target.FromActor(closestTarget.Actor), order.Queued));
				pair.Actor.ShowTargetLines();
				// todo: take into account AttachableTo.Info.Limit in the same way MassEntersCargo takes MaxWeight into account

				availableTargets.Remove(closestTarget);
			}
		}
	}

	sealed class AttachOrderTargeter : UnitOrderTargeter
	{
		readonly Attachable attachable;
		readonly bool forceMove;

		public AttachOrderTargeter(Attachable attachable, bool forceMove = false)
			: base(forceMove ? "MassAttach" : "Attach", 7, attachable.Info.EnterCursor, true, true)
		{
			this.attachable = attachable;
			this.forceMove = forceMove;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			if (forceMove && !modifiers.HasModifier(TargetModifiers.ForceMove))
				return false;

			if (!forceMove && modifiers.HasModifier(TargetModifiers.ForceMove))
				return false;

			var stance = self.Owner.RelationshipWith(target.Owner);
			if (!attachable.Info.ValidRelationships.HasRelationship(stance))
				return false;

			if (attachable.Info.TargetTypes.Any() && !attachable.Info.TargetTypes.Overlaps(target.GetEnabledTargetTypes()))
				return false;

			var attachCursor = modifiers.HasModifier(TargetModifiers.ForceMove) ? attachable.Info.EnterMultiCursor : attachable.Info.EnterCursor;

			cursor = target.TraitsImplementing<AttachableTo>().Any(x => x.CanAttach(attachable)) ? attachCursor : attachable.Info.BlockedCursor;
			return true;
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			if (forceMove && !modifiers.HasModifier(TargetModifiers.ForceMove))
				return false;

			if (!forceMove && modifiers.HasModifier(TargetModifiers.ForceMove))
				return false;

			var stance = self.Owner.RelationshipWith(target.Owner);
			if (!attachable.Info.ValidRelationships.HasRelationship(stance))
				return false;

			if (attachable.Info.TargetTypes.Any() && !attachable.Info.TargetTypes.Overlaps(target.Info.GetAllTargetTypes()))
				return false;

			var attachCursor = modifiers.HasModifier(TargetModifiers.ForceMove) ? attachable.Info.EnterMultiCursor : attachable.Info.EnterCursor;

			cursor = target.Actor.TraitsImplementing<AttachableTo>().Any(x => x.CanAttach(attachable)) ? attachCursor : attachable.Info.BlockedCursor;
			return true;
		}
	}
}
