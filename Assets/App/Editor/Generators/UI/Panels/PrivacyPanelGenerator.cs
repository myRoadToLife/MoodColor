using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;
using UnityEditor;
using Object = UnityEngine.Object;

namespace App.Editor.Generators.UI
{
    public static class PrivacyPanelGenerator
    {
        #region Constants
        private const string PREFAB_DIRECTORY = "Assets/Prefabs/UI";
        private const string PREFAB_NAME = "PrivacyPanel.prefab";
        
        // Styling constants
        private static readonly Color PanelBgColor = new Color(0.12f, 0.13f, 0.16f, 0.98f);
        private static readonly Color SectionBgColor = new Color(0.2f, 0.22f, 0.25f, 0.9f);
        private static readonly Color PrimaryTextColor = Color.white;
        private static readonly Color SecondaryTextColor = new Color(0.7f, 0.7f, 0.7f);
        private static readonly Color ButtonBgColor = new Color(0.3f, 0.4f, 0.6f);
        private static readonly Color DestructiveButtonBgColor = new Color(0.8f, 0.3f, 0.3f);
        private static readonly Color ToggleBgColor = new Color(0.1f, 0.1f, 0.1f);
        private static readonly Color ToggleCheckmarkColor = new Color(0.4f, 0.8f, 1f);
        private static readonly int HeaderFontSize = 28;
        private static readonly int SectionTitleFontSize = 22;
        private static readonly int BodyFontSize = 18;
        private const float PADDING = 20f;
        private const float SPACING = 15f;
        #endregion

