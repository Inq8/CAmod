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

		[Desc("What to base the color on.")]
		public readonly ColorSource ColorSource = ColorSource.Fixed;

		[Desc("Color of the box when not using player/relationship/team color.")]
		public readonly Color Color = Color.White;

		[Desc("If ColorSource is Relationship, use this color for allies.")]
		public readonly Color? AllyColor = null;

		[Desc("If ColorSource is Relationship, use this color for enemies.")]
		public readonly Color? EnemyColor = null;

		[Desc("If ColorSource is Relationship, use this color for neutrals.")]
		public readonly Color? NeutralColor = null;

		[Desc("List of colors to use for teams.")]
		public readonly Color[] TeamColors =
		{
			Color.FromArgb(0, 128, 255),
			Color.FromArgb(255, 0, 0),
			Color.FromArgb(255, 204, 0),
			Color.FromArgb(0, 200, 0),
		};

		[Desc("Dark contrast color.")]
		public readonly Color? ContrastColorDark;

		[Desc("Light contrast color.")]
		public readonly Color? ContrastColorLight;

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
		Color allyColor;
		Color enemyColor;
		Color neutralColor;
		Color contrastColorDark;
		Color contrastColorLight;
		PlayerRelationship relationship;
		int team;

		public WithNameTagDecorationCA(Actor self, WithNameTagDecorationCAInfo info)
			: base(self, info)
		{
			font = Game.Renderer.Fonts[info.Font];
			allyColor = Info.AllyColor ?? ChromeMetrics.Get<Color>("PlayerStanceColorAllies");
			enemyColor = Info.EnemyColor ?? ChromeMetrics.Get<Color>("PlayerStanceColorEnemies");
			neutralColor = Info.NeutralColor ?? ChromeMetrics.Get<Color>("PlayerStanceColorNeutrals");
			contrastColorDark = Info.ContrastColorDark ?? ChromeMetrics.Get<Color>("TextContrastColorDark");
			contrastColorLight = Info.ContrastColorLight ?? ChromeMetrics.Get<Color>("TextContrastColorLight");
			Update(self);

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
				new UITextRenderable(font, self.CenterPosition, screenPos - size / 2, 0, color, contrastColorDark, contrastColorLight, name)
			};
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			Update(self);

			name = self.Owner.PlayerName;
			if (name.Length > Info.MaxLength)
				name = name.Substring(0, Info.MaxLength);
		}

		void Update(Actor self)
		{
			var c = self.World.LobbyInfo.Clients.FirstOrDefault(i => i.Index == self.Owner.ClientIndex);
			team = c?.Team ?? 0;

			if (self.World.RenderPlayer != null)
				relationship = self.Owner.RelationshipWith(self.World.RenderPlayer);
			else
				relationship = PlayerRelationship.None;

			if (Info.ColorSource == ColorSource.Relationship)
			{
				switch (relationship)
				{
					case PlayerRelationship.Ally:
						color = allyColor;
						break;

					case PlayerRelationship.Enemy:
						color = enemyColor;
						break;

					default:
						color = neutralColor;
						break;
				}
			}
			else if (Info.ColorSource == ColorSource.Team)
			{
				if (team > 0 && Info.TeamColors.Length >= team)
					color = Info.TeamColors[team - 1];
				else
					color = neutralColor;
			}
			else if (Info.ColorSource == ColorSource.Player)
			{
				color = self.Owner.Color;
			}
			else
			{
				color = Info.Color;
			}
		}
	}
}
