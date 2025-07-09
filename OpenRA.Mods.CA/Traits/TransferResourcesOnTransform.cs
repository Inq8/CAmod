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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class TransferResourcesOnTransformInfo : ConditionalTraitInfo, Requires<HarvesterInfo>
	{
		public override object Create(ActorInitializer init) { return new TransferResourcesOnTransform(init, this); }
	}

	public class TransferResourcesOnTransform : ConditionalTrait<TransferResourcesOnTransformInfo>, INotifyTransform
	{
		readonly IStoresResources storesResources;
		IReadOnlyDictionary<string, int> contents;

		public TransferResourcesOnTransform(ActorInitializer init, TransferResourcesOnTransformInfo info)
			: base(info)
		{
			storesResources = init.Self.TraitsImplementing<IStoresResources>().First();
		}

		void INotifyTransform.AfterTransform(Actor toActor)
		{
			var newHarvester = toActor.TraitOrDefault<Harvester>();

			if (newHarvester == null || newHarvester.IsTraitDisabled)
				return;

			foreach (var resource in contents)
			{
				var amt = resource.Value;
				while (!newHarvester.IsFull && amt-- > 0)
					newHarvester.AddResource(toActor, resource.Key);
			}
		}

		void INotifyTransform.BeforeTransform(Actor self)
		{
			if (IsTraitDisabled)
				return;

			contents = storesResources.Contents;
		}

		void INotifyTransform.OnTransform(Actor self) {}
	}
}
