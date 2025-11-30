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
using OpenRA.Primitives;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Effects
{
	public class LinkedProducerIndicator : IEffect, IEffectAboveShroud, IEffectAnnotation
	{
		readonly Actor building;
		readonly LinkedProducerTarget lpt;
		readonly LinkedProducerSource lps;

		readonly List<WPos> targetLineNodes = new() { };
		List<WPos> cachedNodes;

		public LinkedProducerIndicator(Actor building, LinkedProducerTarget lpt)
		{
			this.building = building;
			this.lpt = lpt;
			this.lps = null;
			UpdateTargetLineNodes(building.World);
		}

		public LinkedProducerIndicator(Actor building, LinkedProducerSource lps)
		{
			this.building = building;
			this.lpt = null;
			this.lps = lps;
			UpdateTargetLineNodes(building.World);
		}

		void IEffect.Tick(World world)
		{
			if (lpt != null)
			{
				if (cachedNodes == null || !cachedNodes.SequenceEqual(lpt.LinkNodes))
				{
					UpdateTargetLineNodes(world);
				}
			}
			else if (lps != null)
			{
				// For sources, we need to update if the target changes
				UpdateTargetLineNodes(world);
			}

			if (!building.IsInWorld || building.IsDead)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		void UpdateTargetLineNodes(World world)
		{
			targetLineNodes.Clear();

			if (lpt != null)
			{
				// Target mode: show connections to all sources
				cachedNodes = new List<WPos>(lpt.LinkNodes);

				// Add the building position first
				targetLineNodes.Add(building.CenterPosition);

				// Add all source positions
				foreach (var n in cachedNodes)
					targetLineNodes.Add(n);
			}
			else if (lps != null)
			{
				// Source mode: show connection to target
				targetLineNodes.Add(building.CenterPosition);

				if (lps.HasTarget)
				{
					targetLineNodes.Add(lps.Target.Actor.CenterPosition);
				}
			}
		}

		IEnumerable<IRenderable> IEffect.Render(WorldRenderer wr) { return SpriteRenderable.None; }

		IEnumerable<IRenderable> IEffectAboveShroud.RenderAboveShroud(WorldRenderer wr)
		{
			if (!building.IsInWorld || !building.Owner.IsAlliedWith(building.World.LocalPlayer))
				return SpriteRenderable.None;

			if (!building.World.Selection.Contains(building))
				return SpriteRenderable.None;

			return SpriteRenderable.None;
		}

		IEnumerable<IRenderable> IEffectAnnotation.RenderAnnotation(WorldRenderer wr)
		{
			if (Game.Settings.Game.TargetLines == TargetLinesType.Disabled)
				return SpriteRenderable.None;

			if (!building.IsInWorld || !building.Owner.IsAlliedWith(building.World.LocalPlayer))
				return SpriteRenderable.None;

			if (!building.World.Selection.Contains(building))
				return SpriteRenderable.None;

			if (targetLineNodes.Count == 0)
				return SpriteRenderable.None;

			return RenderInner();
		}

		IEnumerable<IRenderable> RenderInner()
		{
			if (targetLineNodes.Count < 2)
				yield break;

			var targetPos = targetLineNodes[0]; // Building position

			// Draw lines from target to each source
			foreach (var sourcePos in targetLineNodes.Skip(1))
			{
				var targetLine = new[] { targetPos, sourcePos };
				yield return new TargetLineRenderable(targetLine, Color.DarkGreen, 4, 7);
				yield return new TargetLineRenderable(targetLine, Color.Lime, 2, 5);
			}
		}
	}
}
