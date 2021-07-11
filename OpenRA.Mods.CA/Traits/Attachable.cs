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
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Use on an actor to make it attachable to other actors with the AttachableTo trait.")]
	public class AttachableInfo : TraitInfo, Requires<IPositionableInfo>
	{
		public override object Create(ActorInitializer init) { return new Attachable(init, this); }
	}

	public class Attachable : INotifyKilled, INotifyActorDisposing
	{
		AttachableTo target;
		readonly Actor self;

		public Attachable(ActorInitializer init, AttachableInfo info)
		{
			self = init.Self;
		}

		public bool IsValid { get { return self != null && !self.IsDead; } }

		public void SetTarget(AttachableTo target)
		{
			this.target = target;
		}

		public void OnTargetLost()
		{
			self.Dispose();
		}

		public void OnTargetMoved(CPos pos)
		{
			var positionable = self.TraitOrDefault<IPositionable>();
			if (positionable == null)
				return;

			self.World.AddFrameEndTask(w =>
			{
				positionable.SetPosition(self, pos);
			});
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			Killed();
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			Killed();
		}

		void Killed()
		{
			target.Detach(self);
		}
	}
}
