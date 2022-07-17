#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Spawns an actor that stays for a limited amount of time.",
		"CA version extends the base version, adding a target circle.")]
	public class SpawnActorPowerCAInfo : SpawnActorPowerInfo
	{
		public readonly WDist TargetCircleRange = WDist.Zero;
		public readonly Color TargetCircleColor = Color.White;
		public readonly bool TargetCircleUsePlayerColor = false;

		public override object Create(ActorInitializer init) { return new SpawnActorPowerCA(init.Self, this); }
	}

	public class SpawnActorPowerCA : SpawnActorPower
	{
		public new readonly SpawnActorPowerCAInfo Info;

		public SpawnActorPowerCA(Actor self, SpawnActorPowerCAInfo info)
			: base(self, info)
		{
			Info = info;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			Game.Sound.PlayToPlayer(SoundType.UI, manager.Self.Owner, Info.SelectTargetSound);
			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
				Info.SelectTargetSpeechNotification, self.Owner.Faction.InternalName);
			self.World.OrderGenerator = new SelectSpawnActorPowerCATarget(order, manager, this, MouseButton.Left);
		}
	}

	public class SelectSpawnActorPowerCATarget : SelectSpawnActorPowerTarget
	{
		readonly SpawnActorPowerCA power;

		public SelectSpawnActorPowerCATarget(string order, SupportPowerManager manager, SpawnActorPowerCA power, MouseButton button)
			: base(order, manager, power, button)
		{
			this.power = power;
		}

		protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
		{
			var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);

			if (power.Info.TargetCircleRange == WDist.Zero)
			{
				yield break;
			}
			else
			{
				yield return new RangeCircleAnnotationRenderable(
					world.Map.CenterOfCell(xy),
					power.Info.TargetCircleRange,
					0,
					power.Info.TargetCircleUsePlayerColor ? power.Self.Owner.Color : power.Info.TargetCircleColor,
					1,
					Color.FromArgb(96, Color.Black),
					3);
			}
		}
	}
}
