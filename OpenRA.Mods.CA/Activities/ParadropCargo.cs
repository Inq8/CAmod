#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	public class ParadropCargo : Activity
	{
		readonly Cargo cargo;
		readonly INotifyUnload[] notifiers;
		readonly bool assignTargetOnFirstRun;
		readonly int dropInterval;
		readonly WDist exitRange;

		Target destination;
		WPos targetPosition;
		int dropDelay;
		bool returningToBase;

		public ParadropCargo(Actor self, int dropInterval, WDist exitRange)
			: this(self, Target.Invalid, dropInterval, exitRange)
		{
			assignTargetOnFirstRun = true;
		}

		public ParadropCargo(Actor self, in Target destination, int dropInterval, WDist exitRange)
		{
			cargo = self.Trait<Cargo>();
			notifiers = self.TraitsImplementing<INotifyUnload>().ToArray();
			this.destination = destination;
			this.dropInterval = dropInterval;
			this.exitRange = exitRange;
			ChildHasPriority = false;
		}

		protected override void OnFirstRun(Actor self)
		{
			if (assignTargetOnFirstRun)
				destination = Target.FromCell(self.World, self.Location);
			targetPosition = destination.CenterPosition;

			QueueChild(new FlyForward(self, -1));
		}

		public override bool Tick(Actor self)
		{
			TickChild(self);

			if (dropDelay > 0)
			{
				dropDelay--;
				return false;
			}

			if (IsCanceling)
				return true;

			if (cargo.IsEmpty(self))
			{
				if (returningToBase)
					return ChildActivity == null;

				ChildActivity?.Cancel(self);
				QueueChild(new ReturnToBase(self));
				returningToBase = true;
			}

			if (cargo.CanUnload())
			{
				foreach (var inu in notifiers)
					inu.Unloading(self);

				var spawn = self.CenterPosition;
				var dropActor = cargo.Peek(self);
				var dropCell = self.Location;
				var dropPositionable = dropActor.Trait<IPositionable>();
				var dropSubCell = dropPositionable.GetAvailableSubCell(dropCell);

				targetPosition = destination.CenterPosition;

				cargo.Unload(self);
				self.World.AddFrameEndTask(w =>
					{
						if (dropActor.Disposed)
							return;

						dropPositionable.SetPosition(dropActor, dropCell, dropSubCell);

						var dropPosition = dropActor.CenterPosition + new WVec(0, 0, self.CenterPosition.Z - dropActor.CenterPosition.Z);
						dropPositionable.SetPosition(dropActor, dropPosition);
						w.Add(dropActor);
					});
				Game.Sound.Play(SoundType.World, self.Info.TraitInfo<ParaDropInfo>().ChuteSound, spawn);
				dropDelay = dropInterval;
			}

			return false;
		}
	}
}
