﻿#region Copyright & License Information
/**
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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class ClassicAirstrikePowerSquadMember
	{
		public readonly string UnitType;

		public readonly WVec SpawnOffset;

		public readonly WVec TargetOffset;

		public ClassicAirstrikePowerSquadMember(MiniYamlNode yamlNode)
		{
			UnitType = yamlNode.Key;
			FieldLoader.Load(this, yamlNode.Value);
		}
	}

	public class ClassicAirstrikePowerInfo : DirectionalSupportPowerInfo
	{
		[FieldLoader.LoadUsing("LoadSquad")]
		[Desc("A list of aircraft in the squad. Each has configurable UnitType, SpawnOffset and TargetOffset.")]
		public readonly List<ClassicAirstrikePowerSquadMember> Squad;

		public readonly int QuantizedFacings = 32;
		public readonly WDist Cordon = new WDist(5120);

		[ActorReference]
		[Desc("Actor to spawn when the aircraft start attacking")]
		public readonly string CameraActor = null;

		[Desc("Amount of time to keep the camera alive after the aircraft have finished attacking")]
		public readonly int CameraRemoveDelay = 25;

		[Desc("Weapon range offset to apply during the beacon clock calculation")]
		public readonly WDist BeaconDistanceOffset = WDist.FromCells(6);

		[Desc("How many attack runs to perform.")]
		public readonly int Strikes = 1;

		[Desc("How long to allow idling in the circle phase between strikes.")]
		public readonly int CircleDelay = 0;

		static object LoadSquad(MiniYaml yaml)
		{
			var ret = new List<ClassicAirstrikePowerSquadMember>();
			var squadNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Squad");
			if (squadNode != null)
				foreach (var d in squadNode.Value.Nodes)
					ret.Add(new ClassicAirstrikePowerSquadMember(d));

			return ret;
		}

		public IEnumerable<string> LintSquadActors { get { return Squad.Select(s => s.UnitType); } }

		public override object Create(ActorInitializer init) { return new ClassicAirstrikePower(init.Self, this); }
	}

	public class ClassicAirstrikePower : DirectionalSupportPower
	{
		readonly ClassicAirstrikePowerInfo info;

		public ClassicAirstrikePower(Actor self, ClassicAirstrikePowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			var facing = info.UseDirectionalTarget && order.ExtraData != uint.MaxValue ? (WAngle?)WAngle.FromFacing((int)order.ExtraData) : null;
			SendAirstrike(self, order.Target.CenterPosition, facing);
		}

		public Actor[] SendAirstrike(Actor self, WPos target, WAngle? facing = null)
		{
			var aircraft = new List<Actor>();
			if (!facing.HasValue)
				facing = new WAngle(1024 * self.World.SharedRandom.Next(info.QuantizedFacings) / info.QuantizedFacings);

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
				// aircraft may be shot down before entering the range.
				// If at the map's edge, they may be removed from world before leaving.
				if (aircraftInRange.All(kv => !kv.Key.IsInWorld))
				{
					RemoveCamera(camera);
					RemoveBeacon(beacon);
				}
			};

			WPos? startPos = null;

			// Create the actors immediately so they can be returned.
			foreach (var squadMember in info.Squad)
			{
				var a = self.World.CreateActor(false, squadMember.UnitType, new TypeDictionary
				{
					new OwnerInit(self.Owner),
					new FacingInit(facing.Value)
				});

				aircraft.Add(a);
				aircraftInRange.Add(a, false);
			}

			self.World.AddFrameEndTask(w =>
			{
				PlayLaunchSounds();

				Actor distanceTestActor = null;
				for (var i = 0; i < aircraft.Count; i++)
				{
					var squadMember = info.Squad[i];
					var actor = aircraft[i];

					var altitude = self.World.Map.Rules.Actors[squadMember.UnitType].TraitInfo<AircraftInfo>().CruiseAltitude.Length;
					var attackRotation = WRot.FromYaw(facing.Value);
					var delta = new WVec(0, -1024, 0).Rotate(attackRotation);
					var targetPos = target + new WVec(0, 0, altitude);
					var startEdge = targetPos - (self.World.Map.DistanceToEdge(target, -delta) + info.Cordon).Length * delta / 1024;
					var finishEdge = targetPos + (self.World.Map.DistanceToEdge(target, delta) + info.Cordon).Length * delta / 1024;

					startPos = startEdge;

					// Includes the 90 degree rotation between body and world coordinates
					var so = squadMember.SpawnOffset;
					var to = squadMember.TargetOffset;
					var spawnOffset = new WVec(so.Y, -1 * so.X, 0).Rotate(attackRotation);
					var targetOffset = new WVec(to.Y, 0, 0).Rotate(attackRotation);

					actor.Trait<IPositionable>().SetPosition(actor, startEdge + spawnOffset);
					w.Add(actor);

					var attack = actor.Trait<AttackBomberCA>();

					attack.SetTarget(self.World, targetPos + targetOffset);
					attack.OnEnteredAttackRange += onEnterRange;
					attack.OnExitedAttackRange += onExitRange;
					attack.OnRemovedFromWorld += onRemovedFromWorld;

					for (var strikes = 0; strikes < info.Strikes; strikes++)
					{
						actor.QueueActivity(new Fly(actor, Target.FromPos(target + spawnOffset)));
						if (info.Strikes > 1)
							actor.QueueActivity(new FlyForward(actor, info.CircleDelay));
					}

					actor.QueueActivity(new Fly(actor, Target.FromPos(finishEdge + spawnOffset)));
					actor.QueueActivity(new RemoveSelf());
					distanceTestActor = actor;
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
						() => 1 - ((distanceTestActor.CenterPosition - target).HorizontalLength - info.BeaconDistanceOffset.Length) * 1f / distance,
						Info.BeaconDelay);

					w.Add(beacon);
				}
			});

			return aircraft.ToArray();
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
	}
}
