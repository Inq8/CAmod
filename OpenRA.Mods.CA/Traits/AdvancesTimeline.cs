#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Traits;
using System.Linq;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("On creation, adds specified number of ticks to a specified `ProvidesPrerequisitesOnTimeline` trait.")]
	public class AdvancesTimelineInfo : TraitInfo
	{
		[FieldLoader.Require]
		[Desc("Timeline type to advance.")]
		public readonly string Type = null;

		[Desc("Number of ticks to advance.")]
		public readonly int Ticks = 1500;

		public override object Create(ActorInitializer init) { return new AdvancesTimeline(init, this); }
	}

	public class AdvancesTimeline : INotifyCreated
	{
		public AdvancesTimelineInfo Info { get; set; }

		public AdvancesTimeline(ActorInitializer init, AdvancesTimelineInfo info)
		{
			Info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			var timeline = self.Owner.PlayerActor.TraitsImplementing<ProvidesPrerequisitesOnTimeline>()
				.FirstOrDefault(c => c.Info.Type == Info.Type);

			if (timeline != null)
				timeline.AddTicks(Info.Ticks);
		}
	}
}
