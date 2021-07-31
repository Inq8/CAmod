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

using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.LoadScreens;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.LoadScreens
{
	public sealed class ImageLoadScreen : SheetLoadScreen
	{
		float2 logoPos;
		Sprite logo;

		Sheet lastSheet;
		int lastDensity;
		Size lastResolution;

		string[] messages = { "Loading..." };

		public override void Init(ModData modData, Dictionary<string, string> info)
		{
			base.Init(modData, info);

			if (info.ContainsKey("Text"))
				messages = info["Text"].Split(',');
		}

		public override void DisplayInner(Renderer r, Sheet s, int density)
		{
            var imageHeight = int.Parse(Info["Height"]);
            var imageWidth = int.Parse(Info["Width"]);
            var textColor = Color.Red;

            if (s != lastSheet || density != lastDensity)
			{
				lastSheet = s;
				lastDensity = density;
				logo = CreateSprite(s, density, new Rectangle(0, 0, imageWidth, imageHeight));
			}

            if (r.Resolution != lastResolution)
			{
				lastResolution = r.Resolution;
				logoPos = new float2(lastResolution.Width / 2 - (imageWidth / 2), lastResolution.Height / 2 - (imageHeight / 2));
			}

            if (logo != null)
				r.RgbaSpriteRenderer.DrawSprite(logo, logoPos);

            if (r.Fonts != null)
			{
				var text = messages.Random(Game.CosmeticRandom);
				var textSize = r.Fonts["Bold"].Measure(text);
				r.Fonts["Bold"].DrawText(text, new float2(r.Resolution.Width - textSize.X - 20, r.Resolution.Height - textSize.Y - 20), textColor);
			}
		}
	}
}
