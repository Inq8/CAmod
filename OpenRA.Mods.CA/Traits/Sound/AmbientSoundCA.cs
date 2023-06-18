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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Sound
{
	[Desc("Plays a looping audio file at the actor position. Attach this to the `World` actor to cover the whole map.",
		"CA version can be made to be non-audible through fog and adds inital and final sounds that play when the sound starts/stops.")]
	class AmbientSoundCAInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		public readonly string[] SoundFiles = null;

		[Desc("Initial sound.")]
		public readonly string InitialSound = null;

		[Desc("Final sound.")]
		public readonly string FinalSound = null;

		[Desc("Initial delay (in ticks) before playing the sound for the first time.",
			"Two values indicate a random delay range.")]
		public readonly int[] Delay = { 0 };

		[Desc("Interval between playing the sound (in ticks).",
			"Two values indicate a random delay range.")]
		public readonly int[] Interval = { 0 };

		[Desc("Ticks before main loop starts. Set to null to base on sound completion.")]
		public readonly int InitialSoundLength = 0;

		[Desc("Do the sounds play under shroud or fog.")]
		public readonly bool AudibleThroughFog = false;

		[Desc("Multiply volume with this factor.")]
		public readonly float VolumeMultiplier = 1f;

		public override object Create(ActorInitializer init) { return new AmbientSoundCA(init.Self, this); }
	}

	class AmbientSoundCA : ConditionalTrait<AmbientSoundCAInfo>, ITick, INotifyRemovedFromWorld
	{
		bool initialSoundComplete = false;
		int initialSoundTicks = 0;
		readonly bool loop;
		readonly HashSet<ISound> currentSounds = new HashSet<ISound>();
		WPos cachedPosition;
		int delay;

		public AmbientSoundCA(Actor self, AmbientSoundCAInfo info)
			: base(info)
		{
			delay = Util.RandomInRange(self.World.SharedRandom, info.Delay);
			loop = Info.Interval.Length == 0 || (Info.Interval.Length == 1 && Info.Interval[0] == 0);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (self.World.IsGameOver)
				StopSounds(self, false);

			if (Info.InitialSound != null && !initialSoundComplete)
			{
				if (Info.InitialSoundLength > 0 && ++initialSoundTicks == Info.InitialSoundLength)
				{
					StopSounds(self, false);
					initialSoundComplete = true;
				}
				else
				{
					foreach (var s in currentSounds)
					{
						if (s != null && s.Complete)
							initialSoundComplete = true;
					}
				}
			}

			currentSounds.RemoveWhere(s => s == null || s.Complete);

			if (self.OccupiesSpace != null)
			{
				var pos = self.CenterPosition;
				if (pos != cachedPosition)
				{
					foreach (var s in currentSounds)
						s.SetPosition(pos);

					cachedPosition = pos;
				}
			}

			if (delay < 0)
				return;

			if (delay == 0 && Info.InitialSound != null && !initialSoundComplete)
			{
				if (currentSounds.Count == 0)
					PlaySound(Info.InitialSound, self, false, true);

				return;
			}

			if (--delay < 0)
			{
				StartSound(self);
				if (!loop)
					delay = Util.RandomInRange(self.World.SharedRandom, Info.Interval);
			}
		}

		void StartSound(Actor self)
		{
			var sound = Info.SoundFiles.RandomOrDefault(Game.CosmeticRandom);
			PlaySound(sound, self, loop, true);
		}

		void StopSounds(Actor self, bool playFinalSound)
		{
			foreach (var s in currentSounds)
				Game.Sound.StopSound(s);

			currentSounds.Clear();
			initialSoundComplete = false;
			initialSoundTicks = 0;

			if (Info.FinalSound != null && playFinalSound)
				PlaySound(Info.FinalSound, self, false, false);
		}

		void PlaySound(string sound, Actor self, bool looped, bool addToCurrentSounds)
		{
			var shouldStart = Info.AudibleThroughFog || (!self.World.ShroudObscures(self.CenterPosition) && !self.World.FogObscures(self.CenterPosition));

			if (!shouldStart)
				return;

			ISound s;
			if (self.OccupiesSpace != null)
			{
				cachedPosition = self.CenterPosition;
				s = looped ? Game.Sound.PlayLooped(SoundType.World, sound, cachedPosition) :
					Game.Sound.Play(SoundType.World, sound, self.CenterPosition, Info.VolumeMultiplier);
			}
			else
				s = looped ? Game.Sound.PlayLooped(SoundType.World, sound) :
					Game.Sound.Play(SoundType.World, sound, Info.VolumeMultiplier);

			if (addToCurrentSounds)
				currentSounds.Add(s);
		}

		protected override void TraitEnabled(Actor self) { delay = Util.RandomInRange(self.World.SharedRandom, Info.Delay); }
		protected override void TraitDisabled(Actor self) { StopSounds(self, true); }

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self) { StopSounds(self, false); }
	}
}
