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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	enum InterceptorState
	{
		Approaching,
		Returning,
		Guarding,
		Exiting
	}

	public class InterceptorInfo : AttackBaseInfo
	{
		public override object Create(ActorInitializer init) { return new Interceptor(init.Self, this); }
	}

	[Desc("Used for actors spawned by InterceptorsPower.")]
	public class Interceptor : ITick, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		WPos targetPos;
		WVec targetOffset;
		WPos exitPos;
		WDist guardRadius;
		int guardDuration;
		InterceptorState state;
		int guardTicksRemaining;
		readonly AutoTarget autoTarget;

		public event Action<Actor> OnRemovedFromWorld = self => { };
		public event Action<Actor> OnEnteredAttackRange = self => { };
		public event Action<Actor> OnExitedAttackRange = self => { };

		public Interceptor(Actor self, InterceptorInfo info)
		{
			autoTarget = self.TraitOrDefault<AutoTarget>();
		}

		public void Initialize(World w, WPos targetPos, WVec targetOffset, WPos exitPos, WDist guardRadius, int guardDuration)
		{
			this.targetPos = targetPos;
			this.targetOffset = targetOffset;
			this.exitPos = exitPos;
			this.guardRadius = guardRadius;
			this.guardDuration = guardDuration;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			state = InterceptorState.Approaching;
			self.QueueActivity(new Fly(self, Target.FromPos(targetPos + targetOffset)));
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			OnRemovedFromWorld(self);
		}

		void ITick.Tick(Actor self)
		{
			if (state == InterceptorState.Approaching)
			{
				var distanceToTarget = (targetPos + targetOffset - self.CenterPosition).HorizontalLength;
				if (distanceToTarget <= guardRadius.Length)
				{
					state = InterceptorState.Guarding;
					guardTicksRemaining = guardDuration;
					self.QueueActivity(false, new AttackMoveActivity(self, () => new Fly(self, Target.FromPos(targetPos + targetOffset))));
					self.QueueActivity(new AttackMoveActivity(self, () => new FlyIdle(self)));
					OnEnteredAttackRange(self);
				}
			}
			else if (state == InterceptorState.Guarding)
			{
				if (--guardTicksRemaining <= 0)
				{
					state = InterceptorState.Exiting;
					self.QueueActivity(false, new Fly(self, Target.FromPos(exitPos)));
					self.QueueActivity(new RemoveSelf());
					OnExitedAttackRange(self);
					return;
				}

				var distanceToTarget = (targetPos - self.CenterPosition).HorizontalLength;
				var distanceToInitialTarget = (targetPos + targetOffset - self.CenterPosition).HorizontalLength;
				if (distanceToTarget > guardRadius.Length && distanceToInitialTarget > guardRadius.Length)
				{
					state = InterceptorState.Returning;
					autoTarget?.SetStance(self, UnitStance.Defend);
					self.QueueActivity(false, new Fly(self, Target.FromPos(targetPos)));
				}
			}
			else if (state == InterceptorState.Returning)
			{
				guardTicksRemaining--;

				var distanceToTarget = (targetPos - self.CenterPosition).HorizontalLength;
				if (distanceToTarget <= WDist.FromCells(2).Length)
				{
					autoTarget?.SetStance(self, UnitStance.HoldFire);
					self.QueueActivity(false, new AttackMoveActivity(self, () => new FlyIdle(self)));
					state = InterceptorState.Guarding;
				}
			}
		}
	}
}
