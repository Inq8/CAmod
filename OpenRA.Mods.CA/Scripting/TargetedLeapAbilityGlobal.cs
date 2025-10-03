#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.CA.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Scripting
{
	[ScriptPropertyGroup("TargetableLeapAbility")]
	public class TargetableLeapAbilityProperties : ScriptActorProperties, Requires<TargetedLeapAbilityInfo>
	{
		readonly TargetedLeapAbility LeapAbility;

		public TargetableLeapAbilityProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			LeapAbility = self.Trait<TargetedLeapAbility>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Queue targeted leap.")]
		public void TargetedLeap(CPos targetCell, bool queued = false)
		{
			var target = Target.FromCell(Self.World, targetCell);
			LeapAbility.ResolveOrder(Self, new Order("TargetableLeapOrderLeap", Self, target, queued));
		}
	}
}
