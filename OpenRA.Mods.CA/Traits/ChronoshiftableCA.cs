#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.CA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Can be teleported via Chronoshift power.",
		"Copy of base version, adding the ability to return to avoid death,",
		"and adding warp to/from sprite effects.")]
	public class ChronoshiftableCAInfo : ConditionalTraitInfo
	{
		[Desc("Should the actor die instead of being teleported?")]
		public readonly bool ExplodeInstead = false;

		[Desc("Types of damage that this trait causes to self when 'ExplodeInstead' is true",
			"or the return-to-origin is blocked. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> DamageTypes = default;

		public readonly string ChronoshiftSound = "chrono2.aud";

		[Desc("Should the actor return to its previous location after the chronoshift wore out?")]
		public readonly bool ReturnToOrigin = true;

		[Desc("The color the bar of the 'return-to-origin' logic has.")]
		public readonly Color TimeBarColor = Color.White;

		[Desc("Image used for the teleport effects. Defaults to the actor's type.")]
		public readonly string Image = null;

		[Desc("Sequence used for the effect played where the unit jumped from initially.")]
		[SequenceReference("Image")]
		public readonly string InitialWarpFromSequence = null;

		[Desc("Sequence used for the effect played where the unit jumped to initially.")]
		[SequenceReference("Image")]
		public readonly string InitialWarpToSequence = null;

		[Desc("Sequence used for the effect played where the unit jumped from when returning.")]
		[SequenceReference("Image")]
		public readonly string ReturnWarpFromSequence = null;

		[Desc("Sequence used for the effect played where the unit jumped to when returning.")]
		[SequenceReference("Image")]
		public readonly string ReturnWarpToSequence = null;

		[Desc("Palette to render the warp in/out sprites in.")]
		[PaletteReference]
		public readonly string Palette = "effect";

		[GrantedConditionReference]
		[Desc("Condition to apply while chronoshifted.")]
		public readonly string Condition = null;

		[Desc("If true, if the actor will return to origin instead of being killed while chronoshifted.",
			"ReturnToOrigin must also be true.")]
		public readonly bool ReturnToAvoidDeath = false;

		[Desc("Sound to apply when returning on would-be-death.")]
		public readonly string ReturnToAvoidDeathSound = null;

		[Desc("Relationships that benefit from returning to avoid death.")]
		public readonly PlayerRelationship ReturnToAvoidDeathRelationships = PlayerRelationship.Ally | PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		[Desc("If ReturnToAvoidDeath is true the amount of HP restored on return.")]
		public readonly int ReturnToAvoidDeathHealthPercent = 10;

		[Desc("If ReturnToAvoidDeath is true, the actor to replace the normal husk with.")]
		public readonly string ReturnToAvoidDeathHuskActor = "camera.dummy";

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (!ai.HasTraitInfo<MobileInfo>() && !ai.HasTraitInfo<HuskInfo>())
				throw new YamlException("Chronoshiftable requires actors to have the Mobile or Husk traits.");
		}

		public override object Create(ActorInitializer init) { return new ChronoshiftableCA(init, this); }
	}

	public class ChronoshiftableCA : ConditionalTrait<ChronoshiftableCAInfo>, ITick, ISync, ISelectionBar,
		IDeathActorInitModifier, ITransformActorInitModifier, INotifyRemovedFromWorld,
		INotifyKilled, IHuskModifier
	{
		readonly Actor self;
		Actor chronosphere;
		bool killCargo;
		int duration;
		IPositionable iPositionable;

		// Return-to-origin logic
		[Sync]
		public CPos Origin;

		[Sync]
		public int ReturnTicks = 0;

		readonly ChronoshiftableCAInfo info;
		readonly string faction;
		readonly IFacing facing;
		Cargo cargo;
		List<Actor> cachedPassengers;
		int conditionToken = Actor.InvalidConditionToken;
		bool returnToAvoidDeath;

		public ChronoshiftableCA(ActorInitializer init, ChronoshiftableCAInfo info)
			: base(info)
		{
			self = init.Self;

			var returnInit = init.GetOrDefault<ChronoshiftReturnInit>();
			if (returnInit != null)
			{
				ReturnTicks = returnInit.Ticks;
				duration = returnInit.Duration;
				Origin = returnInit.Origin;

				// Defer to the end of tick as the lazy value may reference an actor that hasn't been created yet
				if (returnInit.Chronosphere != null)
					init.World.AddFrameEndTask(w => chronosphere = returnInit.Chronosphere.Actor(init.World).Value);
			}

			this.info = info;

			if (info.ReturnToAvoidDeath)
			{
				faction = init.GetValue<FactionInit, string>(init.Self.Owner.Faction.InternalName);
				facing = self.TraitOrDefault<IFacing>();
			}
		}

		protected override void Created(Actor self)
		{
			iPositionable = self.OccupiesSpace as IPositionable;
			base.Created(self);

			if (info.ReturnToAvoidDeath)
				cargo = self.TraitOrDefault<Cargo>();
		}

		public bool Teleport(Actor self, CPos targetLocation, int duration, bool killCargo, Actor chronosphere)
		{
			if (info.ReturnToOrigin && info.Condition != null && ReturnTicks <= 0 && conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(info.Condition);

			var teleported = InnerTeleport(self, targetLocation, duration, killCargo, chronosphere);

			if (teleported)
			{
				var warpFromPos = self.CenterPosition;
				var warpToPos = self.World.Map.CenterOfCell(targetLocation);
				WarpEffect(warpFromPos, warpToPos, false);

				if (info.ReturnToAvoidDeath && !killCargo && cargo != null)
				{
					cachedPassengers = new List<Actor>();
					foreach (var p in cargo.Passengers)
						cachedPassengers.Add(p);
				}
			}

			return teleported;
		}

		// Copy of Teleport from Chronoshiftable but using TeleportCA
		bool InnerTeleport(Actor self, CPos targetLocation, int duration, bool killCargo, Actor chronosphere)
		{
			if (IsTraitDisabled)
				return false;

			// Some things appear chronoshiftable, but instead they just die.
			if (Info.ExplodeInstead)
			{
				self.World.AddFrameEndTask(w =>
				{
					// Damage is inflicted by the chronosphere
					if (!self.Disposed)
						self.Kill(chronosphere, Info.DamageTypes);
				});
				return true;
			}

			// Set up return-to-origin info
			// If this actor is already counting down to return to
			// an existing location then we shouldn't override it
			if (ReturnTicks <= 0)
			{
				Origin = self.Location;
				ReturnTicks = duration;
			}

			this.duration = duration;
			this.chronosphere = chronosphere;
			this.killCargo = killCargo;

			// Set up the teleport
			self.QueueActivity(false, new TeleportCA(chronosphere, targetLocation, null, killCargo, true, Info.ChronoshiftSound));

			return true;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || !Info.ReturnToOrigin || ReturnTicks <= 0)
				return;

			// Return to original location
			if (--ReturnTicks == 0)
			{
				// The Move activity is not immediately cancelled, which, combined
				// with Activity.Cancel discarding NextActivity without checking the
				// IsInterruptable flag, means that a well timed order can cancel the
				// Teleport activity queued below - an exploit / cheat of the return mechanic.
				// The Teleport activity queued below is guaranteed to either complete
				// (force-resetting the actor to the middle of the target cell) or kill
				// the actor. It is therefore safe to force-erase the Move activity to
				// work around the cancellation bug.
				// HACK: this is manipulating private internal actor state
				if (self.CurrentActivity != null
					&& (self.CurrentActivity is Move
					|| self.CurrentActivity is Attack
					|| self.CurrentActivity is AttackMoveActivity
					|| self.CurrentActivity.ToString() == "OpenRA.Mods.Common.Traits.AttackFollow+AttackActivity"))
					typeof(Actor).GetProperty(nameof(Actor.CurrentActivity)).SetValue(self, null);

				if (conditionToken != Actor.InvalidConditionToken)
					conditionToken = self.RevokeCondition(conditionToken);

				self.World.AddFrameEndTask(w =>
				{
					WarpEffect(self.CenterPosition, self.World.Map.CenterOfCell(Origin), true);
				});

				// The actor is killed using Info.DamageTypes if the teleport fails
				self.QueueActivity(false, new TeleportCA(chronosphere ?? self, Origin, null, killCargo, true, Info.ChronoshiftSound,
					false, true, Info.DamageTypes));
			}
		}

		// Can't be used in synced code, except with ignoreVis.
		public virtual bool CanChronoshiftTo(Actor self, CPos targetLocation)
		{
			// TODO: Allow enemy units to be chronoshifted into bad terrain to kill them
			return !IsTraitDisabled && iPositionable != null && iPositionable.CanEnterCell(targetLocation);
		}

		// Show the remaining time as a bar
		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled || !Info.ReturnToOrigin)
				return 0f;

			// Otherwise an empty bar is rendered all the time
			if (ReturnTicks == 0 || !self.Owner.IsAlliedWith(self.World.RenderPlayer))
				return 0f;

			return (float)ReturnTicks / duration;
		}

		Color ISelectionBar.GetColor() { return Info.TimeBarColor; }
		bool ISelectionBar.DisplayWhenEmpty => false;

		void ModifyActorInit(TypeDictionary init)
		{
			if (IsTraitDisabled || !Info.ReturnToOrigin || ReturnTicks <= 0)
				return;

			init.Add(new ChronoshiftReturnInit(ReturnTicks, duration, Origin, chronosphere));
		}

		void IDeathActorInitModifier.ModifyDeathActorInit(Actor self, TypeDictionary init) { ModifyActorInit(init); }
		void ITransformActorInitModifier.ModifyTransformActorInit(Actor self, TypeDictionary init) { ModifyActorInit(init); }

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (!IsTraitDisabled
				&& Info.ReturnToOrigin
				&& info.ReturnToAvoidDeath
				&& ReturnTicks > 0
				&& (chronosphere == null || info.ReturnToAvoidDeathRelationships.HasRelationship(self.Owner.RelationshipWith(chronosphere.Owner))))
			{
				returnToAvoidDeath = true;
				self.World.AddFrameEndTask(w =>
				{
					self.World.Remove(self);
				});
			}
		}

		string IHuskModifier.HuskActor(Actor self)
		{
			if (!returnToAvoidDeath || IsTraitDisabled)
				return null;

			return info.ReturnToAvoidDeathHuskActor;
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (!returnToAvoidDeath || IsTraitDisabled)
				return;

			var defeated = self.Owner.WinState == WinState.Lost;
			if (defeated)
				return;

			var originLocation = self.World.Map.CenterOfCell(Origin);

			var td = new TypeDictionary
			{
				new ParentActorInit(self),
				new FactionInit(faction),
				new EffectiveOwnerInit(self.Owner),
				new OwnerInit(self.Owner),
				new SkipMakeAnimsInit(),
				new HealthInit(info.ReturnToAvoidDeathHealthPercent),
				new LocationInit(self.Location)
			};

			if (!killCargo && cargo != null)
			{
				var passengers = new List<string>();
				foreach (var passenger in cachedPassengers)
				{
					if (passenger.IsInWorld || !passenger.IsDead)
						continue;

					passengers.Add(passenger.Info.Name.ToLowerInvariant());
				}

				td.Add(new CargoInit(new CargoInfo(), passengers.ToArray()));
			}

			if (facing != null)
				td.Add(new FacingInit(facing.Facing));

			self.World.AddFrameEndTask(w =>
			{
				WarpEffect(self.CenterPosition, originLocation, true);
				var a = w.CreateActor(self.Info.Name, td);
				a.QueueActivity(false, new TeleportCA(chronosphere ?? a, Origin, null, killCargo, false, Info.ChronoshiftSound,
					false, true, Info.DamageTypes));

				returnToAvoidDeath = false;
			});
		}

		void WarpEffect(WPos warpFromPos, WPos warpToPos, bool isReturning)
		{
			var image = info.Image ?? self.Info.Name;
			var w = self.World;

			var warpFromSequence = isReturning ? info.ReturnWarpFromSequence : info.InitialWarpFromSequence;
			var warpToSequence = isReturning ? info.ReturnWarpToSequence : info.InitialWarpToSequence;

			if (warpFromSequence != null)
				w.Add(new SpriteEffect(warpFromPos, w, image, warpFromSequence, info.Palette));

			if (warpToSequence != null)
				w.Add(new SpriteEffect(warpToPos, w, image, warpToSequence, info.Palette));
		}
	}

	public class ChronoshiftReturnInit : CompositeActorInit, ISingleInstanceInit
	{
		public readonly int Ticks;
		public readonly int Duration;
		public readonly CPos Origin;
		public readonly ActorInitActorReference Chronosphere;

		public ChronoshiftReturnInit(int ticks, int duration, CPos origin, Actor chronosphere)
		{
			Ticks = ticks;
			Duration = duration;
			Origin = origin;
			Chronosphere = chronosphere;
		}
	}
}
