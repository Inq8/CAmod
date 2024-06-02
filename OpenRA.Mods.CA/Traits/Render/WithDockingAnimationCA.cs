#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public class WithDockingAnimationCAInfo : TraitInfo, Requires<WithSpriteBodyInfo>, Requires<HarvesterInfo>
	{
		[SequenceReference]
		[Desc("Displayed when docking to refinery.")]
		public readonly string DockSequence = "dock";

		[SequenceReference]
		[Desc("Looped while unloading at refinery.")]
		public readonly string DockLoopSequence = "dock-loop";

		[Desc("Valid refinery types at which to play the animation.")]
		public readonly string[] RefineryTypes = { };

		public override object Create(ActorInitializer init) { return new WithDockingAnimationCA(init.Self, this); }
	}

	public class WithDockingAnimationCA : IDockClientBody
	{
		readonly WithDockingAnimationCAInfo info;
		readonly WithSpriteBody wsb;
		readonly Harvester harvester;

		public WithDockingAnimationCA(Actor self, WithDockingAnimationCAInfo info)
		{
			this.info = info;
			wsb = self.Trait<WithSpriteBody>();
			harvester = self.Trait<Harvester>();
		}

		void IDockClientBody.PlayDockAnimation(Actor self, Action after)
		{
			if (info.RefineryTypes.Contains(harvester.LinkedProc.Info.Name))
				wsb.PlayCustomAnimation(self, info.DockSequence, () => { wsb.PlayCustomAnimationRepeating(self, info.DockLoopSequence); after(); });
			else
				after();
		}

		void IDockClientBody.PlayReverseDockAnimation(Actor self, Action after)
		{
			if (info.RefineryTypes.Contains(harvester.LinkedProc.Info.Name))
				wsb.PlayCustomAnimationBackwards(self, info.DockSequence, () => after());
			else
				after();
		}
	}
}
