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
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Warheads
{
	[Desc("This warhead can detach a DelayedWeapon from the target. Requires an appropriate type of DelayedWeaponAttachable trait to function properly.")]
	public class DetachDelayedWeaponWarhead : WarheadAS
	{
		[Desc("Types of DelayedWeapons that it can detach.")]
		public readonly HashSet<string> Types = new HashSet<string> { "bomb" };

		[Desc("Range of targets to be attached.")]
		public readonly WDist Range = new WDist(1024);

		[Desc("Defines how many DelayedWeapons can be detached per impact.")]
		public readonly int DetachLimit = 1;

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			if (!target.IsValidFor(firedBy))
				return;

			var pos = target.CenterPosition;

			if (!IsValidImpact(pos, firedBy))
				return;

			var availableActors = firedBy.World.FindActorsOnCircle(pos, Range);
			foreach (var actor in availableActors)
			{
				if (!IsValidAgainst(actor, firedBy))
					continue;

				if (actor.IsDead)
					continue;

				var activeShapes = actor.TraitsImplementing<HitShape>().Where(Exts.IsTraitEnabled);
				if (!activeShapes.Any())
					continue;

				var distance = activeShapes.Min(t => t.DistanceFromEdge(actor, pos));

				if (distance > Range)
					continue;

				var attachables = actor.TraitsImplementing<DelayedWeaponAttachable>();
				var triggers = attachables.Where(a => Types.Any(at => at == a.Info.Type)).SelectMany(a => a.Container);
				triggers.OrderBy(t => t.RemainingTime).Take(DetachLimit).ToList().ForEach(t => t.Deactivate());
			}
		}
	}
}
