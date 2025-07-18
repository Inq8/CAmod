#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Graphics
{
	[Desc("Exact copy of base version to get around protection level in TelsaZapCA.")]
	class TeslaZapRenderableCA : IPalettedRenderable, IFinalizedRenderable
	{
		static readonly int[][] Steps = new[]
		{
			new int[] { 8, 8, 4, 4, 0 },
			new int[] { -8, -8, -4, -4, 0 },
			new int[] { 8, 0, 4, 4, 1 },
			new int[] { -8, 0, -4, 4, 1 },
			new int[] { 0, 8, 4, 4, 2 },
			new int[] { 0, -8, 4, -4, 2 },
			new int[] { -8, 8, -4, 4, 3 },
			new int[] { 8, -8, 4, -4, 3 }
		};

		readonly WPos pos;
		readonly int zOffset;
		readonly WVec length;
		readonly string image;
		readonly string palette;
		readonly string dimSequence;
		readonly string brightSequence;
		readonly int brightZaps, dimZaps;

		readonly WPos cachedPos;
		readonly WVec cachedLength;
		IEnumerable<IFinalizedRenderable> cache;

		public TeslaZapRenderableCA(WPos pos, int zOffset, in WVec length, string image,
			string brightSequence, int brightZaps,
			string dimSequence, int dimZaps,
			string palette)
		{
			this.pos = pos;
			this.zOffset = zOffset;
			this.length = length;
			this.image = image;
			this.palette = palette;
			this.brightZaps = brightZaps;
			this.dimZaps = dimZaps;
			this.dimSequence = dimSequence;
			this.brightSequence = brightSequence;

			cachedPos = WPos.Zero;
			cachedLength = WVec.Zero;
			cache = Array.Empty<IFinalizedRenderable>();
		}

		public WPos Pos => pos;
		public PaletteReference Palette => null;
		public int ZOffset => zOffset;
		public bool IsDecoration => true;

		public IPalettedRenderable WithPalette(PaletteReference newPalette)
		{
			return new TeslaZapRenderableCA(pos, zOffset, length, image, brightSequence, brightZaps, dimSequence, dimZaps, palette);
		}

		public IRenderable WithZOffset(int newOffset) =>
			new TeslaZapRenderableCA(Pos, ZOffset, length, image, brightSequence, brightZaps, dimSequence, dimZaps, palette);
		public IRenderable OffsetBy(in WVec vec) =>
			new TeslaZapRenderableCA(Pos + vec, ZOffset, length, image, brightSequence, brightZaps, dimSequence, dimZaps, palette);
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void RenderDebugGeometry(WorldRenderer wr) { }
		public void Render(WorldRenderer wr)
		{
			if (wr.World.FogObscures(pos) && wr.World.FogObscures(pos + length))
				return;

			if (!cache.Any() || length != cachedLength || pos != cachedPos)
				cache = GenerateRenderables(wr);

			foreach (var renderable in cache)
				renderable.Render(wr);
		}

		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }

		public IEnumerable<IFinalizedRenderable> GenerateRenderables(WorldRenderer wr)
		{
			var bright = wr.World.Map.Sequences.GetSequence(image, brightSequence);
			var dim = wr.World.Map.Sequences.GetSequence(image, dimSequence);

			var source = wr.ScreenPosition(pos);
			var target = wr.ScreenPosition(pos + length);

			for (var n = 0; n < dimZaps; n++)
				foreach (var z in DrawZapWandering(wr, source, target, dim, palette))
					yield return z;
			for (var n = 0; n < brightZaps; n++)
				foreach (var z in DrawZapWandering(wr, source, target, bright, palette))
					yield return z;
		}

		static IEnumerable<IFinalizedRenderable> DrawZapWandering(WorldRenderer wr, float2 from, float2 to, ISpriteSequence s, string pal)
		{
			var dist = to - from;
			var norm = (1f / dist.Length) * new float2(-dist.Y, dist.X);

			var renderables = new List<IFinalizedRenderable>();
			if (Game.CosmeticRandom.Next(2) != 0)
			{
				var p1 = from + (1 / 3f) * dist + WDist.FromPDF(Game.CosmeticRandom, 2).Length * dist.Length / 4096 * norm;
				var p2 = from + (2 / 3f) * dist + WDist.FromPDF(Game.CosmeticRandom, 2).Length * dist.Length / 4096 * norm;

				renderables.AddRange(DrawZap(wr, from, p1, s, out p1, pal));
				renderables.AddRange(DrawZap(wr, p1, p2, s, out p2, pal));
				renderables.AddRange(DrawZap(wr, p2, to, s, out _, pal));
			}
			else
			{
				var p1 = from + (1 / 2f) * dist + WDist.FromPDF(Game.CosmeticRandom, 2).Length * dist.Length / 4096 * norm;

				renderables.AddRange(DrawZap(wr, from, p1, s, out p1, pal));
				renderables.AddRange(DrawZap(wr, p1, to, s, out _, pal));
			}

			return renderables;
		}

		static IEnumerable<IFinalizedRenderable> DrawZap(WorldRenderer wr, float2 from, float2 to, ISpriteSequence s, out float2 p, string palette)
		{
			var dist = to - from;
			var q = new float2(-dist.Y, dist.X);
			var c = -float2.Dot(from, q);
			var rs = new List<IFinalizedRenderable>();
			var z = from;
			var pal = wr.Palette(palette);

			while ((to - z).X > 5 || (to - z).X < -5 || (to - z).Y > 5 || (to - z).Y < -5)
			{
				var step = Steps.Where(t => (to - (z + new float2(t[0], t[1]))).LengthSquared < (to - z).LengthSquared)
					.MinBy(t => Math.Abs(float2.Dot(z + new float2(t[0], t[1]), q) + c));

				var pos = wr.ProjectedPosition((z + new float2(step[2], step[3])).ToInt2());
				var tintModifiers = s.IgnoreWorldTint ? TintModifiers.IgnoreWorldTint : TintModifiers.None;
				rs.Add(new SpriteRenderable(s.GetSprite(step[4]), pos, WVec.Zero, 0, pal, 1f, s.GetAlpha(step[4]), float3.Ones, tintModifiers, true).PrepareRender(wr));

				z += new float2(step[0], step[1]);
				if (rs.Count >= 1000)
					break;
			}

			p = z;

			return rs;
		}
	}
}
