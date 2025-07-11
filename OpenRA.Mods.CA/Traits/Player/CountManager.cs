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
	[TraitLocation(SystemActors.Player)]
	[Desc("Allows arbitrary counts.")]
	public class CountManagerInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new CountManager(); }
	}

	public class CountManager
	{
		public Dictionary<string, int> Counts { get; }
		public event Action<string, int> Incremented;
		public event Action<string, int> Decremented;

		public CountManager()
		{
			Counts = new Dictionary<string, int>();
		}

		public void Increment(string type)
		{
			if (!Counts.ContainsKey(type))
				Counts[type] = 0;

			Counts[type]++;
			Incremented?.Invoke(type, Counts[type]);
		}

		public void Decrement(string type)
		{
			if (!Counts.TryGetValue(type, out var value))
				return;

			Counts[type] = --value;
			Decremented?.Invoke(type, value);
		}
	}
}
