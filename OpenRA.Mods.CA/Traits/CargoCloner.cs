#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Continuously produces the passenger actor at no cost. Assumes only one passenger.")]
	public class CargoClonerInfo : PausableConditionalTraitInfo, Requires<CargoInfo>
	{
		[FieldLoader.Require]
		[Desc("Production types (must share one or more values of the `Produces` property of the source's Production trait).")]
		public readonly string[] Types = Array.Empty<string>();

		[Desc("List of actors to ignore.")]
		public readonly string[] InvalidActors = Array.Empty<string>();

		[Desc("Actors to use instead of specific source actors.")]
		public readonly Dictionary<string, string> CloneActors = new();

		[Desc("Ticks between producing actors. Use zero to calculate based on cost.")]
		public readonly int BuildTime = 0;

		[Desc("Percentage of conversion cost to use as duration in ticks to convert (if actor has none defined in Buildable).")]
		public readonly int BuildDurationModifier = 60;

		[Desc("If true, BuildDurationModifier will override the equivalent value in Buildable.")]
		public readonly bool OverrideUnitBuildDurationModifier = false;

		[NotificationReference("Speech")]
		[Desc("Notification played when production is complete.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string ReadyAudio = null;

		[FluentReference(optional: true)]
		[Desc("Notification displayed when production is complete.")]
		public readonly string ReadyTextNotification = null;

		[NotificationReference("Speech")]
		[Desc("Notification played when you can't train another actor",
			"when the build limit exceeded or the exit is jammed.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string BlockedAudio = null;

		[FluentReference(optional: true)]
		[Desc("Notification displayed when you can't train another actor",
			"when the build limit exceeded or the exit is jammed.")]
		public readonly string BlockedTextNotification = null;

		public readonly bool ShowSelectionBar = false;
		public readonly Color SelectionBarColor = Color.SkyBlue;

		[Desc("Defines to which players the bar is to be shown.")]
		public readonly PlayerRelationship SelectionBarDisplayRelationships = PlayerRelationship.Ally;

		public override object Create(ActorInitializer init) { return new CargoCloner(init.Self, this); }
	}

	public class CargoCloner : PausableConditionalTrait<CargoClonerInfo>, ITick, INotifyPassengerEntered, INotifyPassengerExited, ISelectionBar, INotifyPowerLevelChanged, INotifyOwnerChanged
	{
		readonly Actor self;
		readonly CargoClonerInfo info;
		int totalTicksToClone;
		int ticksUntilCloned;
		public string[] Types => info.Types;

		PowerState previousPowerState;
		PowerManager playerPower;
		Actor actorToClone;
		BuildableInfo bi;
		Cargo cargo;
		bool exitOnCompletion = false;

		public CargoCloner(Actor self, CargoClonerInfo info)
			: base(info)
		{
			this.info = info;
			this.self = self;
			ticksUntilCloned = 0;
			actorToClone = null;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			playerPower = self.Owner.PlayerActor.Trait<PowerManager>();
			cargo = self.Trait<Cargo>();
			previousPowerState = playerPower.PowerState;
		}

		void INotifyPassengerEntered.OnPassengerEntered(Actor self, Actor passenger)
		{
			if (actorToClone == null)
			{
				bi = passenger.Info.TraitInfoOrDefault<BuildableInfo>();
				var existingCount = GetExistingCount(passenger);

				if (bi != null && bi.BuildLimit > 0)
				{
					if (existingCount >= bi.BuildLimit + 1 || passenger.Owner != self.Owner)
					{
						Unload();
						Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.BlockedAudio, self.Owner.Faction.InternalName);
						return;
					}
					else if (existingCount >= bi.BuildLimit)
					{
						exitOnCompletion = true;
					}
				}

				actorToClone = passenger;
				totalTicksToClone = ticksUntilCloned = CalculateBuildTime();
			}
		}

		void INotifyPassengerExited.OnPassengerExited(Actor self, Actor passenger)
		{
			if (actorToClone == passenger)
			{
				actorToClone = null;
				bi = null;
				totalTicksToClone = ticksUntilCloned = 0;
			}
		}

		void INotifyPowerLevelChanged.PowerLevelChanged(Actor self)
		{
			totalTicksToClone = CalculateBuildTime();

			if (playerPower.PowerState == PowerState.Normal && previousPowerState != PowerState.Normal)
			{
				ticksUntilCloned /= 2;
			}
			else if (playerPower.PowerState != PowerState.Normal && previousPowerState == PowerState.Normal)
			{
				ticksUntilCloned *= 2;
			}

			previousPowerState = playerPower.PowerState;
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			playerPower = newOwner.PlayerActor.Trait<PowerManager>();
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || self.IsDead || actorToClone == null)
			{
				ticksUntilCloned = 0;
				return;
			}

			if (IsTraitPaused)
				return;

			if (--ticksUntilCloned > 0)
				return;

			ProduceClone();

			if (exitOnCompletion)
			{
				Unload();
				exitOnCompletion = false;
			}
			else
				totalTicksToClone = ticksUntilCloned = CalculateBuildTime();
		}

		int GetExistingCount(Actor actorToClone)
		{
			return self.Owner.World.ActorsHavingTrait<Buildable>()
				.Count(a => a.Info.Name == actorToClone.Info.Name && a.Owner == self.Owner);
		}

		void Unload()
		{
			if (cargo != null && !cargo.IsEmpty())
				self.QueueActivity(new UnloadCargo(self, cargo.Info.LoadRange));
		}

		int CalculateBuildTime()
		{
			if (actorToClone == null)
				return 0;

			if (info.BuildTime > 0)
				return info.BuildTime;

			var cost = GetUnitCost(actorToClone.Info);
			if (cost == 0)
				return 0;

			// Check if we should use Buildable trait's BuildDurationModifier
			var buildDurationModifier = info.BuildDurationModifier;

			if (!info.OverrideUnitBuildDurationModifier && bi != null)
				buildDurationModifier = bi.BuildDurationModifier;

			var powerStateBuildDurationModifier = playerPower.PowerState != PowerState.Normal ? 200 : 100;
			return Util.ApplyPercentageModifiers(cost, new int[] { buildDurationModifier, powerStateBuildDurationModifier });
		}

		int GetUnitCost(ActorInfo unit)
		{
			var valued = unit.TraitInfoOrDefault<ValuedInfo>();

			if (valued == null)
				return 0;

			return valued.Cost;
		}

		void ProduceClone()
		{
			if (IsTraitDisabled || self.IsDead || actorToClone == null)
				return;

			var actorName = actorToClone.Info.Name.ToLowerInvariant();

			if (info.InvalidActors.Contains(actorName))
				return;

			var cloneActor = self.World.Map.Rules.Actors[info.CloneActors.ContainsKey(actorName) ? info.CloneActors[actorName] : actorName];

			var sp = self.TraitsImplementing<Production>()
				.FirstOrDefault(p => !p.IsTraitDisabled && !p.IsTraitPaused && p.Info.Produces.Any(produces => info.Types.Contains(produces)));

			var cloned = false;

			if (bi != null && bi.BuildLimit > 0 && GetExistingCount(actorToClone) >= bi.BuildLimit + 1)
			{
				actorToClone = null;
				bi = null;
				totalTicksToClone = ticksUntilCloned = 0;
			}

			if (actorToClone != null && sp != null)
			{
				var inits = new TypeDictionary
				{
					new OwnerInit(self.Owner),
					new FactionInit(sp.Faction)
				};

				cloned |= sp.Produce(self, cloneActor, sp.Info.Produces[0], inits, 0);
			}

			if (exitOnCompletion || !cloned)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.BlockedAudio, self.Owner.Faction.InternalName);
			else
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio, self.Owner.Faction.InternalName);
		}

		float ISelectionBar.GetValue()
		{
			if (!info.ShowSelectionBar || IsTraitDisabled || totalTicksToClone == 0)
				return 0f;

			var viewer = self.World.RenderPlayer ?? self.World.LocalPlayer;
			if (viewer != null && !Info.SelectionBarDisplayRelationships.HasRelationship(self.Owner.RelationshipWith(viewer)))
				return 0f;

			return (float)(totalTicksToClone - ticksUntilCloned) / totalTicksToClone;
		}

		Color ISelectionBar.GetColor()
		{
			return info.SelectionBarColor;
		}

		bool ISelectionBar.DisplayWhenEmpty => false;
	}
}
