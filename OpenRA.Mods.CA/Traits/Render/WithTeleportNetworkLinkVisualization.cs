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

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("Renders links between members of the teleport network when selected.")]
	sealed class WithTeleportNetworkLinkVisualizationInfo : ConditionalTraitInfo
	{
		[Desc("Color of the links.")]
		public readonly Color SelectedColor = Color.FromArgb(128, Color.Cyan);

		[Desc("Player relationships which will be able to see the links.",
			"Valid values are combinations of `None`, `Ally`, `Enemy` and `Neutral`.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		public override object Create(ActorInitializer init) { return new WithTeleportNetworkLinkVisualization(init.Self, this); }
	}

	sealed class WithTeleportNetworkLinkVisualization : ConditionalTrait<WithTeleportNetworkLinkVisualizationInfo>, IRenderAnnotationsWhenSelected
	{
		readonly Actor self;

		public WithTeleportNetworkLinkVisualization(Actor self, WithTeleportNetworkLinkVisualizationInfo info)
			: base(info)
		{
			this.self = self;
		}

		IEnumerable<IRenderable> IRenderAnnotationsWhenSelected.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (IsTraitDisabled)
				yield break;

			var renderPlayer = self.World.RenderPlayer;
			if (renderPlayer != null && !Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(renderPlayer)))
				yield break;

			var network = self.TraitOrDefault<TeleportNetwork>();

			if (network == null)
				yield break;

			var selectedMembers = self.World.Selection.Actors
				.Where(a => a.Owner == self.Owner && a.TraitOrDefault<TeleportNetwork>()?.Info.Type == network.Info.Type)
				.ToArray();

			if (selectedMembers.Length == 0 || selectedMembers.Min(a => a.ActorID) != self.ActorID)
				yield break;

			var members = self.World.ActorsHavingTrait<TeleportNetwork>()
				.Where(a => !a.IsDead && a.IsInWorld && a.Owner == self.Owner && a.Trait<TeleportNetwork>().Info.Type == network.Info.Type)
				.OrderBy(a => a.ActorID)
				.ToArray();

			for (var i = 0; i < members.Length; i++)
				for (var j = i + 1; j < members.Length; j++)
					yield return new LineAnnotationRenderable(members[i].CenterPosition, members[j].CenterPosition, 1, Info.SelectedColor);
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable => false;
	}
}