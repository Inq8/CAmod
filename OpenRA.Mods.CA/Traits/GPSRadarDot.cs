#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.CA.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Show an indicator revealing the actor underneath the fog when a GpsRadarProvider is activated.")]
	class GpsRadarDotInfo : ConditionalTraitInfo
	{
		[Desc("Sprite collection for symbols.")]
		public readonly string Image = "gpsdot";

		[SequenceReference(nameof(Image))]
		[Desc("Sprite used for this actor.")]
		public readonly string Sequence = "idle";

		[PaletteReference(true)]
		public readonly string IndicatorPalettePrefix = "player";

		public readonly bool VisibleInShroud = true;

		public override object Create(ActorInitializer init) { return new GpsRadarDot(this); }
	}

	class GpsRadarDot : ConditionalTrait<GpsRadarDotInfo>, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		GpsRadarDotEffect effect;

		HashSet<Actor> rangedObservers = new HashSet<Actor>();

		public GpsRadarDot(GpsRadarDotInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			effect = new GpsRadarDotEffect(self, this);

			base.Created(self);
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddFrameEndTask(w => w.Add(effect));
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.AddFrameEndTask(w => w.Remove(effect));
		}

		public void AddRangedObserver(Actor a)
		{
			if (rangedObservers.Contains(a))
				return;

			rangedObservers.Add(a);
		}

		public void RemoveRangedObserver(Actor a)
		{
			if (!rangedObservers.Contains(a))
				return;

			rangedObservers.Remove(a);
		}

		void CleanRangedObservers()
		{
			var rangedObserversToRemove = new List<Actor>();

			foreach (var rangedObserver in rangedObservers)
			{
				if (rangedObserver.Disposed || rangedObserver.IsDead)
					rangedObserversToRemove.Add(rangedObserver);
			}

			foreach (var rangedObserver in rangedObserversToRemove)
				rangedObservers.Remove(rangedObserver);
		}

		public bool HasRangedObserver(Player p)
		{
			CleanRangedObservers();

			foreach (var rangedObserver in rangedObservers)
			{
				if (rangedObserver.Owner == p || p.IsAlliedWith(rangedObserver.Owner))
					return true;
			}

			return false;
		}
	}
}
