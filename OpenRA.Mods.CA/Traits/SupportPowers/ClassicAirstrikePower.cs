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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class AirstrikePowerSquadMember
	{
		public readonly string UnitType;

		public readonly WVec SpawnOffset;

		public readonly WVec TargetOffset;

		public readonly int SpawnDelay;

		public AirstrikePowerSquadMember(MiniYamlNode yamlNode)
		{
			UnitType = yamlNode.Key;
			FieldLoader.Load(this, yamlNode.Value);
		}
	}

	public class ClassicAirstrikePowerInfo : SupportPowerInfo
	{
		[FieldLoader.LoadUsing("LoadSquad")]
		[Desc("A list of aircraft in the squad. Each has configurable UnitType, SpawnOffset, TargetOffset and SpawnDelay.")]
		public readonly List<AirstrikePowerSquadMember> Squad;

		public readonly int QuantizedFacings = 32;
		public readonly WDist Cordon = new WDist(5120);

		[ActorReference]
		[Desc("Actor to spawn when the aircraft start attacking")]
		public readonly string CameraActor = null;

		[Desc("Amount of time to keep the camera alive after the aircraft have finished attacking")]
		public readonly int CameraRemoveDelay = 25;

		[Desc("Enables the player directional targeting")]
		public readonly bool UseDirectionalTarget = false;

		[Desc("Animation used to render the direction arrows.")]
		public readonly string DirectionArrowAnimation = null;

		[Desc("Palette for direction cursor animation.")]
		public readonly string DirectionArrowPalette = "chrome";

		[Desc("Weapon range offset to apply during the beacon clock calculation")]
		public readonly WDist BeaconDistanceOffset = WDist.FromCells(6);

		[Desc("How many attack runs to perform.")]
		public readonly int Strikes = 1;

		[Desc("How long to allow idling in the circle phase between strikes.")]
		public readonly int CircleDelay = 0;

		static object LoadSquad(MiniYaml yaml)
		{
			var ret = new List<AirstrikePowerSquadMember>();
			var squadNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Squad");
			if (squadNode != null)
				foreach (var d in squadNode.Value.Nodes)
					ret.Add(new AirstrikePowerSquadMember(d));

			return ret;
		}

		public override object Create(ActorInitializer init) { return new ClassicAirstrikePower(init.Self, this); }
	}

	public class ClassicAirstrikePower : SupportPower
	{
		readonly ClassicAirstrikePowerInfo info;

		public ClassicAirstrikePower(Actor self, ClassicAirstrikePowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			if (info.UseDirectionalTarget)
			{
				Game.Sound.PlayToPlayer(SoundType.UI, manager.Self.Owner, Info.SelectTargetSound);
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
					Info.SelectTargetSpeechNotification, self.Owner.Faction.InternalName);

				self.World.OrderGenerator = new SelectDirectionalTarget(self.World, order, manager, Info.Cursor, info.DirectionArrowAnimation, info.DirectionArrowPalette);
			}
			else
				base.SelectTarget(self, order, manager);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);
			SendAirstrike(self, order.Target.CenterPosition, !info.UseDirectionalTarget || order.ExtraData == uint.MaxValue, (int)order.ExtraData);
		}

		public void SendAirstrike(Actor self, WPos target, bool randomize = true, int attackFacing = 0)
		{
			if (randomize)
				attackFacing = 256 * self.World.SharedRandom.Next(info.QuantizedFacings) / info.QuantizedFacings;

			Actor camera = null;
			Beacon beacon = null;
			var aircraftInRange = new Dictionary<Actor, bool>();

			Action<Actor> onEnterRange = a =>
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

				aircraftInRange[a] = true;
			};

			Action<Actor> onExitRange = a =>
			{
				aircraftInRange[a] = false;

				// Remove the camera when the final plane leaves the target area
				if (!aircraftInRange.Any(kv => kv.Value))
					RemoveCamera(camera);
			};

			Action<Actor> onRemovedFromWorld = a =>
			{
				aircraftInRange[a] = false;

				// Checking for attack range is not relevant here because
				// aircraft may be shot down before entering. Thus we remove
				// the camera and beacon only if the whole squad is dead.
				if (aircraftInRange.All(kv => kv.Key.IsDead))
				{
					RemoveCamera(camera);
					RemoveBeacon(beacon);
				}
			};

			self.World.AddFrameEndTask(w =>
			{
				WPos? startPos = null;
				foreach (var squadMember in info.Squad)
				{
					var altitude = self.World.Map.Rules.Actors[squadMember.UnitType].TraitInfo<AircraftInfo>().CruiseAltitude.Length;
					var attackRotation = WRot.FromFacing(attackFacing);
					var delta = new WVec(0, -1024, 0).Rotate(attackRotation);
					var targetPos = target + new WVec(0, 0, altitude);
					var startEdge = targetPos - (self.World.Map.DistanceToEdge(targetPos, -delta) + info.Cordon).Length * delta / 1024;
					var finishEdge = targetPos + (self.World.Map.DistanceToEdge(targetPos, delta) + info.Cordon).Length * delta / 1024;

					startPos = startEdge;

					PlayLaunchSounds();

					var spawnOffset = squadMember.SpawnOffset.Rotate(attackRotation);
					var targetOffset = squadMember.TargetOffset.Rotate(attackRotation);

					var a = w.CreateActor(squadMember.UnitType, new TypeDictionary
					{
						new CenterPositionInit(startEdge + spawnOffset),
						new OwnerInit(self.Owner),
						new FacingInit(attackFacing),
						new CreationActivityDelayInit(squadMember.SpawnDelay)
					});

					var attack = a.Trait<AttackBomber>();
					attack.SetTarget(w, targetPos + targetOffset);
					attack.OnEnteredAttackRange += onEnterRange;
					attack.OnExitedAttackRange += onExitRange;
					attack.OnRemovedFromWorld += onRemovedFromWorld;

					for (var strikes = 0; strikes < info.Strikes; strikes++)
					{
						a.QueueActivity(new Fly(a, Target.FromPos(target + spawnOffset)));
						if (info.Strikes > 1)
							a.QueueActivity(new FlyTimed(info.CircleDelay, a));
					}

					a.QueueActivity(new Fly(a, Target.FromPos(finishEdge + spawnOffset)));
					a.QueueActivity(new RemoveSelf());
					aircraftInRange.Add(a, false);
				}

				if (Info.DisplayBeacon && startPos.HasValue)
				{
					var distance = (target - startPos.Value).HorizontalLength;

					beacon = new Beacon(
						self.Owner,
						new WPos(target.X, target.Y, 0),
						Info.BeaconPaletteIsPlayerPalette,
						Info.BeaconPalette,
						Info.BeaconImage,
						Info.BeaconPoster,
						Info.BeaconPosterPalette,
						Info.BeaconSequence,
						Info.ArrowSequence,
						Info.CircleSequence,
						Info.ClockSequence,
						() =>
						{
							// To account for different spawn times and potentially different movement speeds.
							var closestActor = aircraftInRange.Keys.MinBy(x => (x.CenterPosition - target).HorizontalLengthSquared);
							return 1 - ((closestActor.CenterPosition - target).HorizontalLength - info.BeaconDistanceOffset.Length) * 1f / distance;
						},
						Info.BeaconDelay);

					w.Add(beacon);
				}
			});
		}

		void RemoveCamera(Actor camera)
		{
			if (camera == null)
				return;

			camera.QueueActivity(new Wait(info.CameraRemoveDelay));
			camera.QueueActivity(new RemoveSelf());
			camera = null;
		}

		void RemoveBeacon(Beacon beacon)
		{
			if (beacon == null)
				return;

			Self.World.AddFrameEndTask(w =>
			{
				w.Remove(beacon);
				beacon = null;
			});
		}
	}
}
