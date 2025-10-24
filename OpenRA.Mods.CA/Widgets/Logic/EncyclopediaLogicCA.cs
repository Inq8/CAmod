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
		readonly LabelWidget prerequisitesLabel;
		readonly SpriteFont descriptionFont;
		readonly Widget actorDetailsContainer;

		readonly ScrollPanelWidget actorList;
		readonly ScrollItemWidget headerTemplate;
		readonly ScrollItemWidget template;
		readonly BackgroundWidget previewBackground;
		readonly ActorPreviewWidget previewWidget;

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
		readonly LabelWidget attributesLabel;
		readonly LabelWidget encyclopediaDescriptionLabel;

		// Subfaction widgets
		readonly LabelWidget subfactionLabel;
		readonly ImageWidget subfactionFlagImage;

		// Additional info widget
		readonly LabelWidget additionalInfoLabel;

		// Build icon widget
		readonly SpriteWidget buildIconWidget;

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
			previewWidget = widget.Get<ActorPreviewWidget>("ACTOR_PREVIEW");
			previewBackground.IsVisible = () => selectedActor != null &&
				selectedActor.TraitInfos<IRenderActorPreviewSpritesInfo>().Count > 0;

			descriptionPanel = widget.Get<ScrollPanelWidget>("ACTOR_DESCRIPTION_PANEL");
			titleLabel = descriptionPanel.GetOrNull<LabelWidget>("ACTOR_TITLE");
			actorDetailsContainer = descriptionPanel.Get("ACTOR_DETAILS");
			descriptionLabel = actorDetailsContainer.Get<LabelWidget>("ACTOR_DESCRIPTION");
			prerequisitesLabel = actorDetailsContainer.Get<LabelWidget>("ACTOR_PREREQUISITES");
			descriptionFont = Game.Renderer.Fonts[descriptionLabel.Font];

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
			attributesLabel = actorDetailsContainer.Get<LabelWidget>("ATTRIBUTES");
			encyclopediaDescriptionLabel = actorDetailsContainer.Get<LabelWidget>("ENCYCLOPEDIA_DESCRIPTION");

			subfactionLabel = actorDetailsContainer.GetOrNull<LabelWidget>("SUBFACTION");
			subfactionFlagImage = actorDetailsContainer.GetOrNull<ImageWidget>("SUBFACTION_FLAG");

			additionalInfoLabel = actorDetailsContainer.GetOrNull<LabelWidget>("ADDITIONAL_INFO");

			buildIconWidget = widget.GetOrNull<SpriteWidget>("BUILD_ICON");

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

			// Build folder hierarchy
			BuildFolderHierarchy();

			// Create tabs for top-level categories
			CreateCategoryTabs();

			// Create the UI from the hierarchy
			CreateFolderStructure();

			// Select the first item in the first category on initial load
			SelectFirstItem();

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

		void BuildFolderHierarchy()
		{
			// Group actors by their category paths (actors can have multiple categories)
			var actorsByCategory = new Dictionary<string, List<ActorInfo>>();

			foreach (var actorInfo in info)
			{
				var actor = actorInfo.Key;
				var encyclopedia = actorInfo.Value;
				var categories = encyclopedia.Category ?? "";

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

					// Only set firstItem and select actor if no actor is currently selected
					if (firstItem == null)
					{
						firstItem = item;
						if (selectedActor == null)
							SelectActor(actor, selectedRootNode.FullPath);
					}

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

					// Only set firstItem and select actor if no actor is currently selected
					if (firstItem == null)
					{
						firstItem = item;
						if (selectedActor == null)
							SelectActor(actor, node.FullPath);
					}

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
		}

		void SelectActor(ActorInfo actor, string categoryPath = null)
		{
			LoadExtras(actor);
			var selectedInfo = info[actor];
			selectedActor = actor;
			currentCategoryPath = categoryPath;

			// Remember this actor for the current top-level category
			if (!string.IsNullOrEmpty(selectedTopLevelCategory))
			{
				lastSelectedActorByCategory[selectedTopLevelCategory] = actor;
			}

			var previewOwner = GetPreviewOwner(selectedInfo);
			var typeDictionary = CreatePreviewTypeDictionary(previewOwner);

			if (previewBackground.IsVisible())
			{
				previewWidget.SetPreview(renderActor, typeDictionary);
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

			var currentY = 0;

			if (productionContainer != null)
			{
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

				if (bi != null && !selectedInfo.HideBuildable)
				{
					var cost = actor.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;
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

					var time = BuildTime(selectedActor, selectedInfo.BuildableQueue);
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
				currentY = productionContainerHeight + 10;
			}

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

			var prerequisitesText = "";
			var descriptionText = "";

			if (bi != null)
			{
				var prereqs = bi.Prerequisites
					.Select(a => ActorName(modData.DefaultRules, a))
					.Where(s => !s.StartsWith('~') && !s.StartsWith('!'))
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
					descriptionText = WidgetUtilsCA.WrapTextWithIndent(
						FluentProvider.GetMessage(encyclopediaExtrasInfo.Description.Replace("\\n", "\n")),
						descriptionLabel.Bounds.Width,
						descriptionFont);
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

			var encyclopediaText = "";
			if (selectedInfo != null && !string.IsNullOrEmpty(selectedInfo.Description))
				encyclopediaText = WidgetUtils.WrapText(FluentProvider.GetMessage(selectedInfo.Description), descriptionLabel.Bounds.Width, descriptionFont);

			if (!string.IsNullOrEmpty(encyclopediaText) && encyclopediaDescriptionLabel != null)
			{
				SetupTextLabel(encyclopediaDescriptionLabel, encyclopediaText, ref currentY, 0);
			}
			else if (encyclopediaDescriptionLabel != null)
			{
				encyclopediaDescriptionLabel.Visible = false;
			}

			actorDetailsContainer.Bounds.Height = currentY;

			descriptionPanel.Layout.AdjustChildren();

			descriptionPanel.ScrollToTop();
		}

		void RotatePreview()
		{
			if (selectedActor == null)
				return;

			currentFacing -= new WAngle(16);
			var selectedInfo = info[selectedActor];
			var previewOwner = GetPreviewOwner(selectedInfo);
			var typeDictionary = CreatePreviewTypeDictionary(previewOwner);

			if (previewBackground.IsVisible())
				previewWidget.SetPreview(renderActor, typeDictionary);
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

			return topLevelCategory switch
			{
				"Allies" => "Greece",
				"Soviets" => "USSR",
				"GDI" => "GDI",
				"Scrin" => "Scrin",
				_ => null
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
				"General" => 9,
				"Debuffs" => 10,
				"Tech Buildings" => 11,
				"Tech Units" => 12,
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
			};

			foreach (var actorPreviewInit in renderActor.TraitInfos<IActorPreviewInitInfo>())
			{
				foreach (var init in actorPreviewInit.ActorPreviewInits(renderActor, ActorPreviewType.ColorPicker))
				{
					if (init is FacingInit)
					{
						typeDictionary.Add(new FacingInit(currentFacing));
					}
					else
					{
						typeDictionary.Add(init);
					}
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
