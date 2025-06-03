#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;
using App.Editor.Generators.UI.Core;
using App.Develop.CommonServices.Emotion;
using App.Develop.Scenes.PersonalAreaScene.UI.Components;
using App.App.Develop.Scenes.PersonalAreaScene.UI.Components;
using App.Develop.Scenes.PersonalAreaScene.UI;
using App.Develop.Scenes.PersonalAreaScene.Handlers;
using SafeAreaComponent = App.Develop.Scenes.PersonalAreaScene.UI.Components.SafeArea;

namespace App.Editor.Generators.UI.Canvases
{
    public class PersonalAreaCanvasGenerator
    {
        #region Constants
        private const string PREFAB_SAVE_FOLDER_PATH = "Assets/App/Prefabs/Generated/UI/Canvases/";
        private const string PREFAB_NAME = "PersonalAreaCanvas";
        private const string REGION_STAT_ITEM_PREFAB_PATH = "Assets/App/Prefabs/Generated/UI/RegionStatItem.prefab";

        // Цветовая палитра MoodRoom
        private static readonly Color WarmWoodLight = new Color(0.9f, 0.8f, 0.7f, 1f);
        private static readonly Color WarmWoodMedium = new Color(0.82f, 0.71f, 0.55f, 1f);
        private static readonly Color WarmWoodDark = new Color(0.7f, 0.6f, 0.45f, 1f);
        private static readonly Color WoodDarkBrown = new Color(0.6f, 0.5f, 0.35f, 1f);
        private static readonly Color TextDark = new Color(0.25f, 0.2f, 0.15f, 1f);
        private static readonly Color TextLight = new Color(0.9f, 0.9f, 0.85f, 1f);
        private static readonly Color PaperBeige = new Color(0.95f, 0.9f, 0.8f, 1f);
        private static readonly Color GlassBlue = new Color(0.7f, 0.85f, 0.9f, 0.3f);
        private static readonly Color AccentGold = new Color(0.95f, 0.8f, 0.3f, 1f);
        #endregion

        [MenuItem("MoodColor/Generate/UI Canvases/Personal Area Canvas")]
        public static void GeneratePrefab()
        {
            Debug.Log("🔄 Начинаем генерацию префаба Personal Area Canvas...");

            var regionStatItemPrefab = CreateRegionStatItemPrefab();
            var root = CreateMainCanvas();
            CreateSimpleBackground(root.transform);
            var safeArea = CreateSafeArea(root.transform);
            var mainContent = CreateMainContent(safeArea.transform);

            var profileInfo = CreateProfileInfo(mainContent.transform);
            var emotionJars = CreateEmotionJars(mainContent.transform);
            var statistics = CreateStatistics(mainContent.transform, regionStatItemPrefab);
            var navigation = CreateNavigation(mainContent.transform);

            SetupControllers(root, profileInfo, emotionJars, statistics, navigation);

            Debug.Log($"💾 Сохраняем префаб в {Path.Combine(PREFAB_SAVE_FOLDER_PATH, PREFAB_NAME + ".prefab")}");
            UIComponentGenerator.SavePrefab(root, PREFAB_SAVE_FOLDER_PATH, PREFAB_NAME);

            if (!Application.isPlaying) Object.DestroyImmediate(root);
            Debug.Log("✅ Генерация префаба Personal Area Canvas завершена");
        }

