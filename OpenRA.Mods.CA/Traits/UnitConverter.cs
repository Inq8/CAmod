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
using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Orders;
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

		[Desc("Ticks between producing actors. Use zero to calculate based on cost.")]
		public readonly int BuildTime = 0;

		[Desc("Percentage of conversion cost to use as duration in ticks to convert (if actor has none defined in Buildable).")]
		public readonly int DefaultBuildDurationModifier = 60;

		[Desc("Whether the player has to pay any difference in cost between the unit being converted and the unit it converts into.")]
		public readonly bool CostDifferenceRequired = false;

		[Desc("Whether to eject all units on deploy command.")]
		public readonly bool EjectOnDeploy = false;

		[Desc("Whether to show a progress bar.")]
		public readonly bool ShowSelectionBar = true;

		[Desc("Color of the progress bar.")]
		public readonly Color SelectionBarColor = Color.Red;

		[GrantedConditionReference]
		[Desc("Converting condition.")]
		public readonly string ConvertingCondition = null;

		public override object Create(ActorInitializer init) { return new UnitConverter(init, this); }
	}

	public class UnitConverter : ConditionalTrait<UnitConverterInfo>, ITick, INotifyOwnerChanged, INotifyKilled, INotifySold, ISelectionBar, IIssueOrder, IResolveOrder
	{
		const string OrderID = "EjectUnitConverter";
		readonly UnitConverterInfo info;
		Queue<UnitConverterQueueItem> queue;
		protected PlayerResources playerResources;
		int conditionToken = Actor.InvalidConditionToken;
		bool eject = false;

		public UnitConverter(ActorInitializer init, UnitConverterInfo info)
			: base(info)
		{
			this.info = info;
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
			if (Info.Voice != null && converting.Owner == player && player != null)
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
					queueItem.InputActor = converting.Info;
					queueItem.OutputActor = self.World.Map.Rules.Actors[name.ToLowerInvariant()];
					queueItem.ProductionType = info.Type;
					queueItem.Inits = inits;
					queueItem.ConversionCost = GetConversionCost(converting.Info, queueItem.OutputActor);
					queueItem.ConversionCostRemaining = queueItem.ConversionCost;

					var buildable = queueItem.OutputActor.TraitInfoOrDefault<BuildableInfo>();
					queueItem.BuildDurationModifier = buildable != null ? buildable.BuildDurationModifier : Info.DefaultBuildDurationModifier;

					queueItem.BuildDuration = Info.BuildTime > 0 ? Info.BuildTime : Util.ApplyPercentageModifiers(queueItem.ConversionCost, new int[] { queueItem.BuildDurationModifier });
					queueItem.BuildDurationRemaining = queueItem.BuildDuration;

					queue.Enqueue(queueItem);
					GrantCondition(self);
				}
			}
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || !queue.Any())
				return;

			var nextItem = queue.Peek();
			var outputActor = eject ? nextItem.InputActor : nextItem.OutputActor;
			var exitSound = info.ReadyAudio;

			if (!eject)
			{
				var expectedRemainingCost = nextItem.BuildDurationRemaining == 1 ? 0 : nextItem.ConversionCost * nextItem.BuildDurationRemaining / Math.Max(1, nextItem.BuildDuration);
				var costThisFrame = nextItem.ConversionCostRemaining - expectedRemainingCost;

				if (costThisFrame != 0 && !playerResources.TakeCash(costThisFrame, true))
					return;

				nextItem.ConversionCostRemaining -= costThisFrame;
				nextItem.BuildDurationRemaining -= 1;
				if (nextItem.BuildDurationRemaining > 0)
					return;
			}
			else
			{
				playerResources.GiveCash(nextItem.ConversionCost - nextItem.ConversionCostRemaining);
			}

			if (nextItem.Producer.Produce(nextItem.Actor, outputActor, nextItem.ProductionType, nextItem.Inits, 0))
			{
				if (!eject)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", exitSound, self.Owner.Faction.InternalName);

				queue.Dequeue();

				if (!queue.Any())
				{
					eject = false;
					RevokeCondition(self);
				}
			}
			else if (!eject)
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.BlockedAudio, self.Owner.Faction.InternalName);
			}
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			ClearQueue(self);
			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e) { ClearQueue(self); }
		void INotifySold.Selling(Actor self) { ClearQueue(self); }
		void INotifySold.Sold(Actor self) { }

		protected void ClearQueue(Actor self)
		{
			queue.Clear();
			eject = false;
			RevokeCondition(self);
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

		void GrantCondition(Actor self)
		{
			if (string.IsNullOrEmpty(Info.ConvertingCondition) || conditionToken != Actor.InvalidConditionToken)
				return;

			conditionToken = self.GrantCondition(Info.ConvertingCondition);
		}

		void RevokeCondition(Actor self)
		{
			if (conditionToken == Actor.InvalidConditionToken)
				return;

			conditionToken = self.RevokeCondition(conditionToken);
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (IsTraitDisabled || !Info.EjectOnDeploy || !queue.Any())
					yield break;

				yield return new DeployOrderTargeter(OrderID, 1);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == OrderID)
				return new Order(order.OrderID, self, false);

			return null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != OrderID)
				return;

			eject = true;
		}

		float ISelectionBar.GetValue()
		{
			if (!Info.ShowSelectionBar || !queue.Any())
				return 0;

			var nextItem = queue.Peek();
			var buildDurationElapsed = nextItem.BuildDuration - nextItem.BuildDurationRemaining;
			return (float)buildDurationElapsed / nextItem.BuildDuration;
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
		public int ConversionCostRemaining;
		public int BuildDurationModifier;
		public int BuildDuration;
		public int BuildDurationRemaining;
	}
}
