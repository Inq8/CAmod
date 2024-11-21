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
using OpenRA.Effects;
using OpenRA.Graphics;

namespace OpenRA.Mods.CA.Effects
{
	class Countdown : IEffect
	{
		readonly int ticks;
		int ticksRemaining;

		public int TicksRemaining => ticksRemaining;
		public int Ticks => ticks;

		public Countdown(int ticks)
		{
			this.ticks = ticksRemaining = ticks;
		}

		public void Tick(World world)
		{
			if (--ticksRemaining <= 0)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			yield break;
		}
	}
}
