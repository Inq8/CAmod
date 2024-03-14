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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[RequireExplicitImplementation]
	public interface IMindControlProgressWatcher
	{
		void Update(Actor self, Actor captor, Actor target, int progress, int total, string controlType);
	}

	[Desc("Visualize capture progress.")]
	class MindControllableProgressBarInfo : ConditionalTraitInfo, Requires<MindControllableInfo>
	{
		[FieldLoader.Require]
		public readonly HashSet<string> ControlTypes = null;

		public readonly Color Color = Color.HotPink;

		public override object Create(ActorInitializer init) { return new MindControllableProgressBar(init.Self, this); }
	}

	class MindControllableProgressBar : ConditionalTrait<MindControllableProgressBarInfo>, ISelectionBar, IMindControlProgressWatcher, INotifyOwnerChanged
	{
		Dictionary<Actor, (int Current, int Total)> progress = new Dictionary<Actor, (int, int)>();

		public MindControllableProgressBar(Actor self, MindControllableProgressBarInfo info)
			: base(info) { }

		void IMindControlProgressWatcher.Update(Actor self, Actor captor, Actor target, int current, int total, string controlType)
		{
			if (IsTraitDisabled || self != target || !Info.ControlTypes.Contains(controlType))
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
