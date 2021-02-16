#region Copyright & License Information
/*
 * Copyright 2016-2021 The CA Developers (see AUTHORS)
 * This file is part of CA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Traits.UnitConverter
{
	[Desc("Allow convertible units to enter and spawn a new actor or actors.")]
	public class UnitConverterInfo : ConditionalTraitInfo
	{
		[Desc("Voice to use when actor enters converter.")]
		[VoiceReference]
		public readonly string Voice = "Action";

		[FieldLoader.Require]
		[Desc("Production queue type to use")]
		public readonly string Type = null;

		[Desc("Notification played when production is activated.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string ReadyAudio = null;

		[Desc("Notification played when the exit is jammed.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string BlockedAudio = null;

		public override object Create(ActorInitializer init) { return new UnitConverter(init, this); }
	}

	public class UnitConverter : ConditionalTrait<UnitConverterInfo>
	{
		readonly UnitConverterInfo info;

		public UnitConverter(ActorInitializer init, UnitConverterInfo info)
			: base(info)
		{
			this.info = info;
		}

		public void Enter(Actor converting, Actor self)
		{
			converting.PlayVoice(Info.Voice);

			var sa = converting.Trait<Convertible>();
			var spawnActors = sa.Info.SpawnActors;

			var sp = self.TraitsImplementing<Production>()
			.FirstOrDefault(p => !p.IsTraitDisabled && !p.IsTraitPaused);

			var activated = false;

			if (sp != null && !IsTraitDisabled)
			{
				foreach (var name in spawnActors)
				{
					var inits = new TypeDictionary
						{
							new OwnerInit(self.Owner),
							new FactionInit(sp.Faction)
						};

					activated |= sp.Produce(self, self.World.Map.Rules.Actors[name.ToLowerInvariant()], info.Type, inits, 0);
				}
			}

			if (activated)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.ReadyAudio, self.Owner.Faction.InternalName);
			else
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.BlockedAudio, self.Owner.Faction.InternalName);
		}
	}
}
