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
	[Desc("Attached to player to track what factions conyards or factories have been captured.")]
	public class CapturedFactionsManagerInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new CapturedFactionsManager(this); }
	}

	public class CapturedFactionsManager
	{
		public HashSet<string> Factions { get; private set; }

		public CapturedFactionsManager(CapturedFactionsManagerInfo info)
		{
			Factions = new HashSet<string>();
		}

		public void AddFaction(string faction)
		{
			Factions.Add(faction);
		}

		public bool HasFaction(string faction)
		{
			return Factions.Contains(faction);
		}
	}
}
