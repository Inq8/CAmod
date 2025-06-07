

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
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("When exiting a transport the actor will scatter.")]
	public class ScatterOnExitCargoInfo : ConditionalTraitInfo
	{
		[Desc("Scatter on exiting these actor types.")]
		public readonly string[] CargoActors = {};

		[Desc("Only scatter if the cargo is dead.")]
		public readonly bool OnlyIfCargoIsDead = false;

		public override object Create(ActorInitializer init) { return new ScatterOnExitCargo(init, this); }
	}

	public class ScatterOnExitCargo : ConditionalTrait<ScatterOnExitCargoInfo>, INotifyExitedCargo
	{
		private readonly ScatterOnExitCargoInfo info;

		public ScatterOnExitCargo(ActorInitializer init, ScatterOnExitCargoInfo info)
			: base(info)
		{
			this.info = info;
		}

		void INotifyExitedCargo.OnExitedCargo(Actor self, Actor cargo)
		{
			if (info.CargoActors.Any() && info.CargoActors.Contains(cargo.Info.Name) && (cargo.IsDead || !info.OnlyIfCargoIsDead))
			{
				self.QueueActivity(false, new Nudge(self));
			}
		}
	}
}
