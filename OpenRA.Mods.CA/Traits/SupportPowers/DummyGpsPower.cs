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
	class DummyGpsPowerInfo : SupportPowerInfo
	{
		[Desc("Delay in ticks between launching and revealing the map.")]
		public readonly int RevealDelay = 0;

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

		[Desc("Requires an actor with an online `ProvidesRadar` to show GPS dots.")]
		public readonly bool RequiresActiveRadar = true;

		[FieldLoader.Require]
		[Desc("The condition to apply. Must be included in the target actor's ExternalConditions list.")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new DummyGpsPower(init.Self, this); }
	}

	class DummyGpsPower : SupportPower, INotifyKilled, INotifySold, INotifyOwnerChanged
	{
		Actor self;
		readonly DummyGpsPowerInfo info;
		int conditionToken = Actor.InvalidConditionToken;

		protected override void Created(Actor self)
		{
			// Special case handling is required for the Player actor.
			// Created is called before Player.PlayerActor is assigned,
			// so we must query other player traits from self, knowing that
			// it refers to the same actor as self.Owner.PlayerActor
			var playerActor = self.Info.Name == "player" ? self : self.Owner.PlayerActor;

			base.Created(self);
		}

		public DummyGpsPower(Actor self, DummyGpsPowerInfo info)
			: base(self, info)
		{
			this.self = self;
			this.info = info;
		}

		public override void Charged(Actor self, string key)
		{
			self.Owner.PlayerActor.Trait<SupportPowerManager>().Powers[key].Activate(new Order());
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			self.World.AddFrameEndTask(w =>
			{
				PlayLaunchSounds();

				w.Add(new SatelliteLaunchCA(self, info));

				if (conditionToken == Actor.InvalidConditionToken)
					conditionToken = self.GrantCondition(info.Condition);
			});
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e) { RemoveGps(self); }
		void INotifySold.Selling(Actor self) { }
		void INotifySold.Sold(Actor self) { RemoveGps(self); }

		void RemoveGps(Actor self)
		{
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
		}
	}
}
