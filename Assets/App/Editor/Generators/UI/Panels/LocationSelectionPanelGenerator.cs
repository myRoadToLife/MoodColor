using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using System.Reflection;
using App.Develop.Scenes.PersonalAreaScene.Panels;

namespace App.Editor.Generators.UI.Panels
{
    public static class LocationSelectionPanelGenerator
    {
        #region Constants
        private const string PREFAB_DIRECTORY = "Assets/Prefabs/UI";
        private const string PREFAB_NAME = "LocationSelectionPanel.prefab";
        
        // Стилевые константы
        private static readonly Color PanelBgColor = new Color(0.12f, 0.13f, 0.16f, 0.98f);
        private static readonly Color SectionBgColor = new Color(0.2f, 0.22f, 0.25f, 0.9f);
        private static readonly Color PrimaryTextColor = Color.white;
        private static readonly Color SecondaryTextColor = new Color(0.7f, 0.7f, 0.7f);
        private static readonly Color ButtonBgColor = new Color(0.3f, 0.4f, 0.6f);
        private static readonly Color HighlightButtonBgColor = new Color(0.4f, 0.5f, 0.7f);
        private static readonly Color ToggleBgColor = new Color(0.1f, 0.1f, 0.1f);
        private static readonly Color ToggleCheckmarkColor = new Color(0.4f, 0.8f, 1f);
        
        private static readonly int HeaderFontSize = 28;
        private static readonly int SectionTitleFontSize = 22;
        private static readonly int BodyFontSize = 18;
        private const float PADDING = 20f;
        private const float SPACING = 15f;
        #endregion

