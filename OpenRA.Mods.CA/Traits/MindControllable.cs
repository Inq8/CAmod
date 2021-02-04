#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("This actor can be mind controlled by other actors.")]
	public class MindControllableInfo : PausableConditionalTraitInfo
	{
		[Desc("Condition to grant when under mindcontrol.")]
		[GrantedConditionReference]
		public readonly string Condition = null;

		[Desc("The sound played when the mindcontrol is revoked.")]
		public readonly string[] RevokeControlSounds = { };

		[Desc("Map player to transfer this actor to if the owner lost the game.")]
		public readonly string FallbackOwner = "Creeps";

		public override object Create(ActorInitializer init) { return new MindControllable(init.Self, this); }
	}

	public class MindControllable : PausableConditionalTrait<MindControllableInfo>, INotifyKilled, INotifyActorDisposing, INotifyOwnerChanged, INotifyTransform
	{
		readonly MindControllableInfo info;
		Player creatorOwner;
		bool controlChanging;
		Actor oldSelf = null;

		int token = Actor.InvalidConditionToken;

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

			if (token == Actor.InvalidConditionToken)
				token = self.GrantCondition(Info.Condition);

			if (master.Owner == creatorOwner)
				UnlinkMaster(self, master);

			self.World.AddFrameEndTask(_ => controlChanging = false);
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

			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}

		public void RevokeMindControl(Actor self)
		{
			self.CancelActivity();

			controlChanging = true;

			if (creatorOwner.WinState == WinState.Lost)
				self.ChangeOwner(self.World.Players.First(p => p.InternalName == info.FallbackOwner));
			else
				self.ChangeOwner(creatorOwner);

			UnlinkMaster(self, Master);

			if (info.RevokeControlSounds.Any())
				Game.Sound.Play(SoundType.World, info.RevokeControlSounds.Random(self.World.SharedRandom), self.CenterPosition);

			self.World.AddFrameEndTask(_ => controlChanging = false);
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
			if (!controlChanging)
				UnlinkMaster(self, Master);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (Master != null)
				RevokeMindControl(self);
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
