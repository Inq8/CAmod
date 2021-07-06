#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Use on a camera trait to make it attachable to actors with the AttachableCameraTarget trait.")]
	public class AttachableCameraInfo : TraitInfo, Requires<MobileInfo>
	{
		public override object Create(ActorInitializer init) { return new AttachableCamera(init, this); }
	}

	public class AttachableCamera : INotifyKilled, INotifyActorDisposing
	{
        AttachableCameraTarget target;
		readonly AttachableCameraInfo info;
        readonly Actor self;

		public AttachableCamera(ActorInitializer init, AttachableCameraInfo info)
		{
			this.info = info;
            this.self = init.Self;
		}

        public bool IsValid { get { return self != null && !self.IsDead; } }

        public void SetTarget(AttachableCameraTarget target)
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
            target.DetachCamera(self);
        }
	}
}
