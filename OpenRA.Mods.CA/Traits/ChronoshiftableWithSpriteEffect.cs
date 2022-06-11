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

using OpenRA.Mods.Cnc.Activities;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Can be teleported via Chronoshift power.")]
	public class ChronoshiftableWithSpriteEffectInfo : ChronoshiftableInfo
	{
		[Desc("Image used for the teleport effects. Defaults to the actor's type.")]
		public readonly string Image = null;

		[Desc("Sequence used for the effect played where the unit jumped from.")]
		[SequenceReference("Image")]
		public readonly string WarpInSequence = null;

		[Desc("Sequence used for the effect played where the unit jumped to.")]
		[SequenceReference("Image")]
		public readonly string WarpOutSequence = null;

		[Desc("Palette to render the warp in/out sprites in.")]
		[PaletteReference]
		public readonly string Palette = "effect";

		[Desc("Condition to apply while chronoshifted.")]
		public readonly string Condition = null;

		[Desc("If true, if the actor will return to origin instead of being killed while chronoshifted.",
			"ReturnToOrigin must also be true.")]
		public readonly bool ReturnToAvoidDeath = false;

		[Desc("Sound to apply when returning on would-be-death.")]
		public readonly string ReturnToAvoidDeathSound = null;

		[Desc("Relationships that benefit from returning to avoid death.")]
		public readonly PlayerRelationship ReturnToAvoidDeathRelationships = PlayerRelationship.Ally;

		[Desc("If ReturnToAvoidDeath is true the amount of HP restored on return.")]
		public readonly int ReturnToAvoidDeathHealthPercent = 20;

		[Desc("If ReturnToAvoidDeath is true, the actor to replace the normal husk with.")]
		public readonly string ReturnToAvoidDeathHuskActor = "camera.dummy";

		public override object Create(ActorInitializer init) { return new ChronoshiftableWithSpriteEffect(init, this); }
	}

	public class ChronoshiftableWithSpriteEffect : Chronoshiftable, ITick, INotifyRemovedFromWorld, INotifyKilled, IHuskModifier
	{
		readonly ChronoshiftableWithSpriteEffectInfo info;
		readonly Actor self;
		readonly string faction;
		readonly IFacing facing;
		int conditionToken = Actor.InvalidConditionToken;
		Actor chronosphere;
		bool killCargo;
		bool returnToAvoidDeath;

		public ChronoshiftableWithSpriteEffect(ActorInitializer init, ChronoshiftableWithSpriteEffectInfo info)
			: base(init, info)
		{
			this.info = info;
			self = init.Self;

			if (info.ReturnToAvoidDeath)
			{
				faction = init.GetValue<FactionInit, string>(init.Self.Owner.Faction.InternalName);
				facing = self.TraitOrDefault<IFacing>();
			}
		}

		public override bool Teleport(Actor self, CPos targetLocation, int duration, bool killCargo, Actor chronosphere)
		{
			var image = info.Image ?? self.Info.Name;

			var cachedSourcePosition = self.CenterPosition;
			var cachedTargetPosition = self.World.Map.CenterOfCell(targetLocation);

			self.World.AddFrameEndTask(w =>
			{
				if (info.WarpInSequence != null)
					w.Add(new SpriteEffect(cachedSourcePosition, w, image, info.WarpInSequence, info.Palette));

				if (info.WarpOutSequence != null)
					w.Add(new SpriteEffect(cachedTargetPosition, w, image, info.WarpOutSequence, info.Palette));
			});

			if (info.ReturnToOrigin && info.Condition != null && ReturnTicks <= 0 && conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(info.Condition);

			this.chronosphere = chronosphere;
			this.killCargo = killCargo;

			return base.Teleport(self, targetLocation, duration, killCargo, chronosphere);
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
					WarpEffect(w, self.CenterPosition, self.World.Map.CenterOfCell(Origin));
				});

				// The actor is killed using Info.DamageTypes if the teleport fails
				self.QueueActivity(false, new Teleport(chronosphere ?? self, Origin, null, true, killCargo, Info.ChronoshiftSound,
					false, true, Info.DamageTypes));
			}
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (!IsTraitDisabled
				&& Info.ReturnToOrigin
				&& info.ReturnToAvoidDeath
				&& ReturnTicks > 0
				&& (chronosphere == null || info.ReturnToAvoidDeathRelationships.HasStance(self.Owner.RelationshipWith(chronosphere.Owner))))
			{
				returnToAvoidDeath = true;
				self.World.Remove(self);
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
				new HealthInit(info.ReturnToAvoidDeathHealthPercent)
			};

			if (facing != null)
				td.Add(new FacingInit(facing.Facing));

			self.World.AddFrameEndTask(w =>
			{
				WarpEffect(w, self.CenterPosition, originLocation);
				Game.Sound.Play(SoundType.World, info.ChronoshiftSound, self.CenterPosition);
				var a = w.CreateActor(self.Info.Name, td);
				a.QueueActivity(false, new Teleport(chronosphere ?? a, Origin, null, true, killCargo, Info.ChronoshiftSound,
					false, true, Info.DamageTypes));
			});
		}

		void WarpEffect(World w, WPos currentLocation, WPos destinationLocation)
		{
			var image = info.Image ?? self.Info.Name;

			if (info.WarpInSequence != null)
				w.Add(new SpriteEffect(currentLocation, w, image, info.WarpInSequence, info.Palette));

			if (info.WarpOutSequence != null)
				w.Add(new SpriteEffect(destinationLocation, w, image, info.WarpOutSequence, info.Palette));
		}
	}
}
