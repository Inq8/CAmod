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

using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Modifies the movement speed of this actor.")]
	public class DynamicSpeedMultiplierInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new DynamicSpeedMultiplier(this); }
	}

	public class DynamicSpeedMultiplier : ConditionalTrait<DynamicSpeedMultiplierInfo>, ISpeedModifier
	{
		public int Modifier { get; private set; }

		public DynamicSpeedMultiplier(DynamicSpeedMultiplierInfo info)
			: base(info)
		{
			Modifier = 100;
		}

		public void SetModifier(int modifier)
		{
			Modifier = modifier;
		}

		int ISpeedModifier.GetSpeedModifier() { return IsTraitDisabled ? 100 : Modifier; }
	}
}
