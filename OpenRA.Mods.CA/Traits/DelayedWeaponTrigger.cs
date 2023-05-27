#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Mods.CA.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class DelayedWeaponTrigger
	{
		readonly WarheadArgs args;

		public readonly BitSet<DamageType> DeathTypes;

		public readonly int TriggerTime;

		public int RemainingTime { get; private set; }

		public Actor AttachedBy { get; private set; }

		WeaponInfo weaponInfo;

		public bool IsValid { get; private set; }

		public DelayedWeaponTrigger(AttachDelayedWeaponWarhead warhead, WarheadArgs args)
		{
			this.args = args;
			TriggerTime = warhead.CalculatedTriggerTime;
			RemainingTime = TriggerTime;
			DeathTypes = warhead.DeathTypes;
			weaponInfo = warhead.WeaponInfo;
			AttachedBy = args.SourceActor;
			IsValid = true;
		}

		public void Tick(Actor attachable)
		{
			if (!attachable.IsDead && attachable.IsInWorld && IsValid)
			{
				if (--RemainingTime < 0)
				{
					Activate(attachable);
				}
			}
		}

		public void Activate(Actor attachable)
		{
			IsValid = false;
			var target = Target.FromPos(attachable.CenterPosition);
			attachable.World.AddFrameEndTask(w => weaponInfo.Impact(target, args));
		}

		public void Deactivate()
		{
			IsValid = false;
		}
	}
}
