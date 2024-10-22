#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Tracks and provides access to player bounty pool.")]
	public class PlayerBountyPoolInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new PlayerBountyPool(); }
	}

	public class PlayerBountyPool
	{
		int availableBounty;

		public int AvailableBounty
		{
			get { return availableBounty; }
		}

		public PlayerBountyPool()
		{
			availableBounty = 0;
		}

		public void AddBounty(int amount)
		{
			availableBounty += amount;
		}

		public int CollectBounty(int amount)
		{
			if (availableBounty < amount)
			{
				amount = availableBounty;
			}

			availableBounty -= amount;
			return amount;
		}
	}
}
