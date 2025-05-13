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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("This actor can mind control other actors.")]
	public class MindControllerCapacityModifierInfo : ConditionalTraitInfo, Requires<MindControllerInfo>
	{
		[Desc("Number to increase mind control capacity by (negative to reduce).")]
		public readonly int Amount = 1;

		public override object Create(ActorInitializer init) { return new MindControllerCapacityModifier(init.Self, this); }
	}

	public class MindControllerCapacityModifier : ConditionalTrait<MindControllerCapacityModifierInfo>
	{
		readonly MindControllerCapacityModifierInfo info;
		readonly IEnumerable<MindController> mindControllers;

		public MindControllerCapacityModifier(Actor self, MindControllerCapacityModifierInfo info)
			: base(info)
		{
			this.info = info;
			mindControllers = self.TraitsImplementing<MindController>();
		}

		public int Amount { get { return IsTraitDisabled ? 0 : info.Amount; } }

		protected override void TraitEnabled(Actor self)
		{
			foreach (var mindController in mindControllers)
				mindController.ModifierUpdated();
		}

		protected override void TraitDisabled(Actor self)
		{
			foreach (var mindController in mindControllers)
				mindController.ModifierUpdated();
		}
	}
}
