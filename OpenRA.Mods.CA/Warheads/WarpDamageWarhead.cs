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
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;

namespace OpenRA.Mods.CA.Warheads
{
	[Desc("Affects warp value on the actors with Warpable trait.")]
	public class WarpDamageWarhead : TargetDamageWarhead
	{
		protected override void InflictDamage(Actor victim, Actor firedBy, HitShapeInfo hitshapeInfo, IEnumerable<int> damageModifiers)
		{
			var warpable = victim.TraitOrDefault<Warpable>();
			if (warpable == null)
				return;

			var damage = Util.ApplyPercentageModifiers(Damage, damageModifiers.Append(DamageVersus(victim, hitshapeInfo)));
			warpable.AddDamage(damage, firedBy);
		}
	}
}
