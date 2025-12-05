#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using Eluant;
using OpenRA.Mods.CA.Scripting;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("TriggerCA")]
	public class TriggerCAGlobal : ScriptGlobal
	{
		public TriggerCAGlobal(ScriptContext context)
			: base(context) { }

		public static ScriptTriggersCA GetScriptTriggersCA(Actor actor)
		{
			var triggers = actor.TraitOrDefault<ScriptTriggersCA>();
			if (triggers == null)
				throw new LuaException($"Actor '{actor.Info.Name}' requires the ScriptTriggersCA trait before attaching a CA trigger");

			return triggers;
		}

		[Desc("Call a function when a player disconnects. " +
			"The callback function will be called as func(player: player).")]
		public void OnPlayerDisconnected(Player player, [ScriptEmmyTypeOverride("fun(player: player)")] LuaFunction func)
		{
			GetScriptTriggersCA(player.PlayerActor).RegisterCallback(TriggerCA.OnPlayerDisconnected, func, Context);
		}
	}
}
