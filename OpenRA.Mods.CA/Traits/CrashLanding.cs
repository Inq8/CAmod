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

using OpenRA.GameRules;
using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Causes aircraft husks that are spawned in the air to crash to the ground.")]
	public class CrashLandingInfo : TraitInfo, IRulesetLoaded, Requires<AircraftInfo>
	{
		[WeaponReference]
		public readonly string Explosion = "UnitExplode";

		public readonly bool Spins = true;
		public readonly bool Moves = false;
		public readonly WDist Velocity = new WDist(43);

		public WeaponInfo ExplosionWeapon { get; private set; }

		public override object Create(ActorInitializer init) { return new CrashLanding(init, this); }
		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (string.IsNullOrEmpty(Explosion))
				return;

			WeaponInfo weapon;
			var weaponToLower = Explosion.ToLowerInvariant();
			if (!rules.Weapons.TryGetValue(weaponToLower, out weapon))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));

			ExplosionWeapon = weapon;
		}
	}

	public class CrashLanding : IEffectiveOwner, INotifyCreated
	{
		readonly CrashLandingInfo info;
		readonly Player effectiveOwner;
		private Actor a;
		private object podDropCellPos;
		private Actor a1;
		private CrashLandingInfo podDropCellPos1;

		public CrashLanding(ActorInitializer init, CrashLandingInfo info)
		{
			this.info = info;
			effectiveOwner = init.GetValue<EffectiveOwnerInit, Player>(info, init.Self.Owner);
		}

		public CrashLanding(Actor a, object podDropCellPos)
		{
			this.a = a;
			this.podDropCellPos = podDropCellPos;
		}

		public CrashLanding(Actor a1, CrashLandingInfo podDropCellPos1)
		{
			this.a1 = a1;
			this.podDropCellPos1 = podDropCellPos1;
		}

		// We return init.Self.Owner if there's no effective owner
		bool IEffectiveOwner.Disguised { get { return true; } }
		Player IEffectiveOwner.Owner { get { return effectiveOwner; } }

		void INotifyCreated.Created(Actor self)
		{
			self.QueueActivity(false, new CrashLand(self, info));
		}
	}
}
