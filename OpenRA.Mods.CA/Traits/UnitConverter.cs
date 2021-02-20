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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

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

		[Desc("Ticks between producing actors.")]
		public readonly int ProductionInterval = 25;

		public override object Create(ActorInitializer init) { return new UnitConverter(init, this); }
	}

	public class UnitConverter : ConditionalTrait<UnitConverterInfo>, ITick
	{
		readonly UnitConverterInfo info;
		int produceIntervalTicks;
		Queue<UnitConverterQueueItem> queue;

		public UnitConverter(ActorInitializer init, UnitConverterInfo info)
			: base(info)
		{
			this.info = info;
			produceIntervalTicks = Info.ProductionInterval;
			queue = new Queue<UnitConverterQueueItem>();
		}

		public void Enter(Actor converting, Actor self)
		{
			converting.PlayVoice(Info.Voice);

			var sa = converting.Trait<Convertible>();
			var spawnActors = sa.Info.SpawnActors;

			var sp = self.TraitsImplementing<Production>()
			.FirstOrDefault(p => !p.IsTraitDisabled && !p.IsTraitPaused);

			if (sp != null && !IsTraitDisabled)
			{
				foreach (var name in spawnActors)
				{
					var inits = new TypeDictionary
						{
							new OwnerInit(self.Owner),
							new FactionInit(sp.Faction)
						};

					var queueItem = new UnitConverterQueueItem();
					queueItem.Producer = sp;
					queueItem.Actor = self;
					queueItem.Producee = self.World.Map.Rules.Actors[name.ToLowerInvariant()];
					queueItem.ProductionType = info.Type;
					queueItem.Inits = inits;
					queue.Enqueue(queueItem);
				}
			}
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (produceIntervalTicks > 0)
			{
				produceIntervalTicks--;
				return;
			}

			produceIntervalTicks = Info.ProductionInterval;

			if (!queue.Any())
				return;

			var nextItem = queue.Peek();

			if (nextItem.Producer.Produce(nextItem.Actor, nextItem.Producee, nextItem.ProductionType, nextItem.Inits, 0))
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.ReadyAudio, self.Owner.Faction.InternalName);
				queue.Dequeue();
			}
			else
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.BlockedAudio, self.Owner.Faction.InternalName);
			}
		}
	}

	public class UnitConverterQueueItem
	{
		public Production Producer;
		public Actor Actor;
		public ActorInfo Producee;
		public string ProductionType;
		public TypeDictionary Inits;
	}
}
