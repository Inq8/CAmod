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
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Deliver the unit in production via skylift.")]
	public class ProductionAirdropCAInfo : ProductionInfo
	{
		[NotificationReference("Speech")]
		public readonly string ReadyAudio = null;

		[NotificationReference("Speech")]
		public readonly string IncomingAudio = null;

		[FieldLoader.Require]
		[ActorReference(typeof(AircraftInfo))]
		[Desc("Cargo aircraft used for delivery. Must have the `Aircraft` trait.")]
		public readonly string ActorType = null;

		[Desc("How the spawn location/direction is calculated for the delivering actor.",
			"Standard: Spawn 1/2 map distance east, in line with the destination.",
			"ClosestEdgeToHome: Spawn from direction of map edge closest to the player spawn at a distance proportional to map size.",
			"ClosestEdgeToDestination: Spawn 1/2 map distance in the direction of closest map edge to the destination.")]
		public readonly string SpawnType = "Standard";

		[Desc("Direction the aircraft should face to land.")]
		public readonly WAngle Facing = new WAngle(256);

		[Desc("If true, the actor's speed will be adjusted based on the distance it needs to travel to deliver its cargo.")]
		public readonly bool ProportionalSpeed = false;

		[Desc("Distance to base the proportional speed multiplier around.")]
		public readonly WDist ProportionalSpeedBaseDistance = WDist.FromCells(50);

		[Desc("Minimum speed modifier.")]
		public readonly int ProportionalSpeedMinimum = 80;

		[Desc("Maximum speed modifier.")]
		public readonly int ProportionalSpeedMaximum = 200;

		public override object Create(ActorInitializer init) { return new ProductionAirdropCA(init, this); }
	}

	class ProductionAirdropCA : Production
	{
		public ProductionAirdropCA(ActorInitializer init, ProductionAirdropCAInfo info)
			: base(init, info) { }

		public override bool Produce(Actor self, ActorInfo producee, string productionType, TypeDictionary inits, int refundableValue)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return false;

			var info = (ProductionAirdropCAInfo)Info;
			var owner = self.Owner;
			var map = owner.World.Map;
			var aircraftInfo = self.World.Map.Rules.Actors[info.ActorType].TraitInfo<AircraftInfo>();

			CPos unadjustedStartPos;
			CPos startPos;
			CPos endPos;
			WAngle spawnFacing;

			if (info.SpawnType == "ClosestEdgeToHome" || info.SpawnType == "ClosestEdgeToDestination")
			{
				var bounds = map.Bounds;
				var center = new MPos(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2).ToCPos(map);
				var spawnVec = owner.HomeLocation - center;

				if (info.SpawnType == "ClosestEdgeToDestination")
				{
					var distFromTopEdge = self.Location.Y;
					var distFromLeftEdge = self.Location.X;
					var distFromBottomEdge = bounds.Height - self.Location.Y;
					var distFromRightEdge = bounds.Width - self.Location.X;
					var halfMapHeight = bounds.Height / 2;
					var halfMapWidth = bounds.Width / 2;

					if (distFromTopEdge <= distFromLeftEdge && distFromTopEdge <= distFromBottomEdge && distFromTopEdge <= distFromRightEdge)
					{
						unadjustedStartPos = new CPos(self.Location.X, self.Location.Y - halfMapHeight);
						startPos = new CPos(unadjustedStartPos.X, unadjustedStartPos.Y - 10);
					}
					else if (distFromRightEdge <= distFromBottomEdge && distFromRightEdge <= distFromLeftEdge)
					{
						unadjustedStartPos = new CPos(self.Location.X + halfMapWidth, self.Location.Y);
						startPos = new CPos(unadjustedStartPos.X + 29, unadjustedStartPos.Y);
					}
					else if (distFromBottomEdge <= distFromLeftEdge)
					{
						unadjustedStartPos = new CPos(self.Location.X, self.Location.Y + halfMapHeight);
						startPos = new CPos(unadjustedStartPos.X, unadjustedStartPos.Y + 10);
					}
					else
					{
						unadjustedStartPos = new CPos(self.Location.X - halfMapWidth, self.Location.Y);
						startPos = new CPos(unadjustedStartPos.X, unadjustedStartPos.Y);
					}
				}
				else
				{
					unadjustedStartPos = startPos = owner.HomeLocation + spawnVec * (Exts.ISqrt((bounds.Height * bounds.Height + bounds.Width * bounds.Width) / (4 * spawnVec.LengthSquared)));
				}

				endPos = startPos;

				var spawnDirection = new WVec((self.Location - startPos).X, (self.Location - startPos).Y, 0);
				spawnFacing = spawnDirection.Yaw;
			}
			else
			{
				// Start a fixed distance away: the width of the map.
				// This makes the production timing independent of spawnpoint
				var loc = self.Location.ToMPos(map);
				unadjustedStartPos = startPos = new MPos(loc.U + map.Bounds.Width, loc.V).ToCPos(map);
				endPos = new MPos(map.Bounds.Left, loc.V).ToCPos(map);
				spawnFacing = info.Facing;
			}

			// Assume a single exit point for simplicity
			var exit = self.Info.TraitInfos<ExitInfo>().First();

			foreach (var tower in self.TraitsImplementing<INotifyDelivery>())
				tower.IncomingDelivery(self);

			owner.World.AddFrameEndTask(w =>
			{
				if (!self.IsInWorld || self.IsDead)
				{
					owner.PlayerActor.Trait<PlayerResources>().GiveCash(refundableValue);
					return;
				}

				if (info.IncomingAudio != null)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.IncomingAudio, self.Owner.Faction.InternalName);

				var actor = w.CreateActor(info.ActorType, new TypeDictionary
				{
					new CenterPositionInit(w.Map.CenterOfCell(startPos) + new WVec(WDist.Zero, WDist.Zero, aircraftInfo.CruiseAltitude)),
					new OwnerInit(owner),
					new FacingInit(spawnFacing)
				});

				var dynamicSpeedMultiplier = actor.TraitOrDefault<DynamicSpeedMultiplier>();

				if (info.ProportionalSpeed && info.ProportionalSpeedBaseDistance.Length > 0 && dynamicSpeedMultiplier != null)
				{
					var travelDistance = (float)((w.Map.CenterOfCell(unadjustedStartPos) - self.CenterPosition).Length);

					var baseDistance = (float)info.ProportionalSpeedBaseDistance.Length;
					var multiplier = (int)Math.Round(travelDistance / baseDistance * 100);

					if (multiplier > info.ProportionalSpeedMaximum)
						multiplier = info.ProportionalSpeedMaximum;
					else if (multiplier < info.ProportionalSpeedMinimum)
						multiplier = info.ProportionalSpeedMinimum;

					dynamicSpeedMultiplier.SetModifier(multiplier);
				}

				var exitCell = self.Location + exit.ExitCell;
				actor.QueueActivity(new Land(actor, Target.FromActor(self), WDist.Zero, WVec.Zero, info.Facing, clearCells: new CPos[1] { exitCell }));
				actor.QueueActivity(new CallFunc(() =>
				{
					if (!self.IsInWorld || self.IsDead)
					{
						owner.PlayerActor.Trait<PlayerResources>().GiveCash(refundableValue);
						return;
					}

					foreach (var cargo in self.TraitsImplementing<INotifyDelivery>())
						cargo.Delivered(self);

					self.World.AddFrameEndTask(ww => DoProduction(self, producee, exit, productionType, inits));
					if (info.ReadyAudio != null)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio, self.Owner.Faction.InternalName);
				}));

				actor.QueueActivity(new FlyOffMap(actor, Target.FromCell(w, endPos)));
				actor.QueueActivity(new RemoveSelf());
			});

			return true;
		}
	}
}
