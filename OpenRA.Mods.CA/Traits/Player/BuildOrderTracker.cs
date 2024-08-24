#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Keeps track of player's initial build order for observer stats.")]
	public class BuildOrderTrackerInfo : TraitInfo
	{
		[Desc("Maximum number of items to track.")]
		public readonly int MaxItems = 12;

		public override object Create(ActorInitializer init) { return new BuildOrderTracker(init.Self, this); }
	}

	public class BuildOrderTracker
	{
		readonly BuildOrderTrackerInfo info;
		List<string> buildOrder;
		public int Count { get; private set; }
		public List<string> BuildOrder => buildOrder;

		public BuildOrderTracker(Actor self, BuildOrderTrackerInfo info)
		{
			this.info = info;
			buildOrder = new List<string>();
			Count = 0;
		}

		public void BuildingCreated(string type)
		{
			if (Count >= info.MaxItems)
				return;

			Count++;
			buildOrder.Add(type);
		}
	}
}
