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
	public class CloneSourceIndicator : IEffect, IEffectAboveShroud, IEffectAnnotation
	{
		readonly Actor building;
		readonly CloneProducer cp;

		readonly List<WPos> targetLineNodes = new() { };
		List<WPos> cachedNodes;

		public CloneSourceIndicator(Actor building, CloneProducer cp)
		{
			this.building = building;
			this.cp = cp;
			UpdateTargetLineNodes(building.World);
		}

		void IEffect.Tick(World world)
		{
			if (cachedNodes == null || !cachedNodes.SequenceEqual(cp.LinkNodes))
			{
				UpdateTargetLineNodes(world);
			}

			if (!building.IsInWorld || building.IsDead)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		void UpdateTargetLineNodes(World world)
		{
			cachedNodes = new List<WPos>(cp.LinkNodes);
			targetLineNodes.Clear();
			foreach (var n in cachedNodes)
				targetLineNodes.Add(n);

			if (targetLineNodes.Count == 0)
				return;

			targetLineNodes.Insert(0, building.CenterPosition);
		}

		IEnumerable<IRenderable> IEffect.Render(WorldRenderer wr) { return SpriteRenderable.None; }

		IEnumerable<IRenderable> IEffectAboveShroud.RenderAboveShroud(WorldRenderer wr)
		{
			if (!building.IsInWorld || !building.Owner.IsAlliedWith(building.World.LocalPlayer))
				return SpriteRenderable.None;

			if (!building.World.Selection.Contains(building))
				return SpriteRenderable.None;

			var renderables = SpriteRenderable.None;

			return renderables;
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
			var prev = targetLineNodes[0];
			foreach (var pos in targetLineNodes.Skip(1))
			{
				var targetLine = new[] { prev, pos };
				prev = pos;
				yield return new TargetLineRenderable(targetLine, Color.DarkGreen, 4, 7);
				yield return new TargetLineRenderable(targetLine, Color.Lime, 2, 5);
			}
		}
	}
}
