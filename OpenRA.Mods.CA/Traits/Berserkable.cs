#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Data;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("When enabled, the actor will randomly try to attack nearby other actors.")]
	public class BerserkableInfo : ConditionalTraitInfo
	{
		[Desc("Will only attack units with these target types.")]
		public readonly BitSet<TargetableType> InvalidTargets = new BitSet<TargetableType>();

		[Desc("Maximum scan range. If zero, uses the maximum range of the unit's weapons and auto-target traits.")]
		public readonly WDist MaxRange = WDist.Zero;

		public override object Create(ActorInitializer init) { return new Berserkable(init.Self, this); }
	}

	class Berserkable : ConditionalTrait<BerserkableInfo>, INotifyIdle
	{
		readonly Mobile mobile;

		public Berserkable(Actor self, BerserkableInfo info)
			: base(info)
        {
            mobile = self.TraitOrDefault<Mobile>();
        }

		void Blink(Actor self)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (self.IsInWorld)
				{
					var stop = new Order("Stop", self, false);
					foreach (var t in self.TraitsImplementing<IResolveOrder>())
						t.ResolveOrder(self, stop);
				}
			});
		}

		protected override void TraitEnabled(Actor self)
		{
			// Getting enraged cancels current activity.
			Blink(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			// Getting unraged should drop the target, too.
			Blink(self);
		}

		WDist GetScanRange(Actor self, AttackBase[] atbs)
		{
			WDist range = WDist.Zero;

			// Get max value of autotarget scan range.
			var autoTargets = self.TraitsImplementing<AutoTarget>().Where(a => !a.IsTraitDisabled).ToArray();
			foreach (var at in autoTargets)
			{
				var r = at.Info.ScanRadius;
				if (r > range.Length)
					range = WDist.FromCells(r);
			}

			// Get maxrange weapon.
			foreach (var atb in atbs)
			{
				var r = atb.GetMaximumRange();
				if (r.Length > range.Length)
					range = r;
			}

			if (Info.MaxRange.Length != 0 && Info.MaxRange.Length < range.Length)
				range = Info.MaxRange;

			return range;
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			if (IsTraitDisabled)
				return;

			var atbs = self.TraitsImplementing<AttackBase>().Where(a => !a.IsTraitDisabled && !a.IsTraitPaused).ToArray();
			if (atbs.Length == 0)
			{
				self.QueueActivity(new Wait(15));
				return;
			}

			WDist range = GetScanRange(self, atbs);

			var targets = self.World.FindActorsInCircle(self.CenterPosition, range)
				.Where(a => !a.Owner.NonCombatant
					&& a != self && a.IsTargetableBy(self)
					&& !Info.InvalidTargets.Overlaps(a.GetEnabledTargetTypes()));

			if (!targets.Any())
			{
				if (mobile != null)
					self.QueueActivity(false, new Nudge(self));

				self.QueueActivity(new Wait(15));
				return;
			}

			// Attack a random target.
			var target = Target.FromActor(targets.Random(self.World.SharedRandom));
			self.QueueActivity(atbs.First().GetAttackActivity(self, AttackSource.AutoTarget, target, true, true));
		}
	}
}
