

#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Scripting;

namespace OpenRA.Mods.CA.Scripting
{
	[ScriptGlobal("UtilsCA")]
	public class UtilsCAGlobal : ScriptGlobal
	{
		readonly World world;

		public UtilsCAGlobal(ScriptContext context)
			: base(context)
		{
			world = context.World;
		}

		[Desc("Returns game speed.")]
		public string GameSpeed()
		{
			var gameSpeeds = Game.ModData.Manifest.Get<GameSpeeds>();
			return world.LobbyInfo.GlobalSettings.OptionOrDefault("gamespeed", gameSpeeds.DefaultSpeed);
		}

		[Desc("Returns whether fog of war is enabled.")]
		public bool FogEnabled()
		{
			return world.LobbyInfo.GlobalSettings.OptionOrDefault("fog", true);
		}
	}
}
