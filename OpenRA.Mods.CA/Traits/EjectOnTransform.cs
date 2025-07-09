#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Eject a ground soldier or a paratrooper while in the air.")]
	public class EjectOnTransformInfo : ConditionalTraitInfo
	{
		[ActorReference]
		[Desc("Name of the unit to eject. This actor type needs to have the Parachutable trait defined.")]
		public readonly string PilotActor = "N1";

		public override object Create(ActorInitializer init) { return new EjectOnTransform(init.Self, this); }
	}

	public class EjectOnTransform : ConditionalTrait<EjectOnTransformInfo>, INotifyTransform
	{
		public EjectOnTransform(Actor self, EjectOnTransformInfo info)
			: base(info) { }

		void INotifyTransform.BeforeTransform(Actor self)
		{
		}

		void INotifyTransform.OnTransform(Actor self)
		{
			self.World.AddFrameEndTask(w =>
			{
				var driver = self.World.CreateActor(Info.PilotActor.ToLowerInvariant(), new TypeDictionary
				{
					new LocationInit(self.Location),
					new OwnerInit(self.Owner)
				});
				var driverMobile = driver.TraitOrDefault<Mobile>();
				if (driverMobile != null)
					self.QueueActivity(false, new Nudge(self));
			});
		}

		void INotifyTransform.AfterTransform(Actor self)
		{
		}
		}
	}
