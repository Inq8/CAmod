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
	[Desc("For storing global support power properties e.g. to limit the number of times timers are modified.")]
	public class SupportPowerInstanceManagerInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new SupportPowerInstanceManager(); }
	}

	public class SupportPowerInstanceManager
	{
		public readonly HashSet<string> InitiallyFullyChargedPowers = new HashSet<string>();

		public SupportPowerInstanceManager()
		{

		}
	}
}
