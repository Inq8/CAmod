#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("A pool of experience that can be added to and taken from.")]
	public class ReclaimableExperiencePoolInfo : TraitInfo
	{
		[Desc("Percentage modifier to apply when adding XP to the pool.")]
		public readonly int Percentage = 100;

		public override object Create(ActorInitializer init) { return new ReclaimableExperiencePool(init, this); }
	}

	public class ReclaimableExperiencePool
	{
		public readonly ReclaimableExperiencePoolInfo Info;
		Dictionary<string, List<int>> xpPool;

		public ReclaimableExperiencePool(ActorInitializer init, ReclaimableExperiencePoolInfo info)
		{
			Info = info;
			xpPool = new Dictionary<string, List<int>>();
		}

		public void AddXpToPool(string type, int amount)
		{
			if (!xpPool.ContainsKey(type))
				xpPool[type] = new List<int>();

			xpPool[type].Add(Util.ApplyPercentageModifiers(amount, new[] { Info.Percentage }));
		}

		public int TakeXpFromPool(string type)
		{
			if (!xpPool.TryGetValue(type, out var value) || value.Count == 0)
				return 0;

			int xp = value.Max();
			value.Remove(xp);
			return xp;
		}
	}
}
