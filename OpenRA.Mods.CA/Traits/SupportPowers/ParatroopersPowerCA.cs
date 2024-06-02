#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Support power that delivers paratroopers. CA version adds overrides based on prerequisites.")]
	public class ParatroopersPowerCAInfo : SupportPowerInfo
	{
		[ActorReference(typeof(AircraftInfo))]
		public readonly string UnitType = "badr";
		public readonly int SquadSize = 1;
		public readonly WVec SquadOffset = new(-1536, 1536, 0);

		[NotificationReference("Speech")]
		[Desc("Speech notification to play when entering the drop zone.")]
		public readonly string ReinforcementsArrivedSpeechNotification = null;

		[Desc("Text notification to display when entering the drop zone.")]
		public readonly string ReinforcementsArrivedTextNotification = null;

		[Desc("Number of facings that the delivery aircraft may approach from.")]
		public readonly int QuantizedFacings = 32;

		[Desc("Spawn and remove the plane this far outside the map.")]
		public readonly WDist Cordon = new(5120);

		[ActorReference(typeof(PassengerInfo))]
		[Desc("Troops to be delivered.  They will be distributed between the planes if SquadSize > 1.")]
		public readonly string[] DropItems = Array.Empty<string>();

		[Desc("Risks stuck units when they don't have the Paratrooper trait.")]
		public readonly bool AllowImpassableCells = false;

		[ActorReference]
		[Desc("Actor to spawn when the paradrop starts.")]
		public readonly string CameraActor = null;

		[Desc("Amount of time (in ticks) to keep the camera alive while the passengers drop.")]
		public readonly int CameraRemoveDelay = 85;

		[Desc("Enables the player directional targeting")]
		public readonly bool UseDirectionalTarget = false;

		[Desc("Animation used to render the direction arrows.")]
		public readonly string DirectionArrowAnimation = null;

		[Desc("Palette for direction cursor animation.")]
		public readonly string DirectionArrowPalette = "chrome";

		[Desc("Weapon range offset to apply during the beacon clock calculation.")]
		public readonly WDist BeaconDistanceOffset = WDist.FromCells(4);

		[Desc("Prerequisites grouped together to be referenced by the Prerequisite based overrides.")]
		public readonly Dictionary<string, string[]> PrerequisiteGroupings = new();

		[Desc("Overrides UnitType based on prerequsites being met. If multiple are met, the first is used.",
			"Keys can either be a single prerequisite or be a key of PrerequisiteGroupings.")]
		public readonly Dictionary<string, string> PrerequisiteUnitTypes = new();

		[Desc("Overrides SquadSize based on prerequsites being met. If multiple are met, the first is used.",
			"Keys can either be a single prerequisite or be a key of PrerequisiteGroupings.")]
		public readonly Dictionary<string, int> PrerequisiteSquadSizes = new();

		[Desc("Overrides DropItems based on prerequsites being met. If multiple are met, the first is used.",
			"Keys can either be a single prerequisite or be a key of PrerequisiteGroupings.")]
		public readonly Dictionary<string, string[]> PrerequisiteDropItems = new();

		public override object Create(ActorInitializer init) { return new ParatroopersPowerCA(init.Self, this); }
	}

	public class ParatroopersPowerCA : SupportPower
	{
		readonly ParatroopersPowerCAInfo info;
		TechTree techTree;

		public ParatroopersPowerCA(Actor self, ParatroopersPowerCAInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			techTree = self.Owner.PlayerActor.Trait<TechTree>();
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			if (info.UseDirectionalTarget)
				self.World.OrderGenerator = new SelectDirectionalTarget(self.World, order, manager, Info.Cursor, info.DirectionArrowAnimation, info.DirectionArrowPalette);
			else
				base.SelectTarget(self, order, manager);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			var facing = info.UseDirectionalTarget && order.ExtraData != uint.MaxValue ? (WAngle?)WAngle.FromFacing((int)order.ExtraData) : null;
			SendParatroopers(self, order.Target.CenterPosition, facing);
		}

		public (Actor[] Aircraft, Actor[] Units) SendParatroopers(Actor self, WPos target, WAngle? facing = null)
		{
			var aircraft = new List<Actor>();
			var units = new List<Actor>();

			var info = Info as ParatroopersPowerCAInfo;

			if (!facing.HasValue)
				facing = new WAngle(1024 * self.World.SharedRandom.Next(info.QuantizedFacings) / info.QuantizedFacings);

			var unitType = GetUnitType();
			var utLower = info.UnitType.ToLowerInvariant();
			if (!self.World.Map.Rules.Actors.TryGetValue(utLower, out var ut))
				throw new YamlException($"Actors ruleset does not include the entry '{utLower}'");

			var altitude = ut.TraitInfo<AircraftInfo>().CruiseAltitude.Length;
			var dropRotation = WRot.FromYaw(facing.Value);
			var delta = new WVec(0, -1024, 0).Rotate(dropRotation);
			target += new WVec(0, 0, altitude);
			var startEdge = target - (self.World.Map.DistanceToEdge(target, -delta) + info.Cordon).Length * delta / 1024;
			var finishEdge = target + (self.World.Map.DistanceToEdge(target, delta) + info.Cordon).Length * delta / 1024;

			Actor camera = null;
			Beacon beacon = null;
			var aircraftInRange = new Dictionary<Actor, bool>();

			void OnEnterRange(Actor a)
			{
				// Spawn a camera and remove the beacon when the first plane enters the target area
				if (info.CameraActor != null && camera == null && !aircraftInRange.Any(kv => kv.Value))
				{
					self.World.AddFrameEndTask(w =>
					{
						camera = w.CreateActor(info.CameraActor, new TypeDictionary
						{
							new LocationInit(self.World.Map.CellContaining(target)),
							new OwnerInit(self.Owner),
						});
					});
				}

				RemoveBeacon(beacon);

				if (!aircraftInRange.Any(kv => kv.Value))
				{
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
						info.ReinforcementsArrivedSpeechNotification, self.Owner.Faction.InternalName);

					TextNotificationsManager.AddTransientLine(info.ReinforcementsArrivedTextNotification, self.Owner);
				}

				aircraftInRange[a] = true;
			}

			void OnExitRange(Actor a)
			{
				aircraftInRange[a] = false;

				// Remove the camera when the final plane leaves the target area
				if (!aircraftInRange.Any(kv => kv.Value))
					RemoveCamera(camera);
			}

			void OnRemovedFromWorld(Actor a)
			{
				aircraftInRange[a] = false;

				// Checking for attack range is not relevant here because
				// aircraft may be shot down before entering the range.
				// If at the map's edge, they may be removed from world before leaving.
				if (aircraftInRange.All(kv => !kv.Key.IsInWorld))
				{
					RemoveCamera(camera);
					RemoveBeacon(beacon);
				}
			}

			var squadSize = GetSquadSize();

			// Create the actors immediately so they can be returned
			for (var i = -squadSize / 2; i <= squadSize / 2; i++)
			{
				// Even-sized squads skip the lead plane
				if (i == 0 && (squadSize & 1) == 0)
					continue;

				// Includes the 90 degree rotation between body and world coordinates
				var so = info.SquadOffset;
				var spawnOffset = new WVec(i * so.Y, -Math.Abs(i) * so.X, 0).Rotate(dropRotation);

				aircraft.Add(self.World.CreateActor(false, unitType, new TypeDictionary
				{
					new CenterPositionInit(startEdge + spawnOffset),
					new OwnerInit(self.Owner),
					new FacingInit(facing.Value),
				}));
			}

			var dropItems = GetDropItems();

			foreach (var p in dropItems)
			{
				units.Add(self.World.CreateActor(false, p.ToLowerInvariant(), new TypeDictionary
				{
					new OwnerInit(self.Owner)
				}));
			}

			self.World.AddFrameEndTask(w =>
			{
				PlayLaunchSounds();

				Actor distanceTestActor = null;

				var passengersPerPlane = (dropItems.Length + squadSize - 1) / squadSize;
				var added = 0;
				var j = 0;
				for (var i = -squadSize / 2; i <= squadSize / 2; i++)
				{
					// Even-sized squads skip the lead plane
					if (i == 0 && (squadSize & 1) == 0)
						continue;

					// Includes the 90 degree rotation between body and world coordinates
					var so = info.SquadOffset;
					var spawnOffset = new WVec(i * so.Y, -Math.Abs(i) * so.X, 0).Rotate(dropRotation);
					var targetOffset = new WVec(i * so.Y, 0, 0).Rotate(dropRotation);
					var a = aircraft[j++];
					w.Add(a);

					var drop = a.Trait<ParaDrop>();
					drop.SetLZ(w.Map.CellContaining(target + targetOffset), !info.AllowImpassableCells);
					drop.OnEnteredDropRange += OnEnterRange;
					drop.OnExitedDropRange += OnExitRange;
					drop.OnRemovedFromWorld += OnRemovedFromWorld;

					var cargo = a.Trait<Cargo>();
					foreach (var unit in units.Skip(added).Take(passengersPerPlane))
					{
						cargo.Load(a, unit);
						added++;
					}

					a.QueueActivity(new Fly(a, Target.FromPos(target + spawnOffset)));
					a.QueueActivity(new Fly(a, Target.FromPos(finishEdge + spawnOffset)));
					a.QueueActivity(new RemoveSelf());
					aircraftInRange.Add(a, false);
					distanceTestActor = a;
				}

				// Dispose any unused units
				for (var i = added; i < units.Count; i++)
					units[i].Dispose();

				if (Info.DisplayBeacon)
				{
					var distance = (target - startEdge).HorizontalLength;

					beacon = new Beacon(
						self.Owner,
						target - new WVec(0, 0, altitude),
						Info.BeaconPaletteIsPlayerPalette,
						Info.BeaconPalette,
						Info.BeaconImage,
						Info.BeaconPoster,
						Info.BeaconPosterPalette,
						Info.BeaconSequence,
						Info.ArrowSequence,
						Info.CircleSequence,
						Info.ClockSequence,
						() => 1 - ((distanceTestActor.CenterPosition - target).HorizontalLength - info.BeaconDistanceOffset.Length) * 1f / distance,
						Info.BeaconDelay);

					w.Add(beacon);
				}
			});

			return (aircraft.ToArray(), units.ToArray());
		}

		void RemoveCamera(Actor camera)
		{
			if (camera == null)
				return;

			camera.QueueActivity(new Wait(info.CameraRemoveDelay));
			camera.QueueActivity(new RemoveSelf());
		}

		void RemoveBeacon(Beacon beacon)
		{
			if (beacon == null)
				return;

			Self.World.AddFrameEndTask(w => w.Remove(beacon));
		}

		string GetUnitType()
		{
			if (info.PrerequisiteUnitTypes.Any())
			{
				foreach (var item in info.PrerequisiteUnitTypes)
				{
					if (techTree.HasPrerequisites(GetPrerequisitesList(item.Key)))
						return item.Value;
				}
			}

			return info.UnitType;
		}

		int GetSquadSize()
		{
			if (info.PrerequisiteSquadSizes.Any())
			{
				foreach (var item in info.PrerequisiteSquadSizes)
				{
					if (techTree.HasPrerequisites(GetPrerequisitesList(item.Key)))
						return item.Value;
				}
			}

			return info.SquadSize;
		}

		string[] GetDropItems()
		{
			if (info.PrerequisiteDropItems.Any())
			{
				foreach (var item in info.PrerequisiteDropItems)
				{
					if (techTree.HasPrerequisites(GetPrerequisitesList(item.Key)))
						return item.Value;
				}
			}

			return info.DropItems;
		}

		string[] GetPrerequisitesList(string key)
		{
			if (info.PrerequisiteGroupings.TryGetValue(key, out var prerequisites))
				return prerequisites;

			return new string[] { key };
		}
	}
}
