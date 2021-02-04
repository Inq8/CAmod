#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Warheads
{
	[Desc("This warhead activates the global flash effect when detonated.")]
	public class FlashEffectWarhead : WarheadAS
	{
		[FieldLoader.Require]
		[Desc("Corresponds to `Type` from `FlashPaletteEffect` on the world actor.")]
		public readonly string FlashType = null;

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			if (!target.IsValidFor(firedBy))
				return;

			if (!IsValidImpact(target.CenterPosition, firedBy))
				return;

			foreach (var flash in firedBy.World.WorldActor.TraitsImplementing<FlashPaletteEffect>())
				if (flash.Info.Type == FlashType)
					flash.Enable(-1);
		}
	}
}
