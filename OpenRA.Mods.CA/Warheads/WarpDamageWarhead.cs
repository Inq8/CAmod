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
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;

namespace OpenRA.Mods.CA.Warheads
{
	[Desc("Affects warp value on the actors with Warpable trait.")]
	public class WarpDamageWarhead : TargetDamageWarhead
	{
		protected override void InflictDamage(Actor victim, Actor firedBy, HitShape hitshape, WarheadArgs args)
		{
			var warpable = victim.TraitOrDefault<Warpable>();
			if (warpable == null)
				return;

			var damage = Util.ApplyPercentageModifiers(Damage, args.DamageModifiers.Append(DamageVersus(victim, hitshape, args)));
			warpable.AddDamage(damage, firedBy);
		}
	}
}
