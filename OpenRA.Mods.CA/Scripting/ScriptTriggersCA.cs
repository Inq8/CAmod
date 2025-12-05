#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using Eluant;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Scripting
{
	public enum TriggerCA
	{
		OnPlayerDisconnected
	}

	[Desc("Allows map scripts to attach CA-specific triggers to this actor via the TriggerCA global.")]
	public class ScriptTriggersCAInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new ScriptTriggersCA(init.World, init.Self); }
	}

	public sealed class ScriptTriggersCA : INotifyPlayerDisconnected, INotifyActorDisposing
	{
		readonly World world;
		readonly Actor self;

		readonly List<Triggerable>[] triggerables = Exts.MakeArray(Enum.GetValues(typeof(TriggerCA)).Length, _ => new List<Triggerable>());

		readonly struct Triggerable : IDisposable
		{
			public readonly LuaFunction Function;
			public readonly ScriptContext Context;
			public readonly LuaValue Self;

			public Triggerable(LuaFunction function, ScriptContext context, Actor self)
			{
				Function = (LuaFunction)function.CopyReference();
				Context = context;
				Self = self.ToLuaValue(Context);
			}

			public void Dispose()
			{
				Function.Dispose();
				Self.Dispose();
			}
		}

		public ScriptTriggersCA(World world, Actor self)
		{
			this.world = world;
			this.self = self;
		}

		List<Triggerable> Triggerables(TriggerCA trigger)
		{
			return triggerables[(int)trigger];
		}

		public void RegisterCallback(TriggerCA trigger, LuaFunction func, ScriptContext context)
		{
			Triggerables(trigger).Add(new Triggerable(func, context, self));
		}

		public bool HasAnyCallbacksFor(TriggerCA trigger)
		{
			return Triggerables(trigger).Count > 0;
		}

		void INotifyPlayerDisconnected.PlayerDisconnected(Actor self, Player p)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggerables(TriggerCA.OnPlayerDisconnected))
			{
				try
				{
					using (var player = p.ToLuaValue(f.Context))
						f.Function.Call(player).Dispose();
				}
				catch (Exception ex)
				{
					f.Context.FatalError(ex);
					return;
				}
			}
		}

		public void Clear(TriggerCA trigger)
		{
			world.AddFrameEndTask(w =>
			{
				var triggerables = Triggerables(trigger);
				foreach (var f in triggerables)
					f.Dispose();
				triggerables.Clear();
			});
		}

		public void ClearAll()
		{
			foreach (TriggerCA t in Enum.GetValues(typeof(TriggerCA)))
				Clear(t);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			ClearAll();
		}
	}
}
