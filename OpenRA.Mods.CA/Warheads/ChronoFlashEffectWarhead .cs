#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Warheads
{
	[Desc("This warhead activates the global flash effect when detonated.")]
	public class ChronoFlashEffectWarhead : WarheadAS
	{
		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			if (!target.IsValidFor(firedBy))
				return;

			if (!IsValidImpact(target.CenterPosition, firedBy))
				return;

			if (target.IsValidFor(firedBy))
				foreach (var a in firedBy.World.ActorsWithTrait<ChronoshiftPaletteEffect>())
					a.Trait.Enable();
		}
	}
}
