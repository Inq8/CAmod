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
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[RequireExplicitImplementation]
	public interface ISmokeParticleInfo
	{
		string Image { get; }
		string[] Sequences { get; }
		string Palette { get; }

		int[] Duration { get; }

		WDist[] Speed { get; }

		WDist[] Gravity { get; }

		WeaponInfo Weapon { get; }

		int TurnRate { get; }

		int RandomRate { get; }
	}

	public interface INotifyActivate { void Launching(Actor self); }

	[RequireExplicitImplementation]
	public interface IPointDefense
	{
		bool Destroy(WPos position, Player attacker, string type);
	}

	public interface IBotCAInfo : ITraitInfoInterface { string Name { get; } }
	public interface IBotCA
	{
		bool IsEnemyUnit(Actor a);
	}

	[RequireExplicitImplementation]
	public interface INotifyChronosphere { void Teleporting(WPos from, WPos to); }

	public interface ILoadsOverlayPlayerPalettes { void LoadOverlayPlayerPalettes(WorldRenderer wr, string playerName, Color playerColor, bool replaceExisting); }

	public interface INotifyPrismCharging { void Charging(Actor self, in Target target); }

	[RequireExplicitImplementation]
	public interface INotifyEnterTeleporter { void Charging(Actor self, Actor teleporter); }
	public interface INotifyExitTeleporter { void Arrived(Actor self); }

	[RequireExplicitImplementation]
	public interface IBotAircraftBuilder { bool CanBuildMoreOfAircraft(ActorInfo actorInfo); }

	[RequireExplicitImplementation]
	public interface IPortableChronoModifier { int GetCooldownModifier(); int GetRangeModifier(); }
}
