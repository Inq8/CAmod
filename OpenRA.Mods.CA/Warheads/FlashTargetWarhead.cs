#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Warheads
{
	[Desc("Flashes the target.")]
	public class FlashTargetWarhead : TargetDamageWarhead
	{
		public readonly Color Color = Color.White;

		protected override void InflictDamage(Actor victim, Actor firedBy, HitShape shape, WarheadArgs args)
		{
			victim.World.AddFrameEndTask(w =>
			{
				w.Add(new FlashTarget(victim, Color, 0.5f, 1, 2, 0));
			});
		}
	}
}
