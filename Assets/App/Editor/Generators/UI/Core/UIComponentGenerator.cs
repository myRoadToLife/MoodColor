using System.IO;
using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using App.Develop.UI.Shared;
using Directory = UnityEngine.Windows.Directory; // Для ButtonPressEffect
using App.Develop.Utils.Logging;

namespace App.Editor.Generators.UI.Core
{
    public static class UIComponentGenerator
    {
        #region Base Panel Structure

        public static GameObject CreateBasePanelRoot(string name, RenderMode renderMode, int sortingOrder, Vector2 referenceResolution, float matchWidthOrHeight = 0.5f)
        {
            GameObject panelRoot = new GameObject(name);
            panelRoot.AddComponent<RectTransform>(); // Добавляем RectTransform сразу
            Canvas canvas = panelRoot.AddComponent<Canvas>();
            canvas.renderMode = renderMode;
            canvas.sortingOrder = sortingOrder;
            
            CanvasScaler scaler = panelRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = matchWidthOrHeight;
            
            panelRoot.AddComponent<GraphicRaycaster>();
            panelRoot.AddComponent<CanvasGroup>(); // Для управления прозрачностью всей панели

            RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;

            return panelRoot;
        }

        public static GameObject CreateBasePanelVisuals(GameObject panelRoot, string titleText, 
                                                        TMP_FontAsset titleFont, Color titleTextColor, float titleFontSize,
                                                        Color panelBackgroundColor, Color titleContainerColor, 
                                                        Sprite panelBackgroundSprite = null, Image.Type panelBackgroundType = Image.Type.Simple,
                                                        Sprite titleContainerSprite = null, Image.Type titleContainerType = Image.Type.Simple)
        {
            // Background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(panelRoot.transform, false);
            RectTransform backgroundRect = background.AddComponent<RectTransform>();
            Image backgroundImage = background.AddComponent<Image>();
            backgroundRect.anchorMin = Vector2.zero; 
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.pivot = new Vector2(0.5f, 0.5f); 
            backgroundRect.sizeDelta = Vector2.zero;
            backgroundImage.color = panelBackgroundColor;
            if (panelBackgroundSprite != null)
            {
                backgroundImage.sprite = panelBackgroundSprite;
                backgroundImage.type = panelBackgroundType;
            }

            // Title Container
            GameObject titleContainer = new GameObject("TitleContainer");
            titleContainer.transform.SetParent(panelRoot.transform, false);
            RectTransform titleContainerRect = titleContainer.AddComponent<RectTransform>();
            Image titleContainerImage = titleContainer.AddComponent<Image>();
            titleContainerRect.anchorMin = new Vector2(0, 1); 
            titleContainerRect.anchorMax = new Vector2(1, 1);
            titleContainerRect.pivot = new Vector2(0.5f, 1f); 
            titleContainerRect.sizeDelta = new Vector2(0, 60); // Increased height for better visual
            titleContainerImage.color = titleContainerColor;
            if (titleContainerSprite != null)
            {
                titleContainerImage.sprite = titleContainerSprite;
                titleContainerImage.type = titleContainerType;
            }

            // Title Text
            GameObject titleObject = new GameObject("TitleText");
            titleObject.transform.SetParent(titleContainer.transform, false);
            RectTransform titleRect = titleObject.AddComponent<RectTransform>();
            TextMeshProUGUI titleTextComponent = titleObject.AddComponent<TextMeshProUGUI>();
            titleRect.anchorMin = new Vector2(0.05f, 0); // Padding
            titleRect.anchorMax = new Vector2(0.95f, 1); // Padding
            titleRect.pivot = new Vector2(0.5f, 0.5f); 
            titleRect.sizeDelta = Vector2.zero;
            titleTextComponent.text = titleText;
            titleTextComponent.fontSize = titleFontSize;
            titleTextComponent.alignment = TextAlignmentOptions.Center;
            if (titleFont != null)
            {
                titleTextComponent.font = titleFont;
            }
            titleTextComponent.color = titleTextColor;
            

            // Content Area
            GameObject content = new GameObject("Content");
            content.transform.SetParent(panelRoot.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0); 
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            // Adjust sizeDelta and anchoredPosition to fit below the title
            contentRect.offsetMin = new Vector2(10, 10); // Left, Bottom padding
            contentRect.offsetMax = new Vector2(-10, -titleContainerRect.sizeDelta.y - 10); // Right, Top padding (below title)
                                                                                            // Top padding = negative (title height + spacing)
            
            // Ensure 'Content' is rendered above 'Background' but below 'TitleContainer' if they are direct children of panelRoot
            background.transform.SetAsFirstSibling();
            content.transform.SetSiblingIndex(1); 
            titleContainer.transform.SetAsLastSibling();


            return content; // Return the content container for further population
        }


