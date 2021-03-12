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

		[Desc("Notification played when production is activated and the player does not have enough cash to convert the unit.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string NoCashAudio = null;

		[Desc("Notification played when the exit is jammed.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string BlockedAudio = null;

		[Desc("Ticks between producing actors.")]
		public readonly int ProductionInterval = 100;

		[Desc("Whether the player has to pay any difference in cost between the unit being converted and the unit it converts into.")]
		public readonly bool CostDifferenceRequired = false;

		[Desc("Whether to eject a unit that can't be converted due to insufficient funds.")]
		public readonly bool EjectOnInsufficientFunds = false;

		[Desc("Whether to show a progress bar.")]
		public readonly bool ShowSelectionBar = true;

		[Desc("Color of the progress bar.")]
		public readonly Color SelectionBarColor = Color.Red;

		public override object Create(ActorInitializer init) { return new UnitConverter(init, this); }
	}

	public class UnitConverter : ConditionalTrait<UnitConverterInfo>, ITick, INotifyOwnerChanged, INotifyKilled, INotifySold, ISelectionBar
	{
		readonly UnitConverterInfo info;
		int produceIntervalTicks;
		Queue<UnitConverterQueueItem> queue;
		protected PlayerResources playerResources;

		public UnitConverter(ActorInitializer init, UnitConverterInfo info)
			: base(info)
		{
			this.info = info;
			produceIntervalTicks = Info.ProductionInterval;
			queue = new Queue<UnitConverterQueueItem>();
		}

		protected override void Created(Actor self)
		{
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			base.Created(self);
		}

		public void Enter(Actor converting, Actor self)
		{
			var player = self.World.LocalPlayer ?? self.World.RenderPlayer;
			if (converting.Owner == player && player != null)
			{
				converting.PlayVoice(Info.Voice);
			}

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
					queueItem.InputActor = converting.Info;
					queueItem.OutputActor = self.World.Map.Rules.Actors[name.ToLowerInvariant()];
					queueItem.ProductionType = info.Type;
					queueItem.Inits = inits;
					queueItem.ConversionCost = GetConversionCost(converting.Info, queueItem.OutputActor);
					queue.Enqueue(queueItem);
				}
			}
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || !queue.Any())
				return;

			if (produceIntervalTicks > 0)
			{
				produceIntervalTicks--;
				return;
			}

			produceIntervalTicks = Info.ProductionInterval;
			var nextItem = queue.Peek();
			var outputActor = nextItem.OutputActor;
			var exitSound = info.ReadyAudio;

			if (playerResources.Cash < nextItem.ConversionCost)
			{
				if (!Info.EjectOnInsufficientFunds)
					return;

				outputActor = nextItem.InputActor;
				exitSound = info.NoCashAudio;
			}

			if (nextItem.Producer.Produce(nextItem.Actor, outputActor, nextItem.ProductionType, nextItem.Inits, 0))
			{
				playerResources.TakeCash(nextItem.ConversionCost);
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", exitSound, self.Owner.Faction.InternalName);
				queue.Dequeue();
			}
			else
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.BlockedAudio, self.Owner.Faction.InternalName);
			}
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			ClearQueue();
			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e) { ClearQueue(); }
		void INotifySold.Selling(Actor self) { ClearQueue(); }
		void INotifySold.Sold(Actor self) { }

		protected void ClearQueue()
		{
			queue.Clear();
		}

		public virtual int GetConversionCost(ActorInfo inputUnit, ActorInfo outputUnit)
		{
			if (!Info.CostDifferenceRequired)
				return 0;

			var inputUnitCost = GetUnitCost(inputUnit);
			var outputUnitCost = GetUnitCost(outputUnit);
			var conversionCost = outputUnitCost - inputUnitCost;

			if (conversionCost < 0)
				return 0;

			return conversionCost;
		}

		public virtual int GetUnitCost(ActorInfo unit)
		{
			var valued = unit.TraitInfoOrDefault<ValuedInfo>();

			if (valued == null)
				return 0;

			return valued.Cost;
		}

		float ISelectionBar.GetValue()
		{
			if (!Info.ShowSelectionBar || !queue.Any())
				return 0;
			var maxTicks = Info.ProductionInterval;
			if (produceIntervalTicks == maxTicks)
				return 0;

			return (float)(maxTicks - produceIntervalTicks) / maxTicks;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

		Color ISelectionBar.GetColor() { return Info.SelectionBarColor; }
	}

	public class UnitConverterQueueItem
	{
		public Production Producer;
		public Actor Actor;
		public ActorInfo InputActor;
		public ActorInfo OutputActor;
		public string ProductionType;
		public TypeDictionary Inits;
		public int ConversionCost;
	}
}
