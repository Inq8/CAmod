#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("This actor provides Radar GPS.")]
	public class RangedGpsRadarProviderInfo : ConditionalTraitInfo
	{
		[Desc("Reveals within this range. Use zero for whole map.")]
		public readonly WDist Range = WDist.Zero;

		[Desc("The maximum vertical range above terrain to search for actors.",
		"Ignored if 0 (actors are selected regardless of vertical distance).")]
		public readonly WDist MaximumVerticalOffset = WDist.Zero;

		public override object Create(ActorInitializer init) { return new RangedGpsRadarProvider(init, this); }
	}

	public class RangedGpsRadarProvider : ConditionalTrait<RangedGpsRadarProviderInfo>, INotifyVisualPositionChanged, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyOtherProduction
	{
		readonly Actor self;

		int proximityTrigger;
		WPos cachedPosition;
		WDist cachedRange;
		WDist desiredRange;
		WDist cachedVRange;
		WDist desiredVRange;

		public RangedGpsRadarProvider(ActorInitializer init, RangedGpsRadarProviderInfo info)
			: base(info)
		{
			self = init.Self;
			cachedRange = WDist.Zero;
			cachedVRange = WDist.Zero;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			cachedPosition = self.CenterPosition;
			proximityTrigger = self.World.ActorMap.AddProximityTrigger(cachedPosition, cachedRange, cachedVRange, ActorEntered, ActorExited);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.ActorMap.RemoveProximityTrigger(proximityTrigger);
		}

		protected override void TraitEnabled(Actor self)
		{
			desiredRange = Info.Range;
			desiredVRange = Info.MaximumVerticalOffset;
			Update();
		}

		protected override void TraitDisabled(Actor self)
		{
			desiredRange = WDist.Zero;
			desiredVRange = WDist.Zero;
			Update();
		}

		void INotifyVisualPositionChanged.VisualPositionChanged(Actor self, byte oldLayer, byte newLayer)
		{
			if (!self.IsInWorld && !IsTraitDisabled)
				return;

			if (self.CenterPosition != cachedPosition)
				Update();
		}

		void Update()
		{
			cachedPosition = self.CenterPosition;
			cachedRange = desiredRange;
			cachedVRange = desiredVRange;
			self.World.ActorMap.UpdateProximityTrigger(proximityTrigger, cachedPosition, cachedRange, cachedVRange);
		}

		void ActorEntered(Actor a)
		{
			if (a.Disposed || self.Disposed)
				return;

			if (self.Owner.IsAlliedWith(a.Owner))
				return;

			var dotTrait = a.TraitOrDefault<GpsRadarDot>();
			if (dotTrait != null)
				dotTrait.AddRangedObserver(self);
		}

		public void UnitProducedByOther(Actor self, Actor producer, Actor produced, string productionType, TypeDictionary init)
		{
			// If the produced Actor doesn't occupy space, it can't be in range
			if (produced.OccupiesSpace == null)
				return;

			// Work around for actors produced within the region not triggering until the second tick
			if ((produced.CenterPosition - self.CenterPosition).HorizontalLengthSquared <= Info.Range.LengthSquared)
			{
				if (self.Owner.IsAlliedWith(produced.Owner))
					return;

				var dotTrait = produced.TraitOrDefault<GpsRadarDot>();
				if (dotTrait != null)
					dotTrait.AddRangedObserver(self);
			}
		}

		void ActorExited(Actor a)
		{
			if (a.Disposed)
				return;

			if (self.Owner.IsAlliedWith(a.Owner))
				return;

			var dotTrait = a.TraitOrDefault<GpsRadarDot>();
			if (dotTrait != null)
				dotTrait.RemoveRangedObserver(self);
		}
	}
}
