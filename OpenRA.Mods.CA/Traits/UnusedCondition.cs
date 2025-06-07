#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Just to prevent YAML errors when a condition isn't used (sometimes cleaner than removing a large number of traits/properties).")]
	public class UnusedConditionInfo : TraitInfo
	{
		[GrantedConditionReference]
		[FieldLoader.Require]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new UnusedCondition(); }
	}

	public class UnusedCondition
	{

	}
}
