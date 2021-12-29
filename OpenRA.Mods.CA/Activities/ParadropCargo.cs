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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	public class ParadropCargo : Activity
	{
		readonly Actor self;
		readonly Cargo cargo;
		readonly INotifyUnload[] notifiers;
		readonly int dropInterval;
		readonly Aircraft aircraft;
		readonly Mobile mobile;

		Target destination;
		int dropDelay;

		public ParadropCargo(Actor self, int dropInterval)
			: this(self, Target.Invalid, dropInterval)
		{
		}

		public ParadropCargo(Actor self, in Target destination, int dropInterval)
		{
			this.self = self;
			cargo = self.Trait<Cargo>();
			notifiers = self.TraitsImplementing<INotifyUnload>().ToArray();
			aircraft = self.TraitOrDefault<Aircraft>();
			mobile = self.TraitOrDefault<Mobile>();
			this.destination = destination;
			this.dropInterval = dropInterval;
		}

		protected override void OnFirstRun(Actor self)
		{
		}

		public override bool Tick(Actor self)
		{
			if (dropDelay > 0)
			{
				dropDelay--;
				return false;
			}

			if (IsCanceling || cargo.IsEmpty(self))
				return true;

			if (cargo.CanUnload())
			{
				QueueChild(new FlyForward(self, dropInterval));
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

			if (!cargo.CanUnload())
			{
				QueueChild(new FlyForward(self, dropInterval));
				if (cargo.IsEmpty(self))
					QueueChild(new ReturnToBase(self));

				return true;
			}

			return false;
		}
	}
}
