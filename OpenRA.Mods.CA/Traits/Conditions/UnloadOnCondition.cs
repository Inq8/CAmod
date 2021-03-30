#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

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
