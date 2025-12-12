#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Grants a condition to the actor when created if fog is enabled.")]
	public class GrantConditionOnLobbyOptionInfo : TraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("Name of the lobby option.")]
		public readonly string Name = null;

		[Desc("Whether value is boolean (condition is enabled if option value is true).")]
		public readonly bool IsBoolean = false;

		[Desc("If not boolean, list of string values that enable the condition.")]
		public readonly string[] Values = new string[] {};

		public override object Create(ActorInitializer init) { return new GrantConditionOnLobbyOption(init.Self, this); }
	}

	public class GrantConditionOnLobbyOption : INotifyCreated
	{
		readonly GrantConditionOnLobbyOptionInfo info;

		public GrantConditionOnLobbyOption(Actor self, GrantConditionOnLobbyOptionInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			if (info.Condition == null)
				return;

			var gs = self.World.LobbyInfo.GlobalSettings;
			bool enabled;

			if (info.IsBoolean)
			{
				enabled = gs.OptionOrDefault(info.Name, false);
			}
			else
			{
				var value = gs.OptionOrDefault(info.Name, "");
				enabled = info.Values.Contains(value);
			}

			if (!enabled)
				return;

			if (!string.IsNullOrEmpty(info.Condition))
				self.GrantCondition(info.Condition);
		}
	}
}
