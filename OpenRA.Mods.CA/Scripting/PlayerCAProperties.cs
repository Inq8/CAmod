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
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("PlayerCA")]
	public class PlayerCAProperties : ScriptPlayerProperties
	{
		public PlayerCAProperties(ScriptContext context, Player player)
			: base(context, player) { }

		[Desc("Returns all living actors of the specified target types of this player.")]
		public Actor[] GetActorsByTargetTypes(string[] targetTypes)
		{
			var result = new List<Actor>();

			result.AddRange(Player.World.Actors
				.Where(actor => actor.Owner == Player && !actor.IsDead && actor.IsInWorld &&
					actor.GetEnabledTargetTypes().Any(t => targetTypes.Contains(t.ToString()))));

			return result.ToArray();
		}

		[Desc("Returns all living actors of the specified armor type of this player.")]
		public Actor[] GetActorsByArmorType(string armorType)
		{
			var result = new List<Actor>();

			result.AddRange(Player.World.Actors
				.Where(actor => actor.Owner == Player && !actor.IsDead && actor.IsInWorld &&
					actor.Info.TraitInfos<ArmorInfo>().Any(ai => ai.Type == armorType)));

			return result.ToArray();
		}
	}
}
