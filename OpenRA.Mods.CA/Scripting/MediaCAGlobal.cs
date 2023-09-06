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
	[Desc("PlaySound includes a volume modifier.")]
	[ScriptGlobal("MediaCA")]
	public class MediaCAGlobal : ScriptGlobal
	{
		readonly World world;

		public MediaCAGlobal(ScriptContext context)
			: base(context)
		{
			world = context.World;
		}

		// todo: use number instead of string for volumeModifier
		[Desc("Play a sound file")]
		public void PlaySound(string file, string volumeModifier)
		{
			Game.Sound.Play(SoundType.World, file, float.Parse(volumeModifier));
		}
	}
}
