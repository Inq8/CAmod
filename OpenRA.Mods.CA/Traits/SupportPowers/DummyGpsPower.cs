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
using OpenRA.Mods.Common.Traits.Radar;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	class DummyGpsPowerInfo : SupportPowerInfo, ITechTreePrerequisiteInfo
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
		[Desc("The prerequisite type that this provides.")]
		public readonly string Prerequisite = null;

		IEnumerable<string> ITechTreePrerequisiteInfo.Prerequisites(ActorInfo info)
		{
			yield return Prerequisite;
		}

		public override object Create(ActorInitializer init) { return new GpsPower(init.Self, this); }
	}

	class GpsPower : SupportPower, INotifyKilled, INotifySold, INotifyOwnerChanged, ITick, ITechTreePrerequisite
	{
		readonly Actor self;
		readonly DummyGpsPowerInfo info;
		TechTree techTree;
		bool active;

		protected override void Created(Actor self)
		{
			// Special case handling is required for the Player actor.
			// Created is called before Player.PlayerActor is assigned,
			// so we must query other player traits from self, knowing that
			// it refers to the same actor as self.Owner.PlayerActor
			var playerActor = self.Info.Name == "player" ? self : self.Owner.PlayerActor;

			techTree = playerActor.Trait<TechTree>();

			base.Created(self);
		}

		public GpsPower(Actor self, DummyGpsPowerInfo info)
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

				active = true;
				techTree.ActorChanged(self);

				w.Add(new SatelliteLaunchCA(self, info));
			});
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e) { RemoveGps(self); }

		void INotifySold.Selling(Actor self) { }
		void INotifySold.Sold(Actor self) { RemoveGps(self); }

		void RemoveGps(Actor self)
		{
			// Extra function just in case something needs to be added later
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			techTree = newOwner.PlayerActor.Trait<TechTree>();
			active = false;
		}

		bool NoActiveRadar { get { return !self.World.ActorsHavingTrait<ProvidesRadar>(r => !r.IsTraitDisabled).Any(a => a.Owner == self.Owner); } }
		bool wasPaused;

		void ITick.Tick(Actor self)
		{
			if (!wasPaused && (IsTraitPaused || (info.RequiresActiveRadar && NoActiveRadar)))
			{
				wasPaused = true;
				active = false;
				techTree.ActorChanged(self);
			}
			else if (wasPaused && !IsTraitPaused && !(info.RequiresActiveRadar && NoActiveRadar))
			{
				wasPaused = false;
				active = true;
				techTree.ActorChanged(self);
			}
		}

		IEnumerable<string> ITechTreePrerequisite.ProvidesPrerequisites
		{
			get
			{
				if (!active)
					yield break;

				yield return info.Prerequisite;
			}
		}
	}
}
