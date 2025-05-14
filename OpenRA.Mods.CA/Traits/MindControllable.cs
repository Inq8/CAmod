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
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public enum CargoBehaviour { DoNothing, Eject, Kill }

	[Desc("This actor can be mind controlled by other actors.")]
	public class MindControllableInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Named type of mind control. Must match that of MindController.")]
		public readonly string ControlType = null;

		[Desc("The sound played when the mindcontrol is revoked.")]
		public readonly string[] RevokeControlSounds = { };

		[Desc("Map player to transfer this actor to if the owner lost the game.")]
		public readonly string FallbackOwner = "Creeps";

		[Desc("What happens to cargo on being mind controlled, and when control is lost.")]
		public readonly CargoBehaviour CargoBehaviour = CargoBehaviour.DoNothing;

		[ActorReference(dictionaryReference: LintDictionaryReference.Keys)]
		[Desc("Condition to grant when under mindcontrol.",
			"A dictionary of [actor id]: [condition].")]
		public readonly Dictionary<string, string> ControlledConditions = new Dictionary<string, string>();

		[ActorReference(dictionaryReference: LintDictionaryReference.Keys)]
		[Desc("Condition to grant when revoking mindcontrol.",
			"A dictionary of [actor id]: [condition].")]
		public readonly Dictionary<string, string> RevokingConditions = new Dictionary<string, string>();

		[GrantedConditionReference]
		public IEnumerable<string> LinterControlledConditions { get { return ControlledConditions.Values; } }

		[GrantedConditionReference]
		public IEnumerable<string> LinterRevokingConditions { get { return RevokingConditions.Values; } }

		public override object Create(ActorInitializer init) { return new MindControllable(init.Self, this); }
	}

	public class MindControllable : PausableConditionalTrait<MindControllableInfo>, INotifyKilled, INotifyActorDisposing, INotifyOwnerChanged, INotifyTransform, ITick, INotifyPassengerExited
	{
		readonly MindControllableInfo info;
		Player creatorOwner;
		bool controlChanging;
		Actor oldSelf = null;

		int controlledToken = Actor.InvalidConditionToken;
		int revokingToken = Actor.InvalidConditionToken;
		int revokeTicks;
		bool revoking = false;

		public TraitPair<MindController>? Master { get; private set; }

		public MindControllable(Actor self, MindControllableInfo info)
			: base(info)
		{
			this.info = info;
		}

		public void LinkMaster(Actor self, Actor masterActor)
		{
			self.CancelActivity();

			HandleCargo(self, masterActor);

			if (Master == null)
				creatorOwner = self.Owner;

			controlChanging = true;
			if (self.Owner != masterActor.Owner)
				self.ChangeOwner(masterActor.Owner);

			UnlinkMaster(self);
			var mindController = masterActor.TraitsImplementing<MindController>().Single(mc => mc.Info.ControlType == info.ControlType);
			Master = new TraitPair<MindController>(masterActor, mindController);

			if (controlledToken == Actor.InvalidConditionToken && Info.ControlledConditions.ContainsKey(masterActor.Info.Name))
				controlledToken = self.GrantCondition(Info.ControlledConditions[masterActor.Info.Name]);

			if (masterActor.Owner == creatorOwner)
				UnlinkMaster(self);

			self.World.AddFrameEndTask(w =>
			{
				controlChanging = false;
				revoking = false;
			});

			foreach (var notify in self.TraitsImplementing<INotifyMindControlled>())
				notify.MindControlled(self, masterActor);
		}

		void UnlinkMaster(Actor self)
		{
			if (Master == null)
				return;

			var master = Master.Value;

			HandleCargo(self, master.Actor);

			self.World.AddFrameEndTask(_ =>
			{
				if (master.Actor.IsDead || master.Actor.Disposed)
					return;

				master.Trait.UnlinkSlave(master.Actor, self);
			});

			Master = null;

			foreach (var notify in self.TraitsImplementing<INotifyMindControlled>())
				notify.Released(self, master.Actor);
		}

		public void RevokeMindControl(Actor self, int ticks)
		{
			controlChanging = true;

			if (Master == null)
				return;

			var masterActor = Master.Value.Actor;
			var masterName = masterActor.Info.Name;
			UnlinkMaster(self);

			if (ticks == 0)
				RevokeComplete(self);
			else
			{
				revokeTicks = ticks;
				revoking = true;

				if (revokingToken == Actor.InvalidConditionToken && Info.RevokingConditions.ContainsKey(masterName))
					revokingToken = self.GrantCondition(Info.RevokingConditions[masterName]);
			}
		}

		void RevokeComplete(Actor self)
		{
			self.CancelActivity();

			if (creatorOwner.WinState == WinState.Lost)
				self.ChangeOwner(self.World.Players.First(p => p.InternalName == info.FallbackOwner));
			else
				self.ChangeOwner(creatorOwner);

			if (controlledToken != Actor.InvalidConditionToken)
				controlledToken = self.RevokeCondition(controlledToken);

			if (revokingToken != Actor.InvalidConditionToken)
				revokingToken = self.RevokeCondition(revokingToken);

			if (info.RevokeControlSounds.Length > 0)
				Game.Sound.Play(SoundType.World, info.RevokeControlSounds.Random(self.World.SharedRandom), self.CenterPosition);

			self.World.AddFrameEndTask(_ => controlChanging = false);
		}

		void HandleCargo(Actor self, Actor master)
		{
			var cargo = self.TraitOrDefault<Cargo>();
			if (cargo != null && master != null)
			{
				if (info.CargoBehaviour == CargoBehaviour.Kill)
				{
					while (!cargo.IsEmpty())
					{
						var a = cargo.Unload(self);
						a.Kill(master);
					}
				}
				else if (info.CargoBehaviour == CargoBehaviour.Eject && !cargo.IsEmpty())
				{
					self.CancelActivity();
					self.QueueActivity(new UnloadCargo(self, WDist.FromCells(5)));
				}
			}
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitPaused || IsTraitDisabled)
				return;

			if (!controlChanging || !revoking)
				return;

			if (--revokeTicks > 0)
				return;

			RevokeComplete(self);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (Master == null)
				return;

			UnlinkMaster(self);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (Master == null)
				return;

			UnlinkMaster(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (Master == null || controlChanging)
				return;

			UnlinkMaster(self);

			if (controlledToken != Actor.InvalidConditionToken)
				controlledToken = self.RevokeCondition(controlledToken);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (Master != null)
				RevokeMindControl(self, 0);
		}

		void TransferMindControl(MindControllable mc)
		{
			Master = mc.Master;
			creatorOwner = mc.creatorOwner;
			controlChanging = mc.controlChanging;
		}

		void INotifyTransform.BeforeTransform(Actor self) { oldSelf = self; }
		void INotifyTransform.OnTransform(Actor self) { }
		void INotifyTransform.AfterTransform(Actor self)
		{
			if (Master != null)
			{
				var mc = self.TraitsImplementing<MindControllable>().FirstOrDefault(m => m.Info.ControlType == Info.ControlType);
				if (mc != null)
				{
					mc.TransferMindControl(this);
					if (oldSelf != null)
						Master.Value.Trait.TransformSlave(Master.Value.Actor, oldSelf, self);
				}
				else
					self.ChangeOwner(creatorOwner);
			}
		}

		void INotifyPassengerExited.OnPassengerExited(Actor self, Actor passenger)
		{
			if (Master == null && passenger.Owner != creatorOwner)
				return;

			passenger.ChangeOwner(creatorOwner);
		}
	}
}
