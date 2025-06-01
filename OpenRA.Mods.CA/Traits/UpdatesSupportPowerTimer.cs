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
using System;

namespace OpenRA.Mods.CA.Traits
{
	public enum SupportPowerTimerUpdateType
	{
		Add,
		Subtract,
		Set
	}

	[Desc("When trait is enabled the named support power will have its timer updated.")]
	class UpdatesSupportPowerTimerInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("The support power to charge.")]
		public readonly string OrderName = null;

		[Desc("If set to true, the support power will be fully charged the first time it becomes available. " +
			"Otherwise it will be fully charged every time the trait is enabled.")]
		public readonly bool InitialOnly = false;

		public readonly int Ticks = 0;

		public readonly SupportPowerTimerUpdateType Type = SupportPowerTimerUpdateType.Set;

		public override object Create(ActorInitializer init) { return new UpdatesSupportPowerTimer(init, this); }
	}

	class UpdatesSupportPowerTimer : ConditionalTrait<UpdatesSupportPowerTimerInfo>
	{
		readonly SupportPowerManager supportPowerManager = null;
		readonly SupportPowerInstanceManager supportPowerInstanceManager;

		public UpdatesSupportPowerTimer(ActorInitializer init, UpdatesSupportPowerTimerInfo info)
			: base(info)
		{
			supportPowerManager = init.Self.Owner.PlayerActor.TraitOrDefault<SupportPowerManager>();
			supportPowerInstanceManager = init.Self.Owner.PlayerActor.TraitOrDefault<SupportPowerInstanceManager>();
		}

		protected override void TraitEnabled(Actor self)
		{
			self.World.AddFrameEndTask(w => {
				if (supportPowerManager.Powers.ContainsKey(Info.OrderName))
				{
					if (supportPowerManager.Powers[Info.OrderName] is SupportPowerInstanceCA instance)
					{
						if (Info.InitialOnly && supportPowerInstanceManager.InitiallyFullyChargedPowers.Contains(Info.OrderName))
							return;

						switch (Info.Type)
						{
							case SupportPowerTimerUpdateType.Add:
								instance.AddToRemainingTicks(Info.Ticks);
								break;
							case SupportPowerTimerUpdateType.Subtract:
								instance.SubtractFromRemainingTicks(Info.Ticks);
								break;
							case SupportPowerTimerUpdateType.Set:
								instance.SetRemainingTicks(Info.Ticks);
								break;
						}

						if (Info.InitialOnly)
							supportPowerInstanceManager.InitiallyFullyChargedPowers.Add(Info.OrderName);

					} else {
						throw new InvalidOperationException(
							$"UpdatesSupportPowerTimer trait requires SupportPowerInstanceCA for {Info.OrderName}, " +
							$"but found {supportPowerManager.Powers[Info.OrderName].GetType().Name}.");
					}
				}
			});
		}
	}
}
