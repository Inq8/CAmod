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
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Selectable dropdown of prerequisites.")]
	[TraitLocation(SystemActors.Player | SystemActors.EditorPlayer)]
	public class LobbyPrerequisiteDropdownInfo : TraitInfo, ILobbyOptions, ITechTreePrerequisiteInfo
	{
		[FieldLoader.Require]
		[Desc("Internal id for this checkbox.")]
		public readonly string ID = null;

		[FieldLoader.Require]
		[FluentReference]
		[Desc("Descriptive label for this dropdown.")]
		public readonly string Label = null;

		[FluentReference]
		[Desc("Tooltip description for this dropdown.")]
		public readonly string Description = null;

		[FieldLoader.Require]
		[Desc("Default option key in the `Values` list.")]
		public readonly string Default = null;

		[FieldLoader.Require]
		[FluentReference(dictionaryReference: LintDictionaryReference.Values)]
		[Desc("Options to choose from.")]
		public readonly Dictionary<string, string> Values = null;

		[Desc("Prevent the dropdown value from being changed in the lobby.")]
		public readonly bool Locked = false;

		[Desc("Whether to display the dropdown in the lobby.")]
		public readonly bool Visible = true;

		[Desc("Display order for the dropdown in the lobby.")]
		public readonly int DisplayOrder = 0;

		public HashSet<string> Prerequisites { get { return Values.Keys.ToHashSet(); } }

		IEnumerable<string> ITechTreePrerequisiteInfo.Prerequisites(ActorInfo info) { return Prerequisites; }

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview map)
		{
			yield return new LobbyOption(map, ID, Label, Description, Visible, DisplayOrder,
				Values, Default, Locked);
		}

		public override object Create(ActorInitializer init) { return new LobbyPrerequisiteDropdown(init.Self, this); }
	}

	public class LobbyPrerequisiteDropdown : ITechTreePrerequisite, INotifyCreated
	{
		readonly LobbyPrerequisiteDropdownInfo info;
		HashSet<string> prerequisites = new HashSet<string>();

		public LobbyPrerequisiteDropdown(Actor self, LobbyPrerequisiteDropdownInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			var selectedPrerequisite = self.World.LobbyInfo.GlobalSettings
				.OptionOrDefault(info.ID, info.Default);

			prerequisites.Add(selectedPrerequisite);
		}

		IEnumerable<string> ITechTreePrerequisite.ProvidesPrerequisites => prerequisites;
	}
}
