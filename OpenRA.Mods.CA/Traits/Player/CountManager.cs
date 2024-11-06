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
using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Simple counter for actors.")]
	public class CountManagerInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new CountManager(); }
	}

	public class CountManager : INotifyCountChanged
	{
		readonly Dictionary<string, int> counts;

		public Dictionary<string, int> Counts => counts;

		public event Action Incremented;
		public event Action Decremented;

		public CountManager()
		{
			counts = new Dictionary<string, int>();
		}

		void INotifyCountChanged.Incremented(string type)
		{
			if (!counts.ContainsKey(type))
				counts[type] = 0;

			counts[type]++;
			Incremented?.Invoke();
		}

		void INotifyCountChanged.Decremented(string type)
		{
			if (!counts.ContainsKey(type))
				return;

			counts[type]--;
			Decremented?.Invoke();
		}
	}
}
