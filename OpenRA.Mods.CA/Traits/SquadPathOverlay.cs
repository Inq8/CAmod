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
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Commands;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Renders a debug overlay showing the pathfinding routes of AI squads. Attach this to the world actor.")]
	public class SquadPathOverlayInfo : TraitInfo
	{
		public Color[] RouteColors = new[] { Color.DeepPink, Color.Cyan, Color.Lime, Color.Yellow, Color.Orange, Color.Red, Color.White, Color.CornflowerBlue, Color.MediumPurple, Color.Tomato, Color.Sienna };

		public override object Create(ActorInitializer init) { return new SquadPathOverlay(this); }
	}

	public class SquadPathOverlay : IRenderAnnotations, IWorldLoaded, IChatCommand
	{
		readonly Color[] routeColors;
		int currentColorIndex = 0;

		const string CommandName = "squadpaths";

		[FluentReference]
		const string CommandDescription = "description-squadpaths-debug-overlay";

		public bool Enabled { get; private set; }

		public SquadPathOverlay(SquadPathOverlayInfo info)
		{
			routeColors = info.RouteColors;
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			var console = w.WorldActor.TraitOrDefault<ChatCommands>();
			var help = w.WorldActor.TraitOrDefault<HelpCommand>();

			if (console == null || help == null)
				return;

			console.RegisterCommand(CommandName, this);
			help.RegisterHelp(CommandName, CommandDescription);
		}

		void IChatCommand.InvokeCommand(string name, string arg)
		{
			if (name == CommandName)
				Enabled ^= true;
		}

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (!Enabled)
				yield break;

			// Find all bot players with squad managers
			var squadManagers = self.World.Players
				.Where(p => p.IsBot)
				.Select(p => p.PlayerActor.TraitsImplementing<SquadManagerBotModuleCA>().FirstOrDefault(t => !t.IsTraitDisabled))
				.Where(sm => sm != null);

			// Cycle through the colors

			currentColorIndex = 0;

			foreach (var squadManager in squadManagers)
			{
				var playerColor = squadManager.Player.Color;

				//currentColorIndex = currentColorIndex % routeColors.Length;
				//var routeColor = routeColors[currentColorIndex];

				var routeColor = playerColor;
				var altRouteColor = Color.FromArgb(32,
					routeColor.R,
					routeColor.G,
					routeColor.B);

				foreach (var squad in squadManager.Squads)
				{
					// Get route information from the state if it's GroundUnitsAttackMoveStateCA
					var routeInfo = (squad.FuzzyStateMachine.CurrentState as GroundUnitsAttackMoveStateCA)?.GetRouteInfo();
					if (routeInfo?.CurrentRoute == null)
						continue;

					// Render the current route in pink
					if (routeInfo.CurrentRoute.Count >= 2)
					{
						var prev = self.World.Map.CenterOfCell(routeInfo.CurrentRoute[0]);
						for (var i = 1; i < routeInfo.CurrentRoute.Count; i++)
						{
							var pos = self.World.Map.CenterOfCell(routeInfo.CurrentRoute[i]);
							var targetLine = new[] { prev, pos };
							prev = pos;
							yield return new TargetLineRenderable(targetLine, routeColor, 2, 2);
						}
					}

					foreach (var route in routeInfo.AlternativeRoutes)
					{
						if (route.Count < 2)
							continue;

						var prev = self.World.Map.CenterOfCell(route[0]);
						for (var i = 1; i < route.Count; i++)
						{
							var pos = self.World.Map.CenterOfCell(route[i]);
							var targetLine = new[] { prev, pos };
							prev = pos;
							yield return new TargetLineRenderable(targetLine, altRouteColor, 2, 2);
						}
					}


					// Render current waypoint marker
					if (routeInfo.CurrentWaypointIndex < routeInfo.CurrentRoute.Count)
					{
						var waypoint = routeInfo.CurrentRoute[routeInfo.CurrentWaypointIndex];
						yield return new MarkerTileRenderable(waypoint, routeColor);
					}

					currentColorIndex++;
				}
			}
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;
	}
}
