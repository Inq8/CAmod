#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Tag trait for Cash Hack support power.")]
	public class CashHackableInfo : TraitInfo
	{
		[Desc("Accepted `CashHack` types. Leave empty to accept all types.")]
		public readonly HashSet<string> ValidTypes = new HashSet<string>() { "Cash-Hack" };

		public override object Create(ActorInitializer init) { return new CashHackable(this); }
	}

	public class CashHackable
	{
		public CashHackable(CashHackableInfo info) { }
	}
}
