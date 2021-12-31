#region Copyright & License Information
/*
 * Copyright 2007-2022 The CA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	public class ParadropCargo : Activity
	{
		readonly Aircraft aircraft;
		readonly AttackAircraft attackAircraft;
		readonly Cargo cargo;
		readonly INotifyUnload[] notifiers;
		readonly bool assignTargetOnFirstRun;
		readonly bool returnToBase;
		readonly int dropInterval;
		readonly WDist dropRange;

		Target destination;
		int dropDelay;
		bool exitParadrop;
		bool inDropRange;

		public ParadropCargo(Actor self, int dropInterval, WDist dropRange, bool returnToBase)
			: this(self, Target.Invalid, dropInterval, dropRange, returnToBase)
		{
			assignTargetOnFirstRun = true;
		}

		public ParadropCargo(Actor self, in Target destination, int dropInterval, WDist dropRange, bool returnToBase)
		{
			cargo = self.Trait<Cargo>();
			notifiers = self.TraitsImplementing<INotifyUnload>().ToArray();
			this.destination = destination;
			this.dropInterval = dropInterval;
			this.dropRange = dropRange;
			this.returnToBase = returnToBase;
			ChildHasPriority = false;
			aircraft = self.Trait<Aircraft>();
			attackAircraft = self.Trait<AttackAircraft>();
		}

		protected override void OnFirstRun(Actor self)
		{
			if (assignTargetOnFirstRun)
				destination = Target.FromCell(self.World, self.Location);

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

			var wasInDropRange = inDropRange;
			inDropRange = destination.IsInRange(self.CenterPosition, dropRange);

			// We have troops, we are not near the DZ & the troops can't get out; Turn around
			if (!cargo.IsEmpty(self) && !inDropRange && !cargo.CanUnload())
			{
				var pos = self.CenterPosition;
				var delta = attackAircraft.GetTargetPosition(pos, destination) - pos;
				var desiredFacing = delta.HorizontalLengthSquared != 0 ? delta.Yaw : aircraft.Facing;
				aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, aircraft.TurnSpeed);
			}

			// Empty; lets go home
			if (cargo.IsEmpty(self))
			{
				if (exitParadrop)
					return ChildActivity == null;

				ChildActivity?.Cancel(self);

				if (returnToBase)
				{
					QueueChild(new ReturnToBase(self));
				}
				else
				{
					QueueChild(new FlyIdle(self));
				}

				exitParadrop = true;
			}

			// Else; everybody out
			if (cargo.CanUnload())
			{
				foreach (var inu in notifiers)
					inu.Unloading(self);

				var spawn = self.CenterPosition;
				var dropActor = cargo.Peek(self);
				var dropCell = self.Location;
				var dropPositionable = dropActor.Trait<IPositionable>();
				var dropSubCell = dropPositionable.GetAvailableSubCell(dropCell);

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
