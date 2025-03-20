#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using Eluant;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("ActorCA")]
	public class ActorCAGlobal : ScriptGlobal
	{
		public ActorCAGlobal(ScriptContext context)
			: base(context) { }

		public int CostOrDefault(string type, int defaultCost = 0)
		{
			if (!Context.World.Map.Rules.Actors.TryGetValue(type, out var ai))
				throw new LuaException($"Unknown actor type '{type}'");

			var vi = ai.TraitInfoOrDefault<ValuedInfo>();
			if (vi == null)
				return defaultCost;

			return vi.Cost;
		}
	}
}
