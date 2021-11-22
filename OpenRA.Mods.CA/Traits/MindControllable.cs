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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("This actor can be mind controlled by other actors.")]
	public class MindControllableInfo : PausableConditionalTraitInfo
	{
		[Desc("The sound played when the mindcontrol is revoked.")]
		public readonly string[] RevokeControlSounds = { };

		[Desc("Map player to transfer this actor to if the owner lost the game.")]
		public readonly string FallbackOwner = "Creeps";

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

	public class MindControllable : PausableConditionalTrait<MindControllableInfo>, INotifyKilled, INotifyActorDisposing, INotifyOwnerChanged, INotifyTransform, ITick
	{
		readonly MindControllableInfo info;
		Player creatorOwner;
		bool controlChanging;
		Actor oldSelf = null;

		int controlledToken = Actor.InvalidConditionToken;
		int revokingToken = Actor.InvalidConditionToken;
		int revokeTicks;
		bool revoking = false;

		public Actor Master { get; private set; }

		public MindControllable(Actor self, MindControllableInfo info)
			: base(info)
		{
			this.info = info;
		}

		public void LinkMaster(Actor self, Actor master)
		{
			self.CancelActivity();

			if (Master == null)
				creatorOwner = self.Owner;

			controlChanging = true;

			var oldOwner = self.Owner;
			self.ChangeOwner(master.Owner);

			UnlinkMaster(self, Master);
			Master = master;

			var mindController = Master.Trait<MindController>();

			if (controlledToken == Actor.InvalidConditionToken && Info.ControlledConditions.ContainsKey(Master.Info.Name))
				controlledToken = self.GrantCondition(Info.ControlledConditions[Master.Info.Name]);

			if (master.Owner == creatorOwner)
				UnlinkMaster(self, master);

			self.World.AddFrameEndTask(w =>
			{
				controlChanging = false;
				revoking = false;
			});
		}

		public void UnlinkMaster(Actor self, Actor master)
		{
			if (master == null)
				return;

			self.World.AddFrameEndTask(_ =>
			{
				if (master.IsDead || master.Disposed)
					return;

				master.Trait<MindController>().UnlinkSlave(master, self);
			});

			Master = null;
		}

		public void RevokeMindControl(Actor self, int ticks)
		{
			controlChanging = true;

			if (Master == null)
				return;

			var masterName = Master.Info.Name;
			UnlinkMaster(self, Master);

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

			if (info.RevokeControlSounds.Any())
				Game.Sound.Play(SoundType.World, info.RevokeControlSounds.Random(self.World.SharedRandom), self.CenterPosition);

			self.World.AddFrameEndTask(_ => controlChanging = false);
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
			UnlinkMaster(self, Master);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			UnlinkMaster(self, Master);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (controlChanging)
				return;

			UnlinkMaster(self, Master);

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
				var mc = self.TraitOrDefault<MindControllable>();
				if (mc != null)
				{
					mc.TransferMindControl(this);
					if (oldSelf != null)
						Master.Trait<MindController>().TransformSlave(Master, oldSelf, self);
				}
				else
					self.ChangeOwner(creatorOwner);
			}
		}
	}
}