        [MenuItem("Tools/MoodColor/Generate Location Selection Panel")]
        public static void Generate()
        {
            // --- Create root canvas ---
            GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            // --- Create base panel ---
            GameObject panelObject = new GameObject("LocationSelectionPanel", typeof(RectTransform), typeof(CanvasGroup));
            panelObject.transform.SetParent(canvasObject.transform, false);
            var panelRT = panelObject.GetComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.sizeDelta = Vector2.zero;
            
            // --- Add panel background ---
            var bgImage = panelObject.AddComponent<Image>();
            bgImage.color = PanelBgColor;
            
            // --- Create header ---
            var header = CreateHeader(panelObject.transform, "🗺️ Выбор локации");
            
            // --- Create main content area ---
            var mainContent = CreateMainContentArea(panelObject.transform, "MainContent");
            
            // --- Current location display ---
            var currentLocationSection = CreateSection(mainContent, "CurrentLocationSection", "Текущее местоположение");
            var currentLocationText = CreateText(currentLocationSection, "CurrentLocationText", "📍 Автоматическое определение", BodyFontSize, FontStyles.Bold, HighlightButtonBgColor);
            
            // --- Manual selection toggle ---
            var selectionSection = CreateSection(mainContent, "SelectionSection", "Способ определения");
            var useManualToggle = CreateToggle(selectionSection, "UseManualSelectionToggle", "Выбрать регион вручную");
            
            // --- Region selection ---
            var regionSection = CreateSection(mainContent, "RegionSection", "Выбор региона");
            var regionDropdown = CreateDropdown(regionSection, "RegionDropdown", "Выберите ваш регион");
            var refreshLocationButton = CreateButton(regionSection, "RefreshLocationButton", "🔄 Обновить местоположение", ButtonBgColor);
            
            // --- Information section ---
            var infoSection = CreateSection(mainContent, "InfoSection", "Информация");
            var descriptionText = CreateText(infoSection, "DescriptionText", 
                "Выберите ваш регион для получения персонализированных данных.", 
                BodyFontSize, FontStyles.Normal, SecondaryTextColor);
            var locationInfoText = CreateText(infoSection, "LocationInfoText", 
                "ℹ️ Автоматическое определение использует GPS", 
                BodyFontSize, FontStyles.Normal, SecondaryTextColor);
            
            // --- Create buttons panel ---
            var buttonsGroup = CreateHorizontalGroup(panelObject.transform, "ActionButtons", 3, 60);
            buttonsGroup.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset((int)PADDING, (int)PADDING, 10, 10);
            buttonsGroup.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
            buttonsGroup.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0);
            buttonsGroup.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0);
            buttonsGroup.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 60);
            buttonsGroup.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, PADDING);
            
            var saveButton = CreateButton(buttonsGroup, "SaveButton", "💾 Сохранить", new Color(0.2f, 0.6f, 0.3f));
            var resetButton = CreateButton(buttonsGroup, "ResetButton", "🔄 Сбросить");
            var closeButton = CreateButton(buttonsGroup, "CloseButton", "❌ Закрыть");
            
            // --- Create notification panel ---
            var notificationPanel = CreateNotificationPanel(panelObject.transform);
            
            // --- Add controller and connect UI elements ---
            var controller = panelObject.AddComponent<LocationSelectionPanelController>();
            
            // Use reflection to set private fields
            SetPrivateField(controller, "_titleText", header.GetComponent<TMP_Text>());
            SetPrivateField(controller, "_currentLocationText", currentLocationText.GetComponent<TMP_Text>());
            SetPrivateField(controller, "_useManualSelectionToggle", useManualToggle.GetComponent<Toggle>());
            SetPrivateField(controller, "_regionDropdown", regionDropdown.GetComponent<TMP_Dropdown>());
            SetPrivateField(controller, "_refreshLocationButton", refreshLocationButton.GetComponent<Button>());
            SetPrivateField(controller, "_descriptionText", descriptionText.GetComponent<TMP_Text>());
            SetPrivateField(controller, "_locationInfoText", locationInfoText.GetComponent<TMP_Text>());
            SetPrivateField(controller, "_saveButton", saveButton.GetComponent<Button>());
            SetPrivateField(controller, "_resetButton", resetButton.GetComponent<Button>());
            SetPrivateField(controller, "_closeButton", closeButton.GetComponent<Button>());
            SetPrivateField(controller, "_notificationPanel", notificationPanel);
            SetPrivateField(controller, "_notificationText", notificationPanel.transform.Find("Content/Message")?.GetComponent<TMP_Text>());
            
            // --- Save as prefab ---
            SavePanelAsPrefab(canvasObject);
            
            // Select the panel in the editor
            Selection.activeObject = canvasObject;
        }

        #region UI Creation Methods
        private static GameObject CreateHeader(Transform parent, string title)
        {
            var textGo = CreateText(parent, "Header", title, HeaderFontSize, FontStyles.Bold, PrimaryTextColor);
            var rt = textGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -PADDING);
            rt.sizeDelta = new Vector2(-PADDING * 2, 50);
            textGo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            return textGo;
        }
        
        private static Transform CreateMainContentArea(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(PADDING, 80); // Отступ снизу для кнопок
            rt.offsetMax = new Vector2(-PADDING, -80); // Отступ сверху для заголовка

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = SPACING * 2;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return go.transform;
        }
        
        private static Transform CreateSection(Transform parent, string name, string title)
        {
            var sectionRoot = new GameObject(name, typeof(Image));
            sectionRoot.transform.SetParent(parent, false);
            sectionRoot.GetComponent<Image>().color = SectionBgColor;
            
            var layout = sectionRoot.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)PADDING, (int)PADDING, (int)PADDING, (int)PADDING);
            layout.spacing = SPACING;
            layout.childControlWidth = true;
            
            var fitter = sectionRoot.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var titleGo = CreateText(sectionRoot.transform, "SectionHeader", title, SectionTitleFontSize, FontStyles.Bold, PrimaryTextColor);
            return sectionRoot.transform;
        }
        
        private static Transform CreateHorizontalGroup(Transform parent, string name, int childCount, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, height);
            
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = SPACING;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return go.transform;
        }
        
        private static GameObject CreateToggle(Transform parent, string name, string label)
        {
            var toggleGo = new GameObject(name, typeof(RectTransform));
            toggleGo.transform.SetParent(parent, false);
            
            var toggle = toggleGo.AddComponent<Toggle>();
            var toggleRT = toggleGo.GetComponent<RectTransform>();
            toggleRT.sizeDelta = new Vector2(0, 30);
            
            var layout = toggleGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            
            // Background
            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(toggleGo.transform, false);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.sizeDelta = new Vector2(20, 20);
            bg.GetComponent<Image>().color = ToggleBgColor;
            
            // Checkmark
            var checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmark.transform.SetParent(bg.transform, false);
            var checkRT = checkmark.GetComponent<RectTransform>();
            checkRT.anchorMin = Vector2.zero;
            checkRT.anchorMax = Vector2.one;
            checkRT.sizeDelta = Vector2.zero;
            checkRT.anchoredPosition = Vector2.zero;
            checkmark.GetComponent<Image>().color = ToggleCheckmarkColor;
            checkmark.SetActive(false);
            
            // Label
            var labelGo = CreateText(toggleGo.transform, "Label", label, BodyFontSize, FontStyles.Normal, PrimaryTextColor);
            var labelRT = labelGo.GetComponent<RectTransform>();
            labelRT.sizeDelta = new Vector2(200, 30);
            
            toggle.targetGraphic = bg.GetComponent<Image>();
            toggle.graphic = checkmark.GetComponent<Image>();
            
            return toggleGo;
        }
        
        private static GameObject CreateDropdown(Transform parent, string name, string label)
        {
            var dropdownGo = new GameObject(name, typeof(RectTransform));
            dropdownGo.transform.SetParent(parent, false);
            
            var dropdown = dropdownGo.AddComponent<TMP_Dropdown>();
            var dropdownRT = dropdownGo.GetComponent<RectTransform>();
            dropdownRT.sizeDelta = new Vector2(0, 40);
            
            // Background
            var bg = dropdownGo.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f);
            
            // Label
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(dropdownGo.transform, false);
            var labelText = labelGo.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = BodyFontSize;
            labelText.color = PrimaryTextColor;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            
            var labelRT = labelGo.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.sizeDelta = Vector2.zero;
            labelRT.anchoredPosition = Vector2.zero;
            labelRT.offsetMin = new Vector2(10, 0);
            labelRT.offsetMax = new Vector2(-30, 0);
            
            // Arrow
            var arrowGo = new GameObject("Arrow", typeof(RectTransform));
            arrowGo.transform.SetParent(dropdownGo.transform, false);
            var arrowText = arrowGo.AddComponent<TextMeshProUGUI>();
            arrowText.text = "▼";
            arrowText.fontSize = BodyFontSize;
            arrowText.color = SecondaryTextColor;
            arrowText.alignment = TextAlignmentOptions.Center;
            
            var arrowRT = arrowGo.GetComponent<RectTransform>();
            arrowRT.anchorMin = new Vector2(1, 0);
            arrowRT.anchorMax = new Vector2(1, 1);
            arrowRT.sizeDelta = new Vector2(30, 0);
            arrowRT.anchoredPosition = new Vector2(-15, 0);
            
            // Template (создаем минимальный template для дропдауна)
            var templateGo = new GameObject("Template", typeof(RectTransform));
            templateGo.transform.SetParent(dropdownGo.transform, false);
            templateGo.SetActive(false);
            
            var templateRT = templateGo.GetComponent<RectTransform>();
            templateRT.anchorMin = new Vector2(0, 0);
            templateRT.anchorMax = new Vector2(1, 0);
            templateRT.pivot = new Vector2(0.5f, 1);
            templateRT.anchoredPosition = new Vector2(0, 2);
            templateRT.sizeDelta = new Vector2(0, 150);
            
            var templateBg = templateGo.AddComponent<Image>();
            templateBg.color = new Color(0.15f, 0.15f, 0.15f);
            
            // Viewport
            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            viewportGo.transform.SetParent(templateGo.transform, false);
            var viewportRT = viewportGo.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.sizeDelta = Vector2.zero;
            viewportRT.anchoredPosition = Vector2.zero;
            
            // Content
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRT = contentGo.GetComponent<RectTransform>();
            contentRT.anchorMin = Vector2.zero;
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.sizeDelta = Vector2.zero;
            contentRT.anchoredPosition = Vector2.zero;
            
            // Item
            var itemGo = new GameObject("Item", typeof(RectTransform));
            itemGo.transform.SetParent(contentGo.transform, false);
            var itemToggle = itemGo.AddComponent<Toggle>();
            var itemRT = itemGo.GetComponent<RectTransform>();
            itemRT.sizeDelta = new Vector2(0, 20);
            
            var itemText = CreateText(itemGo.transform, "Item Label", "Option A", BodyFontSize, FontStyles.Normal, PrimaryTextColor);
            var itemTextRT = itemText.GetComponent<RectTransform>();
            itemTextRT.anchorMin = Vector2.zero;
            itemTextRT.anchorMax = Vector2.one;
            itemTextRT.sizeDelta = Vector2.zero;
            itemTextRT.anchoredPosition = Vector2.zero;
            itemTextRT.offsetMin = new Vector2(10, 0);
            itemTextRT.offsetMax = new Vector2(-10, 0);
            
            // Dropdown setup
            dropdown.targetGraphic = bg;
            dropdown.template = templateRT;
            dropdown.captionText = labelText;
            dropdown.itemText = itemText.GetComponent<TextMeshProUGUI>();
            
            return dropdownGo;
        }

        private static GameObject CreateButton(Transform parent, string name, string text, Color? bgColor = null)
        {
            var buttonGo = new GameObject(name, typeof(RectTransform));
            buttonGo.transform.SetParent(parent, false);
            
            var button = buttonGo.AddComponent<Button>();
            var buttonRT = buttonGo.GetComponent<RectTransform>();
            buttonRT.sizeDelta = new Vector2(120, 40);
            
            var bg = buttonGo.AddComponent<Image>();
            bg.color = bgColor ?? ButtonBgColor;
            
            var textGo = CreateText(buttonGo.transform, "Text", text, BodyFontSize, FontStyles.Normal, PrimaryTextColor);
            var textRT = textGo.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;
            textRT.anchoredPosition = Vector2.zero;
            
            button.targetGraphic = bg;
            
            return buttonGo;
        }
        
        private static GameObject CreateNotificationPanel(Transform parent)
        {
            var panel = new GameObject("NotificationPanel", typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            panel.SetActive(false);
            
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            
            // Overlay
            var overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(panel.transform, false);
            overlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            var overlayRT = overlay.GetComponent<RectTransform>();
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.sizeDelta = Vector2.zero;
            overlayRT.anchoredPosition = Vector2.zero;
            
            // Content
            var content = new GameObject("Content", typeof(RectTransform), typeof(Image));
            content.transform.SetParent(panel.transform, false);
            content.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
            var contentRT = content.GetComponent<RectTransform>();
            contentRT.sizeDelta = new Vector2(300, 100);
            contentRT.anchoredPosition = Vector2.zero;
            
            // Message text
            var message = CreateText(content.transform, "Message", "Уведомление", BodyFontSize, FontStyles.Normal, PrimaryTextColor);
            var messageRT = message.GetComponent<RectTransform>();
            messageRT.anchorMin = Vector2.zero;
            messageRT.anchorMax = Vector2.one;
            messageRT.sizeDelta = Vector2.zero;
            messageRT.anchoredPosition = Vector2.zero;
            messageRT.offsetMin = new Vector2(20, 20);
            messageRT.offsetMax = new Vector2(-20, -20);
            
            return panel;
        }
        
        private static GameObject CreateText(Transform parent, string name, string content, int fontSize, FontStyles style, Color color)
        {
            var textGo = new GameObject(name, typeof(RectTransform));
            textGo.transform.SetParent(parent, false);
            
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            
            return textGo;
        }
        
        private static void SavePanelAsPrefab(GameObject panelObject)
        {
            if (!System.IO.Directory.Exists(PREFAB_DIRECTORY))
            {
                System.IO.Directory.CreateDirectory(PREFAB_DIRECTORY);
            }
            
            string path = $"{PREFAB_DIRECTORY}/{PREFAB_NAME}";
            
            // Удаляем существующий префаб если он есть
            if (System.IO.File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
            }
            
            // Создаем новый префаб
            PrefabUtility.SaveAsPrefabAsset(panelObject, path);
            AssetDatabase.Refresh();
            
            Debug.Log($"📍 Location Selection Panel prefab created at: {path}");
        }
        
        private static void SetPrivateField<T>(object obj, string fieldName, T value) where T : class
        {
            var type = obj.GetType();
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                Debug.LogError($"Field {fieldName} not found in {type.Name}");
            }
        }
        #endregion
    }
} 