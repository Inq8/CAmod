#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("CA version fixes jerky turning caused by fewer harvest facings than normal facings.")]
	public class WithHarvestAnimationCAInfo : TraitInfo, Requires<WithSpriteBodyInfo>, Requires<HarvesterInfo>
	{
		[SequenceReference]
		[Desc("Displayed while harvesting.")]
		public readonly string HarvestSequence = "harvest";

		[Desc("Which sprite body to play the animation on.")]
		public readonly string Body = "body";

		public override object Create(ActorInitializer init) { return new WithHarvestAnimationCA(init, this); }
	}

	public class WithHarvestAnimationCA : INotifyHarvestAction, INotifyMoving
	{
		public readonly WithHarvestAnimationCAInfo Info;
		readonly WithSpriteBody wsb;

		public WithHarvestAnimationCA(ActorInitializer init, WithHarvestAnimationCAInfo info)
		{
			Info = info;
			wsb = init.Self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == Info.Body);
		}

		void INotifyHarvestAction.Harvested(Actor self, string resourceType)
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

		void INotifyHarvestAction.MovingToResources(Actor self, CPos targetCell) { }
		void INotifyHarvestAction.MovementCancelled(Actor self) { }
	}
}
