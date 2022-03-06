﻿#region Copyright & License Information
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
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("This actor places mines around itself, and replenishes them after a while.")]
	public class LaysMinefieldInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Types of mines to place, if multipile is defined, a random one will be selected.")]
		public readonly HashSet<string> Mines = new HashSet<string>();

		[FieldLoader.Require]
		[Desc("Locations to place the mines, from top-left of the building.")]
		public readonly CVec[] Locations = { };

		[Desc("Initial delay to create the mines.")]
		public readonly int InitialDelay = 1;

		[Desc("Recreate the mines, if they are destroyed after this much of time.")]
		public readonly int RecreationInterval = 2500;

		[Desc("Remove the mines if the trait gets disabled.")]
		public readonly bool RemoveOnDisable = true;

		[Desc("Kill the mines if the trait gets disabled or parent is killed, else dispose.")]
		public readonly bool KillOnRemove = true;

		[Desc("Types of damage this actor explodes with due to being killed. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		[Desc("Ignore placement rules")]
		public readonly bool PlacementIgnroesOccupiesSpace = true;

		public override object Create(ActorInitializer init) { return new LaysMinefield(this); }
	}

	public class LaysMinefield : PausableConditionalTrait<LaysMinefieldInfo>, INotifyKilled, INotifyOwnerChanged, INotifyActorDisposing, ITick, ISync
	{
		List<Actor> mines = new List<Actor>();

		[Sync]
		int ticks;

		public LaysMinefield(LaysMinefieldInfo info)
			: base(info)
		{
			ticks = Info.InitialDelay;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitPaused || IsTraitDisabled)
				return;

			if (--ticks < 0)
			{
				ticks = Info.RecreationInterval;
				SpawnMines(self);
			}
		}

		public void SpawnMines(Actor self)
		{
			foreach (var offset in Info.Locations)
			{
				var cell = self.Location + offset;
				{
					foreach (var actor in Info.Mines)
					{
						var ai = self.World.Map.Rules.Actors[actor];
						var ip = ai.TraitInfo<IPositionableInfo>();
						var mine = self.World.CreateActor(actor.ToLowerInvariant(), new TypeDictionary
							{
								new OwnerInit(self.Owner),
								new LocationInit(cell)
							});

						if (Info.PlacementIgnroesOccupiesSpace)
							mines.Add(mine);
						else
						{
							if (!ip.CanEnterCell(self.World, null, cell))
								continue;
							else
								mines.Add(mine);
						}
					}
				}
			}
		}

		public void RemoveMines(Actor self)
		{
				foreach (var mine in mines)
					if (Info.KillOnRemove)
						mine.Kill(mine, Info.DamageTypes);

				mines.Clear();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			foreach (var mine in mines)
				mine.ChangeOwnerSync(newOwner);
		}

		protected override void TraitDisabled(Actor self)
		{
			ticks = Info.InitialDelay;

			if (Info.RemoveOnDisable)
				RemoveMines(self);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			RemoveMines(self);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			RemoveMines(self);
		}
	}
}
