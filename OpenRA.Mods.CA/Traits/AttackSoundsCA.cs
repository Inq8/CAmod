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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Sound
{
	[Desc("Played when preparing for an attack or attacking.",
		"CA version allows for sounds not audible through fog and to be armament specific.")]
	public class AttackSoundsCAInfo : ConditionalTraitInfo
	{
		[Desc("Play a randomly selected sound from this list when preparing for an attack or attacking.")]
		public readonly string[] Sounds = { };

		[Desc("Delay in ticks before sound starts, either relative to attack preparation or attack.")]
		public readonly int Delay = 0;

		[Desc("Should the sound be delayed relative to preparation or actual attack?")]
		public readonly AttackDelayType DelayRelativeTo = AttackDelayType.Preparation;

		[Desc("Do the sounds play under shroud or fog.")]
		public readonly bool AudibleThroughFog = false;

		[Desc("Armament names")]
		public readonly string[] Armaments = { "primary", "secondary" };

		public override object Create(ActorInitializer init) { return new AttackSoundsCA(init, this); }
	}

	public class AttackSoundsCA : ConditionalTrait<AttackSoundsCAInfo>, INotifyAttack, ITick
	{
		readonly AttackSoundsCAInfo info;
		int tick;

		public AttackSoundsCA(ActorInitializer init, AttackSoundsCAInfo info)
			: base(info)
		{
			this.info = info;
		}

		void PlaySound(Actor self)
		{
			if (!info.Sounds.Any())
				return;

			var shouldStart = Info.AudibleThroughFog || (!self.World.ShroudObscures(self.CenterPosition) && !self.World.FogObscures(self.CenterPosition));

			if (!shouldStart)
				return;

			Game.Sound.Play(SoundType.World, info.Sounds, self.World, self.CenterPosition);
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (info.DelayRelativeTo == AttackDelayType.Attack && Info.Armaments.Contains(a.Info.Name))
			{
				if (info.Delay > 0)
					tick = info.Delay;
				else
					PlaySound(self);
			}
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (info.DelayRelativeTo == AttackDelayType.Preparation && Info.Armaments.Contains(a.Info.Name))
			{
				if (info.Delay > 0)
					tick = info.Delay;
				else
					PlaySound(self);
			}
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (info.Delay > 0 && --tick == 0)
				PlaySound(self);
		}
	}
}
