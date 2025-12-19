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
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Automatically adds proxy actors to selection when their parent ally building is selected.",
		"This enables standard traits like RallyPoint on proxy actors to work when ally buildings are selected.")]
	public class AllyProxyFromSelectionInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new AllyProxyFromSelection(init.World); }
	}

	public class AllyProxyFromSelection : INotifySelection
	{
		readonly World world;

		// Track building -> proxy mapping so we know which proxy belongs to which building
		readonly Dictionary<Actor, Actor> buildingToProxy = new();

		public AllyProxyFromSelection(World world)
		{
			this.world = world;
		}

		void INotifySelection.SelectionChanged()
		{
			var localPlayer = world.LocalPlayer;
			if (localPlayer == null)
				return;

			var selectedBuildings = new HashSet<Actor>();

			// Find all selected ally buildings with AllyProxyRallyPoint and get their proxies
			foreach (var actor in world.Selection.Actors.ToList())
			{
				if (actor.IsDead || !actor.IsInWorld)
					continue;

				// Must be an ally's building, not ours
				if (actor.Owner == localPlayer || !actor.Owner.IsAlliedWith(localPlayer))
					continue;

				var allyProxyRallyPoint = actor.TraitOrDefault<AllyProxyRallyPoint>();
				if (allyProxyRallyPoint == null || allyProxyRallyPoint.IsTraitDisabled)
					continue;

				var proxy = allyProxyRallyPoint.GetProxyActor(localPlayer);
				if (proxy != null && !proxy.IsDead && proxy.IsInWorld)
				{
					selectedBuildings.Add(actor);

					// Add proxy to selection if not already tracked
					if (!buildingToProxy.ContainsKey(actor))
					{
						buildingToProxy[actor] = proxy;
						if (!world.Selection.Contains(proxy))
							world.Selection.Add(proxy);
					}
					// If proxy was somehow removed from selection, re-add it
					else if (!world.Selection.Contains(proxy))
					{
						world.Selection.Add(proxy);
					}
				}
			}

			// Remove proxies for buildings that are no longer selected
			var buildingsToRemove = buildingToProxy.Keys.Where(b => !selectedBuildings.Contains(b)).ToList();
			foreach (var building in buildingsToRemove)
			{
				var proxy = buildingToProxy[building];
				if (world.Selection.Contains(proxy))
					world.Selection.Remove(proxy);
				buildingToProxy.Remove(building);
			}
		}
	}
}
