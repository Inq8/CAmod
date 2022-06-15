#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Warheads
{
	[Desc("Does nothing.")]
	public class DummyWarhead : TargetDamageWarhead
	{
		public override void DoImpact(in Target target, WarheadArgs args)
		{
            // do nothing
		}

		public override bool IsValidAgainst(Actor victim, Actor firedBy)
		{
            return true;
        }
	}
}
