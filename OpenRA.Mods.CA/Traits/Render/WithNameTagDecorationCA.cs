#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("Displays the player name above the unit. Copy of WithNameTagDecoration, ",
		"but also allows contrast colors to be specified.")]
	public class WithNameTagDecorationCAInfo : WithDecorationBaseInfo
	{
		public readonly int MaxLength = 10;

		public readonly string Font = "TinyBold";

		[Desc("Display in this color when not using the player color.")]
		public readonly Color Color = Color.White;

		[Desc("Use the player color of the current owner.")]
		public readonly bool UsePlayerColor = false;

		[Desc("Display in this color when not using the player color.")]
		public readonly Color? ContrastColorDark = null;

		[Desc("Display in this color when not using the player color.")]
		public readonly Color? ContrastColorLight = null;

		public override object Create(ActorInitializer init) { return new WithNameTagDecorationCA(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (!Game.ModData.Manifest.Get<Fonts>().FontList.ContainsKey(Font))
				throw new YamlException($"Font '{Font}' is not listed in the mod.yaml's Fonts section");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class WithNameTagDecorationCA : WithDecorationBase<WithNameTagDecorationCAInfo>, INotifyOwnerChanged
	{
		readonly SpriteFont font;
		string name;
		Color color;
		Color bgDark;
		Color bgLight;

		public WithNameTagDecorationCA(Actor self, WithNameTagDecorationCAInfo info)
			: base(self, info)
		{
			font = Game.Renderer.Fonts[info.Font];
			color = info.UsePlayerColor ? self.Owner.Color : info.Color;
			bgDark = info.ContrastColorDark ?? ChromeMetrics.Get<Color>("TextContrastColorDark");
			bgLight = info.ContrastColorLight ?? ChromeMetrics.Get<Color>("TextContrastColorLight");

			name = self.Owner.PlayerName;
			if (name.Length > info.MaxLength)
				name = name.Substring(0, info.MaxLength);
		}

		protected override IEnumerable<IRenderable> RenderDecoration(Actor self, WorldRenderer wr, int2 screenPos)
		{
			if (IsTraitDisabled || self.IsDead || !self.IsInWorld || !ShouldRender(self))
				return Enumerable.Empty<IRenderable>();

			var size = font.Measure(name);
			return new IRenderable[]
			{
				new UITextRenderable(font, self.CenterPosition, screenPos - size / 2, 0, color, bgDark, bgLight, name)
			};
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (Info.UsePlayerColor)
				color = newOwner.Color;

			name = self.Owner.PlayerName;
			if (name.Length > Info.MaxLength)
				name = name.Substring(0, Info.MaxLength);
		}
	}
}