        #endregion

        #region Styled Button

        public static GameObject CreateStyledButton(
            string name, string text, Transform parent,
            TMP_FontAsset font, Color textColor, float fontSize,
            Sprite backgroundSprite, Image.Type backgroundSpriteType, Color spriteTintColor,
            ColorBlock colorBlock, Vector2 size, Vector3 pressedScale,
            bool addPressEffect = true)
        {
            GameObject buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent, false);

            RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.sizeDelta = size;

            Image buttonImage = buttonGO.AddComponent<Image>();
            Button buttonComponent = buttonGO.AddComponent<Button>();

            if (backgroundSprite != null)
            {
                buttonImage.sprite = backgroundSprite;
                buttonImage.type = backgroundSpriteType;
                buttonImage.color = spriteTintColor; // Apply tint to the sprite
            }
            else
            {
                // If no sprite, the ColorBlock.normalColor will be the visible color.
                // Or, you could set a default fallback color here for buttonImage.color
                buttonImage.color = colorBlock.normalColor; 
            }
            
            buttonComponent.targetGraphic = buttonImage; // Important for color transitions
            buttonComponent.colors = colorBlock;

            if (addPressEffect)
            {
                var pressEffect = buttonGO.AddComponent<ButtonPressEffect>();
                pressEffect.PressedScale = pressedScale;
                // OriginalScale will be set in ButtonPressEffect.Awake()
            }

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonGO.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(-10, -10); // Padding for text within button

            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.enableWordWrapping = false; // Adjust if needed
            textComponent.overflowMode = TextOverflowModes.Ellipsis; // Adjust if needed


            if (font != null)
            {
                textComponent.font = font;
            }
            textComponent.color = textColor;

            return buttonGO;
        }

        #endregion

        #region Styled Toggle

        public static GameObject CreateStyledToggle(
            string name, string labelTextContent, Transform parent,
            TMP_FontAsset font, Color labelColor, float labelFontSize,
            Sprite backgroundSprite, Image.Type backgroundSpriteType, Color backgroundSpriteColor,
            Sprite checkmarkSprite, Color checkmarkColor,
            Vector2 toggleSize, Vector2 labelOffset)
        {
            GameObject toggleRoot = new GameObject(name);
            toggleRoot.transform.SetParent(parent, false);
            
            // Using HorizontalLayoutGroup to arrange Label and Toggle visuals
            HorizontalLayoutGroup rootLayout = toggleRoot.AddComponent<HorizontalLayoutGroup>();
            rootLayout.spacing = 5;
            rootLayout.childAlignment = TextAnchor.MiddleLeft;
            rootLayout.childControlWidth = false; 
            rootLayout.childControlHeight = false;
            rootLayout.childForceExpandWidth = false;
            rootLayout.childForceExpandHeight = false;
            // Add ContentSizeFitter to make the root object wrap its content
            ContentSizeFitter rootFitter = toggleRoot.AddComponent<ContentSizeFitter>();
            rootFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;


            // Label
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(toggleRoot.transform, false);
            RectTransform labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchoredPosition = labelOffset;

            TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = labelTextContent;
            labelTMP.fontSize = labelFontSize;
            if (font != null) labelTMP.font = font;
            labelTMP.color = labelColor;
            labelTMP.alignment = TextAlignmentOptions.Left; 

            LayoutElement labelLayoutElement = labelGO.AddComponent<LayoutElement>();
            labelLayoutElement.preferredHeight = toggleSize.y; // Match height of toggle part


            // Toggle Visuals (the interactive part)
            GameObject toggleVisualsGO = new GameObject("ToggleVisuals");
            toggleVisualsGO.transform.SetParent(toggleRoot.transform, false);
            RectTransform toggleVisualsRect = toggleVisualsGO.AddComponent<RectTransform>();
            toggleVisualsRect.sizeDelta = toggleSize;
            
            LayoutElement toggleVisualsLayoutElement = toggleVisualsGO.AddComponent<LayoutElement>();
            toggleVisualsLayoutElement.preferredWidth = toggleSize.x;
            toggleVisualsLayoutElement.preferredHeight = toggleSize.y;

            Toggle toggleComponent = toggleVisualsGO.AddComponent<Toggle>();

            // Background for the Toggle
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(toggleVisualsGO.transform, false);
            RectTransform bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; 
            bgRect.anchorMax = Vector2.one; 
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = bgGO.AddComponent<Image>();
            if (backgroundSprite != null)
            {
                bgImage.sprite = backgroundSprite;
                bgImage.type = backgroundSpriteType;
            }
            bgImage.color = backgroundSpriteColor;
            toggleComponent.targetGraphic = bgImage;

            // Checkmark / Knob
            GameObject checkmarkGO = new GameObject("Checkmark");
            checkmarkGO.transform.SetParent(toggleVisualsGO.transform, false); // Child of ToggleVisuals
            RectTransform checkmarkRect = checkmarkGO.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = Vector2.zero; 
            checkmarkRect.anchorMax = Vector2.one; 
            checkmarkRect.sizeDelta = new Vector2(-toggleSize.x * 0.2f, -toggleSize.y * 0.2f); // Slightly smaller than bg
            Image checkmarkImage = checkmarkGO.AddComponent<Image>();
            if (checkmarkSprite != null)
            {
                checkmarkImage.sprite = checkmarkSprite;
            }
            checkmarkImage.color = checkmarkColor;
            toggleComponent.graphic = checkmarkImage;
            
            toggleComponent.isOn = true; // Default state

            return toggleRoot;
        }

