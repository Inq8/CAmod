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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Grants a condition to this actor if it is one of the specified types.")]
	public class GrantConditionOnActorTypesInfo : TraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("Only grant condition to these actor types.")]
		public readonly HashSet<string> Types = default;

		public override object Create(ActorInitializer init) { return new GrantConditionOnActorTypes(this); }
	}

	public class GrantConditionOnActorTypes : INotifyCreated
	{
		readonly GrantConditionOnActorTypesInfo info;

		public GrantConditionOnActorTypes(GrantConditionOnActorTypesInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			var name = self.Info.Name.ToLowerInvariant();

			if (info.Types.Contains(name))
				self.GrantCondition(info.Condition);
		}
	}
}
