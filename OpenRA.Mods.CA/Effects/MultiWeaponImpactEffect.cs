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
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Effects
{
	class MultiWeaponImpactEffect : IEffect, ISync
	{
		readonly Actor invoker;
		readonly World world;
		readonly IMultiWeaponImpactInfo info;

		[Sync]
		WPos pos;
		int explosionInterval;
		List<CVec> impacts;

		public MultiWeaponImpactEffect(Actor invoker, IMultiWeaponImpactInfo info, WPos pos)
		{
			world = invoker.World;
			this.invoker = invoker;
			this.pos = pos;
			this.info = info;

			impacts = info.ImpactOffsets.ToList();

			if (info.RandomImpactSequence)
				impacts = impacts.Shuffle(world.SharedRandom).ToList();
		}

		public void Tick(World world)
		{
			if (impacts.Count == 0)
			{
				world.AddFrameEndTask(w => { w.Remove(this); w.ScreenMap.Remove(this); });
				return;
			}

			if (--explosionInterval < 0)
			{
				var nextImpactOffset = impacts.First();
				impacts.RemoveAt(0);

				var impactCell = world.Map.CellContaining(pos) + nextImpactOffset;
				var impactPos = world.Map.CenterOfCell(impactCell);
				impactPos += new WVec(0, 0, pos.Z);

				if (info.RandomOffset != WDist.Zero)
					impactPos += new WVec(world.SharedRandom.Next(-info.RandomOffset.Length, info.RandomOffset.Length), world.SharedRandom.Next(-info.RandomOffset.Length, info.RandomOffset.Length), 0);

				var nextInterval = info.Interval != null ? info.Interval.Length == 2 ? world.SharedRandom.Next(info.Interval[0], info.Interval[1]) : info.Interval[0] : info.Weapon.ReloadDelay;

				info.Weapon.Impact(Target.FromPos(impactPos), invoker);
				explosionInterval = nextInterval;
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			yield break;
		}
	}
}
