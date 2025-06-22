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
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Effects
{
	class GpsRadarDotEffect : IEffect, IEffectAnnotation
	{
		readonly Actor actor;
		readonly GpsRadarDot trait;
		readonly Animation anim;

		readonly DotState dotState;
		readonly IDefaultVisibility visibility;
		readonly IVisibilityModifier[] visibilityModifiers;

		readonly int ticksBetweenRenderChecks = 5;
		int ticksUntilRenderCheck = 0;

		class DotState
		{
			public readonly GpsRadarWatcher Watcher;
			public readonly FrozenActor FrozenActor;
			public bool Visible;
			public DotState(Actor a, GpsRadarWatcher watcher, FrozenActorLayer frozenLayer)
			{
				Watcher = watcher;
				if (frozenLayer != null)
					FrozenActor = frozenLayer.FromID(a.ActorID);
			}
		}

		public GpsRadarDotEffect(Actor actor, GpsRadarDot trait)
		{
			this.actor = actor;
			this.trait = trait;
			anim = new Animation(actor.World, trait.Info.Image);
			anim.PlayRepeating(trait.Info.Sequence);

			visibility = actor.Trait<IDefaultVisibility>();
			visibilityModifiers = actor.TraitsImplementing<IVisibilityModifier>().ToArray();
			ticksUntilRenderCheck = ticksBetweenRenderChecks;

			var renderPlayer = actor.World.RenderPlayer;
			if (renderPlayer != null)
				dotState = new DotState(actor, renderPlayer.PlayerActor.Trait<GpsRadarWatcher>(), renderPlayer.FrozenActorLayer);
		}

		bool ShouldRender(DotState state, Player toPlayer)
		{
			// Hide the indicator if the owner trait is disabled
			if (trait.IsTraitDisabled)
				return false;

			// Hide the indicator if no watchers are available and no applicable ranged GPS observers
			if (!state.Watcher.Granted && !state.Watcher.GrantedAllies && !trait.HasRangedObserver(toPlayer))
				return false;

			// Hide the indicator if a frozen actor portrait is visible
			if (state.FrozenActor != null && state.FrozenActor.HasRenderables)
				return false;

			// Hide the indicator if the unit appears to be owned by an allied player
			if (actor.EffectiveOwner != null && actor.EffectiveOwner.Owner != null &&
					toPlayer.IsAlliedWith(actor.EffectiveOwner.Owner))
				return false;

			// Hide indicator if the actor wouldn't otherwise be visible if there wasn't fog
			foreach (var visibilityModifier in visibilityModifiers)
				if (!visibilityModifier.IsVisible(actor, toPlayer))
					return false;

			// Hide the indicator behind shroud
			if (!trait.Info.VisibleInShroud && !toPlayer.Shroud.IsExplored(actor.CenterPosition))
				return false;

			return !visibility.IsVisible(actor, toPlayer);
		}

		void IEffect.Tick(World world)
		{
			// PERF: delay between checking if icon should be rendered
			if (--ticksUntilRenderCheck > 0)
				return;

			if (dotState != null && world.RenderPlayer != null)
				dotState.Visible = ShouldRender(dotState, world.RenderPlayer);

			ticksUntilRenderCheck = ticksBetweenRenderChecks;
		}

		IEnumerable<IRenderable> IEffect.Render(WorldRenderer wr)
		{
			return SpriteRenderable.None;
		}

		IEnumerable<IRenderable> IEffectAnnotation.RenderAnnotation(WorldRenderer wr)
		{
			if (actor.World.RenderPlayer == null || dotState == null || !dotState.Visible)
				return SpriteRenderable.None;

			var effectiveOwner = actor.EffectiveOwner != null && actor.EffectiveOwner.Owner != null ?
				actor.EffectiveOwner.Owner : actor.Owner;

			var palette = wr.Palette(trait.Info.IndicatorPalettePrefix + effectiveOwner.InternalName);
			var screenPos = wr.Viewport.WorldToViewPx(wr.ScreenPxPosition(actor.CenterPosition));
			return anim.RenderUI(wr, screenPos, WVec.Zero, 0, palette, 1f);
		}
	}
}
