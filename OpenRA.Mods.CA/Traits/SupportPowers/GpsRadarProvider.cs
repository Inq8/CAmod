#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("This actor provides Radar GPS.")]
	public class GpsRadarProviderInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new GpsRadarProvider(this); }
	}

	public class GpsRadarProvider : ConditionalTrait<GpsRadarProviderInfo>, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		public GpsRadarProvider(GpsRadarProviderInfo info)
			: base(info) { }

		GpsRadarWatcher watcher;

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			watcher = self.Owner.PlayerActor.Trait<GpsRadarWatcher>();

			if (!IsTraitDisabled)
				TraitEnabled(self);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (!IsTraitDisabled)
				TraitDisabled(self);
		}

		protected override void TraitEnabled(Actor self)
		{
			watcher.ActivateGps(this, self.Owner);
		}

		protected override void TraitDisabled(Actor self)
		{
			watcher.DeactivateGps(this, self.Owner);
		}
	}
}
