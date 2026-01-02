#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.CA.Widgets;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public class EncyclopediaLogicCA : ChromeLogic
	{
		[FluentReference("prerequisites")]
		const string Requires = "label-requires";

		readonly World world;
		readonly ModData modData;
		readonly Dictionary<ActorInfo, EncyclopediaInfo> info = new();

		readonly ScrollPanelWidget descriptionPanel;
		readonly LabelWidget titleLabel;
		readonly LabelWidget descriptionLabel;
		readonly LinkableLabelWidget prerequisitesLabel;
		readonly SpriteFont descriptionFont;
		readonly Widget actorDetailsContainer;

		readonly ScrollPanelWidget actorList;
		readonly ScrollItemWidget headerTemplate;
		readonly ScrollItemWidget template;
		readonly BackgroundWidget previewBackground;
		readonly ActorPreviewCAWidget previewWidget;

		readonly Widget tabContainer;
		readonly ButtonWidget tabTemplate;
		readonly List<ButtonWidget> categoryTabs = new();

		readonly SpriteWidget portraitWidget;
		readonly Sprite portraitSprite;
		readonly Png defaultPortrait;

		readonly Widget productionContainer;
		readonly LabelWidget productionCost;
		readonly LabelWidget productionTime;
		readonly Widget productionPowerIcon;
		readonly LabelWidget productionPower;
		readonly Widget armorTypeIcon;
		readonly LabelWidget armorTypeLabel;
		readonly List<Sheet> sheets = new();

		// Tooltip extras widgets
		readonly LabelWidget strengthsLabel;
		readonly LabelWidget weaknessesLabel;
		readonly LinkableLabelWidget attributesLabel;
		readonly LinkableLabelWidget encyclopediaDescriptionLabel;

		// Entry lookup for cross-reference navigation (by actor name, case-insensitive)
		readonly Dictionary<string, ActorInfo> entryLookupByActorName = new(StringComparer.OrdinalIgnoreCase);

		// Prerequisite provider lookup - maps faction -> prerequisite name -> actor name
		readonly Dictionary<string, Dictionary<string, string>> prerequisiteProvidersByFaction = new(StringComparer.OrdinalIgnoreCase);

		// Subfaction widgets
		readonly LabelWidget subfactionLabel;
		readonly ImageWidget subfactionFlagImage;

		// Additional info widget
		readonly LinkableLabelWidget additionalInfoLabel;

		// Build icon widget
		readonly SpriteWidget buildIconWidget;

		// Variant dropdown widget
		readonly DropDownButtonWidget variantDropdown;

		// Variant lookup - maps parent actor name to list of variant actors (case-insensitive)
		readonly Dictionary<string, List<ActorInfo>> variantsByParent = new(StringComparer.OrdinalIgnoreCase);

		// Variant group order - tracks order groups were first encountered during file scan
		readonly Dictionary<string, int> variantGroupOrder = new();

		// Currently selected variant (if any)
		ActorInfo selectedVariant;

		// Folder structure tracking
		readonly Dictionary<string, FolderNode> folderNodes = new();
		readonly Dictionary<string, bool> folderExpanded = new();

		// Remember last selected actor for each top-level category
		readonly Dictionary<string, ActorInfo> lastSelectedActorByCategory = new();

		WAngle currentFacing = new WAngle(384);

		ActorInfo selectedActor;
		ActorInfo renderActor;
		EncyclopediaExtrasInfo encyclopediaExtrasInfo;

		string currentCategoryPath;
		string selectedTopLevelCategory;
		ScrollItemWidget firstItem;

		Dictionary<string, FactionInfo> factions = new();

		// Helper class to represent folder hierarchy
		class FolderNode
		{
			public string Name;
			public string FullPath;
			public List<FolderNode> Children = new();
			public List<ActorInfo> Actors = new();
			public FolderNode Parent;
			public int Depth;
		}

		[ObjectCreator.UseCtor]
		public EncyclopediaLogicCA(Widget widget, World world, ModData modData, Action onExit)
		{
			this.world = world;
			this.modData = modData;

			actorList = widget.Get<ScrollPanelWidget>("ACTOR_LIST");

			headerTemplate = widget.Get<ScrollItemWidget>("HEADER");
			template = widget.Get<ScrollItemWidget>("TEMPLATE");

			tabContainer = widget.Get("ENCYCLOPEDIA_TABS");
			tabTemplate = widget.Get<ButtonWidget>("ENCYCLOPEDIA_TAB");

			widget.Get("ACTOR_INFO").IsVisible = () => selectedActor != null;

			previewBackground = widget.GetOrNull<BackgroundWidget>("ACTOR_BG");
			previewWidget = widget.Get<ActorPreviewCAWidget>("ACTOR_PREVIEW");
			previewBackground.IsVisible = () => selectedActor != null &&
				selectedActor.TraitInfos<IRenderActorPreviewSpritesInfo>().Count > 0;

			descriptionPanel = widget.Get<ScrollPanelWidget>("ACTOR_DESCRIPTION_PANEL");
			titleLabel = descriptionPanel.GetOrNull<LabelWidget>("ACTOR_TITLE");
			actorDetailsContainer = descriptionPanel.Get("ACTOR_DETAILS");
			descriptionLabel = actorDetailsContainer.Get<LabelWidget>("ACTOR_DESCRIPTION");
			prerequisitesLabel = actorDetailsContainer.Get<LinkableLabelWidget>("ACTOR_PREREQUISITES");
			descriptionFont = Game.Renderer.Fonts[descriptionLabel.Font];

			// Wire up link click handler for prerequisites cross-references
			prerequisitesLabel.OnLinkClicked = NavigateToEntry;
			prerequisitesLabel.IsValidLink = IsValidEntryLink;
			prerequisitesLabel.ResolveDisplayText = ResolveActorDisplayName;

			portraitWidget = widget.GetOrNull<SpriteWidget>("ACTOR_PORTRAIT");
			if (portraitWidget != null)
			{
				defaultPortrait = new Png(modData.DefaultFileSystem.Open("encyclopedia/default.png"));
				var spriteBounds = new Rectangle(0, 0, defaultPortrait.Width, defaultPortrait.Height);
				var sheet = new Sheet(SheetType.BGRA, spriteBounds.Size.NextPowerOf2());
				sheets.Add(sheet);
				sheet.CreateBuffer();
				sheet.GetTexture().ScaleFilter = TextureScaleFilter.Linear;
				portraitSprite = new Sprite(sheet, spriteBounds, TextureChannel.RGBA);
				portraitWidget.GetSprite = () => portraitSprite;
			}

			actorList.RemoveChildren();

			productionContainer = descriptionPanel.GetOrNull("ACTOR_PRODUCTION");
			productionCost = productionContainer?.Get<LabelWidget>("COST");
			productionTime = productionContainer?.Get<LabelWidget>("TIME");
			productionPowerIcon = productionContainer?.Get("POWER_ICON");
			productionPower = productionContainer?.Get<LabelWidget>("POWER");
			armorTypeIcon = productionContainer?.Get("ARMOR_TYPE_ICON");
			armorTypeLabel = productionContainer?.Get<LabelWidget>("ARMOR_TYPE");

			strengthsLabel = actorDetailsContainer.Get<LabelWidget>("STRENGTHS");
			weaknessesLabel = actorDetailsContainer.Get<LabelWidget>("WEAKNESSES");
			attributesLabel = actorDetailsContainer.Get<LinkableLabelWidget>("ATTRIBUTES");

			// Wire up link click handler for Attributes cross-references
			attributesLabel.OnLinkClicked = NavigateToEntry;
			attributesLabel.IsValidLink = IsValidEntryLink;
			attributesLabel.ResolveDisplayText = ResolveActorDisplayName;

			encyclopediaDescriptionLabel = actorDetailsContainer.Get<LinkableLabelWidget>("ENCYCLOPEDIA_DESCRIPTION");

			// Wire up link click handler for cross-references
			encyclopediaDescriptionLabel.OnLinkClicked = NavigateToEntry;
			encyclopediaDescriptionLabel.IsValidLink = IsValidEntryLink;
			encyclopediaDescriptionLabel.ResolveDisplayText = ResolveActorDisplayName;

			subfactionLabel = actorDetailsContainer.GetOrNull<LabelWidget>("SUBFACTION");
			subfactionFlagImage = actorDetailsContainer.GetOrNull<ImageWidget>("SUBFACTION_FLAG");

			additionalInfoLabel = actorDetailsContainer.GetOrNull<LinkableLabelWidget>("ADDITIONAL_INFO");

			// Wire up link click handler for AdditionalInfo cross-references
			if (additionalInfoLabel != null)
			{
				additionalInfoLabel.OnLinkClicked = NavigateToEntry;
				additionalInfoLabel.IsValidLink = IsValidEntryLink;
				additionalInfoLabel.ResolveDisplayText = ResolveActorDisplayName;
			}

			buildIconWidget = widget.GetOrNull<SpriteWidget>("BUILD_ICON");

			variantDropdown = widget.GetOrNull<DropDownButtonWidget>("VARIANT_DROPDOWN");

			foreach (var actor in modData.DefaultRules.Actors.Values)
			{
				var statistics = actor.TraitInfoOrDefault<UpdatesPlayerStatisticsInfo>();
				if (statistics != null && !string.IsNullOrEmpty(statistics.OverrideActor))
					continue;

				var encyclopedia = actor.TraitInfoOrDefault<EncyclopediaInfo>();
				if (encyclopedia == null)
					continue;

				info.Add(actor, encyclopedia);
			}

			foreach (var faction in world.Map.Rules.Actors[SystemActors.World].TraitInfos<FactionInfo>().Where(f => f.Selectable))
				factions.Add(faction.InternalName, faction);

			// Build variant lookup - find all actors that are variants of other actors
			BuildVariantLookup();

			// Build folder hierarchy
			BuildFolderHierarchy();

			// Build lookup dictionary for entry navigation
			BuildEntryLookup();

			// Create tabs for top-level categories
			CreateCategoryTabs();

			// Create the UI from the hierarchy
			CreateFolderStructure();

			widget.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				if (world.Type == WorldType.Shellmap)
					Game.Disconnect();

				Ui.CloseWindow();
				onExit();
			};
		}

		void SelectFirstItem()
		{
			// Find the first available actor in the first top-level category
			var firstCategory = folderNodes.Values
				.Where(n => n.Parent == null && !string.IsNullOrEmpty(n.Name))
				.OrderBy(n => GetTopLevelCategorySortOrder(n.Name))
				.FirstOrDefault();

			if (firstCategory != null)
			{
				// Look for actors directly in the top-level category first
				if (firstCategory.Actors.Count > 0)
				{
					SelectActor(firstCategory.Actors[0], firstCategory.FullPath);
					return;
				}

				// Then look in child categories (ordered by sort order)
				foreach (var childCategory in firstCategory.Children.OrderBy(n => GetSecondLevelCategorySortOrder(n.Name)))
				{
					if (childCategory.Actors.Count > 0)
					{
						SelectActor(childCategory.Actors[0], childCategory.FullPath);
						return;
					}

					// Check deeper levels recursively
					var firstActorInSubcategory = GetFirstActorFromAnyNode(childCategory);
					if (firstActorInSubcategory.actor != null)
					{
						SelectActor(firstActorInSubcategory.actor, firstActorInSubcategory.categoryPath);
						return;
					}
				}
			}
		}

		(ActorInfo actor, string categoryPath) GetFirstActorFromAnyNode(FolderNode node)
		{
			if (node.Actors.Count > 0)
				return (node.Actors[0], node.FullPath);

			foreach (var child in node.Children)
			{
				var (actor, categoryPath) = GetFirstActorFromAnyNode(child);
				if (actor != null)
					return (actor, categoryPath);
			}

			return (null, null);
		}

		void BuildVariantLookup()
		{
			foreach (var actorInfo in info)
			{
				var actor = actorInfo.Key;
				var extras = actor.TraitInfoOrDefault<EncyclopediaExtrasInfo>();

				if (extras?.VariantOf != null)
				{
					if (!variantsByParent.ContainsKey(extras.VariantOf))
						variantsByParent[extras.VariantOf] = new List<ActorInfo>();

					variantsByParent[extras.VariantOf].Add(actor);

					// Track group order based on first encounter during file scan (only for non-null groups)
					if (extras.VariantGroup != null && !variantGroupOrder.ContainsKey(extras.VariantGroup))
						variantGroupOrder[extras.VariantGroup] = variantGroupOrder.Count;
				}
			}
		}

		void BuildEntryLookup()
		{
			foreach (var actor in modData.DefaultRules.Actors.Values)
			{
				var encyclopedia = actor.TraitInfoOrDefault<EncyclopediaInfo>();
				if (encyclopedia == null)
					continue;

				// Add to actor name lookup (actor names are unique)
				entryLookupByActorName[actor.Name] = actor;

				// Build prerequisite provider lookup for each faction this actor belongs to
				if (!string.IsNullOrEmpty(encyclopedia.Category))
				{
					var factions = GetFactionsFromCategory(encyclopedia.Category);
					var providesPrereqs = actor.TraitInfos<ProvidesPrerequisiteInfo>();

					foreach (var faction in factions)
					{
						if (!prerequisiteProvidersByFaction.ContainsKey(faction))
							prerequisiteProvidersByFaction[faction] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

						foreach (var provides in providesPrereqs)
						{
							if (!string.IsNullOrEmpty(provides.Prerequisite) &&
								!prerequisiteProvidersByFaction[faction].ContainsKey(provides.Prerequisite))
							{
								prerequisiteProvidersByFaction[faction][provides.Prerequisite] = actor.Name;
							}
						}
					}
				}
			}
		}

		static IEnumerable<string> GetFactionsFromCategory(string category)
		{
			if (string.IsNullOrEmpty(category))
				yield break;

			foreach (var cat in category.Split(';'))
				yield return cat.Trim().Split('/')[0];
		}

		bool IsValidEntryLink(string actorName)
		{
			return entryLookupByActorName.ContainsKey(actorName);
		}

		/// <summary>
		/// Resolves an actor name to its display name (from TooltipInfo).
		/// Returns null if the actor doesn't exist or has no tooltip.
		/// </summary>
		string ResolveActorDisplayName(string actorName)
		{
			if (!entryLookupByActorName.TryGetValue(actorName, out var actor))
				return null;

			var tooltip = actor.TraitInfos<TooltipInfo>().FirstOrDefault();
			if (tooltip == null || string.IsNullOrEmpty(tooltip.Name))
				return null;

			return FluentProvider.GetMessage(tooltip.Name);
		}

		void NavigateToEntry(string actorName)
		{
			if (!entryLookupByActorName.TryGetValue(actorName, out var actor))
				return;

			var encyclopedia = actor.TraitInfoOrDefault<EncyclopediaInfo>();
			if (encyclopedia == null)
				return;

			// Switch to correct category tab and select actor
			var categoryPath = encyclopedia.Category;
			if (!string.IsNullOrEmpty(categoryPath))
			{
				var topCategory = categoryPath.Split('/')[0];

				// Find and activate the correct tab
				foreach (var tab in categoryTabs)
				{
					if (tab.Text == topCategory)
					{
						tab.OnClick();
						break;
					}
				}
			}

			SelectActor(actor, categoryPath);
		}

		void BuildFolderHierarchy()
		{
			// Group actors by their category paths (actors can have multiple categories)
			var actorsByCategory = new Dictionary<string, List<ActorInfo>>();

			foreach (var actorInfo in info)
			{
				var actor = actorInfo.Key;
				var encyclopedia = actorInfo.Value;
				var categories = encyclopedia.Category ?? "";

				// Skip variants - they are accessed via dropdown only
				var extras = actor.TraitInfoOrDefault<EncyclopediaExtrasInfo>();
				if (extras?.VariantOf != null)
					continue;

				// Split by semicolon to allow multiple categories per actor
				var categoryPaths = ParseCategoryPaths(categories);

				// Skip actors without categories - they should not be shown
				if (categoryPaths.Length == 0)
					continue;

				// Add actor to each category
				foreach (var category in categoryPaths)
				{
					if (!actorsByCategory.ContainsKey(category))
						actorsByCategory[category] = new List<ActorInfo>();

					actorsByCategory[category].Add(actor);
				}
			}

			// Sort actors within each category by their Order
			foreach (var category in actorsByCategory.Keys.ToList())
			{
				actorsByCategory[category] = actorsByCategory[category]
					.OrderBy(a => a.TraitInfoOrDefault<BuildableInfo>()?.BuildPaletteOrder ?? 9999)
					.ToList();
			}

			// Create folder nodes for each unique path
			foreach (var category in actorsByCategory.Keys)
			{
				var pathParts = category.Split('/');
				var currentPath = "";
				FolderNode parentNode = null;

				for (int i = 0; i < pathParts.Length; i++)
				{
					var part = pathParts[i];
					if (i > 0)
						currentPath += "/";
					currentPath += part;

					var node = GetOrCreateFolderNode(part, currentPath, parentNode, i);
					parentNode = node;
				}

				// Add actors to the final folder
				if (parentNode != null)
				{
					parentNode.Actors.AddRange(actorsByCategory[category]);
				}
			}

			// Log final folder structure
			var rootCategories = folderNodes.Values.Where(n => n.Parent == null && !string.IsNullOrEmpty(n.Name)).ToList();
		}

		FolderNode GetOrCreateFolderNode(string name, string fullPath, FolderNode parent, int depth)
		{
			if (folderNodes.TryGetValue(fullPath, out var existingNode))
				return existingNode;

			var node = new FolderNode
			{
				Name = name,
				FullPath = fullPath,
				Parent = parent,
				Depth = depth
			};

			folderNodes[fullPath] = node;

			// Initialize folder as collapsed by default
			folderExpanded[fullPath] = false;

			if (parent != null)
				parent.Children.Add(node);

			return node;
		}

		void CreateFolderStructure()
		{
			// Find the selected top-level category node
			var selectedRootNode = folderNodes.Values.FirstOrDefault(n => n.FullPath == selectedTopLevelCategory);

			// If no selected category, try to select the first available one
			if (selectedRootNode == null)
			{
				var firstCategory = folderNodes.Values
					.Where(n => n.Parent == null && !string.IsNullOrEmpty(n.Name))
					.OrderBy(n => GetTopLevelCategorySortOrder(n.Name))
					.FirstOrDefault();
				if (firstCategory != null)
				{
					selectedTopLevelCategory = firstCategory.FullPath;
					selectedRootNode = firstCategory;
				}
			}

			if (selectedRootNode != null)
			{
				// Show only the children of the selected category, not the category itself
				foreach (var childNode in selectedRootNode.Children.OrderBy(n => GetSecondLevelCategorySortOrder(n.Name)))
				{
					CreateFolderItems(childNode);
				}

				// Also show actors directly under the selected category
				foreach (var actor in selectedRootNode.Actors)
				{
					var item = CreateActorListItem(actor, selectedRootNode.FullPath, 0);

					if (firstItem == null)
						firstItem = item;

					actorList.AddChild(item);
				}
			}
		}

		void CreateFolderItems(FolderNode node)
		{
			// Create folder header - adjust depth since we're not showing top-level categories
			var displayDepth = node.Depth - 1;

			var folderHeader = ScrollItemWidget.Setup(headerTemplate, () => false, () => ToggleFolder(node.FullPath));
			folderHeader.IsHighlighted = () => folderHeader.EventBounds.Contains(Viewport.LastMousePos) && Ui.MouseOverWidget == folderHeader;
			var label = folderHeader.Get<LabelWidget>("LABEL");
			var arrowImage = folderHeader.GetOrNull<ImageWidget>("ICON");

			// Set folder name
			label.GetText = () => $"{node.Name}";
			label.Bounds.X = 24 + displayDepth * 15;

			// Update arrow direction based on expanded state
			if (arrowImage != null)
			{
				arrowImage.GetImageName = () => folderExpanded.GetValueOrDefault(node.FullPath, false) ? "down" : "right";
				arrowImage.Bounds.X = 4 + displayDepth * 15;
			}

			actorList.AddChild(folderHeader);

			// Show child folders and actors if expanded
			if (folderExpanded.GetValueOrDefault(node.FullPath, false))
			{
				// Add child folders first
				foreach (var childNode in node.Children.OrderBy(n => GetSecondLevelCategorySortOrder(n.Name)))
				{
					CreateFolderItems(childNode);
				}

				// Then add actors
				foreach (var actor in node.Actors)
				{
					var item = CreateActorListItem(actor, node.FullPath, displayDepth);

					if (firstItem == null)
						firstItem = item;

					actorList.AddChild(item);
				}
			}
		}

		void CollapseNonAncestorCategories(string targetPath)
		{
			// Get the top-level category of the target path
			var targetTopLevelCategory = targetPath.Split('/')[0];

			// Get all ancestor paths of the target
			var ancestorPaths = new HashSet<string>();
			var currentPath = targetPath;

			while (!string.IsNullOrEmpty(currentPath))
			{
				ancestorPaths.Add(currentPath);

				// Find parent path by removing the last segment
				var lastSeparator = currentPath.LastIndexOf('/');
				if (lastSeparator > 0)
					currentPath = currentPath[..lastSeparator];
				else
					break;
			}

			// Collapse all expanded folders in the same top-level category that are not ancestors of the target
			var foldersToCollapse = new List<string>();
			foreach (var kvp in folderExpanded)
			{
				if (kvp.Value && !ancestorPaths.Contains(kvp.Key))
				{
					// Only collapse if it's in the same top-level category
					var folderTopLevelCategory = kvp.Key.Split('/')[0];
					if (folderTopLevelCategory == targetTopLevelCategory)
					{
						foldersToCollapse.Add(kvp.Key);
					}
				}
			}

			foreach (var folder in foldersToCollapse)
			{
				folderExpanded[folder] = false;
			}
		}

		void ToggleFolder(string folderPath)
		{
			var isCurrentlyExpanded = folderExpanded.GetValueOrDefault(folderPath, false);

			if (!isCurrentlyExpanded)
			{
				// Expanding: collapse all other categories in the same top-level category that are not ancestors of this one
				CollapseNonAncestorCategories(folderPath);
			}

			// Toggle the folder state
			folderExpanded[folderPath] = !isCurrentlyExpanded;

			// Rebuild the entire list
			actorList.RemoveChildren();
			firstItem = null;
			CreateFolderStructure();

			// When expanding, auto-select the first actor in the expanded folder
			if (!isCurrentlyExpanded && folderNodes.TryGetValue(folderPath, out var node))
			{
				var firstActorResult = GetFirstActorFromAnyNode(node);
				if (firstActorResult.actor != null)
					SelectActor(firstActorResult.actor, firstActorResult.categoryPath);
			}
		}

		void SelectActor(ActorInfo actor, string categoryPath = null)
		{
			LoadExtras(actor);
			var selectedInfo = info[actor];
			selectedActor = actor;
			selectedVariant = null;
			currentCategoryPath = categoryPath;

			// Remember this actor for the current top-level category
			if (!string.IsNullOrEmpty(selectedTopLevelCategory))
			{
				lastSelectedActorByCategory[selectedTopLevelCategory] = actor;
			}

			// Setup variant dropdown
			SetupVariantDropdown(actor);

			// Update the encyclopedia color palette with the faction color
			var previewColor = GetPreviewColorFromCategory(categoryPath);
			EncyclopediaColorPalette.SetPreviewColor(previewColor);

			var previewOwner = GetPreviewOwner(selectedInfo);
			var typeDictionary = CreatePreviewTypeDictionary(previewOwner);

			if (previewBackground.IsVisible())
			{
				previewWidget.SetPreview(renderActor, typeDictionary, previewColor);
				previewWidget.GetScale = () => selectedInfo.Scale;
				buildIconWidget.Bounds.Y = previewWidget.Bounds.Bottom + 10;
			}
			else
			{
				buildIconWidget.Bounds.Y = 0;
			}

			if (portraitWidget != null)
			{
				// PERF: Load individual portrait images directly, bypassing ChromeProvider,
				// to avoid stalls when loading a single large sheet.
				// Portrait images are required to all be the same size as the "default.png" image.
				var portrait = defaultPortrait;
				if (modData.DefaultFileSystem.TryOpen($"encyclopedia/{actor.Name}.png", out var s))
				{
					var p = new Png(s);
					if (p.Width == defaultPortrait.Width && p.Height == defaultPortrait.Height)
						portrait = p;
					else
					{
						Log.Write("debug", $"Failed to parse load portrait image for {actor.Name}.");
						Log.Write("debug", $"Expected size {defaultPortrait.Width}, {defaultPortrait.Height}, but found {p.Width}, {p.Height}.");
					}
				}

				OpenRA.Graphics.Util.FastCopyIntoSprite(portraitSprite, portrait);
				portraitSprite.Sheet.CommitBufferedData();
			}

			if (titleLabel != null)
				titleLabel.Text = ActorName(modData.DefaultRules, actor.Name);

			var bi = actor.TraitInfoOrDefault<BuildableInfo>();

			if (buildIconWidget != null)
			{
				if (bi != null && !string.IsNullOrEmpty(bi.Icon))
				{
					try
					{
						var renderSprites = actor.TraitInfos<RenderSpritesInfo>().FirstOrDefault();
						if (renderSprites != null)
						{
							var iconSequence = world.Map.Sequences.GetSequence(renderSprites.Image ?? actor.Name, bi.Icon);
							var iconSprite = iconSequence.GetSprite(0);
							buildIconWidget.GetSprite = () => iconSprite;
							buildIconWidget.GetPalette = () => bi.IconPalette ?? "chrome";
							buildIconWidget.Visible = true;
						}
						else
						{
							buildIconWidget.Visible = false;
						}
					}
					catch
					{
						buildIconWidget.Visible = false;
					}
				}
				else
				{
					buildIconWidget.Visible = false;
				}
			}

			var currentY = SetupProductionContainer(actor);

			FactionInfo subfaction = null;
			var subfactionText = "";
			var subfactionHeight = 0;
			if (encyclopediaExtrasInfo != null && !string.IsNullOrEmpty(encyclopediaExtrasInfo.Subfaction) && subfactionLabel != null)
			{
				subfaction = factions[encyclopediaExtrasInfo.Subfaction];
				subfactionText = $"{FluentProvider.GetMessage(subfaction.Name)} only.";

				// var subfactionText = FluentProvider.GetMessage(SubfactionOnly, "factionName", FluentProvider.GetMessage(subfaction.Name));
				subfactionHeight = descriptionFont.Measure(subfactionText).Y;
			}

			if (subfactionLabel != null)
			{
				subfactionLabel.GetText = () => subfactionText;
				subfactionLabel.Bounds.Height = subfactionHeight;
				subfactionLabel.Visible = !string.IsNullOrEmpty(subfactionText);
			}

			if (subfactionFlagImage != null)
			{
				if (!string.IsNullOrEmpty(subfactionText))
				{
					var flagName = subfaction.InternalName;
					if (!string.IsNullOrEmpty(flagName))
					{
						subfactionFlagImage.GetImageName = () => flagName;
						subfactionFlagImage.Visible = true;
					}
					else
					{
						subfactionFlagImage.Visible = false;
					}
				}
				else
				{
					subfactionFlagImage.Visible = false;
				}
			}

			if (!string.IsNullOrEmpty(subfactionText))
			{
				if (subfactionLabel != null)
				{
					subfactionLabel.Bounds.Y = currentY;
					currentY += subfactionHeight + 8;
				}

				if (subfactionFlagImage != null && subfactionLabel != null)
				{
					subfactionFlagImage.Bounds.Y = currentY - subfactionHeight - 9;
					var textWidth = descriptionFont.Measure(subfactionText).X;
				}
			}

			var additionalInfoText = "";
			var additionalInfoHeight = 0;
			if (encyclopediaExtrasInfo != null && !string.IsNullOrEmpty(encyclopediaExtrasInfo.AdditionalInfo) && additionalInfoLabel != null)
			{
				additionalInfoText = WidgetUtilsCA.WrapTextWithIndent(
					encyclopediaExtrasInfo.AdditionalInfo.Replace("\\n", "\n"),
					additionalInfoLabel.Bounds.Width,
					descriptionFont);
				additionalInfoHeight = descriptionFont.Measure(additionalInfoText).Y;
			}

			if (additionalInfoLabel != null)
			{
				additionalInfoLabel.GetText = () => additionalInfoText;
				additionalInfoLabel.Bounds.Height = additionalInfoHeight;
				additionalInfoLabel.Visible = !string.IsNullOrEmpty(additionalInfoText);
			}

			if (!string.IsNullOrEmpty(additionalInfoText) && additionalInfoLabel != null)
			{
				additionalInfoLabel.Bounds.Y = currentY;
				currentY += additionalInfoHeight + 8;
			}

			currentY = SetupDescriptionSection(actor, currentY, showEncyclopediaDescription: true);

			actorDetailsContainer.Bounds.Height = currentY;

			descriptionPanel.Layout.AdjustChildren();

			descriptionPanel.ScrollToTop();
		}

		void SetupVariantDropdown(ActorInfo actor)
		{
			if (variantDropdown == null)
				return;

			// Check if this actor has variants
			if (!variantsByParent.TryGetValue(actor.Name, out var variants) || variants.Count == 0)
			{
				variantDropdown.IsDisabled = () => true;
				variantDropdown.GetText = () => "";
				return;
			}

			variantDropdown.IsDisabled = () => false;
			variantDropdown.GetText = () => selectedVariant != null
				? GetActorDisplayName(selectedVariant)
				: "Select variant...";

			variantDropdown.OnMouseDown = _ =>
			{
				// Include the base actor along with variants
				var allVariants = new List<ActorInfo> { actor };
				allVariants.AddRange(variants);

				// Separate variants into grouped and ungrouped
				var variantsWithGroups = allVariants
					.Select(v => new
					{
						Actor = v,
						Group = v.TraitInfoOrDefault<EncyclopediaExtrasInfo>()?.VariantGroup
					})
					.ToList();

				var hasAnyGroups = variantsWithGroups.Any(v => v.Group != null);

				ScrollItemWidget SetupItem(ActorInfo variantActor, ScrollItemWidget template)
				{
					bool IsSelected() => selectedVariant == variantActor;
					void OnClick() => SelectVariant(variantActor);

					var scrollItem = ScrollItemWidget.Setup(template, IsSelected, OnClick);
					var label = scrollItem.Get<LabelWidget>("LABEL");
					label.GetText = () => GetActorDisplayName(variantActor);
					return scrollItem;
				}

				if (!hasAnyGroups)
				{
					// No groups - use simple flat dropdown without headers (preserve YAML order)
					var itemHeight = 25;
					var totalHeight = Math.Min(allVariants.Count * itemHeight, 300) + 5;

					variantDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", totalHeight, allVariants, SetupItem);
				}
				else
				{
					// Has groups - use grouped dropdown with headers
					var groupedVariants = variantsWithGroups
						.Where(v => v.Group != null)
						.GroupBy(v => v.Group)
						.OrderBy(g => GetVariantGroupSortOrder(g.Key))
						.ToDictionary(
							g => g.Key,
							g => g.Select(v => v.Actor).AsEnumerable()
						);

					// Add ungrouped variants first (with empty key, handled specially)
					var ungrouped = variantsWithGroups.Where(v => v.Group == null).Select(v => v.Actor).ToList();
					if (ungrouped.Any())
					{
						var orderedGrouped = new Dictionary<string, IEnumerable<ActorInfo>>
						{
							{ "", ungrouped }
						};
						foreach (var kvp in groupedVariants)
							orderedGrouped[kvp.Key] = kvp.Value;
						groupedVariants = orderedGrouped;
					}

					// Calculate dropdown height
					var itemHeight = 25;
					var headerHeight = 13;
					var totalHeight = groupedVariants.Sum(g => (string.IsNullOrEmpty(g.Key) ? 0 : headerHeight) + g.Value.Count() * itemHeight) + 5;
					totalHeight = Math.Min(totalHeight, 300); // Cap at 300px

					variantDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", totalHeight, groupedVariants, SetupItem);
				}
			};
		}

		void SelectVariant(ActorInfo variant)
		{
			if (variant == null || selectedActor == null)
				return;

			selectedVariant = variant;

			// Update the preview to show the variant
			LoadExtras(variant);
			var selectedInfo = info.ContainsKey(variant) ? info[variant] : info[selectedActor];

			var previewColor = GetPreviewColorFromCategory(currentCategoryPath);
			EncyclopediaColorPalette.SetPreviewColor(previewColor);

			var previewOwner = GetPreviewOwner(selectedInfo);
			var typeDictionary = CreatePreviewTypeDictionary(previewOwner);

			if (previewBackground.IsVisible())
			{
				previewWidget.SetPreview(renderActor, typeDictionary, previewColor);
				previewWidget.GetScale = () => selectedInfo.Scale;
			}

			// Update title to show variant name
			if (titleLabel != null)
				titleLabel.Text = GetActorDisplayName(variant);

			// Update description from variant's traits
			UpdateVariantDescription(variant);
		}

		void UpdateVariantDescription(ActorInfo variant)
		{
			var currentY = SetupProductionContainer(variant);

			// Hide subfaction info for variants
			if (subfactionLabel != null)
				subfactionLabel.Visible = false;
			if (subfactionFlagImage != null)
				subfactionFlagImage.Visible = false;
			if (additionalInfoLabel != null)
				additionalInfoLabel.Visible = false;

			currentY = SetupDescriptionSection(variant, currentY, showEncyclopediaDescription: false);

			actorDetailsContainer.Bounds.Height = currentY;
			descriptionPanel.Layout.AdjustChildren();
			descriptionPanel.ScrollToTop();
		}

		int SetupProductionContainer(ActorInfo actor)
		{
			if (productionContainer == null)
				return 0;

			var currentX = 0;
			var productionContainerHeight = 0;
			const int IconWidth = 16;
			const int LabelSpacing = 4;
			const int GroupSpacing = 20;

			var costIcon = productionContainer.GetOrNull("COST_ICON");
			var timeIcon = productionContainer.GetOrNull("TIME_ICON");
			var notProducibleIcon = productionContainer.GetOrNull<ImageWidget>("NOT_PRODUCIBLE_ICON");
			var notProducibleLabel = productionContainer.GetOrNull<LabelWidget>("NOT_PRODUCIBLE");

			if (costIcon != null) costIcon.Visible = false;
			if (timeIcon != null) timeIcon.Visible = false;
			if (productionCost != null) productionCost.Visible = false;
			if (productionTime != null) productionTime.Visible = false;
			if (armorTypeIcon != null) armorTypeIcon.Visible = false;
			if (armorTypeLabel != null) armorTypeLabel.Visible = false;
			if (productionPowerIcon != null) productionPowerIcon.Visible = false;
			if (productionPower != null) productionPower.Visible = false;
			if (notProducibleIcon != null) notProducibleIcon.Visible = false;
			if (notProducibleLabel != null) notProducibleLabel.Visible = false;

			// For variants without BuildableInfo/ValuedInfo, fall back to base actor
			var bi = actor.TraitInfoOrDefault<BuildableInfo>();
			var valued = actor.TraitInfoOrDefault<ValuedInfo>();
			var actorForProduction = actor;

			if ((bi == null || valued == null) && actor != selectedActor && selectedActor != null)
			{
				// Variant doesn't have production info, use base actor
				if (bi == null)
					bi = selectedActor.TraitInfoOrDefault<BuildableInfo>();
				if (valued == null)
				{
					valued = selectedActor.TraitInfoOrDefault<ValuedInfo>();
					actorForProduction = selectedActor;
				}
			}

			var selectedInfo = info.ContainsKey(actor) ? info[actor] : info[selectedActor];

			if (bi != null && !selectedInfo.HideBuildable)
			{
				var cost = valued?.Cost ?? 0;
				if (cost > 0 && productionCost != null && costIcon != null)
				{
					var costText = cost.ToString(NumberFormatInfo.CurrentInfo);
					productionCost.Text = costText;
					costIcon.Bounds.X = currentX;
					productionCost.Bounds.X = currentX + IconWidth + LabelSpacing;
					var costWidth = Game.Renderer.Fonts[productionCost.Font].Measure(costText).X;
					currentX += IconWidth + LabelSpacing + costWidth + GroupSpacing;

					costIcon.Visible = true;
					productionCost.Visible = true;
					productionContainerHeight = descriptionFont.Measure(costText).Y;
				}

				var time = BuildTime(actorForProduction, selectedInfo.BuildableQueue);
				if (time > 0 && productionTime != null && timeIcon != null)
				{
					var timeText = WidgetUtils.FormatTime(time, world.Timestep);
					productionTime.Text = timeText;
					timeIcon.Bounds.X = currentX;
					productionTime.Bounds.X = currentX + IconWidth + LabelSpacing;
					var timeWidth = Game.Renderer.Fonts[productionTime.Font].Measure(timeText).X;
					currentX += IconWidth + LabelSpacing + timeWidth + GroupSpacing;

					timeIcon.Visible = true;
					productionTime.Visible = true;
					productionContainerHeight = Math.Max(productionContainerHeight, descriptionFont.Measure(timeText).Y);
				}
			}
			else
			{
				if (encyclopediaExtrasInfo != null && encyclopediaExtrasInfo.HideNotProducible)
				{
					productionContainer.Visible = false;
				}
				else
				{
					notProducibleIcon.Visible = true;
					notProducibleIcon.Bounds.X = currentX;
					notProducibleLabel.Visible = true;
					notProducibleLabel.Bounds.X = currentX + IconWidth + LabelSpacing;
					var notProducibleLabelWidth = Game.Renderer.Fonts[notProducibleLabel.Font].Measure(notProducibleLabel.Text).X;
					currentX += IconWidth + LabelSpacing + notProducibleLabelWidth + GroupSpacing;
					productionContainerHeight = descriptionFont.Measure(notProducibleLabel.Text).Y;
				}
			}

			if (armorTypeLabel != null && armorTypeIcon != null)
			{
				var armor = actor.TraitInfos<ArmorInfo>().FirstOrDefault();
				if (armor != null && !string.IsNullOrEmpty(armor.Type))
				{
					SelectionTooltipLogic.GetArmorTypeLabel(armorTypeLabel, actor);
					var hasArmorType = !string.IsNullOrEmpty(armorTypeLabel.Text);
					if (hasArmorType)
					{
						armorTypeIcon.Bounds.X = currentX;
						armorTypeLabel.Bounds.X = currentX + IconWidth + LabelSpacing;
						var armorWidth = Game.Renderer.Fonts[armorTypeLabel.Font].Measure(armorTypeLabel.Text).X;
						currentX += IconWidth + LabelSpacing + armorWidth + GroupSpacing;

						armorTypeIcon.Visible = true;
						armorTypeLabel.Visible = true;
						productionContainerHeight = Math.Max(productionContainerHeight, descriptionFont.Measure(armorTypeLabel.Text).Y);
					}
				}
			}

			var power = actor.TraitInfos<PowerInfo>().Where(i => i.EnabledByDefault).Sum(i => i.Amount);
			if (power != 0 && productionPower != null && productionPowerIcon != null)
			{
				var powerText = power.ToString(NumberFormatInfo.CurrentInfo);
				productionPower.Text = powerText;
				productionPowerIcon.Bounds.X = currentX;
				productionPower.Bounds.X = currentX + IconWidth + LabelSpacing;
				var powerWidth = Game.Renderer.Fonts[productionPower.Font].Measure(powerText).X;
				currentX += IconWidth + LabelSpacing + powerWidth + GroupSpacing;

				productionPowerIcon.Visible = true;
				productionPower.Visible = true;
				productionContainerHeight = Math.Max(productionContainerHeight, descriptionFont.Measure(powerText).Y);
			}

			// Only show the production container if it has any visible content
			var hasVisibleContent = (costIcon?.Visible == true) ||
				(timeIcon?.Visible == true) ||
				(armorTypeIcon?.Visible == true) ||
				(productionPowerIcon?.Visible == true) ||
				(notProducibleIcon?.Visible == true);

			productionContainer.Visible = hasVisibleContent;
			return productionContainerHeight + 10;
		}

		int SetupDescriptionSection(ActorInfo actor, int currentY, bool showEncyclopediaDescription)
		{
			// Get prerequisites and description
			var bi = actor.TraitInfoOrDefault<BuildableInfo>();
			var prerequisitesText = "";
			var descriptionText = "";

			// Get current faction for context-aware prerequisite linking
			var currentFaction = "";
			if (!string.IsNullOrEmpty(currentCategoryPath))
				currentFaction = currentCategoryPath.Split('/')[0];

			if (bi != null)
			{
				var prereqs = bi.Prerequisites
					.Where(s => !s.StartsWith('~') && !s.StartsWith('!'))
					.Select(prereqName =>
					{
						// Find the actor name that provides this prerequisite (faction-aware)
						var actorName = FindPrerequisiteActorName(prereqName, currentFaction);
						if (actorName != null)
							return $"[[{actorName}]]"; // Display name resolved by ResolveDisplayText callback
						return ActorName(modData.DefaultRules, prereqName);
					})
					.ToList();

				if (prereqs.Count != 0)
				{
					prerequisitesText = WidgetUtilsCA.WrapTextWithIndent(
						FluentProvider.GetMessage(Requires, "prerequisites", prereqs.JoinWith(", ")),
						descriptionLabel.Bounds.Width,
						descriptionFont);
				}

				if (!string.IsNullOrEmpty(bi.Description))
				{
					descriptionText = WidgetUtilsCA.WrapTextWithIndent(
						FluentProvider.GetMessage(bi.Description.Replace("\\n", "\n")),
						descriptionLabel.Bounds.Width,
						descriptionFont);
				}
			}

			var tooltipExtras = actor.TraitInfos<TooltipExtrasInfo>().FirstOrDefault(info => info.IsStandard);

			if (string.IsNullOrEmpty(descriptionText))
			{
				if (tooltipExtras != null && !string.IsNullOrEmpty(tooltipExtras.Description))
				{
					descriptionText = WidgetUtilsCA.WrapTextWithIndent(
						FluentProvider.GetMessage(tooltipExtras.Description.Replace("\\n", "\n")),
						descriptionLabel.Bounds.Width,
						descriptionFont);
				}
				else if (encyclopediaExtrasInfo != null && !string.IsNullOrEmpty(encyclopediaExtrasInfo.Description))
				{
					// Skip text with links - it will go to the LinkableLabelWidget
					if (!encyclopediaExtrasInfo.Description.Contains("[["))
					{
						descriptionText = WidgetUtilsCA.WrapTextWithIndent(
							FluentProvider.GetMessage(encyclopediaExtrasInfo.Description.Replace("\\n", "\n")),
							descriptionLabel.Bounds.Width,
							descriptionFont);
					}
				}
			}

			var prerequisitesHeight = string.IsNullOrEmpty(prerequisitesText) ? 0 : descriptionFont.Measure(prerequisitesText).Y;
			prerequisitesLabel.GetText = () => prerequisitesText;
			prerequisitesLabel.Bounds.Height = prerequisitesHeight;
			prerequisitesLabel.Visible = !string.IsNullOrEmpty(prerequisitesText);

			var descriptionHeight = string.IsNullOrEmpty(descriptionText) ? 0 : descriptionFont.Measure(descriptionText).Y;
			descriptionLabel.GetText = () => descriptionText;
			descriptionLabel.Bounds.Height = descriptionHeight;
			descriptionLabel.Visible = !string.IsNullOrEmpty(descriptionText);

			if (!string.IsNullOrEmpty(prerequisitesText))
			{
				prerequisitesLabel.Bounds.Y = currentY;
				currentY += prerequisitesHeight + 8;
			}

			if (!string.IsNullOrEmpty(descriptionText))
			{
				descriptionLabel.Bounds.Y = currentY;
				currentY += descriptionHeight + 8;
			}

			// Get strengths/weaknesses/attributes
			var strengthsText = "";
			var weaknessesText = "";
			var attributesText = "";

			if (tooltipExtras != null)
			{
				strengthsText = WidgetUtilsCA.WrapTextWithIndent(tooltipExtras.Strengths.Replace("\\n", "\n"), strengthsLabel.Bounds.Width, descriptionFont, 6);
				weaknessesText = WidgetUtilsCA.WrapTextWithIndent(tooltipExtras.Weaknesses.Replace("\\n", "\n"), weaknessesLabel.Bounds.Width, descriptionFont, 6);
				attributesText = WidgetUtilsCA.WrapTextWithIndent(tooltipExtras.Attributes.Replace("\\n", "\n"), attributesLabel.Bounds.Width, descriptionFont, 6);
			}

			if (!string.IsNullOrEmpty(strengthsText) && strengthsLabel != null)
			{
				SetupTextLabel(strengthsLabel, strengthsText, ref currentY, 0);
			}
			else if (strengthsLabel != null)
			{
				strengthsLabel.Visible = false;
			}

			if (!string.IsNullOrEmpty(weaknessesText) && weaknessesLabel != null)
			{
				SetupTextLabel(weaknessesLabel, weaknessesText, ref currentY, 0);
			}
			else if (weaknessesLabel != null)
			{
				weaknessesLabel.Visible = false;
			}

			if (!string.IsNullOrEmpty(attributesText) && attributesLabel != null)
			{
				SetupTextLabel(attributesLabel, attributesText, ref currentY, 8);
			}
			else if (attributesLabel != null)
			{
				attributesLabel.Visible = false;
			}

			// Show encyclopedia description only for base actors
			if (showEncyclopediaDescription)
			{
				var selectedInfo = info.ContainsKey(actor) ? info[actor] : null;
				var encyclopediaText = "";

				// First check EncyclopediaInfo.Description
				if (selectedInfo != null && !string.IsNullOrEmpty(selectedInfo.Description))
					encyclopediaText = WidgetUtils.WrapText(FluentProvider.GetMessage(selectedInfo.Description), descriptionLabel.Bounds.Width, descriptionFont);

				// Also check EncyclopediaExtrasInfo.Description for text with [[...]] links
				if (string.IsNullOrEmpty(encyclopediaText) && encyclopediaExtrasInfo != null
					&& !string.IsNullOrEmpty(encyclopediaExtrasInfo.Description)
					&& encyclopediaExtrasInfo.Description.Contains("[["))
				{
					encyclopediaText = WidgetUtilsCA.WrapTextWithIndent(
						FluentProvider.GetMessage(encyclopediaExtrasInfo.Description.Replace("\\n", "\n")),
						descriptionLabel.Bounds.Width,
						descriptionFont);
				}

				if (!string.IsNullOrEmpty(encyclopediaText) && encyclopediaDescriptionLabel != null)
				{
					SetupTextLabel(encyclopediaDescriptionLabel, encyclopediaText, ref currentY, 0);
				}
				else if (encyclopediaDescriptionLabel != null)
				{
					encyclopediaDescriptionLabel.Visible = false;
				}
			}
			else if (encyclopediaDescriptionLabel != null)
			{
				encyclopediaDescriptionLabel.Visible = false;
			}

			return currentY;
		}

		int GetVariantGroupSortOrder(string groupName)
		{
			return variantGroupOrder.TryGetValue(groupName, out var order) ? order : int.MaxValue;
		}

		void RotatePreview()
		{
			if (selectedActor == null)
				return;

			currentFacing -= new WAngle(16);
		}

		Player GetPreviewOwner(EncyclopediaInfo selectedInfo)
		{
			if (!string.IsNullOrEmpty(selectedInfo.PreviewOwner))
			{
				return world.Players.FirstOrDefault(p => p.InternalName == selectedInfo.PreviewOwner);
			}
			else if (world.Type == WorldType.Regular && world.LocalPlayer != null)
			{
				return world.LocalPlayer;
			}
			else
			{
				var inferredOwner = InferPreviewOwnerFromCategory(currentCategoryPath);
				if (!string.IsNullOrEmpty(inferredOwner))
					return world.Players.FirstOrDefault(p => p.InternalName == inferredOwner);
			}

			return null;
		}

		public override void Tick()
		{
			RotatePreview();
		}

		static string ActorName(Ruleset rules, string name)
		{
			if (rules.Actors.TryGetValue(name.ToLowerInvariant(), out var actor))
			{
				var actorTooltip = actor.TraitInfos<TooltipInfo>().FirstOrDefault(info => info.EnabledByDefault);
				if (actorTooltip != null)
					return FluentProvider.GetMessage(actorTooltip.Name).Replace("Research: ", "").Replace("Upgrade: ", "");
			}

			return name;
		}

		/// <summary>
		/// Finds the faction-specific building that provides a given prerequisite.
		/// Returns the actor name, or null if not found.
		/// </summary>
		string FindPrerequisiteActorName(string prereqName, string faction)
		{
			// First check if the prerequisite is a direct actor name with an encyclopedia entry
			if (modData.DefaultRules.Actors.TryGetValue(prereqName.ToLowerInvariant(), out var directActor))
			{
				if (entryLookupByActorName.ContainsKey(directActor.Name))
					return directActor.Name;
			}

			// Look up the actor that provides this prerequisite for the given faction
			if (!string.IsNullOrEmpty(faction) &&
				prerequisiteProvidersByFaction.TryGetValue(faction, out var providers) &&
				providers.TryGetValue(prereqName, out var actorName))
			{
				return actorName;
			}

			return null;
		}

		int BuildTime(ActorInfo info, string queue)
		{
			var bi = info.TraitInfoOrDefault<BuildableInfo>();

			if (bi == null)
				return 0;

			var time = bi.BuildDuration;
			if (time == -1)
			{
				var valued = info.TraitInfoOrDefault<ValuedInfo>();
				if (valued == null)
					return 0;
				else
					time = valued.Cost;
			}

			int pbi;
			if (queue != null)
			{
				var pqueue = modData.DefaultRules.Actors.Values.SelectMany(a => a.TraitInfos<ProductionQueueInfo>()
					.Where(x => x.Type == queue)).FirstOrDefault();

				pbi = pqueue?.BuildDurationModifier ?? 100;
			}
			else
			{
				var pqueue = modData.DefaultRules.Actors.Values.SelectMany(a => a.TraitInfos<ProductionQueueInfo>()
					.Where(x => bi.Queue.Contains(x.Type))).FirstOrDefault();

				pbi = pqueue?.BuildDurationModifier ?? 100;
			}

			time = time * bi.BuildDurationModifier * pbi / 10000;
			return time;
		}

		protected override void Dispose(bool disposing)
		{
			foreach (var sheet in sheets)
				sheet.Dispose();

			base.Dispose(disposing);
		}

		string InferPreviewOwnerFromCategory(string categoryPath)
		{
			if (string.IsNullOrEmpty(categoryPath))
				return null;

			var topLevelCategory = categoryPath.Split('/')[0];

			if (topLevelCategory == "Nod")
			{
				var redUnits = new[] { "amcv", "harv.td", "harv.td.upg", "enli", "rmbc", "reap", "tplr", "shad", "scrn" };
				var parts = categoryPath.Split('/');
				if (parts.Length > 1)
				{
					// Nod units use no preview owner so they appear white.
					var secondLevel = parts[1];
					if (!redUnits.Contains(selectedActor.Name) && (secondLevel == "Vehicles" || secondLevel == "Aircraft" || secondLevel == "Infantry"))
						return null;
				}

				return "Nod";
			}

			if (selectedActor.Name == "sbag" || selectedActor.Name == "fenc")
			{
				return "GDI";
			}

			return topLevelCategory switch
			{
				"Allies" => "Greece",
				"Soviets" => "USSR",
				"GDI" => "GDI",
				"Scrin" => "Scrin",
				_ => null
			};
		}

		/// <summary>
		/// Gets an arbitrary color for the preview based on faction/category.
		/// This allows coloring actors without needing a map player.
		/// </summary>
		Color GetPreviewColorFromCategory(string categoryPath)
		{
			if (string.IsNullOrEmpty(categoryPath))
				return Color.White;

			var topLevelCategory = categoryPath.Split('/')[0];

			if (topLevelCategory == "Nod")
			{
				var redUnits = new[] { "amcv", "harv.td", "harv.td.upg", "enli", "rmbc", "reap", "tplr", "shad", "scrn" };
				var parts = categoryPath.Split('/');
				if (parts.Length > 1)
				{
					// Most Nod units appear white/gray
					var secondLevel = parts[1];
					if (!redUnits.Contains(selectedActor.Name) && (secondLevel == "Vehicles" || secondLevel == "Aircraft" || secondLevel == "Infantry"))
						return Color.FromArgb(230, 230, 255); // E6E6FF
				}

				return Color.FromArgb(254, 17, 0); // FE1100
			}

			if (selectedActor.Name == "sbag" || selectedActor.Name == "fenc")
			{
				return Color.FromArgb(242, 207, 116); // F2CF74
			}

			return topLevelCategory switch
			{
				"Allies" => Color.FromArgb(153, 172, 242), // 99ACF2
				"Soviets" => Color.FromArgb(254, 17, 0), // FE1100
				"GDI" => Color.FromArgb(242, 207, 116), // F2CF74
				"Scrin" => Color.FromArgb(128, 0, 200), // 7700FF
				_ => Color.FromArgb(158, 166, 179) // 9EA6B3
			};
		}

		void CreateCategoryTabs()
		{
			// Get all top-level categories
			var topLevelCategories = folderNodes.Values
				.Where(n => n.Parent == null && !string.IsNullOrEmpty(n.Name))
				.OrderBy(n => GetTopLevelCategorySortOrder(n.Name))
				.ToList();

			// If no categories, select the first one
			if (selectedTopLevelCategory == null && topLevelCategories.Count > 0)
				selectedTopLevelCategory = topLevelCategories[0].FullPath;

			// Calculate tab width to fill available space
			var availableWidth = tabContainer.Bounds.Width;
			var tabCount = topLevelCategories.Count;
			var tabWidth = tabCount > 0 ? availableWidth / tabCount : 0;

			// Create tab buttons
			var tabX = 0;
			foreach (var category in topLevelCategories)
			{
				var tabButton = (ButtonWidget)tabTemplate.Clone();
				tabButton.Bounds.X = tabX;
				tabButton.Bounds.Width = tabWidth;
				tabButton.GetText = () => category.Name;
				tabButton.IsHighlighted = () => selectedTopLevelCategory == category.FullPath;
				tabButton.OnClick = () => SelectTopLevelCategory(category.FullPath);
				tabButton.IsVisible = () => true;

				tabContainer.AddChild(tabButton);
				categoryTabs.Add(tabButton);

				tabX += tabWidth;
			}
		}

		void SelectTopLevelCategory(string categoryPath)
		{
			selectedTopLevelCategory = categoryPath;

			// Rebuild the folder structure to show only the selected category
			actorList.RemoveChildren();
			firstItem = null;
			CreateFolderStructure();

			// Try to restore the previously selected actor for this category
			if (lastSelectedActorByCategory.TryGetValue(categoryPath, out var rememberedActor))
			{
				// Verify the actor still exists and is in the current category
				if (info.ContainsKey(rememberedActor))
				{
					var encyclopedia = info[rememberedActor];
					var categories = encyclopedia.Category ?? "";
					var categoryPaths = ParseCategoryPaths(categories);

					// Check if the remembered actor belongs to the current top-level category
					// and find the appropriate category path for this actor in the current context
					foreach (var actorCategoryPath in categoryPaths)
					{
						if (actorCategoryPath.Split('/')[0] == categoryPath)
						{
							SelectActor(rememberedActor, actorCategoryPath);
							return;
						}
					}
				}
			}
		}

		// Helper methods for category sorting
		static int GetTopLevelCategorySortOrder(string categoryName)
		{
			return categoryName switch
			{
				"Allies" => 0,
				"Soviets" => 1,
				"GDI" => 2,
				"Nod" => 3,
				"Scrin" => 4,
				_ => 1000
			};
		}

		static int GetSecondLevelCategorySortOrder(string categoryName)
		{
			return categoryName switch
			{
				"Infantry" => 0,
				"Vehicles" => 1,
				"Aircraft" => 2,
				"Buildings" => 3,
				"Defenses" => 4,
				"Naval" => 5,
				"Upgrades" => 6,
				"Support Powers" => 7,
				"Subfactions" => 8,
				"Tips" => 9,
				"Buffs" => 10,
				"Debuffs" => 11,
				"Tech Buildings" => 12,
				"Tech Units" => 13,
				_ => 1000
			};
		}

		void LoadExtras(ActorInfo actor)
		{
			encyclopediaExtrasInfo = actor.TraitInfoOrDefault<EncyclopediaExtrasInfo>();

			renderActor = encyclopediaExtrasInfo != null && !string.IsNullOrEmpty(encyclopediaExtrasInfo.RenderPreviewActor) ?
				modData.DefaultRules.Actors.GetValueOrDefault(encyclopediaExtrasInfo.RenderPreviewActor) ?? actor : actor;
		}

		static string GetActorDisplayName(ActorInfo actor)
		{
			// Check for EncyclopediaExtrasInfo.Name first
			var extras = actor.TraitInfoOrDefault<EncyclopediaExtrasInfo>();
			if (extras != null && !string.IsNullOrEmpty(extras.Name))
			{
				return FluentProvider.GetMessage(extras.Name);
			}

			// Fall back to TooltipInfo
			var name = actor.TraitInfos<TooltipInfo>().FirstOrDefault(info => info.EnabledByDefault)?.Name;
			if (!string.IsNullOrEmpty(name))
			{
				return FluentProvider.GetMessage(name).Replace("Upgrade: ", "").Replace("Research: ", "");
			}

			return "";
		}

		ScrollItemWidget CreateActorListItem(ActorInfo actor, string categoryPath, int indentLevel = 0)
		{
			var item = ScrollItemWidget.Setup(template,
				() => selectedActor != null && selectedActor.Name == actor.Name,
				() => SelectActor(actor, categoryPath));

			item.IsHighlighted = () => selectedActor != null && selectedActor.Name == actor.Name;

			var bullet = item.GetOrNull<ImageWidget>("ICON");
			if (bullet != null)
				bullet.Bounds.X = 4 + indentLevel * 15;

			var label = item.Get<LabelWithTooltipWidget>("TITLE");
			label.Bounds.X = 26 + indentLevel * 15;

			var displayName = GetActorDisplayName(actor);
			if (!string.IsNullOrEmpty(displayName))
			{
				label.GetText = () => displayName;
				WidgetUtils.TruncateLabelToTooltip(label, displayName);
			}

			return item;
		}

		TypeDictionary CreatePreviewTypeDictionary(Player previewOwner)
		{
			var typeDictionary = new TypeDictionary()
			{
				new OwnerInit(previewOwner ?? world.WorldActor.Owner),
				new FactionInit(world.WorldActor.Owner.PlayerReference.Faction),
				new DynamicFacingInit(() => currentFacing),
			};

			// Add DynamicTurretFacingInit for each turret so they rotate with the body
			foreach (var turretInfo in renderActor.TraitInfos<TurretedInfo>())
			{
				// The turret facing is relative to the body, so we add body facing + initial turret facing
				typeDictionary.Add(new DynamicTurretFacingInit(turretInfo, () => currentFacing + turretInfo.InitialFacing));
			}

			foreach (var actorPreviewInit in renderActor.TraitInfos<IActorPreviewInitInfo>())
			{
				foreach (var init in actorPreviewInit.ActorPreviewInits(renderActor, ActorPreviewType.ColorPicker))
				{
					// Skip FacingInit since we're using DynamicFacingInit
					if (init is FacingInit)
						continue;

					// Skip TurretFacingInit since we're using DynamicTurretFacingInit
					if (init is TurretFacingInit)
						continue;

					typeDictionary.Add(init);
				}
			}

			return typeDictionary;
		}

		void SetupTextLabel(LabelWidget label, string text, ref int currentY, int additionalSpacing = 8)
		{
			if (!string.IsNullOrEmpty(text) && label != null)
			{
				label.Bounds.Y = currentY;
				label.Visible = true;
				label.GetText = () => text;
				var textHeight = descriptionFont.Measure(text).Y;
				label.Bounds.Height = textHeight;
				currentY += textHeight + additionalSpacing;
			}
			else if (label != null)
			{
				label.Visible = false;
			}
		}

		static string[] ParseCategoryPaths(string categories)
		{
			return categories?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(c => c.Trim())
				.ToArray() ?? Array.Empty<string>();
		}
	}
}