        #endregion

        #region Styled Dropdown

        public static GameObject CreateStyledDropdown(
            string name, string labelTextContent, string[] options, Transform parent,
            TMP_FontAsset globalFont, // Font for label, caption, and items
            Color labelTextColor, float labelFontSize,
            Color captionTextColor, float captionFontSize,
            Color itemTextColor, float itemFontSize,
            Sprite mainBackgroundSprite, Image.Type mainBackgroundSpriteType, Color mainBackgroundColor,
            Sprite arrowSprite, Color arrowColor,
            Sprite templateBackgroundSprite, Image.Type templateBackgroundSpriteType, Color templateBackgroundColor,
            Sprite itemBackgroundSprite, Image.Type itemBackgroundSpriteType, Color itemBackgroundColor, // For item's own background
            Sprite itemCheckmarkSprite, Color itemCheckmarkColor,
            Vector2 dropdownSize, float labelToDropdownSpacing = 5f)
        {
            GameObject dropdownRoot = new GameObject(name);
            dropdownRoot.transform.SetParent(parent, false);

            HorizontalLayoutGroup rootLayout = dropdownRoot.AddComponent<HorizontalLayoutGroup>();
            rootLayout.spacing = labelToDropdownSpacing;
            rootLayout.childAlignment = TextAnchor.MiddleLeft;
            ContentSizeFitter rootFitter = dropdownRoot.AddComponent<ContentSizeFitter>();
            rootFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            rootFitter.verticalFit = ContentSizeFitter.FitMode.MinSize; // Or PreferredSize

            // Label (Optional based on labelTextContent)
            if (!string.IsNullOrEmpty(labelTextContent))
            {
                GameObject labelGO = new GameObject("Label");
                labelGO.transform.SetParent(dropdownRoot.transform, false);
                TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
                labelTMP.text = labelTextContent;
                labelTMP.fontSize = labelFontSize;
                if (globalFont != null) labelTMP.font = globalFont;
                labelTMP.color = labelTextColor;
                labelTMP.alignment = TextAlignmentOptions.Left;
                LayoutElement labelLayout = labelGO.AddComponent<LayoutElement>();
                labelLayout.minHeight = dropdownSize.y; // Align height with dropdown
            }

            // Dropdown Main GameObject
            GameObject dropdownGO = new GameObject("DropdownComponent");
            dropdownGO.transform.SetParent(dropdownRoot.transform, false);
            RectTransform dropdownRect = dropdownGO.AddComponent<RectTransform>();
            dropdownRect.sizeDelta = dropdownSize;
            LayoutElement dropdownLayoutEl = dropdownGO.AddComponent<LayoutElement>(); // To control size in parent layout
            dropdownLayoutEl.preferredWidth = dropdownSize.x;
            dropdownLayoutEl.minHeight = dropdownSize.y;

            Image mainBgImage = dropdownGO.AddComponent<Image>();
            if (mainBackgroundSprite != null)
            {
                mainBgImage.sprite = mainBackgroundSprite;
                mainBgImage.type = mainBackgroundSpriteType;
            }
            mainBgImage.color = mainBackgroundColor;

            TMP_Dropdown dropdown = dropdownGO.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = mainBgImage;

            // Caption Text (Selected Item Text)
            GameObject captionTextGO = new GameObject("Label"); // TMP_Dropdown expects child named "Label" for caption
            captionTextGO.transform.SetParent(dropdownGO.transform, false);
            RectTransform captionTextRect = captionTextGO.AddComponent<RectTransform>();
            captionTextRect.anchorMin = Vector2.zero;
            captionTextRect.anchorMax = Vector2.one;
            captionTextRect.offsetMin = new Vector2(5, 2); // Left, Bottom padding
            captionTextRect.offsetMax = new Vector2(-25, -2); // Right (space for arrow), Top padding
            TextMeshProUGUI captionText = captionTextGO.AddComponent<TextMeshProUGUI>();
            if (globalFont != null) captionText.font = globalFont;
            captionText.color = captionTextColor;
            captionText.fontSize = captionFontSize;
            captionText.alignment = TextAlignmentOptions.Left;
            dropdown.captionText = captionText;

            // Arrow
            GameObject arrowGO = new GameObject("Arrow"); // TMP_Dropdown expects child "Arrow"
            arrowGO.transform.SetParent(dropdownGO.transform, false);
            RectTransform arrowRect = arrowGO.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f); 
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.pivot = new Vector2(1, 0.5f);
            arrowRect.sizeDelta = new Vector2(Mathf.Min(20, dropdownSize.y * 0.8f), Mathf.Min(20, dropdownSize.y * 0.8f)); // Adaptive size
            arrowRect.anchoredPosition = new Vector2(-5, 0); // Right padding
            Image arrowImage = arrowGO.AddComponent<Image>();
            if (arrowSprite != null) arrowImage.sprite = arrowSprite;
            arrowImage.color = arrowColor;
            // dropdown.arrowImage = arrowImage; // Not directly settable, relies on child "Arrow" with Image.

