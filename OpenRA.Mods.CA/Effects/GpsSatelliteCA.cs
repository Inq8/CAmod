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

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.CA.Effects
{
	class GpsSatelliteCA : IEffect, ISpatiallyPartitionable
	{
		readonly Player launcher;
		readonly Animation anim;
		readonly string palette;
		readonly int animationDuration;
		WPos pos;
		int tick;

		public GpsSatelliteCA(World world, WPos pos, string image, string sequence, string palette, int animationDuration, Player launcher)
		{
			this.palette = palette;
			this.pos = pos;
			this.launcher = launcher;
			this.animationDuration = animationDuration;

			anim = new Animation(world, image);
			anim.PlayRepeating(sequence);
			world.ScreenMap.Add(this, pos, anim.Image);
		}

		public void Tick(World world)
		{
			anim.Tick();
			pos += new WVec(0, 0, 427);

			if (++tick > animationDuration)
			{
				world.AddFrameEndTask(w => { w.Remove(this); w.ScreenMap.Remove(this); });
			}

			world.ScreenMap.Update(this, pos, anim.Image);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			return anim.Render(pos, wr.Palette(palette));
		}
	}
}
