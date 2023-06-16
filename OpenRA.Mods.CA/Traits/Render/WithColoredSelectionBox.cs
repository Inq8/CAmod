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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("Renders a player coloured selection box.")]
	class WithColoredSelectionBoxInfo : ConditionalTraitInfo
	{
		[Desc("Use the player color of the current owner.")]
		public readonly bool UsePlayerColor = false;

		[Desc("Use red for hostile, green for friendly, grey for neutral (will override player color).")]
		public readonly bool UseRelationshipColor = false;

		[Desc("Color of the box when not using player colour or relationship color.")]
		public readonly Color Color = Color.White;

		[Desc("Player relationships who can view the decoration.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		public override object Create(ActorInitializer init) { return new WithColoredSelectionBox(init.Self, this); }
	}

	class WithColoredSelectionBox : ConditionalTrait<WithColoredSelectionBoxInfo>, IRenderAnnotations, INotifyCreated
	{
		public new readonly WithColoredSelectionBoxInfo Info;
		Selectable selectable;

		public WithColoredSelectionBox(Actor self, WithColoredSelectionBoxInfo info)
			: base(info)
		{
			Info = info;
		}

		protected override void Created(Actor self)
		{
			selectable = self.TraitOrDefault<Selectable>();
		}

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (IsTraitDisabled || selectable == null)
				yield break;

			var color = Info.UsePlayerColor ? self.Owner.Color : Info.Color;

			if (self.World.RenderPlayer != null)
			{
				var relationship = self.Owner.RelationshipWith(self.World.RenderPlayer);
				if (!Info.ValidRelationships.HasRelationship(relationship))
					yield break;

				if (Info.UseRelationshipColor)
				{
					switch (relationship)
					{
						case PlayerRelationship.Ally:
							color = Color.Lime;
							break;

						case PlayerRelationship.Enemy:
							color = Color.Red;
							break;

						default:
							color = Color.Gray;
							break;
					}
				}
			}

			var bounds = selectable.DecorationBounds(self, wr);
			var boxBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
			yield return new SelectionBoxAnnotationRenderable(self, boxBounds, color);
		}

		bool IRenderAnnotations.SpatiallyPartitionable { get { return false; } }
	}
}
