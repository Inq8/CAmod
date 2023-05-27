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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("Renders a player coloured selection box.")]
	class WithPlayerColorSelectionBoxInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new WithPlayerColorSelectionBox(init.Self, this); }
	}

	class WithPlayerColorSelectionBox : ConditionalTrait<WithPlayerColorSelectionBoxInfo>, IRenderAnnotations, INotifyCreated
	{
		public new readonly WithPlayerColorSelectionBoxInfo Info;
		Selectable selectable;

		public WithPlayerColorSelectionBox(Actor self, WithPlayerColorSelectionBoxInfo info)
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

			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				yield break;

			var bounds = selectable.DecorationBounds(self, wr);
			var boxBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
			yield return new SelectionBoxAnnotationRenderable(self, boxBounds, self.Owner.Color);
		}

		bool IRenderAnnotations.SpatiallyPartitionable { get { return false; } }
	}
}
