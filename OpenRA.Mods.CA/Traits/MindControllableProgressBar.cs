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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[RequireExplicitImplementation]
	public interface IMindControlProgressWatcher
	{
		void Update(Actor self, Actor captor, Actor target, int progress, int total);
	}

	[Desc("Visualize capture progress.")]
	class MindControllableProgressBarInfo : ConditionalTraitInfo, Requires<MindControllableInfo>
	{
		public readonly Color Color = Color.Orange;

		public override object Create(ActorInitializer init) { return new MindControllableProgressBar(init.Self, this); }
	}

	class MindControllableProgressBar : ConditionalTrait<MindControllableProgressBarInfo>, ISelectionBar, IMindControlProgressWatcher, INotifyOwnerChanged
	{
		Dictionary<Actor, (int Current, int Total)> progress = new Dictionary<Actor, (int, int)>();

		public MindControllableProgressBar(Actor self, MindControllableProgressBarInfo info)
			: base(info) { }

		void IMindControlProgressWatcher.Update(Actor self, Actor captor, Actor target, int current, int total)
		{
			if (IsTraitDisabled || self != target)
				return;

			if (total == 0)
				progress.Remove(captor);
			else
				progress[captor] = (current, total);
		}

		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled || !progress.Any())
				return 0f;

			return progress.Values.Max(p => (float)p.Current / p.Total);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			progress = new Dictionary<Actor, (int, int)>();
		}

		Color ISelectionBar.GetColor() { return Info.Color; }
		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }
	}
}