        #region Region Stat Item Prefab Creation
        private static GameObject CreateRegionStatItemPrefab()
        {
            Debug.Log("🔧 Создаем префаб RegionStatItem...");
            var regionStatItem = CreateUIObject("RegionStatItem", null);
            var itemRect = regionStatItem.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(360, 110);

            var itemLayoutElement = regionStatItem.AddComponent<LayoutElement>();
            itemLayoutElement.minHeight = 110;
            itemLayoutElement.preferredHeight = 110;
            itemLayoutElement.flexibleHeight = 0;

            var itemBackground = regionStatItem.AddComponent<Image>();
            itemBackground.color = new Color(WarmWoodMedium.r, WarmWoodMedium.g, WarmWoodMedium.b, 0.85f); // Сделал чуть насыщеннее
            itemBackground.type = Image.Type.Sliced;

            var horizontalLayout = regionStatItem.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.padding = new RectOffset(20, 20, 20, 20);
            horizontalLayout.spacing = 20;
            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.childControlHeight = true;
            horizontalLayout.childControlWidth = false;
            horizontalLayout.childForceExpandHeight = true;
            horizontalLayout.childForceExpandWidth = false;

            var regionNameText = CreateTextElement("RegionNameText", regionStatItem.transform);
            var regionNameLayoutElement = regionNameText.AddComponent<LayoutElement>();
            regionNameLayoutElement.minWidth = 150;
            regionNameLayoutElement.preferredWidth = 180;
            regionNameLayoutElement.flexibleWidth = 0;

            var regionNameComponent = regionNameText.GetComponent<TextMeshProUGUI>();
            regionNameComponent.text = "Название района";
            regionNameComponent.fontSize = 26;
            regionNameComponent.fontWeight = FontWeight.Bold;
            regionNameComponent.color = TextDark;
            regionNameComponent.alignment = TextAlignmentOptions.TopLeft; // Выравнивание по верху для многострочного
            regionNameComponent.enableWordWrapping = true; // Разрешить перенос слов
            regionNameComponent.overflowMode = TextOverflowModes.Ellipsis; // Обрезать если не влезает

            var emotionInfoContainer = CreateUIObject("EmotionInfoContainer", regionStatItem.transform);
            var emotionInfoLayoutElement = emotionInfoContainer.AddComponent<LayoutElement>();
            emotionInfoLayoutElement.flexibleWidth = 1;

            var emotionInfoLayout = emotionInfoContainer.AddComponent<VerticalLayoutGroup>();
            emotionInfoLayout.padding = new RectOffset(0, 0, 0, 0); // Убрал, т.к. элементы Text сами будут иметь отступы
            emotionInfoLayout.spacing = 10;
            emotionInfoLayout.childAlignment = TextAnchor.UpperLeft;
            emotionInfoLayout.childControlHeight = false;
            emotionInfoLayout.childControlWidth = true;
            emotionInfoLayout.childForceExpandHeight = false;
            emotionInfoLayout.childForceExpandWidth = true;

            var dominantEmotionText = CreateTextElement("DominantEmotionText", emotionInfoContainer.transform);
            dominantEmotionText.AddComponent<LayoutElement>().preferredHeight = 28; // Задать высоту
            var dominantEmotionComponent = dominantEmotionText.GetComponent<TextMeshProUGUI>();
            dominantEmotionComponent.text = "Преобладает: Радость";
            dominantEmotionComponent.fontSize = 22;
            dominantEmotionComponent.fontWeight = FontWeight.SemiBold;
            dominantEmotionComponent.color = AccentGold;
            dominantEmotionComponent.alignment = TextAlignmentOptions.Left;

            var percentageText = CreateTextElement("PercentageText", emotionInfoContainer.transform);
            percentageText.AddComponent<LayoutElement>().preferredHeight = 24; // Задать высоту
            var percentageComponent = percentageText.GetComponent<TextMeshProUGUI>();
            percentageComponent.text = "42% населения";
            percentageComponent.fontSize = 20;
            percentageComponent.color = new Color(TextDark.r, TextDark.g, TextDark.b, 0.9f); // Чуть темнее для контраста
            percentageComponent.alignment = TextAlignmentOptions.Left;

            var regionStatItemView = regionStatItem.AddComponent<RegionStatItemView>();
            var serializedRegionStatItem = new SerializedObject(regionStatItemView);
            serializedRegionStatItem.FindProperty("_regionNameText").objectReferenceValue = regionNameComponent;
            serializedRegionStatItem.FindProperty("_dominantEmotionText").objectReferenceValue = dominantEmotionComponent;
            serializedRegionStatItem.FindProperty("_percentageText").objectReferenceValue = percentageComponent;
            serializedRegionStatItem.ApplyModifiedProperties();

            string prefabDirectory = Path.GetDirectoryName(REGION_STAT_ITEM_PREFAB_PATH);
            if (!Directory.Exists(prefabDirectory))
            {
                Directory.CreateDirectory(prefabDirectory);
                AssetDatabase.Refresh();
            }
            var prefabAsset = PrefabUtility.SaveAsPrefabAsset(regionStatItem, REGION_STAT_ITEM_PREFAB_PATH);
            if (!Application.isPlaying) Object.DestroyImmediate(regionStatItem);
            Debug.Log($"✅ Префаб RegionStatItem создан: {REGION_STAT_ITEM_PREFAB_PATH}");
            return prefabAsset;
        }
        #endregion

        #region Canvas Creation
        private static GameObject CreateMainCanvas()
        {
            var canvasGO = UIComponentGenerator.CreateBasePanelRoot(
                PREFAB_NAME,
                RenderMode.ScreenSpaceOverlay,
                0,
                new Vector2(1080, 1920),
                0.5f
            );
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer == -1) uiLayer = LayerMask.NameToLayer("Default");
            canvasGO.layer = uiLayer;
            SetLayerRecursively(canvasGO.transform, uiLayer);

