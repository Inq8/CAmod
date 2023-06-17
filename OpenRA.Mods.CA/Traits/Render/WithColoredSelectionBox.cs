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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Traits.Render
{
	public enum ColorSource { Fixed, Player, Relationship, Team }

	[Desc("Renders a player colored selection box.")]
	class WithColoredSelectionBoxInfo : ConditionalTraitInfo
	{
		[Desc("What to base the color on.")]
		public readonly ColorSource ColorSource = ColorSource.Fixed;

		[Desc("Color of the box when not using player/relationship/team color.")]
		public readonly Color Color = Color.White;

		[Desc("If ColorSource is Relationship, use this color for allies.")]
		public readonly Color AllyColor = ChromeMetrics.Get<Color>("PlayerStanceColorAllies");

		[Desc("If ColorSource is Relationship, use this color for allies.")]
		public readonly Color EnemyColor = ChromeMetrics.Get<Color>("PlayerStanceColorEnemies");

		[Desc("If ColorSource is Relationship, use this color for allies.")]
		public readonly Color NeutralColor = ChromeMetrics.Get<Color>("PlayerStanceColorNeutrals");

		[Desc("List of colors to use for teams.")]
		public readonly Color[] TeamColors =
		{
			Color.FromArgb(0, 128, 255),
			Color.FromArgb(255, 0, 0),
			Color.FromArgb(255, 204, 0),
			Color.FromArgb(0, 200, 0),
		};

		[Desc("Player relationships who can view the decoration.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		public override object Create(ActorInitializer init) { return new WithColoredSelectionBox(init.Self, this); }
	}

	class WithColoredSelectionBox : ConditionalTrait<WithColoredSelectionBoxInfo>, IRenderAnnotations, INotifyCreated, INotifyOwnerChanged
	{
		public new readonly WithColoredSelectionBoxInfo Info;
		Selectable selectable;
		Color color;
		PlayerRelationship relationship;
		int team;

		public WithColoredSelectionBox(Actor self, WithColoredSelectionBoxInfo info)
			: base(info)
		{
			Info = info;
			Update(self);
		}

		protected override void Created(Actor self)
		{
			selectable = self.TraitOrDefault<Selectable>();
		}

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (IsTraitDisabled || selectable == null)
				yield break;

			if (self.World.RenderPlayer != null)
			{
				if (!Info.ValidRelationships.HasRelationship(relationship))
					yield break;
			}

			if (self.World.FogObscures(self))
				yield break;

			var bounds = selectable.DecorationBounds(self, wr);
			var boxBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
			yield return new SelectionBoxAnnotationRenderable(self, boxBounds, color);
		}

		bool IRenderAnnotations.SpatiallyPartitionable { get { return false; } }

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			Update(self);
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
						color = Info.AllyColor;
						break;

					case PlayerRelationship.Enemy:
						color = Info.EnemyColor;
						break;

					default:
						color = Info.NeutralColor;
						break;
				}
			}
			else if (Info.ColorSource == ColorSource.Team)
			{
				if (team > 0 && Info.TeamColors.Length >= team)
					color = Info.TeamColors[team - 1];
				else
					color = Info.NeutralColor;
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