            // Template (The list that drops down)
            GameObject templateGO = new GameObject("Template"); // TMP_Dropdown expects "Template"
            templateGO.transform.SetParent(dropdownGO.transform, false); // Should be child of DropdownComponent
            templateGO.SetActive(false);
            RectTransform templateRect = templateGO.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0); 
            templateRect.anchorMax = new Vector2(1, 0); // Anchor to bottom of main dropdown
            templateRect.pivot = new Vector2(0.5f, 1); // Pivot at top-center
            templateRect.anchoredPosition = new Vector2(0, 2); // Small offset below main
            templateRect.sizeDelta = new Vector2(0, 150); // Default height for list, width matches parent

            Image templateBgImage = templateGO.AddComponent<Image>();
            if (templateBackgroundSprite != null)
            {
                templateBgImage.sprite = templateBackgroundSprite;
                templateBgImage.type = templateBackgroundSpriteType;
            }
            templateBgImage.color = templateBackgroundColor;

            ScrollRect scrollRect = templateGO.AddComponent<ScrollRect>();
            scrollRect.scrollSensitivity = 10;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // Viewport for ScrollRect
            GameObject viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(templateGO.transform, false);
            RectTransform vpRect = viewportGO.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero; 
            vpRect.anchorMax = Vector2.one; 
            vpRect.pivot = new Vector2(0, 1); // Top-left pivot
            vpRect.sizeDelta = Vector2.zero; // No padding if no scrollbar visuals
            viewportGO.AddComponent<Mask>().showMaskGraphic = false;
            Image vpImg = viewportGO.AddComponent<Image>(); vpImg.color = Color.clear; // Transparent

            // Content for ScrollRect (holds the items)
            GameObject contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1); 
            contentRect.anchorMax = new Vector2(1, 1); // Stretch width
            contentRect.pivot = new Vector2(0.5f, 1); // Top-center pivot
            contentRect.sizeDelta = new Vector2(0, 28); // Initial height for one item

            VerticalLayoutGroup contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(2, 2, 2, 2); // Small padding
            contentLayout.childControlHeight = true; 
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false; 
            contentLayout.childForceExpandWidth = true;
            ContentSizeFitter contentFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;
            scrollRect.viewport = vpRect;
            dropdown.template = templateRect; // Assign template to dropdown

            // Item Prototype (TMP_Dropdown will clone this)
            GameObject itemGO = new GameObject("Item"); // TMP_Dropdown expects "Item"
            itemGO.transform.SetParent(contentGO.transform, false); // Child of Content
            // RectTransform itemRect = itemGO.AddComponent<RectTransform>(); // Size controlled by LayoutGroup
            LayoutElement itemLayout = itemGO.AddComponent<LayoutElement>();
            itemLayout.minHeight = Mathf.Max(25, itemFontSize + 6); // Ensure enough height for text

            Toggle itemToggle = itemGO.AddComponent<Toggle>(); // Each item is a toggle

            // Item Background
            GameObject itemBackgroundGO = new GameObject("Item Background"); // Expected by Toggle
            itemBackgroundGO.transform.SetParent(itemGO.transform, false);
            RectTransform itemBgRect = itemBackgroundGO.AddComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero; 
            itemBgRect.anchorMax = Vector2.one; 
            itemBgRect.sizeDelta = Vector2.zero;
            Image itemBgImage = itemBackgroundGO.AddComponent<Image>();
            if (itemBackgroundSprite != null)
            {
                itemBgImage.sprite = itemBackgroundSprite;
                itemBgImage.type = itemBackgroundSpriteType;
            }
            itemBgImage.color = itemBackgroundColor;
            itemToggle.targetGraphic = itemBgImage;

            // Item Checkmark
            GameObject itemCheckmarkGO = new GameObject("Item Checkmark"); // Expected by Toggle
            itemCheckmarkGO.transform.SetParent(itemGO.transform, false);
            RectTransform itemCheckmarkRect = itemCheckmarkGO.AddComponent<RectTransform>();
            // Position checkmark to the left or as desired
            itemCheckmarkRect.anchorMin = new Vector2(0, 0.5f);
            itemCheckmarkRect.anchorMax = new Vector2(0, 0.5f);
            itemCheckmarkRect.pivot = new Vector2(0, 0.5f);
            itemCheckmarkRect.sizeDelta = new Vector2(itemLayout.minHeight * 0.7f, itemLayout.minHeight * 0.7f);
            itemCheckmarkRect.anchoredPosition = new Vector2(5, 0); // Indent
            Image itemCheckmarkImage = itemCheckmarkGO.AddComponent<Image>();
            if (itemCheckmarkSprite != null) itemCheckmarkImage.sprite = itemCheckmarkSprite;
            itemCheckmarkImage.color = itemCheckmarkColor;
            itemToggle.graphic = itemCheckmarkImage;
            itemCheckmarkImage.enabled = false; // Typically hidden unless item is selected, Toggle handles this

            // Item Label
            GameObject itemLabelGO = new GameObject("Item Label"); // Expected by TMP_Dropdown
            itemLabelGO.transform.SetParent(itemGO.transform, false);
            RectTransform itemLabelRect = itemLabelGO.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(itemCheckmarkRect.sizeDelta.x + 10, 2); // Left (after checkmark), Bottom
            itemLabelRect.offsetMax = new Vector2(-5, -2); // Right, Top
            TextMeshProUGUI itemLabelText = itemLabelGO.AddComponent<TextMeshProUGUI>();
            if (globalFont != null) itemLabelText.font = globalFont;
            itemLabelText.color = itemTextColor;
            itemLabelText.fontSize = itemFontSize;
            itemLabelText.alignment = TextAlignmentOptions.Left;
            dropdown.itemText = itemLabelText;

            // Populate options
            dropdown.options.Clear();
            if (options != null)
            {
                foreach (string option in options)
                {
                    dropdown.options.Add(new TMP_Dropdown.OptionData(option));
                }
            }
            dropdown.RefreshShownValue();

            return dropdownRoot;
        }


        #endregion

        #region Popup Panel

        public static GameObject CreatePopupPanel(
            Transform parent, string message,
            TMP_FontAsset font, Color textColor, float fontSize,
            Color backgroundColor, Sprite backgroundSprite = null, Image.Type backgroundSpriteType = Image.Type.Simple,
            Vector2 relativeSize = default, Vector2 absoluteOffset = default)
        {
            if (relativeSize == default) relativeSize = new Vector2(0.4f, 0.2f); // Default relative size

            GameObject popupPanelGO = new GameObject("PopupPanel");
            popupPanelGO.transform.SetParent(parent, false); // Set parent first

            RectTransform popupRect = popupPanelGO.AddComponent<RectTransform>();
            // Anchoring to center of parent
            popupRect.anchorMin = new Vector2(0.5f - relativeSize.x / 2, 0.5f - relativeSize.y / 2);
            popupRect.anchorMax = new Vector2(0.5f + relativeSize.x / 2, 0.5f + relativeSize.y / 2);
            popupRect.pivot = new Vector2(0.5f, 0.5f);
            popupRect.sizeDelta = Vector2.zero; // size is controlled by anchors
            popupRect.anchoredPosition = absoluteOffset; // Apply offset if any

            Image popupImage = popupPanelGO.AddComponent<Image>();
            if (backgroundSprite != null)
            {
                popupImage.sprite = backgroundSprite;
                popupImage.type = backgroundSpriteType;
            }
            popupImage.color = backgroundColor;

            // Text for the popup
            GameObject textObj = new GameObject("PopupText");
            textObj.transform.SetParent(popupPanelGO.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            // Anchor text to fill the popup panel with some padding
            textRect.anchorMin = new Vector2(0.05f, 0.05f); // 5% padding
            textRect.anchorMax = new Vector2(0.95f, 0.95f); // 5% padding
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = message;
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.enableWordWrapping = true;
            if (font != null)
            {
                textComponent.font = font;
            }
            textComponent.color = textColor;

            popupPanelGO.SetActive(false); // Initially hidden
            return popupPanelGO;
        }

        #endregion
        
        #region Utility to save prefab (can be used by specific generators)
        public static void SavePrefab(GameObject rootGameObject, string folderPath, string prefabName)
        {
            if (rootGameObject == null)
            {
                MyLogger.EditorLogError("[UIComponentGenerator] Root GameObject is null. Cannot save prefab.");
                return;
            }

            if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(prefabName))
            {
                MyLogger.EditorLogError("[UIComponentGenerator] Folder path or prefab name is null or empty. Cannot save prefab.");
                return;
            }

            // Создаем директорию, если она не существует
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh(); // Обновляем AssetDatabase, чтобы Unity "увидел" новую папку
            }
            
            string fullPath = Path.Combine(folderPath, prefabName + ".prefab");
            
            // Check if prefab already exists to avoid unique name generation if not desired
            // or to explicitly overwrite. For now, we'll allow overwriting.
            // localPath = AssetDatabase.GenerateUniqueAssetPath(localPath); 

            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
            if (existingPrefab != null)
            {
                // Unpack if it's an instance of another prefab to avoid nesting issues,
                // or if we want to fully replace it.
                // PrefabUtility.UnpackPrefabInstance(rootGameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                // For replacing, just saving over it is usually fine.
                MyLogger.EditorLogWarning($"[UIComponentGenerator] Prefab already exists at {fullPath}. It will be overwritten.");
            }

            bool prefabSuccess;
            // Using SaveAsPrefabAssetAndConnect might be an option if you want to keep the scene GO connected
            // but for a generator that typically creates and then might destroy the scene GO, SaveAsPrefabAsset is simpler.
            PrefabUtility.SaveAsPrefabAsset(rootGameObject, fullPath, out prefabSuccess);

            if (prefabSuccess)
            {
                MyLogger.EditorLog($"[UIComponentGenerator] Prefab '{prefabName}' saved successfully to: {fullPath}");
                // Optional: Add labels
                // AssetDatabase.SetLabels(AssetDatabase.LoadAssetAtPath<GameObject>(localPath), new string[] { "GeneratedUI", "Component" });
            }
            else
            {
                MyLogger.EditorLogError($"[UIComponentGenerator] Failed to save prefab '{prefabName}' to {fullPath}. Check for errors in the console.");
            }
        }
        #endregion
    }
} 