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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class DummyGpsPowerInfo : PausableConditionalTraitInfo
	{
		[Desc("Delay before launching.")]
		public readonly int Delay = 0;

		public readonly int AnimationDuration = 0;

		public readonly string DoorImage = "atek";

		[SequenceReference("DoorImage")]
		public readonly string DoorSequence = "active";

		[PaletteReference("DoorPaletteIsPlayerPalette")]
		[Desc("Palette to use for rendering the launch animation")]
		public readonly string DoorPalette = "player";

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool DoorPaletteIsPlayerPalette = true;

		public readonly string SatelliteImage = "sputnik";

		[SequenceReference("SatelliteImage")]
		public readonly string SatelliteSequence = "idle";

		[PaletteReference("SatellitePaletteIsPlayerPalette")]
		[Desc("Palette to use for rendering the satellite projectile")]
		public readonly string SatellitePalette = "player";

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool SatellitePaletteIsPlayerPalette = true;

		[FieldLoader.Require]
		[Desc("The condition to apply. Must be included in the target actor's ExternalConditions list.")]
		public readonly string Condition = null;

		public readonly string LaunchSound = null;

		[NotificationReference("Speech")]
		public readonly string LaunchSpeechNotification = null;

		public readonly string IncomingSound = null;

		[NotificationReference("Speech")]
		public readonly string IncomingSpeechNotification = null;

		public override object Create(ActorInitializer init) { return new DummyGpsPower(init.Self, this); }
	}

	public class DummyGpsPower : PausableConditionalTrait<DummyGpsPowerInfo>, ITick
	{
		Actor self;
		readonly DummyGpsPowerInfo info;
		int conditionToken = Actor.InvalidConditionToken;
		int ticksRemaining;

		public DummyGpsPower(Actor self, DummyGpsPowerInfo info)
			: base(info)
		{
			this.self = self;
			this.info = info;
			ticksRemaining = info.Delay;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (--ticksRemaining == 0)
				Activate(self);
		}

		void Activate(Actor self)
		{
			self.World.AddFrameEndTask(w =>
			{
				PlayLaunchSounds();

				w.Add(new SatelliteLaunchCA(self, info));

				if (conditionToken == Actor.InvalidConditionToken)
					conditionToken = self.GrantCondition(info.Condition);
			});
		}

		void PlayLaunchSounds()
		{
			var renderPlayer = self.World.RenderPlayer;
			var isAllied = self.Owner.IsAlliedWith(renderPlayer);
			Game.Sound.Play(SoundType.UI, isAllied ? info.LaunchSound : info.IncomingSound);

			// IsAlliedWith returns true if renderPlayer is null, so we are safe here.
			var toPlayer = isAllied ? renderPlayer ?? self.Owner : renderPlayer;
			var speech = isAllied ? info.LaunchSpeechNotification : info.IncomingSpeechNotification;
			Game.Sound.PlayNotification(self.World.Map.Rules, toPlayer, "Speech", speech, toPlayer.Faction.InternalName);
		}
	}
}
