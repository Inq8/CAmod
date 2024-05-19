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
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Projectiles
{
	public class MeteorStrike : IProjectile, ISpatiallyPartitionable
	{
		readonly Player firedBy;
		readonly Animation anim;
		readonly WeaponInfo weapon;
		readonly string weaponPalette;
		readonly string downSequence;

		readonly WPos descendSource;
		readonly WPos descendTarget;
		readonly WDist detonationAltitude;
		readonly bool removeOnDetonation;
		readonly int impactDelay;
		readonly string trailImage;
		readonly string[] trailSequences;
		readonly string trailPalette;
		readonly int trailInterval;
		readonly int trailDelay;

		WPos pos;
		int ticks, trailTicks;
		bool isLaunched;
		bool detonated;

		public MeteorStrike(Player firedBy, string image, WeaponInfo weapon, string weaponPalette, string downSequence,
			WPos targetPos, WDist detonationAltitude, bool removeOnDetonation, WDist velocity, int impactDelay,
			string trailImage, string[] trailSequences, string trailPalette, bool trailUsePlayerPalette, int trailDelay, int trailInterval)
		{
			this.firedBy = firedBy;
			this.weapon = weapon;
			this.weaponPalette = weaponPalette;
			this.downSequence = downSequence;
			this.impactDelay = impactDelay;
			this.trailImage = trailImage;
			this.trailSequences = trailSequences;
			this.trailPalette = trailPalette;
			if (trailUsePlayerPalette)
				this.trailPalette += firedBy.InternalName;

			this.trailInterval = trailInterval;
			this.trailDelay = trailDelay;
			trailTicks = trailDelay;

			var offset = new WVec(WDist.Zero, WDist.Zero, velocity * impactDelay);

			// Rotate pitch 20 degrees (counterlockwise), causing meteor to descend from the left of the target
			offset = offset.Rotate(new WRot(
				new WAngle(0),
				WAngle.FromDegrees(20),
				new WAngle(0)
			));;

			descendSource = targetPos + offset;
			descendTarget = targetPos;
			this.detonationAltitude = detonationAltitude;
			this.removeOnDetonation = removeOnDetonation;

			if (!string.IsNullOrEmpty(image))
				anim = new Animation(firedBy.World, image);

			pos = descendSource;
		}

		public void Tick(World world)
		{
			if (!isLaunched)
			{
				if (weapon.Report != null && weapon.Report.Length > 0)
					Game.Sound.Play(SoundType.World, weapon.Report, world, pos);

				if (anim != null)
				{
					anim.PlayRepeating(downSequence);
					world.ScreenMap.Add(this, pos, anim.Image);
				}

				isLaunched = true;
			}

			if (anim != null)
			{
				anim.Tick();
			}

			var isDescending = true;
			pos = WPos.LerpQuadratic(descendSource, descendTarget, WAngle.Zero, ticks, impactDelay);

			if (!string.IsNullOrEmpty(trailImage) && --trailTicks < 0)
			{
				var trailPos = WPos.LerpQuadratic(descendSource, descendTarget, WAngle.Zero, ticks - trailDelay, impactDelay);

				world.AddFrameEndTask(w => w.Add(new SpriteEffect(trailPos, w, trailImage, trailSequences.Random(world.SharedRandom),
					trailPalette)));

				trailTicks = trailInterval;
			}

			var dat = world.Map.DistanceAboveTerrain(pos);
			if (ticks == impactDelay || (isDescending && dat <= detonationAltitude))
				Explode(world, ticks == impactDelay || removeOnDetonation);

			if (anim != null)
				world.ScreenMap.Update(this, pos, anim.Image);

			ticks++;
		}

		void Explode(World world, bool removeProjectile)
		{
			if (removeProjectile)
				world.AddFrameEndTask(w => { w.Remove(this); w.ScreenMap.Remove(this); });

			if (detonated)
				return;

			var target = Target.FromPos(pos);
			var warheadArgs = new WarheadArgs
			{
				Weapon = weapon,
				Source = target.CenterPosition,
				SourceActor = firedBy.PlayerActor,
				WeaponTarget = target
			};

			weapon.Impact(target, warheadArgs);

			detonated = true;
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (!isLaunched || anim == null)
				return Enumerable.Empty<IRenderable>();

			return anim.Render(pos, wr.Palette(weaponPalette));
		}

		public float FractionComplete => ticks * 1f / impactDelay;
	}
}
