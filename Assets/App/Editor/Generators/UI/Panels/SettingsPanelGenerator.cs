using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using System.Reflection;
using App.Develop.Scenes.PersonalAreaScene.Settings;

namespace App.Editor.Generators.UI.Panels
{
    public static class SettingsPanelGenerator
    {
        #region Constants
        private const string PREFAB_DIRECTORY = "Assets/Prefabs/UI";
        private const string PREFAB_NAME = "SettingsPanel.prefab";
        
        // Стилевые константы
        private static readonly Color PanelBgColor = new Color(0.12f, 0.13f, 0.16f, 0.98f);
        private static readonly Color SectionBgColor = new Color(0.2f, 0.22f, 0.25f, 0.9f);
        private static readonly Color PrimaryTextColor = Color.white;
        private static readonly Color SecondaryTextColor = new Color(0.7f, 0.7f, 0.7f);
        private static readonly Color ButtonBgColor = new Color(0.3f, 0.4f, 0.6f);
        private static readonly Color HighlightButtonBgColor = new Color(0.4f, 0.5f, 0.7f);
        private static readonly Color DestructiveButtonBgColor = new Color(0.8f, 0.3f, 0.3f);
        private static readonly Color ToggleBgColor = new Color(0.1f, 0.1f, 0.1f);
        private static readonly Color ToggleCheckmarkColor = new Color(0.4f, 0.8f, 1f);
        
        private static readonly int HeaderFontSize = 28;
        private static readonly int SectionTitleFontSize = 22;
        private static readonly int BodyFontSize = 18;
        private const float PADDING = 20f;
        private const float SPACING = 15f;
        #endregion

