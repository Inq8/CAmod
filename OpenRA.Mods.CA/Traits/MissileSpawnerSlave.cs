#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	[Desc("This unit is \"slaved\" to a missile spawner master.")]
	public class MissileSpawnerSlaveInfo : BaseSpawnerSlaveInfo
	{
		public override object Create(ActorInitializer init) { return new MissileSpawnerSlave(init, this); }
	}

	public class MissileSpawnerSlave : BaseSpawnerSlave
	{
		public CarrierSlaveInfo Info { get; set; }

		public MissileSpawnerSlave(ActorInitializer init, MissileSpawnerSlaveInfo info)
			: base(init, info) { }
	}
}
