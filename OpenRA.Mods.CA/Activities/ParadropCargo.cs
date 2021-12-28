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
	public class ParaDropCargo : Activity
	{
		readonly Actor self;
		readonly Cargo cargo;
		readonly INotifyUnload[] notifiers;
		readonly bool unloadAll;
		readonly Aircraft aircraft;
		readonly Mobile mobile;
		readonly bool assignTargetOnFirstRun;
		readonly WDist unloadRange;

		Target destination;
		bool flying;

		public ParaDropCargo(Actor self, WDist unloadRange, bool unloadAll = true)
			: this(self, Target.Invalid, unloadRange, unloadAll)
		{
			assignTargetOnFirstRun = true;
		}

		public ParaDropCargo(Actor self, in Target destination, WDist unloadRange, bool unloadAll = true)
		{
			this.self = self;
			cargo = self.Trait<Cargo>();
			notifiers = self.TraitsImplementing<INotifyUnload>().ToArray();
			this.unloadAll = unloadAll;
			aircraft = self.TraitOrDefault<Aircraft>();
			mobile = self.TraitOrDefault<Mobile>();
			this.destination = destination;
			this.unloadRange = unloadRange;
		}

		protected override void OnFirstRun(Actor self)
		{
			if (assignTargetOnFirstRun)
				destination = Target.FromCell(self.World, self.Location);

			// Move to the target destination
			QueueChild(new FlyAttackRun(self, destination, unloadRange));
			flying = !aircraft.AtLandAltitude;
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling || cargo.IsEmpty(self))
				return true;

			if (cargo.CanUnload())
			{
				foreach (var inu in notifiers)
					inu.Unloading(self);

				var actor = cargo.Peek(self);
				var spawn = self.CenterPosition;

				cargo.Unload(self);
				self.World.AddFrameEndTask(w =>
				{
					if (actor.Disposed)
						return;

					var move = actor.Trait<IMove>();
					var pos = actor.Trait<IPositionable>();

					pos.SetVisualPosition(actor, spawn);

					actor.CancelActivity();
					actor.QueueActivity(new Parachute(actor));
					w.Add(actor);
				});
				Game.Sound.Play(SoundType.World, self.Info.TraitInfo<ParaDropInfo>().ChuteSound, spawn);
			}

			if (flying && !cargo.CanUnload())
			{
				if (cargo.Info.AfterUnloadDelay > 0)
					QueueChild(new FlyForward(self, cargo.Info.AfterUnloadDelay));

				if (cargo.IsEmpty(self))
					QueueChild(new ReturnToBase(self));

				return true;
			}

			return false;
		}
	}
}
