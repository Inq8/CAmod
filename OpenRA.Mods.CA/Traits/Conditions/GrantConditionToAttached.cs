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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Grants a condition to any attached actors.")]
	public class GrantConditionToAttachedInfo : ConditionalTraitInfo, Requires<AttachableToInfo>
	{
		[FieldLoader.Require]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new GrantConditionToAttached(init.Self, this); }
	}

	public class GrantConditionToAttached : ConditionalTrait<GrantConditionToAttachedInfo>, INotifyAttachedTo
	{
		public readonly new GrantConditionToAttachedInfo Info;
		Dictionary<Attachable, int> attachedTokens = new();

		public GrantConditionToAttached(Actor self, GrantConditionToAttachedInfo info)
			: base(info)
		{
			Info = info;
		}

		protected override void TraitEnabled(Actor self)
		{
			foreach (var kv in attachedTokens)
			{
				attachedTokens[kv.Key] = kv.Key.GrantConditionFromAttachedTo(Info.Condition);
			}
		}

		protected override void TraitDisabled(Actor self)
		{
			foreach (var kv in attachedTokens)
			{
				kv.Key.RevokeConditionFromAttachedTo(attachedTokens[kv.Key]);
			}
		}

		void INotifyAttachedTo.Attached(Actor self, Actor attachedActor, Attachable attachable)
		{
			if (!attachedTokens.ContainsKey(attachable))
				attachedTokens.Add(attachable, Actor.InvalidConditionToken);

			if (IsTraitDisabled)
				return;

			attachedTokens[attachable] = attachable.GrantConditionFromAttachedTo(Info.Condition);
		}

		void INotifyAttachedTo.Detached(Actor self, Actor detachedActor, Attachable attachable)
		{
			attachedTokens.Remove(attachable);

			if (IsTraitDisabled)
				return;

			if (attachedTokens.ContainsKey(attachable))
			{
				if (attachedTokens[attachable] != Actor.InvalidConditionToken)
					attachable.RevokeConditionFromAttachedTo(attachedTokens[attachable]);

				attachedTokens.Remove(attachable);
			}
		}
	}
}
