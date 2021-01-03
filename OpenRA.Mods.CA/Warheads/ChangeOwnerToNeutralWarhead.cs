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

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Warheads
{
	[Desc("Changes targets to neutral.")]
	public class ChangeOwnerToNeutralWarhead : Warhead
	{
		[Desc("Faction to change to.")]
		public readonly string Owner = "Neutral";

		public readonly WDist Range = WDist.FromCells(1);

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			var actors = target.Type == TargetType.Actor ? new[] { target.Actor } :
				firedBy.World.FindActorsInCircle(target.CenterPosition, Range);

			foreach (var a in actors)
			{
				// Don't do anything on friendly fire
				if (a.Owner == firedBy.Owner)
					continue;

				if (!target.IsValidFor(firedBy))
					return;

				if (!IsValidAgainst(a, firedBy))
					continue;

				a.ChangeOwner(a.World.Players.First(p => p.InternalName == Owner)); // Permanent

				// Stop shooting, you have new enemies
				a.CancelActivity();
			}
		}
	}
}
