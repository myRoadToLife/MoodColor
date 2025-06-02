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

            // Создаем основной корневой Canvas с правильными настройками
            var root = CreateMainCanvas();

            // Создаем простой фон без декоративных элементов
            CreateSimpleBackground(root.transform);

            // Создаем SafeArea для адаптации к различным экранам
            var safeArea = CreateSafeArea(root.transform);

            // Создаем основной контент с responsive разметкой
            var mainContent = CreateMainContent(safeArea.transform);

            // Создаем компоненты интерфейса
            var profileInfo = CreateProfileInfo(mainContent.transform);
            var emotionJars = CreateEmotionJars(mainContent.transform);
            var statistics = CreateStatistics(mainContent.transform);
            var navigation = CreateNavigation(mainContent.transform);

            // Добавляем компоненты управления
            SetupControllers(root, profileInfo, emotionJars, statistics, navigation);

            // Сохраняем префаб
            Debug.Log($"💾 Сохраняем префаб в {Path.Combine(PREFAB_SAVE_FOLDER_PATH, PREFAB_NAME + ".prefab")}");
            UIComponentGenerator.SavePrefab(root, PREFAB_SAVE_FOLDER_PATH, PREFAB_NAME);

            if (!Application.isPlaying) Object.DestroyImmediate(root);

            Debug.Log("✅ Генерация префаба Personal Area Canvas завершена");
        }

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

            // Установка Tag и Layer
            canvasGO.tag = "Untagged"; // Стандартный тэг
            // Предполагаем, что UI элементы должны быть на слое "UI". Если нет, используйте "Default"
            // Убедитесь, что слой "UI" существует в вашем проекте.
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer == -1) // Если слой UI не найден, используем Default
            {
                Debug.LogWarning("Слой 'UI' не найден. Используется слой 'Default' для Canvas.");
                uiLayer = LayerMask.NameToLayer("Default");
            }
            canvasGO.layer = uiLayer;
            // Рекурсивно установить слой для всех дочерних элементов, если это необходимо
            // SetLayerRecursively(canvasGO.transform, uiLayer);

            var canvasScaler = canvasGO.GetComponent<CanvasScaler>();
            if (canvasScaler == null) canvasScaler = canvasGO.AddComponent<CanvasScaler>();

            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1080, 1920);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            canvasScaler.referencePixelsPerUnit = 100;

            // Graphic Raycaster добавляется автоматически с Canvas

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
            backgroundImage.color = WarmWoodLight; // Простой теплый фон
        }
        #endregion

        #region Room Background
        private static void CreateRoomBackground(Transform parent)
        {
            var background = CreateUIObject("RoomBackground", parent);
            var backgroundRect = background.GetComponent<RectTransform>();
            SetFullStretch(backgroundRect);

            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = WarmWoodLight;

            // Создаем оконную раму как декоративный элемент
            CreateWindowFrame(background.transform);

            // Создаем деревянную полку
            CreateWoodenShelf(background.transform);
        }

        private static void CreateWindowFrame(Transform parent)
        {
            var windowFrame = CreateUIObject("WindowFrame", parent);
            var windowFrameRect = windowFrame.GetComponent<RectTransform>();
            windowFrameRect.anchorMin = new Vector2(0.5f, 1f);
            windowFrameRect.anchorMax = new Vector2(0.5f, 1f);
            windowFrameRect.sizeDelta = new Vector2(400, 250);
            windowFrameRect.anchoredPosition = new Vector2(0, -50);

            var windowFrameImage = windowFrame.AddComponent<Image>();
            windowFrameImage.color = WarmWoodDark;

            // Внутренняя часть окна
            var windowView = CreateUIObject("WindowView", windowFrame.transform);
            var windowViewRect = windowView.GetComponent<RectTransform>();
            windowViewRect.anchorMin = Vector2.zero;
            windowViewRect.anchorMax = Vector2.one;
            windowViewRect.offsetMin = new Vector2(15, 15);
            windowViewRect.offsetMax = new Vector2(-15, -15);

            var windowViewImage = windowView.AddComponent<Image>();
            windowViewImage.color = GlassBlue;
        }

        private static void CreateWoodenShelf(Transform parent)
        {
            var shelf = CreateUIObject("WoodenShelf", parent);
            var shelfRect = shelf.GetComponent<RectTransform>();
            shelfRect.anchorMin = new Vector2(0, 0);
            shelfRect.anchorMax = new Vector2(1, 0);
            shelfRect.sizeDelta = new Vector2(0, 25);
            shelfRect.anchoredPosition = new Vector2(0, 25);

            var shelfImage = shelf.AddComponent<Image>();
            shelfImage.color = WoodDarkBrown;
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
            mainLayout.padding = new RectOffset(20, 20, 20, 20); // Уменьшаем отступы для большего контента
            mainLayout.spacing = 15; // Уменьшаем расстояние между секциями
            mainLayout.childAlignment = TextAnchor.UpperCenter;
            mainLayout.childControlHeight = true; // MainContent КОНТРОЛИРУЕТ высоту дочерних секций
            mainLayout.childControlWidth = true;
            mainLayout.childForceExpandHeight = true; // И ЗАСТАВЛЯЕТ их растягиваться
            mainLayout.childForceExpandWidth = true;

            return mainContent;
        }
        #endregion

        #region Profile Info Component
        private static GameObject CreateProfileInfo(Transform parent)
        {
            var profileInfo = CreateUIObject("ProfileInfo", parent);
            var layoutElement = profileInfo.AddComponent<LayoutElement>();
            layoutElement.minHeight = 100;
            layoutElement.preferredHeight = 120; // Фиксированная предпочтительная высота
            layoutElement.flexibleHeight = 0; // Не растягивается вертикально в MainContent

            var backgroundImage = profileInfo.AddComponent<Image>();
            backgroundImage.color = WarmWoodMedium;

            var horizontalLayout = profileInfo.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.padding = new RectOffset(20, 20, 15, 15);
            horizontalLayout.spacing = 15;
            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.childControlHeight = true; // Контролируем высоту аватара и текстового блока
            horizontalLayout.childControlWidth = false; // Ширина будет управляться LayoutElement
            horizontalLayout.childForceExpandHeight = true;
            horizontalLayout.childForceExpandWidth = false;

            var avatarContainer = CreateUIObject("AvatarContainer", profileInfo.transform);
            var avatarLayoutElement = avatarContainer.AddComponent<LayoutElement>();
            avatarLayoutElement.minWidth = 60;
            avatarLayoutElement.minHeight = 60;
            avatarLayoutElement.preferredWidth = 80;
            avatarLayoutElement.preferredHeight = 80;
            avatarLayoutElement.flexibleWidth = 0;
            avatarLayoutElement.flexibleHeight = 0;

            var avatarImage = avatarContainer.AddComponent<Image>();
            avatarImage.color = AccentGold;
            avatarImage.preserveAspect = true;

            var userInfoContainer = CreateUIObject("UserInfoContainer", profileInfo.transform);
            var userInfoContainerLayoutElement = userInfoContainer.AddComponent<LayoutElement>();
            userInfoContainerLayoutElement.flexibleWidth = 1; // Занимает оставшуюся ширину

            var userInfoLayout = userInfoContainer.AddComponent<VerticalLayoutGroup>();
            userInfoLayout.padding = new RectOffset(10, 0, 5, 5); // Небольшой отступ сверху и снизу
            userInfoLayout.spacing = 8; // Увеличим немного расстояние между именем и статусом
            userInfoLayout.childAlignment = TextAnchor.MiddleLeft;
            userInfoLayout.childControlHeight = false;
            userInfoLayout.childControlWidth = true;
            userInfoLayout.childForceExpandHeight = false;
            userInfoLayout.childForceExpandWidth = true;

            var usernameText = CreateTextElement("UsernameText", userInfoContainer.transform);
            var usernameComponent = usernameText.GetComponent<TextMeshProUGUI>();
            usernameComponent.text = "Пользователь";
            usernameComponent.fontSize = 32; // Увеличено
            usernameComponent.fontWeight = FontWeight.Bold;
            usernameComponent.color = TextDark;
            usernameComponent.alignment = TextAlignmentOptions.Left;
            var usernameLayout = usernameText.AddComponent<LayoutElement>();
            usernameLayout.minHeight = 38; // Увеличено
            usernameLayout.preferredHeight = 42; // Увеличено

            var statusText = CreateTextElement("StatusText", userInfoContainer.transform);
            var statusComponent = statusText.GetComponent<TextMeshProUGUI>();
            statusComponent.text = "Как дела сегодня?";
            statusComponent.fontSize = 22; // Увеличено
            statusComponent.color = TextDark;
            statusComponent.alpha = 0.8f;
            statusComponent.alignment = TextAlignmentOptions.Left;
            var statusLayout = statusText.AddComponent<LayoutElement>();
            statusLayout.minHeight = 28; // Увеличено
            statusLayout.preferredHeight = 30; // Увеличено

            var profileInfoComponent = profileInfo.AddComponent<ProfileInfoComponent>();
            var serializedProfileInfo = new SerializedObject(profileInfoComponent);
            serializedProfileInfo.FindProperty("_usernameText").objectReferenceValue = usernameComponent;
            serializedProfileInfo.FindProperty("_currentEmotionImage").objectReferenceValue = avatarImage;
            serializedProfileInfo.ApplyModifiedProperties();

            return profileInfo;
        }
        #endregion

        #region Emotion Jars Component
        private static GameObject CreateEmotionJars(Transform parent)
        {
            var emotionJarsContainer = CreateUIObject("EmotionJars", parent);
            var layoutElement = emotionJarsContainer.AddComponent<LayoutElement>();
            layoutElement.minHeight = 200; // Минимальная высота для заголовка и одной строки банок
            layoutElement.flexibleHeight = 1; // Основная растягиваемая секция

            var backgroundImage = emotionJarsContainer.AddComponent<Image>();
            backgroundImage.color = PaperBeige;

            var verticalLayout = emotionJarsContainer.AddComponent<VerticalLayoutGroup>();
            verticalLayout.padding = new RectOffset(15, 15, 15, 15);
            verticalLayout.spacing = 10;
            verticalLayout.childAlignment = TextAnchor.UpperCenter;
            verticalLayout.childControlHeight = false; // Заголовок и Grid сами управляют высотой
            verticalLayout.childControlWidth = true;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.childForceExpandWidth = true;

            var titleText = CreateTextElement("Title", emotionJarsContainer.transform);
            var titleComponent = titleText.GetComponent<TextMeshProUGUI>();
            titleComponent.text = "Банки Эмоций";
            titleComponent.fontSize = 28; // Увеличено
            titleComponent.fontWeight = FontWeight.Bold;
            titleComponent.color = TextDark;
            titleComponent.alignment = TextAlignmentOptions.Center;
            var titleLayoutElement = titleText.AddComponent<LayoutElement>();
            titleLayoutElement.minHeight = 30;
            titleLayoutElement.preferredHeight = 35; // Увеличено

            var jarsGrid = CreateUIObject("JarsGrid", emotionJarsContainer.transform);
            var jarsGridLayoutElement = jarsGrid.AddComponent<LayoutElement>();
            jarsGridLayoutElement.flexibleHeight = 1; // Позволяем сетке растягиваться внутри своей секции

            var gridLayout = jarsGrid.AddComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(0, 0, 5, 0);
            gridLayout.cellSize = new Vector2(100, 130); // Немного уменьшил высоту банок
            gridLayout.spacing = new Vector2(15, 15);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;

            var gridContentSizeFitter = jarsGrid.AddComponent<ContentSizeFitter>();
            gridContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var emotionJars = new Image[11];
            var emotionTypes = System.Enum.GetValues(typeof(EmotionTypes));
            for (int i = 0; i < emotionTypes.Length; i++)
            {
                var emotionType = (EmotionTypes)emotionTypes.GetValue(i);
                var jarObject = CreateEmotionJar(jarsGrid.transform, emotionType, emotionJarsContainer.GetComponent<EmotionJarView>());
                emotionJars[i] = jarObject.GetComponentInChildren<Image>();
            }

            var bubblesContainer = CreateUIObject("BubblesContainer", emotionJarsContainer.transform);
            var bubblesRect = bubblesContainer.GetComponent<RectTransform>();
            SetFullStretch(bubblesRect);
            bubblesRect.SetAsLastSibling();

            var emotionJarView = emotionJarsContainer.AddComponent<EmotionJarView>();
            var serializedEmotionJars = new SerializedObject(emotionJarView);
            var emotionJarFields = new[] { "_joyJarFill", "_sadnessJarFill", "_angerJarFill", "_fearJarFill", "_disgustJarFill", "_trustJarFill", "_anticipationJarFill", "_surpriseJarFill", "_loveJarFill", "_anxietyJarFill", "_neutralJarFill" };
            for (int i = 0; i < emotionJars.Length && i < emotionJarFields.Length; i++)
            {
                serializedEmotionJars.FindProperty(emotionJarFields[i]).objectReferenceValue = emotionJars[i];
            }
            serializedEmotionJars.FindProperty("_bubblesContainer").objectReferenceValue = bubblesContainer.transform;
            serializedEmotionJars.ApplyModifiedProperties();

            return emotionJarsContainer;
        }

        private static GameObject CreateEmotionJar(Transform parent, EmotionTypes emotionType, EmotionJarView emotionJarViewComponent)
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
            jarFillRect.offsetMin = new Vector2(5, 5);
            jarFillRect.offsetMax = new Vector2(-5, -5);
            var jarFillImage = jarFill.AddComponent<Image>();
            jarFillImage.color = GetEmotionColor(emotionType);
            jarFillImage.type = Image.Type.Filled;
            jarFillImage.fillMethod = Image.FillMethod.Vertical;
            jarFillImage.fillOrigin = 0;
            jarFillImage.fillAmount = Random.Range(0.1f, 0.7f);

            var labelText = CreateTextElement("Label", jarContainer.transform);
            var labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 0);
            labelRect.sizeDelta = new Vector2(0, 30);
            labelRect.anchoredPosition = new Vector2(0, -12);
            var labelComponent = labelText.GetComponent<TextMeshProUGUI>();
            labelComponent.text = GetEmotionDisplayName(emotionType);
            labelComponent.fontSize = 15;
            labelComponent.color = TextDark;
            labelComponent.alignment = TextAlignmentOptions.Center;
            labelComponent.enableAutoSizing = true;
            labelComponent.fontSizeMin = 10;
            labelComponent.fontSizeMax = 15;

            var button = jarContainer.AddComponent<Button>();
            button.targetGraphic = jarBackgroundImage;

            var colors = button.colors;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            button.colors = colors;

            // Здесь НЕ настраиваем onClick, это будет сделано в SetupControllers
            // Сохраняем тип эмоции в имени кнопки для дальнейшего использования
            jarContainer.name = $"{emotionType}Jar"; // Убедимся, что имя содержит тип эмоции

            return jarFill;
        }
        #endregion

        #region Statistics Component
        private static GameObject CreateStatistics(Transform parent)
        {
            var statisticsContainer = CreateUIObject("Statistics", parent);
            var layoutElement = statisticsContainer.AddComponent<LayoutElement>();
            layoutElement.minHeight = 120;
            layoutElement.preferredHeight = 160; // Фиксированная предпочтительная высота
            layoutElement.flexibleHeight = 0; // Не растягивается

            var backgroundImage = statisticsContainer.AddComponent<Image>();
            backgroundImage.color = WarmWoodMedium;

            var verticalLayout = statisticsContainer.AddComponent<VerticalLayoutGroup>();
            verticalLayout.padding = new RectOffset(20, 20, 15, 15);
            verticalLayout.spacing = 8;
            verticalLayout.childAlignment = TextAnchor.UpperCenter;
            verticalLayout.childControlHeight = false; // Внутренние элементы сами управляют высотой
            verticalLayout.childControlWidth = true;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.childForceExpandWidth = true;

            var titleText = CreateTextElement("Title", statisticsContainer.transform);
            var titleComponent = titleText.GetComponent<TextMeshProUGUI>();
            titleComponent.text = "Статистика";
            titleComponent.fontSize = 26; // Увеличено
            titleComponent.fontWeight = FontWeight.Bold;
            titleComponent.color = TextDark;
            titleComponent.alignment = TextAlignmentOptions.Center;
            var titleLayoutElement = titleText.AddComponent<LayoutElement>();
            titleLayoutElement.minHeight = 30; // Увеличено
            titleLayoutElement.preferredHeight = 35; // Увеличено

            var generalStats = CreateUIObject("GeneralStats", statisticsContainer.transform);
            var generalStatsLayout = generalStats.AddComponent<HorizontalLayoutGroup>();
            generalStatsLayout.spacing = 20;
            generalStatsLayout.childAlignment = TextAnchor.MiddleCenter;
            generalStatsLayout.childControlHeight = true;
            generalStatsLayout.childControlWidth = true;
            generalStatsLayout.childForceExpandWidth = true;
            var generalStatsLayoutElement = generalStats.AddComponent<LayoutElement>();
            generalStatsLayoutElement.minHeight = 30;
            generalStatsLayoutElement.preferredHeight = 35;

            var pointsText = CreateTextElement("PointsText", generalStats.transform);
            var pointsComponent = pointsText.GetComponent<TextMeshProUGUI>();
            pointsComponent.text = "Очки: 0";
            pointsComponent.fontSize = 20; // Увеличено
            pointsComponent.color = AccentGold;
            pointsComponent.alignment = TextAlignmentOptions.Center;
            pointsComponent.fontWeight = FontWeight.Bold;

            var entriesText = CreateTextElement("EntriesText", generalStats.transform);
            var entriesComponent = entriesText.GetComponent<TextMeshProUGUI>();
            entriesComponent.text = "Записей: 0";
            entriesComponent.fontSize = 20; // Увеличено
            entriesComponent.color = AccentGold;
            entriesComponent.alignment = TextAlignmentOptions.Center;
            entriesComponent.fontWeight = FontWeight.Bold;

            var regionalSection = CreateUIObject("RegionalSection", statisticsContainer.transform);
            var regionalSectionLayoutElement = regionalSection.AddComponent<LayoutElement>();
            regionalSectionLayoutElement.flexibleHeight = 1; // Эта часть растягивается внутри статистики
            regionalSectionLayoutElement.minHeight = 50;

            var regionalSectionLayout = regionalSection.AddComponent<VerticalLayoutGroup>();
            regionalSectionLayout.spacing = 5;
            regionalSectionLayout.childAlignment = TextAnchor.UpperCenter;
            regionalSectionLayout.childControlHeight = false;
            regionalSectionLayout.childControlWidth = true;

            var regionalTitleText = CreateTextElement("RegionalTitle", regionalSection.transform);
            var regionalTitleComponent = regionalTitleText.GetComponent<TextMeshProUGUI>();
            regionalTitleComponent.text = "Эмоции по районам";
            regionalTitleComponent.fontSize = 18; // Увеличено
            regionalTitleComponent.color = TextDark;
            regionalTitleComponent.alignment = TextAlignmentOptions.Center;
            var regionalTitleLayoutElement = regionalTitleText.AddComponent<LayoutElement>();
            regionalTitleLayoutElement.minHeight = 22; // Увеличено
            regionalTitleLayoutElement.preferredHeight = 25; // Увеличено

            var scrollView = CreateUIObject("RegionalScrollView", regionalSection.transform);
            var scrollViewLayoutElement = scrollView.AddComponent<LayoutElement>();
            scrollViewLayoutElement.flexibleHeight = 1;
            var scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 20;

            var viewport = CreateUIObject("Viewport", scrollView.transform);
            var viewportRect = viewport.GetComponent<RectTransform>();
            SetFullStretch(viewportRect);
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = viewportRect;

            var content = CreateUIObject("Content", viewport.transform);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);

            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(5, 5, 5, 5);
            contentLayout.spacing = 5;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRect;

            var noDataText = CreateTextElement("NoDataText", content.transform);
            var noDataComponent = noDataText.GetComponent<TextMeshProUGUI>();
            noDataComponent.text = "Нет данных по районам";
            noDataComponent.fontSize = 18; // Увеличено
            noDataComponent.color = TextDark;
            noDataComponent.alpha = 0.7f;
            noDataComponent.alignment = TextAlignmentOptions.Center;

            var statisticsView = statisticsContainer.AddComponent<StatisticsView>();
            var serializedStatistics = new SerializedObject(statisticsView);
            serializedStatistics.FindProperty("_pointsText").objectReferenceValue = pointsComponent;
            serializedStatistics.FindProperty("_entriesText").objectReferenceValue = entriesComponent;
            serializedStatistics.FindProperty("_regionalStatsContainer").objectReferenceValue = content.transform;
            serializedStatistics.FindProperty("_noRegionalDataText").objectReferenceValue = noDataComponent;
            serializedStatistics.FindProperty("_regionalStatsTitle").objectReferenceValue = regionalTitleComponent;
            serializedStatistics.ApplyModifiedProperties();

            return statisticsContainer;
        }
        #endregion

        #region Navigation Component
        private static GameObject CreateNavigation(Transform parent)
        {
            var navigationContainer = CreateUIObject("Navigation", parent);
            var layoutElement = navigationContainer.AddComponent<LayoutElement>();
            layoutElement.minHeight = 70;
            layoutElement.preferredHeight = 80; // Фиксированная высота
            layoutElement.flexibleHeight = 0; // Не растягивается

            var backgroundImage = navigationContainer.AddComponent<Image>();
            backgroundImage.color = WoodDarkBrown;

            var horizontalLayout = navigationContainer.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.padding = new RectOffset(10, 10, 10, 10);
            horizontalLayout.spacing = 8;
            horizontalLayout.childAlignment = TextAnchor.MiddleCenter;
            horizontalLayout.childControlHeight = true; // Кнопки сами определяют свою высоту через LayoutElement
            horizontalLayout.childControlWidth = true;
            horizontalLayout.childForceExpandHeight = true; // Растягиваем кнопки по высоте контейнера
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
            layoutElement.minHeight = 40; // Минимальная высота кнопки
            layoutElement.flexibleWidth = 1; // Равномерно делят ширину

            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = backgroundColor;

            var buttonComponent = buttonObject.AddComponent<Button>();
            buttonComponent.targetGraphic = buttonImage;

            var colors = buttonComponent.colors;
            colors.normalColor = backgroundColor;
            colors.highlightedColor = new Color(backgroundColor.r * 0.9f, backgroundColor.g * 0.9f, backgroundColor.b * 0.9f, backgroundColor.a);
            colors.pressedColor = new Color(backgroundColor.r * 0.8f, backgroundColor.g * 0.8f, backgroundColor.b * 0.8f, backgroundColor.a);
            colors.selectedColor = colors.highlightedColor;
            buttonComponent.colors = colors;

            var buttonText = CreateTextElement("Text", buttonObject.transform);
            var textRect = buttonText.GetComponent<RectTransform>();
            SetFullStretch(textRect);
            var textComponent = buttonText.GetComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 18; // Увеличено
            textComponent.fontWeight = FontWeight.SemiBold; // Изменено на SemiBold для лучшей читаемости
            textComponent.color = TextDark;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.enableAutoSizing = true;
            textComponent.fontSizeMin = 12; // Увеличено
            textComponent.fontSizeMax = 18; // Увеличено

            return buttonComponent;
        }
        #endregion

        #region Controllers Setup
        private static void SetupControllers(GameObject root, GameObject profileInfo, GameObject emotionJars, GameObject statistics, GameObject navigation)
        {
            // Настройка PersonalAreaUIController
            var uiController = root.AddComponent<PersonalAreaUIController>();
            var serializedUI = new SerializedObject(uiController);
            serializedUI.FindProperty("_profileInfo").objectReferenceValue = profileInfo.GetComponent<ProfileInfoComponent>();
            serializedUI.FindProperty("_emotionJars").objectReferenceValue = emotionJars.GetComponent<EmotionJarView>();
            serializedUI.FindProperty("_statistics").objectReferenceValue = statistics.GetComponent<StatisticsView>();
            serializedUI.FindProperty("_navigation").objectReferenceValue = navigation.GetComponent<NavigationComponent>();
            serializedUI.ApplyModifiedProperties();

            // Добавляем JarInteractionHandler для обработки кликов по банкам эмоций
            var jarInteractionHandler = root.AddComponent<JarInteractionHandler>();

            // Находим все кнопки банок эмоций и настраиваем их
            var jarsGrid = emotionJars.transform.Find("JarsGrid");
            if (jarsGrid != null)
            {
                // Перебираем все дочерние объекты в сетке банок
                foreach (Transform jarContainer in jarsGrid)
                {
                    // Извлекаем тип эмоции из имени объекта
                    string jarName = jarContainer.name;
                    if (jarName.EndsWith("Jar") && jarName.Length > 3)
                    {
                        // Получаем имя эмоции из имени объекта (без "Jar" в конце)
                        string emotionName = jarName.Substring(0, jarName.Length - 3);

                        // Кнопка находится на самом объекте jarContainer
                        var button = jarContainer.GetComponent<Button>();
                        if (button != null)
                        {
                            // Очищаем существующие обработчики, чтобы избежать дублирования
                            button.onClick.RemoveAllListeners();

                            // Настраиваем вызов OnJarClicked с именем эмоции
                            UnityEditor.Events.UnityEventTools.AddStringPersistentListener(
                                button.onClick,
                                jarInteractionHandler.OnJarClicked,
                                emotionName);

                            Debug.Log($"Подключена кнопка для {emotionName}Jar к JarInteractionHandler.OnJarClicked");
                        }
                        else
                        {
                            Debug.LogWarning($"Не найдена кнопка на объекте {jarName}");
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("Не найдена сетка банок JarsGrid в контейнере emotionJars");
            }

            // Настройка PersonalAreaManager
            var personalAreaManager = root.AddComponent<PersonalAreaManager>();
            var serializedManager = new SerializedObject(personalAreaManager);
            // Имя поля для ссылки на Canvas в PersonalAreaManager может отличаться, 
            // предполагаем, что это '_uiCanvas' или '_personalAreaCanvas'
            // Пожалуйста, проверьте корректное имя поля в скрипте PersonalAreaManager
            var uiCanvasProperty = serializedManager.FindProperty("_ui"); // Распространенное имя для UI ссылки
            if (uiCanvasProperty == null)
            {
                uiCanvasProperty = serializedManager.FindProperty("ui"); // Попробуем с маленькой буквы
            }
            if (uiCanvasProperty == null)
            {
                uiCanvasProperty = serializedManager.FindProperty("_personalAreaCanvas");
            }
            if (uiCanvasProperty == null)
            {
                uiCanvasProperty = serializedManager.FindProperty("_personalAreaView"); // Еще один вариант
            }

            if (uiCanvasProperty != null)
            {
                // Предполагаем, что PersonalAreaManager ожидает ссылку на GameObject Canvas'а
                // или на компонент PersonalAreaCanvas (если такой существует и используется как View)
                // В данном случае, скорее всего, нужна ссылка на сам GameObject Canvas'а
                if (uiCanvasProperty.propertyType == SerializedPropertyType.ObjectReference)
                {
                    // Пытаемся присвоить GameObject корневого Canvas
                    uiCanvasProperty.objectReferenceValue = root;
                    if (uiCanvasProperty.objectReferenceValue == null)
                    {
                        // Если не присвоился GameObject, возможно, нужен компонент типа Canvas или специфический View
                        // Попробуем присвоить компонент Canvas
                        uiCanvasProperty.objectReferenceValue = root.GetComponent<Canvas>();
                        if (uiCanvasProperty.objectReferenceValue == null)
                        {
                            // Если PersonalAreaManager ожидает PersonalAreaUIController или подобный компонент
                            uiCanvasProperty.objectReferenceValue = uiController;
                        }
                    }
                }
                else
                {
                    Debug.LogError("Свойство для UI в PersonalAreaManager не является ObjectReference.");
                }
            }
            else
            {
                Debug.LogError("Не удалось найти свойство для UI Canvas в PersonalAreaManager. Проверьте имена полей: '_ui', 'ui', '_personalAreaCanvas', '_personalAreaView'.");
            }
            serializedManager.ApplyModifiedProperties();
        }
        #endregion

        #region Utility Methods
        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var uiObject = new GameObject(name, typeof(RectTransform));
            uiObject.transform.SetParent(parent, false);
            return uiObject;
        }

        private static GameObject CreateTextElement(string name, Transform parent)
        {
            var textObject = CreateUIObject(name, parent);
            var textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = 16;
            textComponent.color = TextDark;
            textComponent.alignment = TextAlignmentOptions.Left;
            return textObject;
        }

        private static void SetFullStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Color GetEmotionColor(EmotionTypes emotionType)
        {
            return emotionType switch
            {
                EmotionTypes.Joy => new Color(1f, 0.85f, 0.1f, 0.8f),
                EmotionTypes.Sadness => new Color(0.15f, 0.3f, 0.8f, 0.8f),
                EmotionTypes.Anger => new Color(0.9f, 0.1f, 0.1f, 0.8f),
                EmotionTypes.Fear => new Color(0.5f, 0.1f, 0.6f, 0.8f),
                EmotionTypes.Disgust => new Color(0.1f, 0.6f, 0.2f, 0.8f),
                EmotionTypes.Trust => new Color(0f, 0.6f, 0.9f, 0.8f),
                EmotionTypes.Anticipation => new Color(1f, 0.5f, 0f, 0.8f),
                EmotionTypes.Surprise => new Color(0.8f, 0.4f, 0.9f, 0.8f),
                EmotionTypes.Love => new Color(0.95f, 0.3f, 0.6f, 0.8f),
                EmotionTypes.Anxiety => new Color(0.7f, 0.7f, 0.7f, 0.8f),
                EmotionTypes.Neutral => new Color(0.9f, 0.9f, 0.9f, 0.8f),
                _ => Color.white
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