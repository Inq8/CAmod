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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Render
{
	public class WithChronoshiftChargePipsDecorationInfo : WithDecorationBaseInfo, Requires<PortableChronoCAInfo>
	{
		[Desc("Number of pips to display. Defaults to Cargo.MaxWeight.")]
		public readonly int PipCount = -1;

		[Desc("If non-zero, override the spacing between adjacent pips.")]
		public readonly int2 PipStride = int2.Zero;

		[Desc("Image that defines the pip sequences.")]
		public readonly string Image = "pips";

		[SequenceReference(nameof(Image))]
		[Desc("Sequence used for empty pips.")]
		public readonly string EmptySequence = "pip-empty";

		[SequenceReference(nameof(Image))]
		[Desc("Sequence used for full pips that aren't defined in CustomPipSequences.")]
		public readonly string FullSequence = "pip-green";

		[SequenceReference(nameof(Image), dictionaryReference: LintDictionaryReference.Values)]
		[Desc("Pip sequence to use for specific passenger actors.")]
		public readonly Dictionary<string, string> CustomPipSequences = new Dictionary<string, string>();

		[PaletteReference]
		public readonly string Palette = "chrome";

		[Desc("Will not show anything if total pips is less than this.")]
		public readonly int MinimumPips = 2;

		public override object Create(ActorInitializer init) { return new WithChronoshiftChargePipsDecoration(init.Self, this); }
	}

	public class WithChronoshiftChargePipsDecoration : WithDecorationBase<WithChronoshiftChargePipsDecorationInfo>
	{
		readonly PortableChronoCA pc;
		readonly Animation pips;
		readonly int pipCount;

		public WithChronoshiftChargePipsDecoration(Actor self, WithChronoshiftChargePipsDecorationInfo info)
			: base(self, info)
		{
			pc = self.Trait<PortableChronoCA>();
			pipCount = info.PipCount > 0 ? info.PipCount : pc.MaxCharges;
			pips = new Animation(self.World, info.Image);
		}

		protected override IEnumerable<IRenderable> RenderDecoration(Actor self, WorldRenderer wr, int2 screenPos)
		{
			pips.PlayRepeating(Info.EmptySequence);

			var palette = wr.Palette(Info.Palette);
			var pipSize = pips.Image.Size.XY.ToInt2();
			var pipStride = Info.PipStride != int2.Zero ? Info.PipStride : new int2(pipSize.X, 0);
			screenPos -= pipSize / 2;

			var currentAmmo = pc.Charges;
			var totalAmmo = pc.MaxCharges;

			var pipCount = Info.PipCount > 0 ? Info.PipCount : totalAmmo;

			if (pipCount < Info.MinimumPips)
				yield break;

			for (var i = 0; i < pipCount; i++)
			{
				pips.PlayRepeating(currentAmmo * pipCount > i * totalAmmo ? Info.FullSequence : Info.EmptySequence);
				yield return new UISpriteRenderable(pips.Image, self.CenterPosition, screenPos, 0, palette);

				screenPos += pipStride;
			}
		}
	}
}
