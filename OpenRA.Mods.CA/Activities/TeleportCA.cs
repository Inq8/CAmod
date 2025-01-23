#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Activities
{
	[Desc("Same as Teleport but can be made to require an empty destination.")]
	public class TeleportCA : Activity
	{
		readonly Actor teleporter;
		readonly int? maximumDistance;
		readonly bool killOnFailure;
		readonly BitSet<DamageType> killDamageTypes;
		CPos destination;
		readonly bool killCargo;
		readonly bool screenFlash;
		readonly string sound;
		readonly bool requireEmptyDestination;
		int delayRemaining;
		readonly Func<Actor, CPos, int> calculatePreChargeTicks;
		readonly Action<Actor, int> onPreChargeStart;
		readonly Action<Actor> onPreChargeComplete;

		public TeleportCA(Actor teleporter, CPos destination, int? maximumDistance,
			bool killCargo, bool screenFlash, string sound, bool interruptable = true,
			bool killOnFailure = false, BitSet<DamageType> killDamageTypes = default,
			bool requireEmptyDestination = false, Func<Actor, CPos, int> calculatePreChargeTicks = null,
			Action<Actor, int> onPreChargeStart = null, Action<Actor> onPreChargeComplete = null)
		{
			var max = teleporter.World.Map.Grid.MaximumTileSearchRange;
			if (maximumDistance > max)
				throw new InvalidOperationException($"Teleport distance cannot exceed the value of MaximumTileSearchRange ({max}).");

			this.teleporter = teleporter;
			this.destination = destination;
			this.maximumDistance = maximumDistance;
			this.killCargo = killCargo;
			this.screenFlash = screenFlash;
			this.sound = sound;
			this.killOnFailure = killOnFailure;
			this.killDamageTypes = killDamageTypes;
			this.requireEmptyDestination = requireEmptyDestination;
			this.calculatePreChargeTicks = calculatePreChargeTicks;
			this.onPreChargeStart = onPreChargeStart;
			this.onPreChargeComplete = onPreChargeComplete;

			if (!interruptable)
				IsInterruptible = false;
		}

		protected override void OnFirstRun(Actor self)
		{
			delayRemaining = calculatePreChargeTicks?.Invoke(self, destination) ?? 0;
			onPreChargeStart?.Invoke(self, delayRemaining);
		}

		public override bool Tick(Actor self)
		{
			if (delayRemaining-- > 0)
			    return false;
			else
			    onPreChargeComplete?.Invoke(self);

			var pc = self.TraitOrDefault<PortableChronoCA>();
			if (teleporter == self && pc != null && !pc.CanTeleport)
			{
				if (killOnFailure)
					self.Kill(teleporter, killDamageTypes);

				return true;
			}

			var bestCell = ChooseBestDestinationCell(self, destination);
			if (bestCell == null)
			{
				if (killOnFailure)
					self.Kill(teleporter, killDamageTypes);

				return true;
			}

			destination = bestCell.Value;

			Game.Sound.Play(SoundType.World, sound, self.CenterPosition);
			Game.Sound.Play(SoundType.World, sound, self.World.Map.CenterOfCell(destination));

			var positionable = self.Trait<IPositionable>();
			var aircraft = self.TraitOrDefault<Aircraft>();

			var subCell = positionable.GetAvailableSubCell(destination);
			if (subCell != SubCell.Invalid)
				positionable.SetPosition(self, destination, subCell);
			else if (aircraft != null)
				positionable.SetPosition(self, destination);

			self.Generation++;

			if (killCargo)
			{
				var cargo = self.TraitOrDefault<Cargo>();
				if (cargo != null && teleporter != null)
				{
					while (!cargo.IsEmpty())
					{
						var a = cargo.Unload(self);

						// Kill all the units that are unloaded into the void
						// Kill() handles kill and death statistics
						a.Kill(teleporter);
					}
				}
			}

			// Consume teleport charges if this wasn't triggered via chronosphere
			if (teleporter == self && pc != null)
			{
				pc.ConsumeCharge();
				pc.GrantCondition(self);
			}

			// Trigger screen desaturate effect
			if (screenFlash)
				foreach (var a in self.World.ActorsWithTrait<ChronoshiftPaletteEffect>())
					a.Trait.Enable();

			if (teleporter != null && self != teleporter && !teleporter.Disposed)
			{
				var building = teleporter.TraitOrDefault<WithSpriteBody>();
				if (building != null && building.DefaultAnimation.HasSequence("active"))
					building.PlayCustomAnimation(teleporter, "active");
			}

			return true;
		}

		CPos? ChooseBestDestinationCell(Actor self, CPos destination)
		{
			if (teleporter == null)
				return null;

			var restrictTo = maximumDistance == null ? null : self.World.Map.FindTilesInCircle(self.Location, maximumDistance.Value);

			if (maximumDistance != null)
				destination = restrictTo.MinBy(x => (x - destination).LengthSquared);

			var pos = self.Trait<IPositionable>();
			if (pos.CanEnterCell(destination)
				&& teleporter.Owner.Shroud.IsExplored(destination)
				&& (!requireEmptyDestination || !self.World.ActorMap.GetActorsAt(destination).Any()))
				return destination;

			var max = maximumDistance != null ? maximumDistance.Value : teleporter.World.Map.Grid.MaximumTileSearchRange;
			foreach (var tile in self.World.Map.FindTilesInCircle(destination, max))
			{
				if (teleporter.Owner.Shroud.IsExplored(tile)
					&& (restrictTo == null || (restrictTo != null && restrictTo.Contains(tile)))
					&& pos.CanEnterCell(tile)
					&& (!requireEmptyDestination || !self.World.ActorMap.GetActorsAt(tile).Any()))
					return tile;
			}

			return null;
		}
	}
}
