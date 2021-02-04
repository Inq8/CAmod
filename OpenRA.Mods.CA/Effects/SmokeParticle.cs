#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Effects
{
	class SmokeParticle : IEffect, ISync
	{
		readonly Actor invoker;
		readonly World world;
		readonly ISmokeParticleInfo smoke;
		readonly Animation anim;
		readonly WDist[] speed;
		readonly WDist[] gravity;
		readonly bool visibleThroughFog;
		readonly bool canDamage;
		readonly int turnRate;

		[Sync]
		WPos pos;
		int lifetime;
		int explosionInterval;

		int facing;

		public SmokeParticle(Actor invoker, ISmokeParticleInfo smoke, WPos pos, int facing = -1, bool visibleThroughFog = false)
		{
			this.invoker = invoker;
			world = invoker.World;
			this.pos = pos;
			this.smoke = smoke;
			speed = smoke.Speed;
			gravity = smoke.Gravity;
			this.visibleThroughFog = visibleThroughFog;

			this.facing = facing > -1
				? facing
				: world.SharedRandom.Next(255);

			turnRate = smoke.TurnRate;
			anim = new Animation(world, smoke.Image, () => WAngle.FromFacing(facing));
			anim.PlayRepeating(smoke.Sequences.Random(world.SharedRandom));
			world.ScreenMap.Add(this, pos, anim.Image);
			lifetime = smoke.Duration.Length == 2
				? world.SharedRandom.Next(smoke.Duration[0], smoke.Duration[1])
				: smoke.Duration[0];

			canDamage = smoke.Weapon != null;
		}

		public void Tick(World world)
		{
			if (--lifetime < 0)
			{
				world.AddFrameEndTask(w => { w.Remove(this); w.ScreenMap.Remove(this); });
				return;
			}

			anim.Tick();

			var forward = speed.Length == 2
				? world.SharedRandom.Next(speed[0].Length, speed[1].Length)
				: speed[0].Length;

			var height = gravity.Length == 2
				? world.SharedRandom.Next(gravity[0].Length, gravity[1].Length)
				: gravity[0].Length;

			var offset = new WVec(forward, 0, height);

			if (turnRate > 0)
				facing = (facing + world.SharedRandom.Next(-turnRate, turnRate)) & 0xFF;

			offset = offset.Rotate(WRot.FromFacing(facing));

			pos += offset;

			world.ScreenMap.Update(this, pos, anim.Image);

			if (canDamage && --explosionInterval < 0)
			{
				smoke.Weapon.Impact(Target.FromPos(pos), invoker);
				explosionInterval = smoke.Weapon.ReloadDelay;
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (world.FogObscures(pos) && !visibleThroughFog)
				return SpriteRenderable.None;

			return anim.Render(pos, wr.Palette(smoke.Palette));
		}
	}
}
