#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	public class ChronoResourceTeleport : Activity
	{
		readonly Actor refinery;
		readonly CPos destination;
		readonly ChronoResourceDeliveryInfo info;
		readonly CPos harvestedField;
		readonly WAngle dockAngle;

		public ChronoResourceTeleport(CPos destination, ChronoResourceDeliveryInfo info, CPos harvestedField, Actor refinery)
		{
			this.destination = destination;
			this.info = info;
			this.harvestedField = harvestedField;
			this.refinery = refinery;

			var refInfo = refinery.Info.TraitInfoOrDefault<DockHostInfo>();
			if (refInfo != null)
				dockAngle = refInfo.DockAngle;
		}

		public override bool Tick(Actor self)
		{
			var image = info.Image ?? self.Info.Name;

			var sourcepos = self.CenterPosition;

			if (info.WarpInSequence != null)
				self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(sourcepos, w, image, info.WarpInSequence, info.Palette)));

			if (info.WarpInSound != null && (info.AudibleThroughFog || !self.World.FogObscures(sourcepos)))
				Game.Sound.Play(SoundType.World, info.WarpInSound, self.CenterPosition, info.SoundVolume);

			self.Trait<IPositionable>().SetPosition(self, destination);
			self.Generation++;

			var facing = self.TraitOrDefault<IFacing>();
			if (facing != null)
				facing.Facing = dockAngle;

			var destinationpos = self.CenterPosition;

			if (info.WarpOutSequence != null)
				self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(destinationpos, w, image, info.WarpOutSequence, info.Palette)));

			if (info.WarpOutSound != null && (info.AudibleThroughFog || !self.World.FogObscures(sourcepos)))
				Game.Sound.Play(SoundType.World, info.WarpOutSound, self.CenterPosition, info.SoundVolume);

			self.QueueActivity(new MoveToDock(self, refinery));
			self.QueueActivity(new FindAndDeliverResources(self, harvestedField));

			return true;
		}
	}
}
