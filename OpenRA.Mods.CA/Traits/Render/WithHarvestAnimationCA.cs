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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public class WithHarvestAnimationCAInfo : TraitInfo, Requires<WithSpriteBodyInfo>, Requires<HarvesterInfo>
	{
		[SequenceReference]
		[Desc("Displayed while harvesting.")]
		public readonly string HarvestSequence = "harvest";

		[Desc("Which sprite body to play the animation on.")]
		public readonly string Body = "body";

		public override object Create(ActorInitializer init) { return new WithHarvestAnimationCA(init, this); }
	}

	public class WithHarvestAnimationCA : INotifyHarvesterAction, INotifyMoving
	{
		public readonly WithHarvestAnimationCAInfo Info;
		readonly WithSpriteBody wsb;

		public WithHarvestAnimationCA(ActorInitializer init, WithHarvestAnimationCAInfo info)
		{
			Info = info;
			wsb = init.Self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == Info.Body);
		}

		void INotifyHarvesterAction.Harvested(Actor self, ResourceType resource)
		{
			var sequence = wsb.NormalizeSequence(self, Info.HarvestSequence);
			if (wsb.DefaultAnimation.HasSequence(sequence) && wsb.DefaultAnimation.CurrentSequence.Name != sequence)
				wsb.PlayCustomAnimation(self, sequence);
		}

		// ---- added for CA, prevents jerky turning due to fewer harvest facings than normal facings
		void INotifyMoving.MovementTypeChanged(Actor self, MovementType type)
		{
			if (type == MovementType.None)
				return;

			wsb.CancelCustomAnimation(self);
		}

		void INotifyHarvesterAction.Docked() { }
		void INotifyHarvesterAction.Undocked() { }
		void INotifyHarvesterAction.MovingToResources(Actor self, CPos targetCell) { }
		void INotifyHarvesterAction.MovingToRefinery(Actor self, Actor refineryActor) { }
		void INotifyHarvesterAction.MovementCancelled(Actor self) { }
	}
}
