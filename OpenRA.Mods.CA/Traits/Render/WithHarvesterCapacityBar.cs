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

using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Shows a bar displaying how full the harvester is.")]
	public class WithHarvesterCapacityBarInfo : TraitInfo, Requires<HarvesterInfo>
	{
		public readonly Color SelectionBarColor = Color.FromArgb(128, 200, 255);

		public readonly bool DisplayWhenFull = true;

		public override object Create(ActorInitializer init) { return new WithHarvesterCapacityBar(init, this); }
	}

	public class WithHarvesterCapacityBar : ISelectionBar
	{
        public readonly WithHarvesterCapacityBarInfo Info;
        readonly Harvester harv;

        public WithHarvesterCapacityBar(ActorInitializer init, WithHarvesterCapacityBarInfo info)
		{
            Info = info;
            harv = init.Self.Trait<Harvester>();
        }

        float ISelectionBar.GetValue()
		{
            var fullness = harv.Fullness;

            if (fullness == 100 && !Info.DisplayWhenFull)
                return 0;

            return (float)fullness / 100;
		}

        bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

        Color ISelectionBar.GetColor() { return Info.SelectionBarColor; }
	}
}