        [MenuItem("Tools/MoodColor/Generate Settings Panel")]
        public static void Generate()
        {
            // --- Create root canvas ---
            GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            // --- Create base panel ---
            GameObject panelObject = new GameObject("SettingsPanel", typeof(RectTransform), typeof(CanvasGroup));
            panelObject.transform.SetParent(canvasObject.transform, false);
            var panelRT = panelObject.GetComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.sizeDelta = Vector2.zero;
            
            // --- Add panel background ---
            var bgImage = panelObject.AddComponent<Image>();
            bgImage.color = PanelBgColor;
            
            // --- Create header ---
            var header = CreateHeader(panelObject.transform, "Настройки приложения");
            
            // --- Create main content area ---
            var mainContent = CreateMainContentArea(panelObject.transform, "MainContent");
            
            // --- Create sections ---
            var generalSettingsSection = CreateSection(mainContent, "GeneralSettings", "Общие настройки");
            var notificationsToggle = CreateToggle(generalSettingsSection, "NotificationsToggle", "Разрешить уведомления");
            var soundToggle = CreateToggle(generalSettingsSection, "SoundToggle", "Включить звук");
            
            var appearanceSection = CreateSection(mainContent, "AppearanceSettings", "Внешний вид");
            var themeDropdown = CreateDropdown(appearanceSection, "ThemeDropdown", "Тема оформления");
            var languageDropdown = CreateDropdown(appearanceSection, "LanguageDropdown", "Язык интерфейса");
            
            var regionSection = CreateSection(mainContent, "RegionSettings", "Региональные настройки");
            var currentRegionText = CreateText(regionSection, "CurrentRegionText", "Текущий регион: Не выбран", BodyFontSize, FontStyles.Normal, SecondaryTextColor);
            var regionDropdown = CreateDropdown(regionSection, "RegionDropdown", "Выберите регион");
            
            // --- Create buttons panel ---
            var buttonsGroup = CreateHorizontalGroup(panelObject.transform, "ActionButtons", 5, 60);
            buttonsGroup.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset((int)PADDING, (int)PADDING, 10, 10);
            buttonsGroup.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
            buttonsGroup.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0);
            buttonsGroup.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0);
            buttonsGroup.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 60);
            buttonsGroup.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, PADDING);
            
            var saveButton = CreateButton(buttonsGroup, "SaveButton", "Сохранить", new Color(0.2f, 0.6f, 0.3f));
            var resetButton = CreateButton(buttonsGroup, "ResetButton", "Сбросить");
            var privacyButton = CreateButton(buttonsGroup, "PrivacySettingsButton", "Конфиденциальность", HighlightButtonBgColor);
            var locationButton = CreateButton(buttonsGroup, "LocationSettingsButton", "📍 Локация", new Color(0.3f, 0.5f, 0.8f));
            var closeButton = CreateButton(buttonsGroup, "CloseButton", "Закрыть");
            
            // --- Create delete account button (separately) ---
            var deleteAccountButton = CreateButton(panelObject.transform, "DeleteAccountButton", "Удалить аккаунт", DestructiveButtonBgColor);
            var deleteButtonRT = deleteAccountButton.GetComponent<RectTransform>();
            deleteButtonRT.anchorMin = new Vector2(0.5f, 0);
            deleteButtonRT.anchorMax = new Vector2(0.5f, 0);
            deleteButtonRT.pivot = new Vector2(0.5f, 0);
            deleteButtonRT.sizeDelta = new Vector2(200, 40);
            deleteButtonRT.anchoredPosition = new Vector2(0, PADDING * 4);
            
            // --- Create popup panel ---
            var popupPanel = CreatePopupPanel(panelObject.transform);
            
            // --- Создаем ссылку для панели конфиденциальности ---
            var privacyPanelContainer = new GameObject("PrivacyPanelContainer", typeof(RectTransform));
            privacyPanelContainer.transform.SetParent(panelObject.transform, false);
            privacyPanelContainer.SetActive(false); // Изначально отключен
            
            // --- Создаем ссылку для панели выбора локации ---
            var locationPanelContainer = new GameObject("LocationPanelContainer", typeof(RectTransform));
            locationPanelContainer.transform.SetParent(panelObject.transform, false);
            locationPanelContainer.SetActive(false); // Изначально отключен
            
            // Пытаемся найти и подключить префаб панели конфиденциальности
            var privacyPanelPath = "Assets/Prefabs/UI/PrivacyPanel.prefab";
            if (System.IO.File.Exists(privacyPanelPath))
            {
                var privacyPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(privacyPanelPath);
                if (privacyPanelPrefab != null)
                {
                    // Создаем копию префаба внутри контейнера
                    var privacyPanelInstance = Object.Instantiate(privacyPanelPrefab, privacyPanelContainer.transform);
                    privacyPanelInstance.name = "PrivacyPanel";
                    
                    Debug.Log("🔒 Префаб панели конфиденциальности найден и подключен к панели настроек");
                }
                else
                {
                    Debug.LogWarning("⚠️ Префаб панели конфиденциальности найден, но не удалось загрузить его");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Префаб панели конфиденциальности не найден по пути: " + privacyPanelPath);
                Debug.LogWarning("Сначала создайте префаб панели конфиденциальности через Tools > MoodColor > Generate Privacy Panel");
            }
            
            // --- Add controller and connect UI elements ---
            var controller = panelObject.AddComponent<SettingsPanelController>();
            
            // Use reflection to set private fields
            SetPrivateField(controller, "_notificationsToggle", notificationsToggle.GetComponent<Toggle>());
            SetPrivateField(controller, "_soundToggle", soundToggle.GetComponent<Toggle>());
            SetPrivateField(controller, "_themeDropdown", themeDropdown.GetComponent<TMP_Dropdown>());
            SetPrivateField(controller, "_languageDropdown", languageDropdown.GetComponent<TMP_Dropdown>());
            SetPrivateField(controller, "_regionDropdown", regionDropdown.GetComponent<TMP_Dropdown>());
            SetPrivateField(controller, "_currentRegionText", currentRegionText.GetComponent<TMP_Text>());
            SetPrivateField(controller, "_saveButton", saveButton.GetComponent<Button>());
            SetPrivateField(controller, "_resetButton", resetButton.GetComponent<Button>());
            SetPrivateField(controller, "_deleteAccountButton", deleteAccountButton.GetComponent<Button>());
            SetPrivateField(controller, "_privacySettingsButton", privacyButton.GetComponent<Button>());
            SetPrivateField(controller, "_locationSettingsButton", locationButton.GetComponent<Button>());
            SetPrivateField(controller, "_closeButton", closeButton.GetComponent<Button>());
            SetPrivateField(controller, "_popupPanel", popupPanel);
            SetPrivateField(controller, "_popupText", popupPanel.transform.Find("Content/Message")?.GetComponent<TMP_Text>());
            SetPrivateField(controller, "_privacyPanel", privacyPanelContainer);
            SetPrivateField(controller, "_locationPanel", locationPanelContainer);
            
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
        
        private static GameObject CreateDropdown(Transform parent, string name, string label)
        {
            // Создаем контейнер с вертикальным лейаутом
            var container = new GameObject(name + "Container", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            container.transform.SetParent(parent, false);
            container.GetComponent<LayoutElement>().minHeight = 80;
            
            var layout = container.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
            
            // Создаем лейбл
            var labelGo = CreateText(container.transform, name + "Label", label, BodyFontSize - 2, FontStyles.Normal, PrimaryTextColor);
            labelGo.AddComponent<LayoutElement>().minHeight = 30;
            
            // Создаем сам дропдаун
            var go = new GameObject(name, typeof(Image), typeof(TMP_Dropdown), typeof(LayoutElement));
            go.transform.SetParent(container.transform, false);
            go.GetComponent<LayoutElement>().minHeight = 40;
            go.GetComponent<Image>().color = ToggleBgColor;
            
            var dropdown = go.GetComponent<TMP_Dropdown>();
            
            // Настройка текста для текущего выбранного элемента
            var captionLabel = CreateText(go.transform, "Label", "", BodyFontSize, FontStyles.Normal, PrimaryTextColor);
            var captionLabelRt = captionLabel.GetComponent<RectTransform>();
            captionLabelRt.anchorMin = Vector2.zero;
            captionLabelRt.anchorMax = Vector2.one;
            captionLabelRt.offsetMin = new Vector2(15, 0);
            captionLabelRt.offsetMax = new Vector2(-40, 0);
            dropdown.captionText = captionLabel.GetComponent<TMP_Text>();
            
            // Стрелка выпадающего списка
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
            
            // Создаем шаблон для выпадающего списка с Canvas для правильной отрисовки
            var template = new GameObject("Template", typeof(RectTransform));
            template.transform.SetParent(go.transform, false);
            
            // Добавляем Canvas для правильной отрисовки
            var templateCanvas = template.AddComponent<Canvas>();
            templateCanvas.overrideSorting = true;
            templateCanvas.sortingOrder = 30000; // Высокий sortingOrder для отображения поверх других элементов
            template.AddComponent<GraphicRaycaster>();
            
            // Настраиваем RectTransform для шаблона
            dropdown.template = template.GetComponent<RectTransform>();
            var templateRT = template.GetComponent<RectTransform>();
            templateRT.anchorMin = new Vector2(0, 0);
            templateRT.anchorMax = new Vector2(1, 0);
            templateRT.pivot = new Vector2(0.5f, 1);
            templateRT.anchoredPosition = new Vector2(0, 0);
            templateRT.sizeDelta = new Vector2(0, 150);
            
            // Добавляем фон для шаблона
            var templateBg = new GameObject("Background", typeof(Image));
            templateBg.transform.SetParent(template.transform, false);
            templateBg.GetComponent<Image>().color = PanelBgColor;
            var templateBgRT = templateBg.GetComponent<RectTransform>();
            templateBgRT.anchorMin = Vector2.zero;
            templateBgRT.anchorMax = Vector2.one;
            templateBgRT.sizeDelta = Vector2.zero;
            
            // Добавляем ScrollRect и настраиваем прокрутку
            var scrollRect = template.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 15;
            
            // Создаем Viewport
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(template.transform, false);
            viewport.GetComponent<Image>().color = Color.white;
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            var viewportRT = viewport.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.sizeDelta = new Vector2(-18, -2); // Отступ для полосы прокрутки
            viewportRT.pivot = new Vector2(0, 1);
            
            // Создаем Content для элементов списка
            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(viewport.transform, false);
            var contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0, 0);
            
            // Настраиваем VerticalLayoutGroup
            var contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 0;
            contentLayout.padding = new RectOffset(5, 5, 5, 5);
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            
            // Добавляем ContentSizeFitter чтобы Content растягивался под содержимое
            var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Связываем ScrollRect с Viewport и Content
            scrollRect.viewport = viewportRT;
            scrollRect.content = contentRT;
            
            // Создаем пример элемента для списка
            var item = new GameObject("Item", typeof(Toggle), typeof(LayoutElement));
            item.transform.SetParent(content.transform, false);
            item.GetComponent<LayoutElement>().minHeight = 30;
            
            // Настраиваем Toggle
            var toggle = item.GetComponent<Toggle>();
            toggle.targetGraphic = item.AddComponent<Image>();
            toggle.targetGraphic.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            
            // Добавляем текст элемента
            var itemText = CreateText(item.transform, "Item Label", "Option", BodyFontSize, FontStyles.Normal, PrimaryTextColor);
            var itemTextRT = itemText.GetComponent<RectTransform>();
            itemTextRT.anchorMin = Vector2.zero;
            itemTextRT.anchorMax = Vector2.one;
            itemTextRT.offsetMin = new Vector2(5, 0);
            itemTextRT.offsetMax = new Vector2(-5, 0);
            
            // Связываем с дропдауном
            dropdown.itemText = itemText.GetComponent<TextMeshProUGUI>();
            dropdown.itemText.alignment = TextAlignmentOptions.Left;
            
            // Добавляем полосу прокрутки
            var scrollbar = new GameObject("Scrollbar", typeof(Image), typeof(Scrollbar));
            scrollbar.transform.SetParent(template.transform, false);
            scrollbar.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            var scrollbarRT = scrollbar.GetComponent<RectTransform>();
            scrollbarRT.anchorMin = new Vector2(1, 0);
            scrollbarRT.anchorMax = new Vector2(1, 1);
            scrollbarRT.pivot = new Vector2(1, 1);
            scrollbarRT.sizeDelta = new Vector2(15, 0);
            
            // Настраиваем Scrollbar
            var scrollbarComp = scrollbar.GetComponent<Scrollbar>();
            scrollbarComp.direction = Scrollbar.Direction.BottomToTop;
            
            // Создаем SlidingArea для полосы прокрутки
            var slidingArea = new GameObject("SlidingArea", typeof(RectTransform));
            slidingArea.transform.SetParent(scrollbar.transform, false);
            var slidingAreaRT = slidingArea.GetComponent<RectTransform>();
            slidingAreaRT.anchorMin = Vector2.zero;
            slidingAreaRT.anchorMax = Vector2.one;
            slidingAreaRT.sizeDelta = new Vector2(-10, -10);
            
            // Создаем Handle для полосы прокрутки
            var handle = new GameObject("Handle", typeof(Image));
            handle.transform.SetParent(slidingArea.transform, false);
            handle.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f, 0.7f);
            var handleRT = handle.GetComponent<RectTransform>();
            handleRT.anchorMin = Vector2.zero;
            handleRT.anchorMax = Vector2.one;
            handleRT.sizeDelta = Vector2.zero;
            
            // Связываем Scrollbar с Handle
            scrollbarComp.targetGraphic = handle.GetComponent<Image>();
            scrollbarComp.handleRect = handleRT;
            
            // Связываем ScrollRect с Scrollbar
            scrollRect.verticalScrollbar = scrollbarComp;
            
            // Отключаем шаблон
            template.SetActive(false);
            
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
        
        private static GameObject CreatePopupPanel(Transform parent)
        {
            var go = new GameObject("PopupPanel", typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            
            // Добавляем изображение на весь экран
            var bgImage = go.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.5f);
            
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            // Создаем контент-панель
            var content = new GameObject("Content", typeof(RectTransform), typeof(Image));
            content.transform.SetParent(go.transform, false);
            content.GetComponent<Image>().color = PanelBgColor;
            
            var contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0.5f, 0.5f);
            contentRT.anchorMax = new Vector2(0.5f, 0.5f);
            contentRT.sizeDelta = new Vector2(400, 150);
            
            // Текст сообщения
            var message = CreateText(content.transform, "Message", "Сообщение", BodyFontSize, FontStyles.Normal, PrimaryTextColor);
            var messageRT = message.GetComponent<RectTransform>();
            messageRT.anchorMin = Vector2.zero;
            messageRT.anchorMax = Vector2.one;
            messageRT.offsetMin = new Vector2(PADDING, PADDING);
            messageRT.offsetMax = new Vector2(-PADDING, -PADDING);
            message.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            
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
                Debug.Log($"🎉 Префаб панели настроек успешно сохранен: {prefabPath}", prefab);
                Selection.activeObject = prefab;
                
                // Проверяем, нужно ли сделать адресабельным
                if (EditorUtility.DisplayDialog("Addressable Asset", 
                    "Сделать префаб панели настроек адресабельным ассетом?\n\n" +
                    "Для использования через AssetAddresses.SettingsPanel", 
                    "Да, сделать адресабельным", "Нет"))
                {
                    Debug.Log("Добавление панели настроек в адресабельные ассеты...");
                    // Здесь можно добавить код для автоматического добавления в адресабельные ассеты
                    // Но это требует дополнительных зависимостей, поэтому просто инструкция:
                    Debug.Log("Чтобы сделать ассет адресабельным, откройте окно Addressables и добавьте префаб вручную с ключом: UIPanel_Settings");
                }
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