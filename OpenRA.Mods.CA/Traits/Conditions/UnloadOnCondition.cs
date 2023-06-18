#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class UnloadOnConditionInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new UnloadOnCondition(init, this); }

		[Desc("Bot only?")]
		public readonly bool BotOnly = true;
	}

	public class UnloadOnCondition : ConditionalTrait<UnloadOnConditionInfo>
	{
		readonly UnloadOnConditionInfo info;

		public UnloadOnCondition(ActorInitializer init, UnloadOnConditionInfo info)
			: base(info)
		{
			this.info = info;
		}

		protected override void TraitEnabled(Actor self)
		{
			int unloadRange = 5;

			if (self.Owner.IsBot && info.BotOnly)
			{
				self.CancelActivity();
				self.QueueActivity(new UnloadCargo(self, WDist.FromCells(unloadRange)));
			}

			if (!info.BotOnly)
			{
				self.CancelActivity();
				self.QueueActivity(new UnloadCargo(self, WDist.FromCells(unloadRange)));
			}
		}
	}
}
