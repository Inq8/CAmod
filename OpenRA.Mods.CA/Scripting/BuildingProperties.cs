#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;
using System.Linq;

namespace OpenRA.Mods.CA.Scripting
{
	[ScriptPropertyGroup("Building")]
	public class BuildingProperties : ScriptActorProperties, Requires<BuildingInfo>
	{
		readonly Building building;

		public BuildingProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			building = self.Trait<Building>();
		}

		[Desc("Returns the building's footprint cells.")]
		public CPos[] FootprintCells => building.Info.Tiles(Self.Location).ToArray();
	}
}
