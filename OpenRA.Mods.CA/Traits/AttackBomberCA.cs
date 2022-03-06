﻿#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class AttackBomberCAInfo : AttackBaseInfo
	{
		public override object Create(ActorInitializer init) { return new AttackBomberCA(init.Self, this); }
	}

	[Desc("CA version makes FacingTolerance take effect and allows AttackMove activity to be used.")]
	public class AttackBomberCA : AttackBase, ITick, ISync, INotifyRemovedFromWorld
	{
		readonly AttackBomberCAInfo info;

		[Sync]
		Target target;

		[Sync]
		bool inAttackRange;

		[Sync]
		bool facingTarget = true;

		public event Action<Actor> OnRemovedFromWorld = self => { };
		public event Action<Actor> OnEnteredAttackRange = self => { };
		public event Action<Actor> OnExitedAttackRange = self => { };

		public AttackBomberCA(Actor self, AttackBomberCAInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		void ITick.Tick(Actor self)
		{
			var wasInAttackRange = inAttackRange;
			inAttackRange = false;

			if (self.IsInWorld)
			{
				var dat = self.World.Map.DistanceAboveTerrain(target.CenterPosition);
				target = Target.FromPos(target.CenterPosition - new WVec(WDist.Zero, WDist.Zero, dat));

				var wasFacingTarget = facingTarget;
				facingTarget = TargetInFiringArc(self, target, info.FacingTolerance);

				foreach (var a in Armaments)
				{
					if (!target.IsInRange(self.CenterPosition, a.MaxRange()))
						continue;

					inAttackRange = true;

					if (facingTarget)
						a.CheckFire(self, facing, target);
				}

				// Actors without armaments may want to trigger an action when it passes the target
				if (!Armaments.Any())
					inAttackRange = !wasInAttackRange && !facingTarget && wasFacingTarget;
			}

			if (inAttackRange && !wasInAttackRange)
				OnEnteredAttackRange(self);

			if (!inAttackRange && wasInAttackRange)
				OnExitedAttackRange(self);
		}

		public void SetTarget(World w, WPos pos) { target = Target.FromPos(pos); }

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			OnRemovedFromWorld(self);
		}

		public override Activity GetAttackActivity(Actor self, AttackSource source, in Target newTarget, bool allowMove, bool forceAttack, Color? targetLineColor)
		{
			return new FlyAttack(self, source, newTarget, forceAttack, targetLineColor);
		}
	}
}
