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
using OpenRA.Activities;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	enum AttachState { Approaching, Attaching, Finished }

	public class Attach : Activity
	{
		Target target;
		Attachable attachable;
		AttachableTo attachableTo;
		IMove move;
		readonly Color targetLineColor;
		AttachState state;

		bool TargetIsValid => target.Actor != null && target.Type == TargetType.Actor && target.Actor.IsInWorld && !target.Actor.IsDead && attachableTo.CanAttach(attachable);

		public Attach(Actor self, in Target target, Attachable attachable, Color? targetLineColor)
		{
			this.target = target;
			this.attachable = attachable;
			move = self.TraitOrDefault<IMove>();
			var moveInfo = self.Info.TraitInfoOrDefault<IMoveInfo>();
			attachableTo = target.Actor.TraitsImplementing<AttachableTo>().FirstOrDefault();
			this.targetLineColor = targetLineColor ?? moveInfo.GetTargetLineColor();
			state = AttachState.Approaching;
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling)
			{
				return true;
			}

			if (!TargetIsValid)
				return true;

			bool isCloseEnough;

			if (attachable.Info.MinAttachDistance < WDist.Zero)
				isCloseEnough = true;
			else
				isCloseEnough = (target.CenterPosition - self.CenterPosition).HorizontalLengthSquared <= attachable.Info.MinAttachDistance.LengthSquared;

			if (isCloseEnough)
			{
				if (state == AttachState.Approaching)
				{
					OnMinimumDistanceReached(self);
				}
			}
			else if (state != AttachState.Attaching && state != AttachState.Finished)
			{
				QueueChild(move.MoveWithinRange(target, attachable.Info.MinAttachDistance, targetLineColor: targetLineColor));
				return false;
			}

			if (state == AttachState.Finished)
			{
				return true;
			}

			return false;
		}

		void OnMinimumDistanceReached(Actor self)
		{
			BeginAttachment(self);
		}

		void BeginAttachment(Actor self)
		{
			state = AttachState.Attaching;

			if (!attachableTo.CanAttach(attachable))
			{
				state = AttachState.Finished;
				return;
			}

			attachableTo.Reserve();

			if (attachable.Info.OnAttachTransformInto != null)
			{
				var faction = self.Owner.Faction.InternalName;
				var transform = new InstantTransform(self, attachable.Info.OnAttachTransformInto)
				{
					ForceHealthPercentage = 0,
					Faction = faction,
					OnComplete = a => CompleteAttachment(a),
					SkipMakeAnims = true,
				};

				QueueChild(transform);
			}
			else
				CompleteAttachment(self);
		}

		void CompleteAttachment(Actor a)
		{
			state = AttachState.Finished;
			var attachable = a.TraitOrDefault<Attachable>();
			if (attachable == null)
				return;

			var attached = attachableTo.Attach(attachable, true);

			if (attached && attachable.Info.AttachSound != null)
				Game.Sound.Play(SoundType.World, attachable.Info.AttachSound, a.CenterPosition);
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (ChildActivity == null)
				yield return new TargetLineNode(target, targetLineColor);
			else
			{
				var current = ChildActivity;
				while (current != null)
				{
					foreach (var n in current.TargetLineNodes(self))
						yield return n;

					current = current.NextActivity;
				}
			}
		}
	}
}