        [MenuItem("Tools/MoodColor/Generate Privacy Panel")]
        public static void Generate()
        {
            var canvas = GetOrCreateCanvas();
            var panelObject = CreateRootPanel(canvas.transform, "PrivacyPanel");

            var privacyPanelScript = panelObject.AddComponent<PrivacyPanel>();
            
            // --- Create UI structure ---
            var header = CreateHeader(panelObject.transform, "🔒 Настройки конфиденциальности");
            
            var mainContent = CreateMainContentArea(panelObject.transform, "MainContent");

            // Main Settings
            var mainSettingsGroup = CreateSection(mainContent, "MainSettings", "Основные настройки");
            var globalToggle = CreateToggle(mainSettingsGroup, "AllowGlobalDataSharingToggle", "Разрешить сбор данных для глобальной статистики");
            var locationToggle = CreateToggle(mainSettingsGroup, "AllowLocationTrackingToggle", "Разрешить отслеживание геолокации");
            var anonymizeToggle = CreateToggle(mainSettingsGroup, "AnonymizeDataToggle", "Анонимизировать личные данные");
            
            // Region Settings
            var regionSettingsGroup = CreateSection(mainContent, "RegionSettings", "Настройки региона");
            var manualRegionToggle = CreateToggle(regionSettingsGroup, "UseManualRegionSelectionToggle", "Выбирать регион вручную");
            var regionDropdown = CreateDropdown(regionSettingsGroup, "RegionDropdown");
            var locationInfoGroup = CreateHorizontalGroup(regionSettingsGroup, "LocationInfo", 2, 40);
            var locationText = CreateText(locationInfoGroup, "CurrentLocationText", "Текущее местоположение: определяется...", BodyFontSize - 2, FontStyles.Italic, SecondaryTextColor);
            var refreshButton = CreateButton(locationInfoGroup, "RefreshLocationButton", "Обновить GPS");
            
            // Action Buttons
            var buttonsGroup = CreateHorizontalGroup(panelObject.transform, "ActionButtons", 4, 60);
            buttonsGroup.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset((int)PADDING, (int)PADDING, 10, 10);
            buttonsGroup.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
            buttonsGroup.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0);
            buttonsGroup.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0);
            buttonsGroup.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 60);
            buttonsGroup.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, PADDING);

            var saveButton = CreateButton(buttonsGroup, "SaveButton", "Сохранить", new Color(0.2f, 0.6f, 0.3f));
            var resetButton = CreateButton(buttonsGroup, "ResetButton", "Сбросить");
            var revokeButton = CreateButton(buttonsGroup, "RevokeAllButton", "Отозвать все", DestructiveButtonBgColor);
            var closeButton = CreateButton(buttonsGroup, "CloseButton", "Закрыть");
            
            // Notification Panel
            var notificationPanel = CreateNotificationPanel(panelObject.transform, "NotificationPanel");

            // --- Assign fields using reflection ---
            SetPrivateField(privacyPanelScript, "_allowGlobalDataSharingToggle", globalToggle.GetComponent<Toggle>());
            SetPrivateField(privacyPanelScript, "_allowLocationTrackingToggle", locationToggle.GetComponent<Toggle>());
            SetPrivateField(privacyPanelScript, "_anonymizeDataToggle", anonymizeToggle.GetComponent<Toggle>());
            SetPrivateField(privacyPanelScript, "_useManualRegionSelectionToggle", manualRegionToggle.GetComponent<Toggle>());
            SetPrivateField(privacyPanelScript, "_regionDropdown", regionDropdown.GetComponent<TMP_Dropdown>());
            SetPrivateField(privacyPanelScript, "_currentLocationText", locationText.GetComponent<TMP_Text>());
            SetPrivateField(privacyPanelScript, "_refreshLocationButton", refreshButton.GetComponent<Button>());
            SetPrivateField(privacyPanelScript, "_saveButton", saveButton.GetComponent<Button>());
            SetPrivateField(privacyPanelScript, "_resetButton", resetButton.GetComponent<Button>());
            SetPrivateField(privacyPanelScript, "_revokeAllButton", revokeButton.GetComponent<Button>());
            SetPrivateField(privacyPanelScript, "_closeButton", closeButton.GetComponent<Button>());
            SetPrivateField(privacyPanelScript, "_notificationPanel", notificationPanel);
            SetPrivateField(privacyPanelScript, "_notificationText", notificationPanel.transform.Find("ContentPane/NotificationText")?.GetComponent<TMP_Text>());
            SetPrivateField(privacyPanelScript, "_notificationOkButton", notificationPanel.transform.Find("ContentPane/NotificationOkButton")?.GetComponent<Button>());

            Selection.activeObject = panelObject;
            Undo.RegisterCreatedObjectUndo(panelObject, "Generate Privacy Panel");
            
            // --- Save as Prefab ---
            SavePanelAsPrefab(panelObject);
        }

        #region UI Creation Helpers

        private static Canvas GetOrCreateCanvas()
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas != null) return canvas;
            
            var canvasGo = new GameObject("Canvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static GameObject CreateRootPanel(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(800, 700);
            go.GetComponent<Image>().color = PanelBgColor;
            return go;
        }

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
            titleGo.AddComponent<LayoutElement>().minHeight = 40;

            return sectionRoot.transform;
        }
        
        private static Transform CreateHorizontalGroup(Transform parent, string name, int childCount, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);
            
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, height);
            
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = SPACING;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            
            return go.transform;
        }

        private static GameObject CreateToggle(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(Toggle), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().minHeight = 40;

            var toggle = go.GetComponent<Toggle>();
            
            var bg = new GameObject("Background", typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.5f);
            bgRt.anchorMax = new Vector2(0, 0.5f);
            bgRt.pivot = new Vector2(0, 0.5f);
            bgRt.sizeDelta = new Vector2(30, 30);
            bg.GetComponent<Image>().color = ToggleBgColor;

            var checkmark = new GameObject("Checkmark", typeof(Image));
            checkmark.transform.SetParent(bg.transform, false);
            var checkmarkRt = checkmark.GetComponent<RectTransform>();
            checkmarkRt.anchorMin = Vector2.zero;
            checkmarkRt.anchorMax = Vector2.one;
            checkmarkRt.offsetMin = new Vector2(5, 5);
            checkmarkRt.offsetMax = new Vector2(-5, -5);
            checkmark.GetComponent<Image>().color = ToggleCheckmarkColor;

            toggle.targetGraphic = bg.GetComponent<Image>();
            toggle.graphic = checkmark.GetComponent<Image>();
            toggle.isOn = true;

            var labelGo = CreateText(go.transform, "Label", label, BodyFontSize, FontStyles.Normal, PrimaryTextColor);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(40, 0);
            labelRt.offsetMax = new Vector2(0, 0);
            labelGo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
            
            return go;
        }
        
        private static GameObject CreateDropdown(Transform parent, string name)
        {
            // This is a complex prefab to generate from scratch. We'll create a basic version.
            // For a production-ready generator, it's better to instantiate a pre-made dropdown template.
            var go = new GameObject(name, typeof(Image), typeof(TMP_Dropdown), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().minHeight = 40;
            go.GetComponent<Image>().color = ToggleBgColor;
            
            var dropdown = go.GetComponent<TMP_Dropdown>();
            
            var label = CreateText(go.transform, "Label", "", BodyFontSize, FontStyles.Normal, PrimaryTextColor);
            var labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(15, 0);
            labelRt.offsetMax = new Vector2(-40, 0);
            dropdown.captionText = label.GetComponent<TMP_Text>();
            
            var arrow = new GameObject("Arrow", typeof(Image));
            arrow.transform.SetParent(go.transform, false);
            arrow.GetComponent<Image>().color = PrimaryTextColor;
            var arrowRt = arrow.GetComponent<RectTransform>();
            arrowRt.anchorMin = new Vector2(1, 0.5f);
            arrowRt.anchorMax = new Vector2(1, 0.5f);
            arrowRt.pivot = new Vector2(1, 0.5f);
            arrowRt.sizeDelta = new Vector2(20, 20);
            arrowRt.anchoredPosition = new Vector2(-15, 0);
            
            dropdown.options.Add(new TMP_Dropdown.OptionData("Загрузка..."));
            
            // Template for dropdown items
            var template = new GameObject("Template", typeof(RectTransform), typeof(ScrollRect));
            template.transform.SetParent(go.transform, false);
            dropdown.template = template.GetComponent<RectTransform>();
            var templateRT = template.GetComponent<RectTransform>();
            templateRT.anchorMin = new Vector2(0, 0);
            templateRT.anchorMax = new Vector2(1, 0);
            templateRT.pivot = new Vector2(0.5f, 1);
            templateRT.anchoredPosition = new Vector2(0, 2);
            templateRT.sizeDelta = new Vector2(0, 150);

            var item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            item.transform.SetParent(template.transform, false);
            var itemRT = item.GetComponent<RectTransform>();
            itemRT.anchorMin = new Vector2(0, 0.5f);
            itemRT.anchorMax = new Vector2(1, 0.5f);
            itemRT.sizeDelta = new Vector2(0, 30);
            
            // Сначала создаем текст для элемента выпадающего списка
            var itemTextComponent = CreateText(item.transform, "Item Label", "Option", BodyFontSize, FontStyles.Normal, PrimaryTextColor).GetComponent<TextMeshProUGUI>();
            
            // Теперь назначаем его и устанавливаем свойства
            dropdown.itemText = itemTextComponent;
            dropdown.itemText.alignment = TextAlignmentOptions.Left;

            return go;
        }
        
        private static GameObject CreateButton(Transform parent, string name, string text, Color? bgColor = null)
        {
            var go = new GameObject(name, typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            
            var image = go.GetComponent<Image>();
            image.color = bgColor ?? ButtonBgColor;
            
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            
            var textGo = CreateText(go.transform, "Text", text, BodyFontSize, FontStyles.Bold, PrimaryTextColor);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            textGo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            
            return go;
        }
        
        private static GameObject CreateNotificationPanel(Transform parent, string name)
        {
            var go = CreateRootPanel(parent, name);
            go.GetComponent<Image>().color = new Color(0, 0, 0, 0.85f);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = Vector2.zero; // Stretch to fill parent
            
            var contentPane = new GameObject("ContentPane", typeof(Image));
            contentPane.transform.SetParent(go.transform, false);
            contentPane.GetComponent<Image>().color = PanelBgColor;
            var contentRt = contentPane.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 0.5f);
            contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.sizeDelta = new Vector2(500, 250);
            
            var text = CreateText(contentPane.transform, "NotificationText", "Текст уведомления здесь", BodyFontSize, FontStyles.Normal, PrimaryTextColor);
            var textRt = text.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0, 0.3f);
            textRt.anchorMax = new Vector2(1, 1);
            textRt.offsetMin = new Vector2(PADDING, PADDING);
            textRt.offsetMax = new Vector2(-PADDING, -PADDING);

            var button = CreateButton(contentPane.transform, "NotificationOkButton", "OK");
            var buttonRt = button.GetComponent<RectTransform>();
            buttonRt.anchorMin = new Vector2(0.5f, 0);
            buttonRt.anchorMax = new Vector2(0.5f, 0);
            buttonRt.pivot = new Vector2(0.5f, 0);
            buttonRt.sizeDelta = new Vector2(200, 50);
            buttonRt.anchoredPosition = new Vector2(0, PADDING);
            
            go.SetActive(false);
            return go;
        }

        private static GameObject CreateText(Transform parent, string name, string content, int fontSize, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var textMesh = go.GetComponent<TextMeshProUGUI>();
            textMesh.text = content;
            textMesh.fontSize = fontSize;
            textMesh.fontStyle = style;
            textMesh.color = color;
            textMesh.alignment = TextAlignmentOptions.Left;
            textMesh.enableWordWrapping = true;
            return go;
        }

        #endregion

        #region Helper Methods
        
        private static void SavePanelAsPrefab(GameObject panelObject)
        {
            if (!AssetDatabase.IsValidFolder(PREFAB_DIRECTORY))
            {
                // Create the directory if it doesn't exist
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                {
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                }
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }

            string prefabPath = $"{PREFAB_DIRECTORY}/{PREFAB_NAME}";
            
            // Delete existing prefab to avoid conflicts
            AssetDatabase.DeleteAsset(prefabPath);

            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(panelObject, prefabPath, InteractionMode.UserAction);
            
            if (prefab != null)
            {
                Debug.Log($"🎉 Панель успешно сохранена как префаб: {prefabPath}", prefab);
                Selection.activeObject = prefab;
            }
            else
            {
                Debug.LogError($"[Generator] Не удалось сохранить префаб для '{panelObject.name}'.");
            }
        }

        private static void SetPrivateField<T>(object obj, string fieldName, T value) where T : class
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                if (value == null)
                {
                    Debug.LogError($"[Generator] Попытка присвоить null значение для поля '{fieldName}' в '{obj.GetType().Name}'. Возможно, UI элемент не был найден.");
                    return;
                }
                field.SetValue(obj, value);
            }
            else
            {
                Debug.LogWarning($"[Generator] Не удалось найти приватное поле '{fieldName}' в '{obj.GetType().Name}'. Убедитесь, что оно существует и имеет модификатор [SerializeField].");
            }
        }

        #endregion
    }
} 