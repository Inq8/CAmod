#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class DropPodsPowerInfo : SupportPowerInfo, IRulesetLoaded
	{
		[FieldLoader.Require]
		[Desc("DropPod Unit")]
		[ActorReference(typeof(AircraftInfo))]
		public readonly string DropPodType = null;

		[Desc("DropPod Unit")]
		public readonly string DropPodType2 = null;

		[Desc("Notification to play when spawning drop pods.")]
		public readonly string DropPodsAvailableNotification = null;

		[ActorReference(typeof(PassengerInfo))]
		[Desc("Troops to be delivered.  They will each get their own pod.")]
		public readonly string[] DropItems = { };

		[Desc("Integer determining the drop pod's facing if it moves.")]
		public readonly int[] PodFacing = { 96, 160 };

		[Desc("Integer determining maximum offset of drop pod drop from targetLocation")]
		public readonly int PodScatter = 3;

		[Desc("Risks stuck units when they don't have the Paratrooper trait.")]
		public readonly bool AllowImpassableCells = false;

		[Desc("Effect sequence sprite image")]
		public readonly string Effect = "explosion";

		[Desc("Effect sequence to display")]
		[SequenceReference("Effect")]
		public readonly string EffectSequence = "piffs";

		[PaletteReference]
		public readonly string EffectPalette = "effect";

		public WeaponInfo WeaponInfo { get; private set; }

		[Desc("Apply the weapon impact this many ticks into the effect")]
		public readonly int WeaponDelay = 0;

		[Desc("Sound to instantly play at the targeted area.")]
		public readonly string OnFireSound = null;

		[Desc("List of sounds that can be played at the spawning location.")]
		public readonly string[] SpawnSounds = new string[0];

		public override object Create(ActorInitializer init) { return new DropPodsPower(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);
		}
	}

	public class DropPodsPower : SupportPower
	{
		readonly DropPodsPowerInfo info;
		public DropPodsPower(Actor self, DropPodsPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			SendDropPods(self, order, info.PodFacing, info.SpawnSounds);
		}

		public Actor[] SendDropPods(Actor self, Order order, int[] podFacing, string[] spawnSounds)
		{
			var units = new List<Actor>();
			var info = Info as DropPodsPowerInfo;

			var utLower = info.DropPodType.ToLowerInvariant();
			ActorInfo unitType;
			if (!self.World.Map.Rules.Actors.TryGetValue(utLower, out unitType))
				throw new YamlException("Actors ruleset does not include the entry '{0}'".F(utLower));

			var altitude = unitType.TraitInfo<AircraftInfo>().CruiseAltitude.Length;
			var pFacing = podFacing.RandomOrDefault(Game.CosmeticRandom);
			var approachRotation = WRot.FromFacing(pFacing);
			var delta = new WVec(0, -altitude, 0).Rotate(approachRotation);

			foreach (var p in info.DropItems)
			{
				var unit = self.World.CreateActor(false, p.ToLowerInvariant(),
					new TypeDictionary { new OwnerInit(self.Owner) });

				units.Add(unit);
			}

			self.World.AddFrameEndTask(w =>
			{
				PlayLaunchSounds();

				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
					info.DropPodsAvailableNotification, self.Owner.Faction.InternalName);

				var target = order.Target.CenterPosition;
				var posOffset = new WVec(-altitude, -altitude, altitude);
				var targetCell = self.World.Map.CellContaining(target);
				var podLocations = self.World.Map.FindTilesInCircle(targetCell, info.PodScatter).Shuffle(self.World.SharedRandom);
				string dropType = null;

				if (pFacing >= 160)
					{
					posOffset = new WVec(-altitude, -altitude, altitude);
					dropType = info.DropPodType;
					}
					else
					{
					posOffset = new WVec(altitude, -altitude, altitude);
					dropType = info.DropPodType2;
					}

				using (var pe = podLocations.GetEnumerator())
					foreach (var u in units)
					{
						CPos podDropCellPos = pe.Current;

						var a = w.CreateActor(dropType, new TypeDictionary
						{
							new CenterPositionInit(self.World.Map.CenterOfCell(podDropCellPos) - delta + posOffset),
							new OwnerInit(self.Owner),
							new FacingInit(pFacing)
						});

						var sound = spawnSounds.RandomOrDefault(Game.CosmeticRandom);
						if (sound != null)
							Game.Sound.Play(SoundType.World, sound, target);

						var cargo = a.Trait<Cargo>();
						var unloadDist = new WDist(10);
						cargo.Load(a, u);

						a.QueueActivity(new Land(a, Target.FromCell(a.World, podDropCellPos)));
						a.QueueActivity(new UnloadCargo(a, unloadDist, true));
						a.QueueActivity(new CallFunc(() => a.Kill(a)));
						pe.MoveNext();
					}
			});

			return units.ToArray();
		}
	}
}
