#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Required for GPS Radar related logic to function. Attach this to the player actor.")]
	class GpsRadarWatcherInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new GpsRadarWatcher(init.Self.Owner); }
	}

	interface IOnGpsRadarRefreshed { void OnGpsRadarRefresh(Actor self, Player player); }

	class GpsRadarWatcher : ISync, IPreventsShroudReset
	{
		[Sync]
		public bool GrantedAllies { get; private set; }

		[Sync]
		public bool Granted { get; private set; }

		readonly Player owner;

		readonly List<GpsRadarProvider> providers = new List<GpsRadarProvider>();
		readonly HashSet<TraitPair<IOnGpsRadarRefreshed>> notifyOnRefresh = new HashSet<TraitPair<IOnGpsRadarRefreshed>>();

		public GpsRadarWatcher(Player owner)
		{
			this.owner = owner;
		}

		public void DeactivateGps(GpsRadarProvider trait, Player owner)
		{
			if (!providers.Contains(trait))
				return;

			providers.Remove(trait);
			RefreshGps(owner);
		}

		public void ActivateGps(GpsRadarProvider trait, Player owner)
		{
			if (providers.Contains(trait))
				return;

			providers.Add(trait);
			RefreshGps(owner);
		}

		public void RefreshGps(Player launcher)
		{
			RefreshGranted();

			foreach (var i in launcher.World.ActorsWithTrait<GpsRadarWatcher>())
				i.Trait.RefreshGranted();
		}

		void RefreshGranted()
		{
			var wasGranted = Granted;
			var wasGrantedAllies = GrantedAllies;
			var allyWatchers = owner.World.ActorsWithTrait<GpsRadarWatcher>().Where(kv => kv.Actor.Owner.IsAlliedWith(owner));

			Granted = providers.Count > 0;
			GrantedAllies = allyWatchers.Any(w => w.Trait.Granted);

			if (wasGranted != Granted || wasGrantedAllies != GrantedAllies)
				foreach (var tp in notifyOnRefresh.ToList())
					tp.Trait.OnGpsRadarRefresh(tp.Actor, owner);
		}

		bool IPreventsShroudReset.PreventShroudReset(Actor self)
		{
			return Granted || GrantedAllies;
		}

		public void RegisterForOnGpsRefreshed(Actor actor, IOnGpsRadarRefreshed toBeNotified)
		{
			notifyOnRefresh.Add(new TraitPair<IOnGpsRadarRefreshed>(actor, toBeNotified));
		}

		public void UnregisterForOnGpsRefreshed(Actor actor, IOnGpsRadarRefreshed toBeNotified)
		{
			notifyOnRefresh.Remove(new TraitPair<IOnGpsRadarRefreshed>(actor, toBeNotified));
		}
	}
}
