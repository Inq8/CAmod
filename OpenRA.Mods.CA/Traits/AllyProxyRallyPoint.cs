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
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Allows allied players to set rally points on proxy actors created by CreateProxyActorForAllies.",
		"Requires AllyProxyFromSelection on the World actor.")]
	public class AllyProxyRallyPointInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new AllyProxyRallyPoint(init.Self, this); }
	}

	public class AllyProxyRallyPoint : ConditionalTrait<AllyProxyRallyPointInfo>, INotifyCreated
	{
		CreateProxyActorForAllies createProxyActorForAllies;

		public AllyProxyRallyPoint(Actor self, AllyProxyRallyPointInfo info)
			: base(info)
		{
		}

		void INotifyCreated.Created(Actor self)
		{
			createProxyActorForAllies = self.TraitOrDefault<CreateProxyActorForAllies>();
		}

		public Actor GetProxyActor(Player player)
		{
			return createProxyActorForAllies?.GetProxyForPlayer(player);
		}

		public RallyPoint GetProxyRallyPoint(Player player)
		{
			var proxy = GetProxyActor(player);
			return proxy?.TraitOrDefault<RallyPoint>();
		}
	}
}