            var canvasScaler = canvasGO.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1080, 1920);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0f; // Предпочтение высоте для портретного режима
            return canvasGO;
        }
        #endregion

        #region Simple Background
        private static void CreateSimpleBackground(Transform parent)
        {
            var background = CreateUIObject("Background", parent);
            var backgroundRect = background.GetComponent<RectTransform>();
            SetFullStretch(backgroundRect);
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = WarmWoodLight;
            backgroundImage.raycastTarget = false;
        }
        #endregion

        #region Safe Area
        private static GameObject CreateSafeArea(Transform parent)
        {
            var safeArea = CreateUIObject("SafeArea", parent);
            var safeAreaRect = safeArea.GetComponent<RectTransform>();
            SetFullStretch(safeAreaRect);
            safeArea.AddComponent<SafeAreaComponent>();
            return safeArea;
        }
        #endregion

        #region Main Content
        private static GameObject CreateMainContent(Transform parent)
        {
            var mainContent = CreateUIObject("MainContent", parent);
            var mainContentRect = mainContent.GetComponent<RectTransform>();
            SetFullStretch(mainContentRect);

            var mainLayout = mainContent.AddComponent<VerticalLayoutGroup>();
            mainLayout.padding = new RectOffset(35, 35, 35, 35);
            mainLayout.spacing = 30; // Увеличено расстояние между основными блоками
            mainLayout.childAlignment = TextAnchor.UpperCenter;
            mainLayout.childControlHeight = true;
            mainLayout.childControlWidth = true;
            mainLayout.childForceExpandHeight = false; // Разрешаем элементам иметь свою высоту
            mainLayout.childForceExpandWidth = true;
            return mainContent;
        }
        #endregion

        #region Profile Info Component
        private static GameObject CreateProfileInfo(Transform parent)
        {
            var profileInfo = CreateUIObject("ProfileInfo", parent);
            var layoutElement = profileInfo.AddComponent<LayoutElement>();
            layoutElement.minHeight = 120; // Базовая высота
            layoutElement.preferredHeight = 140; // Увеличена предпочтительная высота
            layoutElement.flexibleHeight = 0;

            var backgroundImage = profileInfo.AddComponent<Image>();
            backgroundImage.color = WarmWoodMedium;

            var horizontalLayout = profileInfo.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.padding = new RectOffset(25, 25, 20, 20);
            horizontalLayout.spacing = 20;
            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.childControlHeight = true;
            horizontalLayout.childControlWidth = false;
            horizontalLayout.childForceExpandHeight = true;
            horizontalLayout.childForceExpandWidth = false;

            var avatarContainer = CreateUIObject("AvatarContainer", profileInfo.transform);
            var avatarLayoutElement = avatarContainer.AddComponent<LayoutElement>();
            avatarLayoutElement.minWidth = 90;
            avatarLayoutElement.minHeight = 90;
            avatarLayoutElement.preferredWidth = 90;
            avatarLayoutElement.preferredHeight = 90;
            avatarContainer.AddComponent<Image>().color = AccentGold;

            var userInfoContainer = CreateUIObject("UserInfoContainer", profileInfo.transform);
            userInfoContainer.AddComponent<LayoutElement>().flexibleWidth = 1;

            var userInfoLayout = userInfoContainer.AddComponent<VerticalLayoutGroup>();
            userInfoLayout.padding = new RectOffset(10, 0, 10, 10); // Отступы для текстов
            userInfoLayout.spacing = 12;
            userInfoLayout.childAlignment = TextAnchor.MiddleLeft;
            userInfoLayout.childControlHeight = false; // Тексты сами определяют высоту
            userInfoLayout.childControlWidth = true;
            userInfoLayout.childForceExpandHeight = false;
            userInfoLayout.childForceExpandWidth = true;

            var usernameText = CreateTextElement("UsernameText", userInfoContainer.transform);
            var usernameComponent = usernameText.GetComponent<TextMeshProUGUI>();
            usernameComponent.text = "Пользователь";
            usernameComponent.fontSize = 36;
            usernameComponent.fontWeight = FontWeight.Bold;
            usernameComponent.color = TextDark;
            usernameText.AddComponent<LayoutElement>().preferredHeight = 45;


            var statusText = CreateTextElement("StatusText", userInfoContainer.transform);
            var statusComponent = statusText.GetComponent<TextMeshProUGUI>();
            statusComponent.text = "Как дела сегодня?";
            statusComponent.fontSize = 26;
            statusComponent.color = TextDark;
            statusComponent.alpha = 0.85f;
            statusText.AddComponent<LayoutElement>().preferredHeight = 35;

            var profileInfoComponent = profileInfo.AddComponent<ProfileInfoComponent>();
            var serializedProfileInfo = new SerializedObject(profileInfoComponent);
            serializedProfileInfo.FindProperty("_usernameText").objectReferenceValue = usernameComponent;
            serializedProfileInfo.FindProperty("_currentEmotionImage").objectReferenceValue = avatarContainer.GetComponent<Image>();
            serializedProfileInfo.ApplyModifiedProperties();
            return profileInfo;
        }
        #endregion

        #region Emotion Jars Component
        private static GameObject CreateEmotionJars(Transform parent)
        {
            var emotionJarsContainer = CreateUIObject("EmotionJars", parent);
            var layoutElement = emotionJarsContainer.AddComponent<LayoutElement>();
            layoutElement.minHeight = 450; // Увеличена минимальная высота для банок
            layoutElement.preferredHeight = -1; // Позволяем GridLayout определить высоту
            layoutElement.flexibleHeight = 1; // Растягивается, если есть место

            var backgroundImage = emotionJarsContainer.AddComponent<Image>();
            backgroundImage.color = PaperBeige;

            var verticalLayout = emotionJarsContainer.AddComponent<VerticalLayoutGroup>();
            verticalLayout.padding = new RectOffset(25, 25, 25, 25);
            verticalLayout.spacing = 20;
            verticalLayout.childAlignment = TextAnchor.UpperCenter;
            verticalLayout.childControlHeight = false;
            verticalLayout.childControlWidth = true;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.childForceExpandWidth = true;

            var titleText = CreateTextElement("Title", emotionJarsContainer.transform);
            var titleComponent = titleText.GetComponent<TextMeshProUGUI>();
            titleComponent.text = "Банки Эмоций";
            titleComponent.fontSize = 34;
            titleComponent.fontWeight = FontWeight.Bold;
            titleComponent.color = TextDark;
            titleComponent.alignment = TextAlignmentOptions.Center;
            titleText.AddComponent<LayoutElement>().minHeight = 40;

            var jarsGrid = CreateUIObject("JarsGrid", emotionJarsContainer.transform);
            var jarsGridLayoutElement = jarsGrid.AddComponent<LayoutElement>();
            jarsGridLayoutElement.flexibleHeight = 1;


            var gridLayout = jarsGrid.AddComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(10, 10, 15, 10); // Небольшие отступы
            gridLayout.cellSize = new Vector2(120, 170);
            gridLayout.spacing = new Vector2(25, 25);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;

            // ContentSizeFitter не нужен, если minHeight у родителя и flexibleHeight у grid
            // var gridContentSizeFitter = jarsGrid.AddComponent<ContentSizeFitter>();
            // gridContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var emotionJarView = emotionJarsContainer.AddComponent<EmotionJarView>();
            var serializedEmotionJars = new SerializedObject(emotionJarView);
            var emotionJarFields = new[] { "_joyJarFill", "_sadnessJarFill", "_angerJarFill", "_fearJarFill", "_disgustJarFill", "_trustJarFill", "_anticipationJarFill", "_surpriseJarFill", "_loveJarFill", "_anxietyJarFill", "_neutralJarFill" };
            var emotionTypes = System.Enum.GetValues(typeof(EmotionTypes));

            for (int i = 0; i < emotionTypes.Length; i++)
            {
                var emotionType = (EmotionTypes)emotionTypes.GetValue(i);
                var jarFillImage = CreateEmotionJar(jarsGrid.transform, emotionType, emotionJarView); // Возвращает Image заливки
                if (i < emotionJarFields.Length)
                {
                    serializedEmotionJars.FindProperty(emotionJarFields[i]).objectReferenceValue = jarFillImage;
                }
            }

            var bubblesContainer = CreateUIObject("BubblesContainer", emotionJarsContainer.transform);
            var bubblesRect = bubblesContainer.GetComponent<RectTransform>();
            SetFullStretch(bubblesRect);
            bubblesRect.SetAsLastSibling(); // Убедимся, что пузырьки поверх всего в этой секции
            serializedEmotionJars.FindProperty("_bubblesContainer").objectReferenceValue = bubblesContainer.transform;
            serializedEmotionJars.ApplyModifiedProperties();

            return emotionJarsContainer;
        }

        private static Image CreateEmotionJar(Transform parent, EmotionTypes emotionType, EmotionJarView emotionJarViewComponent) // Возвращает Image заливки
        {
            var jarContainer = CreateUIObject($"{emotionType}Jar", parent);

            var jarBackground = CreateUIObject("JarBackground", jarContainer.transform);
            var jarBackgroundRect = jarBackground.GetComponent<RectTransform>();
            SetFullStretch(jarBackgroundRect);
            var jarBackgroundImage = jarBackground.AddComponent<Image>();
            jarBackgroundImage.color = WarmWoodMedium;
            jarBackgroundImage.type = Image.Type.Sliced;

            var jarFill = CreateUIObject("JarFill", jarContainer.transform);
            var jarFillRect = jarFill.GetComponent<RectTransform>();
            SetFullStretch(jarFillRect);
            jarFillRect.offsetMin = new Vector2(8, 8); // Увеличены отступы для заливки
            jarFillRect.offsetMax = new Vector2(-8, -8);
            var jarFillImage = jarFill.AddComponent<Image>();
            jarFillImage.color = GetEmotionColor(emotionType);
            jarFillImage.type = Image.Type.Filled;
            jarFillImage.fillMethod = Image.FillMethod.Vertical;
            jarFillImage.fillOrigin = 0; // 0 = Bottom
            jarFillImage.fillAmount = Random.Range(0.1f, 0.7f);

            var labelText = CreateTextElement("Label", jarContainer.transform);
            var labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 0);
            labelRect.sizeDelta = new Vector2(0, 45);
            labelRect.anchoredPosition = new Vector2(0, -20);
            var labelComponent = labelText.GetComponent<TextMeshProUGUI>();
            labelComponent.text = GetEmotionDisplayName(emotionType);
            labelComponent.fontSize = 20;
            labelComponent.color = TextDark;
            labelComponent.alignment = TextAlignmentOptions.Center;
            labelComponent.enableAutoSizing = true;
            labelComponent.fontSizeMin = 16;
            labelComponent.fontSizeMax = 20;

            var button = jarContainer.AddComponent<Button>();
            button.targetGraphic = jarBackgroundImage;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            button.colors = colors;

            jarContainer.name = $"{emotionType}Jar";
            return jarFillImage; // Возвращаем Image заливки
        }
        #endregion

        #region Statistics Component
        private static GameObject CreateStatistics(Transform parent, GameObject regionStatItemPrefab)
        {
            var statisticsContainer = CreateUIObject("Statistics", parent);
            var layoutElement = statisticsContainer.AddComponent<LayoutElement>();
            layoutElement.minHeight = 220;
            layoutElement.preferredHeight = 280;
            layoutElement.flexibleHeight = 0;

            var backgroundImage = statisticsContainer.AddComponent<Image>();
            backgroundImage.color = WarmWoodMedium;

            var verticalLayout = statisticsContainer.AddComponent<VerticalLayoutGroup>();
            verticalLayout.padding = new RectOffset(30, 30, 30, 30);
            verticalLayout.spacing = 20;
            verticalLayout.childAlignment = TextAnchor.UpperCenter;
            verticalLayout.childControlHeight = false;
            verticalLayout.childControlWidth = true;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.childForceExpandWidth = true;

            var titleText = CreateTextElement("Title", statisticsContainer.transform);
            var titleComponent = titleText.GetComponent<TextMeshProUGUI>();
            titleComponent.text = "Статистика";
            titleComponent.fontSize = 38;
            titleComponent.fontWeight = FontWeight.Bold;
            titleComponent.color = TextDark;
            titleComponent.alignment = TextAlignmentOptions.Center;
            titleText.AddComponent<LayoutElement>().minHeight = 55; // Увеличена высота


            // --- General Stats - Ручное позиционирование ---
            var generalStats = CreateUIObject("GeneralStats", statisticsContainer.transform);
            var generalStatsRect = generalStats.GetComponent<RectTransform>();
            // generalStatsRect займет всю ширину благодаря родительскому VerticalLayoutGroup
            // Устанавливаем фиксированную высоту для этого блока
            var generalStatsLayoutElement = generalStats.AddComponent<LayoutElement>();
            generalStatsLayoutElement.minHeight = 50;
            generalStatsLayoutElement.preferredHeight = 50;
            generalStatsLayoutElement.flexibleHeight = 0;

            var pointsText = CreateTextElement("PointsText", generalStats.transform);
            var pointsRect = pointsText.GetComponent<RectTransform>();
            pointsRect.anchorMin = new Vector2(0, 0.5f);
            pointsRect.anchorMax = new Vector2(0.5f, 0.5f); // Половина ширины
            pointsRect.pivot = new Vector2(0, 0.5f);
            pointsRect.anchoredPosition = new Vector2(15, 0); // Отступ слева
            pointsRect.sizeDelta = new Vector2(-30, 0); // Ширина минус отступы с обеих сторон (если бы было до центра)
            var pointsComponent = pointsText.GetComponent<TextMeshProUGUI>();
            pointsComponent.text = "Очки: 0";
            pointsComponent.fontSize = 28;
            pointsComponent.color = AccentGold;
            pointsComponent.alignment = TextAlignmentOptions.Left;
            pointsComponent.fontWeight = FontWeight.Bold;

            var entriesText = CreateTextElement("EntriesText", generalStats.transform);
            var entriesRect = entriesText.GetComponent<RectTransform>();
            entriesRect.anchorMin = new Vector2(0.5f, 0.5f); // От середины
            entriesRect.anchorMax = new Vector2(1, 0.5f);
            entriesRect.pivot = new Vector2(1, 0.5f);
            entriesRect.anchoredPosition = new Vector2(-15, 0); // Отступ справа
            entriesRect.sizeDelta = new Vector2(-30, 0);
            var entriesComponent = entriesText.GetComponent<TextMeshProUGUI>();
            entriesComponent.text = "Записей: 0";
            entriesComponent.fontSize = 28;
            entriesComponent.color = AccentGold;
            entriesComponent.alignment = TextAlignmentOptions.Right;
            entriesComponent.fontWeight = FontWeight.Bold;
            // --- End of General Stats ---


            var regionalSection = CreateUIObject("RegionalSection", statisticsContainer.transform);
            var regionalSectionLayoutElement = regionalSection.AddComponent<LayoutElement>();
            regionalSectionLayoutElement.flexibleHeight = 1;
            regionalSectionLayoutElement.minHeight = 100;

            var regionalSectionLayout = regionalSection.AddComponent<VerticalLayoutGroup>();
            regionalSectionLayout.spacing = 12;
            regionalSectionLayout.childAlignment = TextAnchor.UpperCenter;
            regionalSectionLayout.childControlHeight = false;
            regionalSectionLayout.childControlWidth = true;

            var regionalTitleButtonObject = CreateUIObject("RegionalTitleButton", regionalSection.transform);
            var regionalTitleButton = regionalTitleButtonObject.AddComponent<Button>();
            var regionalTitleButtonImage = regionalTitleButtonObject.AddComponent<Image>();
            regionalTitleButtonImage.color = new Color(0, 0, 0, 0);
            regionalTitleButton.targetGraphic = regionalTitleButtonImage;
            var regionalTitleLayoutElement = regionalTitleButtonObject.AddComponent<LayoutElement>();
            regionalTitleLayoutElement.minHeight = 45;
            regionalTitleLayoutElement.preferredHeight = 45;

            var regionalTitleText = CreateTextElement("RegionalTitleText", regionalTitleButtonObject.transform);
            var regionalTitleComponent = regionalTitleText.GetComponent<TextMeshProUGUI>();
            regionalTitleComponent.text = "🏙️ Эмоции по районам города";
            regionalTitleComponent.fontSize = 26;
            regionalTitleComponent.color = TextDark;
            regionalTitleComponent.alignment = TextAlignmentOptions.Center;
            SetFullStretch(regionalTitleText.GetComponent<RectTransform>());

            var scrollView = CreateUIObject("RegionalScrollView", regionalSection.transform);
            scrollView.AddComponent<LayoutElement>().flexibleHeight = 1;
            var scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 20;

            var viewport = CreateUIObject("Viewport", scrollView.transform);
            var viewportRect = viewport.GetComponent<RectTransform>();
            SetFullStretch(viewportRect);
            var viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(WarmWoodLight.r, WarmWoodLight.g, WarmWoodLight.b, 0.5f); // Слегка затемненный для маски
            viewportImage.raycastTarget = false;
            var viewportMask = viewport.AddComponent<Mask>();
            viewportMask.showMaskGraphic = true; // Показать маску, чтобы скруглять края
            scrollRect.viewport = viewportRect;

            var content = CreateUIObject("Content", viewport.transform);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);

            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(15, 15, 15, 15);
            contentLayout.spacing = 12;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRect;

            var noDataText = CreateTextElement("NoDataText", content.transform);
            var noDataComponent = noDataText.GetComponent<TextMeshProUGUI>();
            noDataComponent.text = "🔄 Загружаем данные по районам...";
            noDataComponent.fontSize = 24;
            noDataComponent.color = TextDark;
            noDataComponent.alpha = 0.7f;
            noDataComponent.alignment = TextAlignmentOptions.Center;
            var noDataLayoutElement = noDataText.AddComponent<LayoutElement>();
            noDataLayoutElement.minHeight = 65;
            noDataLayoutElement.preferredHeight = 65;


            var regionalDetailPanel = CreateUIObject("RegionalStatisticsDetailPanel", statisticsContainer.transform);
            var regionalDetailPanelRect = regionalDetailPanel.GetComponent<RectTransform>();
            SetFullStretch(regionalDetailPanelRect);
            regionalDetailPanel.SetActive(false);
            var regionalDetailBackground = regionalDetailPanel.AddComponent<Image>();
            regionalDetailBackground.color = PaperBeige;
            var regionalDetailLayout = regionalDetailPanel.AddComponent<VerticalLayoutGroup>();
            regionalDetailLayout.padding = new RectOffset(20, 20, 20, 20); // Увеличены отступы
            regionalDetailLayout.spacing = 15;
            regionalDetailLayout.childAlignment = TextAnchor.UpperCenter;
            regionalDetailLayout.childControlHeight = false;
            regionalDetailLayout.childControlWidth = true;
            regionalDetailLayout.childForceExpandHeight = false;
            regionalDetailLayout.childForceExpandWidth = true;

            var regionalDetailTitle = CreateTextElement("DetailPanelTitle", regionalDetailPanel.transform);
            var regionalDetailTitleComp = regionalDetailTitle.GetComponent<TextMeshProUGUI>();
            regionalDetailTitleComp.text = "🏙️ Статистика эмоций по районам";
            regionalDetailTitleComp.fontSize = 32; // Увеличен шрифт
            regionalDetailTitleComp.fontWeight = FontWeight.Bold;
            regionalDetailTitleComp.color = TextDark;
            regionalDetailTitleComp.alignment = TextAlignmentOptions.Center;
            regionalDetailTitle.AddComponent<LayoutElement>().minHeight = 45;


            var regionalDetailScrollView = CreateUIObject("DetailScrollView", regionalDetailPanel.transform);
            regionalDetailScrollView.AddComponent<LayoutElement>().flexibleHeight = 1;
            var regionalDetailScrollRect = regionalDetailScrollView.AddComponent<ScrollRect>();
            regionalDetailScrollRect.horizontal = false;
            regionalDetailScrollRect.vertical = true;
            regionalDetailScrollRect.scrollSensitivity = 20;
            var regionalDetailViewport = CreateUIObject("DetailViewport", regionalDetailScrollView.transform);
            var regionalDetailViewportRect = regionalDetailViewport.GetComponent<RectTransform>();
            SetFullStretch(regionalDetailViewportRect);
            var regionalDetailViewportImage = regionalDetailViewport.AddComponent<Image>();
            regionalDetailViewportImage.color = new Color(0, 0, 0, 0.01f);
            regionalDetailViewportImage.raycastTarget = false;
            var regionalDetailViewportMask = regionalDetailViewport.AddComponent<Mask>();
            regionalDetailViewportMask.showMaskGraphic = false;
            regionalDetailScrollRect.viewport = regionalDetailViewportRect;
            var regionalDetailContent = CreateUIObject("DetailContent", regionalDetailViewport.transform);
            var regionalDetailContentRect = regionalDetailContent.GetComponent<RectTransform>();
            regionalDetailContentRect.anchorMin = new Vector2(0, 1);
            regionalDetailContentRect.anchorMax = new Vector2(1, 1);
            regionalDetailContentRect.pivot = new Vector2(0.5f, 1);
            regionalDetailContentRect.sizeDelta = new Vector2(0, 0);
            var regionalDetailContentLayout = regionalDetailContent.AddComponent<VerticalLayoutGroup>();
            regionalDetailContentLayout.padding = new RectOffset(10, 10, 10, 10);
            regionalDetailContentLayout.spacing = 10; // Увеличен spacing
            regionalDetailContentLayout.childAlignment = TextAnchor.UpperCenter;
            regionalDetailContentLayout.childControlHeight = true;
            regionalDetailContentLayout.childControlWidth = true;
            regionalDetailContentLayout.childForceExpandHeight = false;
            regionalDetailContentLayout.childForceExpandWidth = true;
            regionalDetailContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            regionalDetailScrollRect.content = regionalDetailContentRect;
            var placeholderDetailItem = CreateTextElement("PlaceholderDetailItem", regionalDetailContent.transform);
            placeholderDetailItem.GetComponent<TextMeshProUGUI>().text = "📊 Детальная статистика по районам загрузится автоматически...";
            placeholderDetailItem.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            placeholderDetailItem.GetComponent<TextMeshProUGUI>().fontSize = 20; // Увеличен шрифт
            placeholderDetailItem.AddComponent<LayoutElement>().minHeight = 60;


            var closeButtonObject = CreateNavigationButton(regionalDetailPanel.transform, "Закрыть", WarmWoodDark);
            var closeButtonLayoutElement = closeButtonObject.GetComponent<LayoutElement>();
            closeButtonLayoutElement.minHeight = 60; // Увеличена высота кнопки
            closeButtonLayoutElement.preferredHeight = 60;
            closeButtonLayoutElement.flexibleHeight = 0;
            closeButtonLayoutElement.preferredWidth = 250; // Увеличена ширина

            var statisticsView = statisticsContainer.AddComponent<StatisticsView>();
            var serializedStatistics = new SerializedObject(statisticsView);
            serializedStatistics.FindProperty("_pointsText").objectReferenceValue = pointsComponent;
            serializedStatistics.FindProperty("_entriesText").objectReferenceValue = entriesComponent;
            serializedStatistics.FindProperty("_regionalStatsContainer").objectReferenceValue = content.transform;
            serializedStatistics.FindProperty("_regionStatItemPrefab").objectReferenceValue = regionStatItemPrefab;
            serializedStatistics.FindProperty("_noRegionalDataText").objectReferenceValue = noDataComponent;
            serializedStatistics.FindProperty("_regionalStatsTitle").objectReferenceValue = regionalTitleComponent;
            var showRegionalDetailButtonProp = serializedStatistics.FindProperty("_showRegionalDetailButton");
            if (showRegionalDetailButtonProp != null) showRegionalDetailButtonProp.objectReferenceValue = regionalTitleButton;
            var regionalDetailPanelProp = serializedStatistics.FindProperty("_regionalDetailPanel");
            if (regionalDetailPanelProp != null) regionalDetailPanelProp.objectReferenceValue = regionalDetailPanel;
            var regionalDetailContentProp = serializedStatistics.FindProperty("_regionalDetailContentContainer");
            if (regionalDetailContentProp != null) regionalDetailContentProp.objectReferenceValue = regionalDetailContent.transform;
            var closeRegionalDetailButtonProp = serializedStatistics.FindProperty("_closeRegionalDetailButton");
            if (closeRegionalDetailButtonProp != null) closeRegionalDetailButtonProp.objectReferenceValue = closeButtonObject.GetComponent<Button>();
            serializedStatistics.ApplyModifiedProperties();
            Debug.Log("✅ StatisticsView настроен для региональной статистики");
            return statisticsContainer;
        }
        #endregion

        #region Navigation Component
        private static GameObject CreateNavigation(Transform parent)
        {
            var navigationContainer = CreateUIObject("Navigation", parent);
            var layoutElement = navigationContainer.AddComponent<LayoutElement>();
            layoutElement.minHeight = 80; // Увеличена высота панели навигации
            layoutElement.preferredHeight = 90;
            layoutElement.flexibleHeight = 0;

            var backgroundImage = navigationContainer.AddComponent<Image>();
            backgroundImage.color = WoodDarkBrown;

            var horizontalLayout = navigationContainer.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.padding = new RectOffset(15, 15, 10, 10); // Увеличены отступы
            horizontalLayout.spacing = 15; // Увеличен spacing
            horizontalLayout.childAlignment = TextAnchor.MiddleCenter;
            horizontalLayout.childControlHeight = true;
            horizontalLayout.childControlWidth = true;
            horizontalLayout.childForceExpandHeight = true;
            horizontalLayout.childForceExpandWidth = true;

            var buttonTexts = new[] { "Записать", "История", "Друзья", "Мастерская", "Настройки", "Выход" };
            var buttonColors = new[] { AccentGold, WarmWoodLight, WarmWoodLight, WarmWoodLight, WarmWoodLight, new Color(0.8f, 0.3f, 0.3f, 1f) };
            var buttons = new Button[6];
            for (int i = 0; i < buttonTexts.Length; i++)
            {
                buttons[i] = CreateNavigationButton(navigationContainer.transform, buttonTexts[i], buttonColors[i]);
            }

            var navigationComponent = navigationContainer.AddComponent<NavigationComponent>();
            var serializedNavigation = new SerializedObject(navigationComponent);
            serializedNavigation.FindProperty("_logEmotionButton").objectReferenceValue = buttons[0];
            serializedNavigation.FindProperty("_historyButton").objectReferenceValue = buttons[1];
            serializedNavigation.FindProperty("_friendsButton").objectReferenceValue = buttons[2];
            serializedNavigation.FindProperty("_workshopButton").objectReferenceValue = buttons[3];
            serializedNavigation.FindProperty("_settingsButton").objectReferenceValue = buttons[4];
            serializedNavigation.FindProperty("_quitButton").objectReferenceValue = buttons[5];
            serializedNavigation.ApplyModifiedProperties();
            return navigationContainer;
        }

        private static Button CreateNavigationButton(Transform parent, string text, Color backgroundColor)
        {
            var buttonObject = CreateUIObject($"Button_{text}", parent);
            var layoutElement = buttonObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 50; // Увеличена высота кнопок
            layoutElement.flexibleWidth = 1;

            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = backgroundColor;
            var buttonComponent = buttonObject.AddComponent<Button>();
            buttonComponent.targetGraphic = buttonImage;
            var colors = buttonComponent.colors;
            colors.normalColor = backgroundColor;
            colors.highlightedColor = new Color(backgroundColor.r * 0.92f, backgroundColor.g * 0.92f, backgroundColor.b * 0.92f, backgroundColor.a); // Меньше затемнение
            colors.pressedColor = new Color(backgroundColor.r * 0.85f, backgroundColor.g * 0.85f, backgroundColor.b * 0.85f, backgroundColor.a);
            colors.selectedColor = colors.highlightedColor;
            buttonComponent.colors = colors;

            var buttonText = CreateTextElement("Text", buttonObject.transform);
            var textRect = buttonText.GetComponent<RectTransform>();
            SetFullStretch(textRect);
            var textComponent = buttonText.GetComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 22; // Увеличен шрифт
            textComponent.fontWeight = FontWeight.SemiBold;
            textComponent.color = (backgroundColor.grayscale > 0.5f) ? TextDark : TextLight; // Адаптивный цвет текста
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.enableAutoSizing = true;
            textComponent.fontSizeMin = 16;
            textComponent.fontSizeMax = 22;
            return buttonComponent;
        }
        #endregion

        #region Controllers Setup
        private static void SetupControllers(GameObject root, GameObject profileInfo, GameObject emotionJars, GameObject statistics, GameObject navigation)
        {
            var uiController = root.AddComponent<PersonalAreaUIController>();
            var serializedUI = new SerializedObject(uiController);
            serializedUI.FindProperty("_profileInfo").objectReferenceValue = profileInfo.GetComponent<ProfileInfoComponent>();
            serializedUI.FindProperty("_emotionJars").objectReferenceValue = emotionJars.GetComponent<EmotionJarView>();
            serializedUI.FindProperty("_statistics").objectReferenceValue = statistics.GetComponent<StatisticsView>();
            serializedUI.FindProperty("_navigation").objectReferenceValue = navigation.GetComponent<NavigationComponent>();
            serializedUI.ApplyModifiedProperties();

            var jarInteractionHandler = root.AddComponent<JarInteractionHandler>();
            var jarsGrid = emotionJars.transform.Find("JarsGrid");
            if (jarsGrid != null)
            {
                foreach (Transform jarContainer in jarsGrid)
                {
                    string jarName = jarContainer.name;
                    if (jarName.EndsWith("Jar") && jarName.Length > 3)
                    {
                        string emotionName = jarName.Substring(0, jarName.Length - 3);
                        var button = jarContainer.GetComponent<Button>();
                        if (button != null)
                        {
                            button.onClick.RemoveAllListeners();
                            UnityEditor.Events.UnityEventTools.AddStringPersistentListener(
                                button.onClick,
                                jarInteractionHandler.OnJarClicked,
                                emotionName);
                        }
                    }
                }
            }

            var personalAreaManager = root.AddComponent<PersonalAreaManager>();
            var serializedManager = new SerializedObject(personalAreaManager);
            var uiCanvasProperty = serializedManager.FindProperty("_ui") ??
                                   serializedManager.FindProperty("ui") ??
                                   serializedManager.FindProperty("_personalAreaCanvas") ??
                                   serializedManager.FindProperty("_personalAreaView");

            if (uiCanvasProperty != null && uiCanvasProperty.propertyType == SerializedPropertyType.ObjectReference)
            {
                uiCanvasProperty.objectReferenceValue = uiController; // Связываем с UIController
            }
            else
            {
                Debug.LogError("Не удалось найти свойство для UI в PersonalAreaManager или тип свойства не ObjectReference.");
            }
            serializedManager.ApplyModifiedProperties();
        }
        #endregion

        #region Utility Methods
        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var uiObject = new GameObject(name, typeof(RectTransform));
            uiObject.transform.SetParent(parent, false);
            // Устанавливаем слой UI для всех создаваемых объектов
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer != -1) uiObject.layer = uiLayer;
            return uiObject;
        }

        private static GameObject CreateTextElement(string name, Transform parent)
        {
            var textObject = CreateUIObject(name, parent); // Уже установит слой UI
            var textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = 18; // Базовый размер, будет переопределен
            textComponent.color = TextDark;
            textComponent.alignment = TextAlignmentOptions.Left;
            textComponent.raycastTarget = false; // Текст обычно не интерактивен
            return textObject;
        }

        private static void SetFullStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetLayerRecursively(Transform parent, int layer)
        {
            parent.gameObject.layer = layer;
            foreach (Transform child in parent)
            {
                SetLayerRecursively(child, layer);
            }
        }

        private static Color GetEmotionColor(EmotionTypes emotionType)
        {
            // Используем более пастельные и мягкие тона для эмоций
            return emotionType switch
            {
                EmotionTypes.Joy => new Color(1f, 0.9f, 0.3f, 0.85f),      // Ярче, теплее желтый
                EmotionTypes.Sadness => new Color(0.4f, 0.55f, 0.85f, 0.85f),// Мягкий синий
                EmotionTypes.Anger => new Color(0.9f, 0.4f, 0.4f, 0.85f),    // Менее кричащий красный
                EmotionTypes.Fear => new Color(0.6f, 0.4f, 0.7f, 0.85f),     // Приглушенный фиолетовый
                EmotionTypes.Disgust => new Color(0.4f, 0.7f, 0.4f, 0.85f),  // Спокойный зеленый
                EmotionTypes.Trust => new Color(0.3f, 0.7f, 0.9f, 0.85f),   // Светло-голубой
                EmotionTypes.Anticipation => new Color(1f, 0.65f, 0.2f, 0.85f),// Теплый оранжевый
                EmotionTypes.Surprise => new Color(0.85f, 0.55f, 0.9f, 0.85f),// Лавандовый
                EmotionTypes.Love => new Color(0.95f, 0.5f, 0.7f, 0.85f),   // Нежный розовый
                EmotionTypes.Anxiety => new Color(0.75f, 0.75f, 0.75f, 0.85f),// Светло-серый
                EmotionTypes.Neutral => new Color(0.88f, 0.88f, 0.88f, 0.85f),// Очень светло-серый, почти белый
                _ => new Color(0.8f, 0.8f, 0.8f, 0.85f) // Дефолтный серый
            };
        }

        private static string GetEmotionDisplayName(EmotionTypes emotionType)
        {
            return emotionType switch
            {
                EmotionTypes.Joy => "Радость",
                EmotionTypes.Sadness => "Грусть",
                EmotionTypes.Anger => "Гнев",
                EmotionTypes.Fear => "Страх",
                EmotionTypes.Disgust => "Отвращение",
                EmotionTypes.Trust => "Доверие",
                EmotionTypes.Anticipation => "Предвкушение",
                EmotionTypes.Surprise => "Удивление",
                EmotionTypes.Love => "Любовь",
                EmotionTypes.Anxiety => "Тревога",
                EmotionTypes.Neutral => "Нейтральное",
                _ => emotionType.ToString()
            };
        }
        #endregion
    }
}
#endif