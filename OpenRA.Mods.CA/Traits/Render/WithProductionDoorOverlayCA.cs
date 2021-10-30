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
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("Play an animation when a unit exits or blocks the exit after production finished.")]
	class WithProductionDoorOverlayCAInfo : ConditionalTraitInfo, IRenderActorPreviewSpritesInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>, Requires<BuildingInfo>
	{
		[SequenceReference]
		public readonly string Sequence = "build-door";

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			var anim = new Animation(init.World, image);
			anim.PlayFetchIndex(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence), () => 0);

			var bi = init.Actor.TraitInfo<BuildingInfo>();
			var offset = bi.CenterOffset(init.World).Y + 512; // Additional 512 units move from center -> top of cell
			yield return new SpriteActorPreview(anim, () => WVec.Zero, () => offset, p, rs.Scale);
		}

		public override object Create(ActorInitializer init) { return new WithProductionDoorOverlayCA(init.Self, this); }
	}

	class WithProductionDoorOverlayCA : ConditionalTrait<WithProductionDoorOverlayCAInfo>, ITick, INotifyProduction, INotifyDamageStateChanged
	{
		readonly Animation door;
		int desiredFrame;
		CPos openExit;
		Actor exitingActor;

		public WithProductionDoorOverlayCA(Actor self, WithProductionDoorOverlayCAInfo info)
			: base(info)
		{
			var renderSprites = self.Trait<RenderSprites>();
			door = new Animation(self.World, renderSprites.GetImage(self));
			door.PlayFetchDirection(RenderSprites.NormalizeSequence(door, self.GetDamageState(), info.Sequence),
				() => desiredFrame - door.CurrentFrame);

			var buildingInfo = self.Info.TraitInfo<BuildingInfo>();

			var offset = buildingInfo.CenterOffset(self.World).Y + 512;
			renderSprites.Add(new AnimationWithOffset(door, null, () => IsTraitDisabled, offset));
		}

		void ITick.Tick(Actor self)
		{
			if (exitingActor == null)
				return;

			if (!exitingActor.IsInWorld || exitingActor.Location != openExit || !(exitingActor.CurrentActivity is Mobile.ReturnToCellActivity))
			{
				desiredFrame = 0;
				exitingActor = null;
			}
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			if (door.CurrentSequence != null)
				door.ReplaceAnim(RenderSprites.NormalizeSequence(door, e.DamageState, door.CurrentSequence.Name));
		}

		void INotifyProduction.UnitProduced(Actor self, Actor other, CPos exit)
		{
			// CA change here - prevents incorporeal actors from triggering the exit (e.g. upgrades)
			if (!other.Info.HasTraitInfo<IOccupySpaceInfo>())
				return;

			openExit = exit;
			exitingActor = other;
			desiredFrame = door.CurrentSequence.Length - 1;
		}
	}
}
