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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Allows actor to have actors with AttachableCamera trait attached to it.")]
	public class AttachableCameraTargetInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new AttachableCameraTarget(init, this); }
	}

	public class AttachableCameraTarget : INotifyKilled, INotifyActorDisposing, INotifyVisualPositionChanged
	{
		readonly Actor self;
		readonly HashSet<Actor> cameraActors = new HashSet<Actor>();

		public AttachableCameraTarget(ActorInitializer init, AttachableCameraTargetInfo info)
		{
			self = init.Self;
		}

		void INotifyVisualPositionChanged.VisualPositionChanged(Actor self, byte oldLayer, byte newLayer)
		{
			if (!self.IsInWorld)
				return;

			var pos = self.Location;

			foreach (var cameraActor in cameraActors)
			{
				var cameraTrait = cameraActor.TraitOrDefault<AttachableCamera>();
				if (cameraTrait != null && cameraTrait.IsValid)
					cameraTrait.OnTargetMoved(pos);
			}
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			KillCameras();
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			KillCameras();
		}

		void KillCameras()
		{
			foreach (var cameraActor in cameraActors)
			{
				if (cameraActor == null || cameraActor.IsDead)
					continue;

				var cameraTrait = cameraActor.TraitOrDefault<AttachableCamera>();
				if (cameraTrait != null && cameraTrait.IsValid)
					cameraTrait.OnTargetLost();
			}
		}

		public void AttachCamera(Actor cameraActor)
		{
			var cameraTrait = cameraActor.TraitOrDefault<AttachableCamera>();
			if (cameraTrait != null && cameraTrait.IsValid)
			{
				cameraActors.Add(cameraActor);
				cameraTrait.SetTarget(this);
			}
		}

		public void DetachCamera(Actor cameraActor)
		{
			cameraActors.Remove(cameraActor);
		}
	}
}
