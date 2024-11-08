#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Provides a prerequisite that is used for upgrades.")]
	public class ProvidesUpgradeInfo : ConditionalTraitInfo
	{
		[Desc("Type.")]
		public readonly string Type = null;

		public override object Create(ActorInitializer init) { return new ProvidesUpgrade(init.Self, this); }
	}

	public class ProvidesUpgrade : ConditionalTrait<ProvidesUpgradeInfo>, INotifyCreated
	{
		public readonly new ProvidesUpgradeInfo Info;
		readonly string type;
		UpgradesManager upgradesManager;

		public ProvidesUpgrade(Actor self, ProvidesUpgradeInfo info)
			: base(info)
		{
			Info = info;
			upgradesManager = self.Owner.PlayerActor.Trait<UpgradesManager>();
			type = Info.Type;

			if (string.IsNullOrEmpty(type))
				type = self.Info.Name;
		}

		protected override void Created(Actor self)
		{
			if (IsTraitDisabled)
				return;

			upgradesManager.UpgradeProviderCreated(type);

			base.Created(self);
		}

		protected override void TraitEnabled(Actor self)
		{
			upgradesManager.UpgradeProviderCreated(type);
		}
	}
}
