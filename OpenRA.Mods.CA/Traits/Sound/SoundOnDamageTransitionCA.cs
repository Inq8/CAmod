#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Sound
{
	public class SoundOnDamageTransitionCAInfo : ConditionalTraitInfo
	{
		[Desc("Play a random sound from this list when damaged.")]
		public readonly string[] DamagedSounds = Array.Empty<string>();

		[Desc("Play a random sound from this list when destroyed.")]
		public readonly string[] DestroyedSounds = Array.Empty<string>();

		[Desc("DamageType(s) that trigger the sounds. Leave empty to always trigger a sound.")]
		public readonly BitSet<DamageType> DamageTypes = default;

		public override object Create(ActorInitializer init) { return new SoundOnDamageTransitionCA(init.Self, this); }
	}

	public class SoundOnDamageTransitionCA : ConditionalTrait<SoundOnDamageTransitionCAInfo>, INotifyDamageStateChanged
	{
		public SoundOnDamageTransitionCA(Actor self, SoundOnDamageTransitionCAInfo info)
			: base(info) { }

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled)
				return;

			if (!Info.DamageTypes.IsEmpty && !e.Damage.DamageTypes.Overlaps(Info.DamageTypes))
				return;

			var rand = Game.CosmeticRandom;

			if (e.DamageState == DamageState.Dead)
			{
				var sound = Info.DestroyedSounds.RandomOrDefault(rand);
				Game.Sound.Play(SoundType.World, sound, self.CenterPosition);
			}
			else if (e.DamageState >= DamageState.Heavy && e.PreviousDamageState < DamageState.Heavy)
			{
				var sound = Info.DamagedSounds.RandomOrDefault(rand);
				Game.Sound.Play(SoundType.World, sound, self.CenterPosition);
			}
		}
	}
}
