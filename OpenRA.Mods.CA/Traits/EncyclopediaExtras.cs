#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("To override encyclopedia preview.")]
	public class EncyclopediaExtrasInfo : TraitInfo
	{
		[FluentReference]
		[Desc("If set, will be used instead of the tooltip name in encyclopedia.")]
		public readonly string Name = null;

		[Desc("If set will override the preview with this actor.")]
		public readonly string RenderPreviewActor;

		[Desc("Subfaction to disaply (with flag).")]
		public readonly string Subfaction;

		[Desc("Additional info/requirements.")]
		public readonly string AdditionalInfo;

		[Desc("If true, will not show anything for the production info.")]
		public readonly bool HideNotProducible = false;

		[FluentReference]
		[Desc("If no Buildable Description exists, this will be shown instead.")]
		public readonly string Description = "";

		[Desc("Actor name this entry is a variant of (e.g., 'IFV'). Hides entry from main list.")]
		public readonly string VariantOf = null;

		[Desc("Group name for variant dropdown (e.g., 'Allies Infantry').")]
		public readonly string VariantGroup = null;

		public override object Create(ActorInitializer init) { return new EncyclopediaExtras(init, this); }
	}

	public class EncyclopediaExtras
	{
		public EncyclopediaExtras(ActorInitializer init, EncyclopediaExtrasInfo info) { }
	}
}
