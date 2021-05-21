#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Grants a condition to the actor when created if fog is enabled.")]
	public class GrantConditionOnFogEnabledInfo : TraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new GrantConditionOnFogEnabled(init.Self, this); }
	}

	public class GrantConditionOnFogEnabled : INotifyCreated
	{
		readonly GrantConditionOnFogEnabledInfo info;

		public GrantConditionOnFogEnabled(Actor self, GrantConditionOnFogEnabledInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			if (info.Condition == null)
				return;

			var gs = self.World.LobbyInfo.GlobalSettings;
			var fogEnabled = gs.OptionOrDefault("fog", true);

			if (!fogEnabled)
				return;

			if (!string.IsNullOrEmpty(info.Condition))
				self.GrantCondition(info.Condition);
		}
	}
}
