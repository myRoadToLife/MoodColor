#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using App.Develop.Scenes.PersonalAreaScene.UI.Components;
using App.App.Develop.Scenes.PersonalAreaScene.UI.Components;
using System.Collections;

namespace App.Develop.Scenes.PersonalAreaScene.UI
{
    public class PersonalAreaPrefabGenerator
    {
        private const string RESOURCES_FOLDER = "Assets/App/Resources";
        private const string UI_FOLDER = RESOURCES_FOLDER + "/UI";
        private const string PREFAB_PATH = UI_FOLDER + "/PersonalAreaCanvas.prefab";

        // Цветовая палитра стиля MoodRoom
        private static readonly Color WarmWoodLight = new Color(0.9f, 0.8f, 0.7f, 1f); // Светлое дерево
        private static readonly Color WarmWoodMedium = new Color(0.82f, 0.71f, 0.55f, 1f); // Среднее дерево
        private static readonly Color WarmWoodDark = new Color(0.7f, 0.6f, 0.45f, 1f); // Темное дерево
        private static readonly Color WoodDarkBrown = new Color(0.6f, 0.5f, 0.35f, 1f); // Коричневое дерево
        private static readonly Color TextDark = new Color(0.25f, 0.2f, 0.15f, 1f); // Темный текст
        private static readonly Color PaperBeige = new Color(0.95f, 0.9f, 0.8f, 1f); // Бежевая бумага
        private static readonly Color GlassBlue = new Color(0.7f, 0.85f, 0.9f, 0.3f); // Стекло с голубым оттенком

        [MenuItem("MoodColor/Generate/Personal Area Prefab")]
        public static void GeneratePrefab()
        {
            Debug.Log("🔄 Начинаем генерацию префаба Personal Area...");

            // 1) Убедимся, что папки существуют
            if (!AssetDatabase.IsValidFolder(RESOURCES_FOLDER))
                AssetDatabase.CreateFolder("Assets/App", "Resources");

            if (!AssetDatabase.IsValidFolder(UI_FOLDER))
                AssetDatabase.CreateFolder(RESOURCES_FOLDER, "UI");

            // 2) Создаем корневой Canvas
            var root = CreateUIObject("PersonalAreaCanvas", null);
            root.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            // Используем адаптивный подход для matchWidthOrHeight:
            // 0 = ширина, 1 = высота, 0.5 = среднее
            // Для портретной ориентации лучше 0.5, чтобы быть более гибким
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            // 3) Создаем фон комнаты
            var background = CreateUIObject("RoomBackground", root.transform);
            var backgroundRect = background.GetComponent<RectTransform>();
            SetFullStretch(backgroundRect);
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = WarmWoodLight;

            // Добавляем декоративные элементы фона
            var windowFrame = CreateUIObject("WindowFrame", background.transform);
            var windowFrameRect = windowFrame.GetComponent<RectTransform>();
            windowFrameRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowFrameRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowFrameRect.sizeDelta = new Vector2(600, 400);
            windowFrameRect.anchoredPosition = new Vector2(0, 300);
            var windowFrameImage = windowFrame.AddComponent<Image>();
            windowFrameImage.color = WarmWoodDark;

            // Окно с видом
            var windowView = CreateUIObject("WindowView", windowFrame.transform);
            var windowViewRect = windowView.GetComponent<RectTransform>();
            windowViewRect.anchorMin = new Vector2(0, 0);
            windowViewRect.anchorMax = new Vector2(1, 1);
            windowViewRect.sizeDelta = new Vector2(-40, -40);
            windowViewRect.anchoredPosition = Vector2.zero;
            var windowViewImage = windowView.AddComponent<Image>();
            windowViewImage.color = GlassBlue;

            // Полки на стене
            var shelf = CreateUIObject("WoodenShelf", background.transform);
            var shelfRect = shelf.GetComponent<RectTransform>();
            shelfRect.anchorMin = new Vector2(0, 0);
            shelfRect.anchorMax = new Vector2(1, 0);
            shelfRect.sizeDelta = new Vector2(0, 30);
            shelfRect.anchoredPosition = new Vector2(0, 500);
            var shelfImage = shelf.AddComponent<Image>();
            shelfImage.color = WoodDarkBrown;

            // 4) Создаем SafeArea для безопасного размещения UI
            var safeArea = CreateUIObject("SafeArea", root.transform);
            var safeAreaRect = safeArea.GetComponent<RectTransform>();
            SetFullStretch(safeAreaRect);

#if UNITY_EDITOR
            // Более надежное удаление всех "Missing Script" компонентов с объекта SafeArea
            GameObject safeAreaGameObject = safeArea; // Используем сам GameObject
            var allComponents = safeAreaGameObject.GetComponents<Component>();
            int destroyedCount = 0;

            for (int i = allComponents.Length - 1; i >= 0; i--)
            {
                if (allComponents[i] == null)
                {
                    // Уничтожаем именно "null" (missing) компонент
                    UnityEditor.Undo.DestroyObjectImmediate(allComponents[i]);
                    destroyedCount++;
                }
            }

            if (destroyedCount > 0)
            {
                Debug.LogWarning($"[PrefabGenerator] Removed {destroyedCount} missing script(s) from SafeArea.");
            }
#endif

            // Добавляем компонент SafeArea, если он еще не существует и скрипт доступен
            // Проверяем тип более надежно, чтобы убедиться, что скрипт существует и компилируется
            System.Type safeAreaType = System.Type.GetType("App.Develop.Scenes.PersonalAreaScene.UI.SafeArea, Assembly-CSharp");

            if (safeAreaType != null)
            {
                if (safeAreaGameObject.GetComponent<SafeArea>() == null)
                {
                    safeAreaGameObject.AddComponent<SafeArea>();
                    Debug.Log("[PrefabGenerator] Added SafeArea component.");
                }
            }
            else
            {
                Debug.LogError(
                    "[PrefabGenerator] CRITICAL: SafeArea script (App.Develop.Scenes.PersonalAreaScene.UI.SafeArea) not found or not compiled! Prefab might not work correctly.");
            }

            // 5) Создаем контейнер для основного контента с вертикальным лейаутом
            var mainContent = CreateUIObject("MainContent", safeArea.transform);
            var mainContentRect = mainContent.GetComponent<RectTransform>();
            SetFullStretch(mainContentRect);

            var mainLayout = mainContent.AddComponent<VerticalLayoutGroup>();
            // Адаптивные отступы в зависимости от ширины экрана
            mainLayout.padding = new RectOffset(40, 40, 60, 40);
            mainLayout.spacing = 20; // Уменьшаем расстояние между элементами
            mainLayout.childAlignment = TextAnchor.UpperCenter;
            mainLayout.childControlHeight = true;
            mainLayout.childControlWidth = true;
            mainLayout.childForceExpandHeight = true; // ВАЖНО!
            mainLayout.childForceExpandWidth = true;
            // НЕ добавляем ContentSizeFitter!

            // 6) Создаем компоненты UI
            Debug.Log("🔄 Создаем UI компоненты...");
            var profileInfo = CreateProfileInfo(mainContent.transform);
            var emotionJars = CreateEmotionJars(mainContent.transform);
            var statistics = CreateStatistics(mainContent.transform);
            var navigation = CreateNavigation(mainContent.transform);

            // 7) Добавляем и настраиваем контроллеры
            Debug.Log("🔄 Настраиваем контроллеры...");
            var manager = root.AddComponent<PersonalAreaManager>();
            var uiController = root.AddComponent<PersonalAreaUIController>();

            // Настраиваем SerializeField через SerializedObject
            var serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("_ui").objectReferenceValue = uiController;
            serializedManager.ApplyModifiedProperties();

            var serializedUI = new SerializedObject(uiController);
            serializedUI.FindProperty("_profileInfo").objectReferenceValue = profileInfo.GetComponent<ProfileInfoComponent>();
            serializedUI.FindProperty("_emotionJars").objectReferenceValue = emotionJars.GetComponent<EmotionJarView>();
            serializedUI.FindProperty("_statistics").objectReferenceValue = statistics.GetComponent<StatisticsView>();
            serializedUI.FindProperty("_navigation").objectReferenceValue = navigation.GetComponent<NavigationComponent>();
            serializedUI.ApplyModifiedProperties();

            // 8) Сохраняем префаб
            Debug.Log($"💾 Сохраняем префаб в {PREFAB_PATH}");
            SaveAsPrefab(root);

            // 9) Очищаем сцену
            Object.DestroyImmediate(root);

            Debug.Log("✅ Генерация префаба Personal Area завершена");
        }

        private static GameObject CreateProfileInfo(Transform parent)
        {
            Debug.Log("🔄 Создаем ProfileInfo...");

            // Основной объект
            var profileInfo = CreateUIObject("ProfileInfo", parent);
            var profileRect = profileInfo.GetComponent<RectTransform>();
            profileRect.anchorMin = new Vector2(0, 1);
            profileRect.anchorMax = new Vector2(1, 1);
            profileRect.pivot = new Vector2(0.5f, 1);
            profileRect.offsetMin = new Vector2(0, 0);
            profileRect.offsetMax = new Vector2(0, 0);
            profileRect.sizeDelta = new Vector2(0, 0);
            var layoutElement = profileInfo.AddComponent<LayoutElement>();
            layoutElement.flexibleHeight = 1;
            layoutElement.minHeight = 150; // Меньшая высота для портретной ориентации

            // Добавляем фон с деревянной фактурой
            var background = CreateUIObject("WoodenPanel", profileInfo.transform);
            var backgroundRect = background.GetComponent<RectTransform>();
            SetFullStretch(backgroundRect);
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = WarmWoodMedium;

            // Добавляем закругленные углы для фона
            backgroundImage.sprite = Resources.Load<Sprite>("UI/RoundedPanel") ?? CreateRoundedRectSprite(20);
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.pixelsPerUnitMultiplier = 1f;

            // Добавляем рамку
            var frame = CreateUIObject("Frame", background.transform);
            var frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = new Vector2(10, 10);
            frameRect.offsetMax = new Vector2(-10, -10);
            var frameImage = frame.AddComponent<Image>();
            frameImage.color = new Color(WarmWoodDark.r, WarmWoodDark.g, WarmWoodDark.b, 0.3f);
            frameImage.sprite = Resources.Load<Sprite>("UI/RoundedPanel") ?? CreateRoundedRectSprite(15);
            frameImage.type = Image.Type.Sliced;
            frameImage.pixelsPerUnitMultiplier = 1f;

            // Добавляем контейнер контента
            var content = CreateUIObject("Content", frame.transform);
            var contentRect = content.GetComponent<RectTransform>();
            SetFullStretch(contentRect);
            var contentLayout = content.AddComponent<HorizontalLayoutGroup>();
            contentLayout.padding = new RectOffset(20, 20, 10, 10);
            contentLayout.spacing = 30;
            contentLayout.childAlignment = TextAnchor.MiddleLeft;
            contentLayout.childControlWidth = false;
            contentLayout.childForceExpandWidth = false;

            // Аватар пользователя
            var avatarContainer = CreateUIObject("AvatarContainer", content.transform);
            var avatarContainerRect = avatarContainer.GetComponent<RectTransform>();
            avatarContainerRect.sizeDelta = new Vector2(80, 80); // Уменьшенный размер аватара

            var avatarBg = CreateUIObject("AvatarBackground", avatarContainer.transform);
            var avatarBgRect = avatarBg.GetComponent<RectTransform>();
            SetFullStretch(avatarBgRect);
            var avatarBgImage = avatarBg.AddComponent<Image>();
            avatarBgImage.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            avatarBgImage.sprite = Resources.Load<Sprite>("UI/RoundedPanel") ?? CreateRoundedRectSprite(50);
            avatarBgImage.type = Image.Type.Sliced;
            avatarBgImage.pixelsPerUnitMultiplier = 1f;

            // Изображение аватара
            var avatarImage = CreateUIObject("AvatarImage", avatarBg.transform);
            var avatarImageRect = avatarImage.GetComponent<RectTransform>();
            SetFullStretch(avatarImageRect);
            avatarImageRect.offsetMin = new Vector2(5, 5);
            avatarImageRect.offsetMax = new Vector2(-5, -5);
            var avatar = avatarImage.AddComponent<Image>();
            avatar.sprite = Resources.Load<Sprite>("UI/DefaultAvatar");
            avatar.preserveAspect = true;
            avatar.color = new Color(1f, 1f, 1f, 0.9f);

            // Username Text
            var textContainer = CreateUIObject("TextContainer", content.transform);
            var textContainerRect = textContainer.GetComponent<RectTransform>();
            textContainerRect.sizeDelta = new Vector2(0, 80); // Меньшая высота для текста
            var textLayout = textContainer.AddComponent<VerticalLayoutGroup>();
            textLayout.childAlignment = TextAnchor.MiddleLeft;
            textLayout.spacing = 5; // Меньше расстояние между элементами
            textLayout.childControlWidth = true;
            textLayout.childForceExpandWidth = true;

            var usernameText = CreateTextObject("UsernameText", textContainer.transform, "Иван Петров", 24); // Меньший размер шрифта
            usernameText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            var statusText = CreateTextObject("StatusText", textContainer.transform, "Онлайн", 16); // Меньший размер шрифта
            var statusTextComp = statusText.GetComponent<TextMeshProUGUI>();
            statusTextComp.color = new Color(0.0f, 0.7f, 0.0f, 1f);
            statusTextComp.fontStyle = FontStyles.Italic;

            // Current Emotion - уменьшаем размер
            var emotionContainer = CreateUIObject("CurrentEmotionContainer", content.transform);
            var emotionContainerRect = emotionContainer.GetComponent<RectTransform>();
            emotionContainerRect.sizeDelta = new Vector2(70, 80);

            var emotionLabel = CreateTextObject("EmotionLabel", emotionContainer.transform, "Настроение", 12);
            var emotionLabelRect = emotionLabel.GetComponent<RectTransform>();
            emotionLabelRect.anchorMin = new Vector2(0, 1);
            emotionLabelRect.anchorMax = new Vector2(1, 1);
            emotionLabelRect.sizeDelta = new Vector2(0, 20);
            emotionLabelRect.anchoredPosition = new Vector2(0, -10);

            var emotionImage = CreateUIObject("CurrentEmotionImage", emotionContainer.transform);
            var emotionRect = emotionImage.GetComponent<RectTransform>();
            emotionRect.anchorMin = new Vector2(0.5f, 0);
            emotionRect.anchorMax = new Vector2(0.5f, 0);
            emotionRect.sizeDelta = new Vector2(50, 50); // Уменьшенный размер иконки
            emotionRect.anchoredPosition = new Vector2(0, 35);

            // Добавляем фон для иконки
            var emotionBg = CreateUIObject("EmotionBackground", emotionImage.transform);
            var emotionBgRect = emotionBg.GetComponent<RectTransform>();
            SetFullStretch(emotionBgRect);
            var emotionBgImage = emotionBg.AddComponent<Image>();
            emotionBgImage.color = GlassBlue;
            emotionBgImage.sprite = Resources.Load<Sprite>("UI/RoundedPanel") ?? CreateRoundedRectSprite(30);
            emotionBgImage.type = Image.Type.Sliced;
            emotionBgImage.pixelsPerUnitMultiplier = 1f;

            // Добавляем иконку
            var emotionIcon = emotionImage.AddComponent<Image>();
            emotionIcon.color = new Color(1, 1, 1, 0.8f);
            emotionIcon.sprite = null; // Будет установлен через компонент

            // Настройка компонента
            var profileComponent = profileInfo.AddComponent<ProfileInfoComponent>();
            var serializedProfile = new SerializedObject(profileComponent);
            serializedProfile.FindProperty("_usernameText").objectReferenceValue = usernameText.GetComponent<TextMeshProUGUI>();
            serializedProfile.FindProperty("_currentEmotionImage").objectReferenceValue = emotionIcon;
            serializedProfile.ApplyModifiedProperties();

            return profileInfo;
        }

        private static GameObject CreateEmotionJars(Transform parent)
        {
            Debug.Log("🔄 Создаем EmotionJars...");

            // Основной объект
            var emotionJars = CreateUIObject("EmotionJars", parent);
            var jarsRect = emotionJars.GetComponent<RectTransform>();
            jarsRect.anchorMin = new Vector2(0, 1);
            jarsRect.anchorMax = new Vector2(1, 1);
            jarsRect.pivot = new Vector2(0.5f, 1);
            jarsRect.offsetMin = new Vector2(0, 0);
            jarsRect.offsetMax = new Vector2(0, 0);
            jarsRect.sizeDelta = new Vector2(0, 0);
            var layoutElement = emotionJars.AddComponent<LayoutElement>();
            layoutElement.flexibleHeight = 1;
            layoutElement.minHeight = 400;

            // Добавляем деревянную полку как фон
            var shelfBg = CreateUIObject("WoodenShelf", emotionJars.transform);
            var shelfBgRect = shelfBg.GetComponent<RectTransform>();
            SetFullStretch(shelfBgRect);
            var shelfBgImage = shelfBg.AddComponent<Image>();
            shelfBgImage.color = WarmWoodDark;

            // Добавляем скругленные углы для фона
            shelfBgImage.sprite = Resources.Load<Sprite>("UI/RoundedPanel") ?? CreateRoundedRectSprite(15);
            shelfBgImage.type = Image.Type.Sliced;
            shelfBgImage.pixelsPerUnitMultiplier = 1f;

            // Текстура дерева
            var woodTexture = CreateUIObject("WoodTexture", shelfBg.transform);
            var woodTextureRect = woodTexture.GetComponent<RectTransform>();
            SetFullStretch(woodTextureRect);
            woodTextureRect.offsetMin = new Vector2(5, 5);
            woodTextureRect.offsetMax = new Vector2(-5, -5);
            var woodTextureImage = woodTexture.AddComponent<Image>();
            woodTextureImage.color = new Color(1f, 1f, 1f, 0.1f);

            // Добавляем контейнер для банок
            var jarsContainer = CreateUIObject("JarsContainer", woodTexture.transform);
            var jarsContainerRect = jarsContainer.GetComponent<RectTransform>();
            SetFullStretch(jarsContainerRect);
            var gridLayout = jarsContainer.AddComponent<GridLayoutGroup>();
            
            // Адаптивный размер ячеек в зависимости от ширины экрана
            float cellSize = 160f; // базовый размер
            gridLayout.cellSize = new Vector2(cellSize, 220); // крупнее банки
            gridLayout.spacing = new Vector2(40, 30);
            gridLayout.padding = new RectOffset(20, 20, 20, 20);
            gridLayout.childAlignment = TextAnchor.MiddleCenter;

            // Добавляем скроллинг для банок
            var scrollRect = jarsContainer.AddComponent<ScrollRect>();
            
            // Портретный режим: вертикальный скролл (лучше для узких экранов)
            // Альбомный режим: горизонтальный скролл
            // В редакторе мы не можем проверить ориентацию, поэтому настраиваем оба варианта
            scrollRect.horizontal = true;
            scrollRect.vertical = true; // Поддерживаем оба направления для адаптивности
            scrollRect.scrollSensitivity = 20f;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;

            // Создаем контент для скроллинга
            var scrollContent = CreateUIObject("ScrollContent", jarsContainer.transform);
            var scrollContentRect = scrollContent.GetComponent<RectTransform>();
            scrollContentRect.anchorMin = Vector2.zero;
            scrollContentRect.anchorMax = new Vector2(1, 1);
            scrollContentRect.pivot = new Vector2(0.5f, 0.5f);
            scrollContentRect.sizeDelta = Vector2.zero;

            // Настраиваем grid layout для содержимого скролла
            var contentGridLayout = scrollContent.AddComponent<GridLayoutGroup>();
            contentGridLayout.cellSize = new Vector2(cellSize, 220);
            contentGridLayout.spacing = new Vector2(40, 30);
            contentGridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            contentGridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            contentGridLayout.childAlignment = TextAnchor.MiddleCenter;
            contentGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            
            // Адаптивное количество столбцов (для узких экранов меньше)
            contentGridLayout.constraintCount = 2; // 2 банки в ряду для портретного режима

            // Настраиваем ScrollRect
            scrollRect.content = scrollContentRect;
            scrollRect.viewport = jarsContainerRect;

            // Создаем примеры банок для каждой эмоции
            string[] emotionNames = { "Радость", "Грусть", "Гнев", "Страх", "Удивление", "Доверие" };

            Color[] jarColors =
            {
                new Color(1f, 0.9f, 0.2f, 0.7f), // Радость - желтый
                new Color(0.3f, 0.5f, 0.9f, 0.7f), // Грусть - синий
                new Color(0.9f, 0.3f, 0.2f, 0.7f), // Гнев - красный
                new Color(0.5f, 0.2f, 0.7f, 0.7f), // Страх - фиолетовый
                new Color(0.2f, 0.8f, 0.9f, 0.7f), // Удивление - голубой
                new Color(0.3f, 0.8f, 0.4f, 0.7f) // Доверие - зеленый
            };

            for (int i = 0; i < emotionNames.Length; i++)
            {
                CreateJar(scrollContent.transform, emotionNames[i], jarColors[i], i + 1);
            }

            // Добавляем компонент для автоматической настройки размера контента
            var contentSizeFitter = scrollContent.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Добавляем компонент
            emotionJars.AddComponent<EmotionJarView>();

            return emotionJars;
        }

        private static GameObject CreateJar(Transform parent, string emotionName, Color liquidColor, int level)
        {
            // Создаем банку
            var jar = CreateUIObject(emotionName + "Jar", parent);

            // Стеклянная банка (фон)
            var glassJar = CreateUIObject("GlassJar", jar.transform);
            var glassJarRect = glassJar.GetComponent<RectTransform>();
            glassJarRect.anchorMin = new Vector2(0.5f, 0.5f);
            glassJarRect.anchorMax = new Vector2(0.5f, 0.5f);
            glassJarRect.sizeDelta = new Vector2(90, 120); // Меньший размер банки
            glassJarRect.anchoredPosition = Vector2.zero;

            var glassImage = glassJar.AddComponent<Image>();
            glassImage.color = new Color(0.9f, 0.9f, 0.95f, 0.4f); // Полупрозрачное стекло
            glassImage.sprite = Resources.Load<Sprite>("UI/JarSprite") ?? CreateJarSprite();
            glassImage.type = Image.Type.Sliced;
            glassImage.pixelsPerUnitMultiplier = 1f;

            // Жидкость внутри банки
            var liquid = CreateUIObject("Liquid", glassJar.transform);
            var liquidRect = liquid.GetComponent<RectTransform>();
            liquidRect.anchorMin = new Vector2(0, 0);

            // Динамически устанавливаем уровень заполнения от 0.1 до 0.9 в зависимости от level
            float fillLevel = Mathf.Clamp01(level / 10.0f + 0.1f); // От 0.1 до 0.9
            liquidRect.anchorMax = new Vector2(1, fillLevel);

            liquidRect.offsetMin = new Vector2(7, 7); // Меньшие отступы
            liquidRect.offsetMax = new Vector2(-7, 0);
            var liquidImage = liquid.AddComponent<Image>();
            liquidImage.color = liquidColor;

            // Создаем маску для жидкости
            liquid.AddComponent<Mask>().showMaskGraphic = true;

            // Добавляем "пузырьки" для эффекта - используем меньше пузырьков
            for (int i = 0; i < 3; i++) // 3 вместо 5 пузырьков
            {
                var bubble = CreateUIObject("Bubble" + i, liquid.transform);
                var bubbleRect = bubble.GetComponent<RectTransform>();

                // Случайный размер (меньше)
                float size = Random.Range(4f, 8f);
                bubbleRect.sizeDelta = new Vector2(size, size);

                // Случайная позиция (меньший диапазон)
                float x = Random.Range(-30f, 30f);
                float y = Random.Range(-50f, 50f);
                bubbleRect.anchorMin = new Vector2(0.5f, 0.5f);
                bubbleRect.anchorMax = new Vector2(0.5f, 0.5f);
                bubbleRect.anchoredPosition = new Vector2(x, y);

                var bubbleImage = bubble.AddComponent<Image>();
                bubbleImage.sprite = Resources.Load<Sprite>("UI/CircleSprite") ?? CreateCircleSprite();
                bubbleImage.color = new Color(1f, 1f, 1f, 0.4f);
            }

            // Этикетка с названием
            var label = CreateUIObject("Label", jar.transform);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0);
            labelRect.anchorMax = new Vector2(0.5f, 0);
            labelRect.sizeDelta = new Vector2(80, 25); // Меньшая этикетка
            labelRect.anchoredPosition = new Vector2(0, -10);

            var labelBg = label.AddComponent<Image>();
            labelBg.color = PaperBeige;
            labelBg.sprite = Resources.Load<Sprite>("UI/RoundedPanel") ?? CreateRoundedRectSprite(8);
            labelBg.type = Image.Type.Sliced;
            labelBg.pixelsPerUnitMultiplier = 1f;

            var labelText = CreateTextObject("LabelText", label.transform, emotionName, 12); // Меньший размер текста
            var labelTextComp = labelText.GetComponent<TextMeshProUGUI>();
            labelTextComp.color = TextDark;
            labelTextComp.fontStyle = FontStyles.Bold;

            // Добавляем количество записей
            var countText = CreateTextObject("CountText", glassJar.transform, level.ToString(), 20); // Меньший размер текста
            var countTextRect = countText.GetComponent<RectTransform>();
            countTextRect.anchorMin = new Vector2(1, 1);
            countTextRect.anchorMax = new Vector2(1, 1);
            countTextRect.sizeDelta = new Vector2(30, 30); // Меньший размер счетчика
            countTextRect.anchoredPosition = new Vector2(-5, -5);

            var countTextComp = countText.GetComponent<TextMeshProUGUI>();
            countTextComp.color = Color.white;
            countTextComp.fontStyle = FontStyles.Bold;

            // Создаем фон для количества
            var countBg = CreateUIObject("CountBg", countText.transform);
            countBg.transform.SetSiblingIndex(0); // Помещаем позади текста
            var countBgRect = countBg.GetComponent<RectTransform>();
            SetFullStretch(countBgRect);
            var countBgImage = countBg.AddComponent<Image>();
            countBgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            countBgImage.sprite = Resources.Load<Sprite>("UI/CircleSprite") ?? CreateCircleSprite();

            return jar;
        }

        private static GameObject CreateStatistics(Transform parent)
        {
            Debug.Log("🔄 Создаем Statistics...");

            // Основной объект
            var statistics = CreateUIObject("Statistics", parent);
            var statsRect = statistics.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0, 1);
            statsRect.anchorMax = new Vector2(1, 1);
            statsRect.pivot = new Vector2(0.5f, 1);
            statsRect.offsetMin = new Vector2(0, 0);
            statsRect.offsetMax = new Vector2(0, 0);
            statsRect.sizeDelta = new Vector2(0, 0);
            var layoutElement = statistics.AddComponent<LayoutElement>();
            layoutElement.flexibleHeight = 1;
            layoutElement.minHeight = 150; // Уменьшаем высоту статистики

            // Добавляем фон в виде свитка/записки
            var scroll = CreateUIObject("PaperScroll", statistics.transform);
            var scrollRect = scroll.GetComponent<RectTransform>();
            SetFullStretch(scrollRect);
            var scrollImage = scroll.AddComponent<Image>();
            scrollImage.color = PaperBeige;
            scrollImage.sprite = Resources.Load<Sprite>("UI/RoundedPanel") ?? CreateRoundedRectSprite(15);
            scrollImage.type = Image.Type.Sliced;
            scrollImage.pixelsPerUnitMultiplier = 1f;

            // Добавляем "загнутые углы" свитка для эффекта
            var cornerTL = CreateUIObject("CornerTopLeft", scroll.transform);
            var cornerTLRect = cornerTL.GetComponent<RectTransform>();
            cornerTLRect.anchorMin = new Vector2(0, 1);
            cornerTLRect.anchorMax = new Vector2(0, 1);
            cornerTLRect.sizeDelta = new Vector2(25, 25); // Меньшие углы
            cornerTLRect.anchoredPosition = Vector2.zero;
            var cornerTLImage = cornerTL.AddComponent<Image>();
            cornerTLImage.color = new Color(0.8f, 0.75f, 0.65f, 1f);
            cornerTLImage.sprite = Resources.Load<Sprite>("UI/CornerFold") ?? CreateCornerFoldSprite();

            var cornerBR = CreateUIObject("CornerBottomRight", scroll.transform);
            var cornerBRRect = cornerBR.GetComponent<RectTransform>();
            cornerBRRect.anchorMin = new Vector2(1, 0);
            cornerBRRect.anchorMax = new Vector2(1, 0);
            cornerBRRect.sizeDelta = new Vector2(25, 25); // Меньшие углы
            cornerBRRect.anchoredPosition = Vector2.zero;
            var cornerBRImage = cornerBR.AddComponent<Image>();
            cornerBRImage.color = new Color(0.8f, 0.75f, 0.65f, 1f);
            cornerBRImage.sprite = Resources.Load<Sprite>("UI/CornerFold") ?? CreateCornerFoldSprite();
            // Поворачиваем на 180 градусов
            cornerBRRect.localRotation = Quaternion.Euler(0, 0, 180);

            // Создаем контейнер для статистики
            var content = CreateUIObject("Content", scroll.transform);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(15, 15); // Меньшие отступы
            contentRect.offsetMax = new Vector2(-15, -15); // Меньшие отступы

            // Добавляем текстуру "бумаги"
            var paperTexture = CreateUIObject("PaperTexture", content.transform);
            var paperTextureRect = paperTexture.GetComponent<RectTransform>();
            SetFullStretch(paperTextureRect);
            var paperTextureImage = paperTexture.AddComponent<Image>();
            paperTextureImage.color = new Color(0, 0, 0, 0.05f);
            paperTextureImage.sprite = Resources.Load<Sprite>("UI/PaperTexture") ?? CreatePaperTextureSprite();
            paperTextureImage.type = Image.Type.Tiled;

            var titleText = CreateTextObject("StatisticsTitle", content.transform, "Статистика", 20);
            var titleTextRect = titleText.GetComponent<RectTransform>();
            titleTextRect.anchorMin = new Vector2(0, 1);
            titleTextRect.anchorMax = new Vector2(1, 1);
            titleTextRect.sizeDelta = new Vector2(0, 25); // Меньшая высота заголовка
            titleTextRect.anchoredPosition = new Vector2(0, -12);
            var titleTextComp = titleText.GetComponent<TextMeshProUGUI>();
            titleTextComp.fontStyle = FontStyles.Bold;
            titleTextComp.alignment = TextAlignmentOptions.Center;

            // Добавляем линию под заголовком
            var titleLine = CreateUIObject("TitleLine", titleText.transform);
            var titleLineRect = titleLine.GetComponent<RectTransform>();
            titleLineRect.anchorMin = new Vector2(0.2f, 0);
            titleLineRect.anchorMax = new Vector2(0.8f, 0);
            titleLineRect.sizeDelta = new Vector2(0, 1); // Тоньше линия
            titleLineRect.anchoredPosition = new Vector2(0, -3); // Ближе к тексту
            var titleLineImage = titleLine.AddComponent<Image>();
            titleLineImage.color = new Color(0.4f, 0.3f, 0.2f, 0.5f);

            // Создаем горизонтальный лейаут для статистики
            var statsRow = CreateUIObject("StatsRow", content.transform);
            var statsRowRect = statsRow.GetComponent<RectTransform>();
            statsRowRect.anchorMin = new Vector2(0, 0);
            statsRowRect.anchorMax = new Vector2(1, 1);
            statsRowRect.offsetMin = new Vector2(0, 0);
            statsRowRect.offsetMax = new Vector2(0, -30); // Меньше места под заголовком

            var rowLayout = statsRow.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 20; // Меньше расстояние
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = true;

            // Создаем контейнеры для статистики очков и записей
            var pointsContainer = CreateUIObject("PointsContainer", statsRow.transform);
            var entriesContainer = CreateUIObject("EntriesContainer", statsRow.transform);

            // Настраиваем отображение очков
            var pointsIcon = CreateUIObject("PointsIcon", pointsContainer.transform);
            var pointsIconRect = pointsIcon.GetComponent<RectTransform>();
            pointsIconRect.anchorMin = new Vector2(0, 0.5f);
            pointsIconRect.anchorMax = new Vector2(0, 0.5f);
            pointsIconRect.sizeDelta = new Vector2(30, 30); // Меньшая иконка
            pointsIconRect.anchoredPosition = new Vector2(15, 0); // Меньший отступ
            var pointsIconImage = pointsIcon.AddComponent<Image>();
            pointsIconImage.color = new Color(1f, 0.8f, 0.2f, 1f); // Золотистый цвет для монетки/очков
            pointsIconImage.sprite = Resources.Load<Sprite>("UI/CoinIcon") ?? CreateCoinSprite();
            pointsIconImage.preserveAspect = true;

            var pointsText = CreateTextObject("PointsText", pointsContainer.transform, "Очки: 0", 16); // Меньший размер текста
            var pointsTextRect = pointsText.GetComponent<RectTransform>();
            pointsTextRect.anchorMin = new Vector2(0, 0);
            pointsTextRect.anchorMax = new Vector2(1, 1);
            pointsTextRect.offsetMin = new Vector2(50, 0); // Меньший отступ
            pointsTextRect.offsetMax = new Vector2(0, 0);
            var pointsTextComp = pointsText.GetComponent<TextMeshProUGUI>();
            pointsTextComp.alignment = TextAlignmentOptions.Left;
            pointsTextComp.fontStyle = FontStyles.Bold;

            // Настраиваем отображение записей
            var entriesIcon = CreateUIObject("EntriesIcon", entriesContainer.transform);
            var entriesIconRect = entriesIcon.GetComponent<RectTransform>();
            entriesIconRect.anchorMin = new Vector2(0, 0.5f);
            entriesIconRect.anchorMax = new Vector2(0, 0.5f);
            entriesIconRect.sizeDelta = new Vector2(30, 30); // Меньшая иконка
            entriesIconRect.anchoredPosition = new Vector2(15, 0); // Меньший отступ
            var entriesIconImage = entriesIcon.AddComponent<Image>();
            entriesIconImage.color = new Color(0.4f, 0.6f, 1f, 1f); // Голубой цвет для записей
            entriesIconImage.sprite = Resources.Load<Sprite>("UI/NoteIcon") ?? CreateNoteSprite();
            entriesIconImage.preserveAspect = true;

            var entriesText = CreateTextObject("EntriesText", entriesContainer.transform, "Записей: 0", 16); // Меньший размер текста
            var entriesTextRect = entriesText.GetComponent<RectTransform>();
            entriesTextRect.anchorMin = new Vector2(0, 0);
            entriesTextRect.anchorMax = new Vector2(1, 1);
            entriesTextRect.offsetMin = new Vector2(50, 0); // Меньший отступ
            entriesTextRect.offsetMax = new Vector2(0, 0);
            var entriesTextComp = entriesText.GetComponent<TextMeshProUGUI>();
            entriesTextComp.alignment = TextAlignmentOptions.Left;
            entriesTextComp.fontStyle = FontStyles.Bold;

            // Настройка компонента
            var statsView = statistics.AddComponent<StatisticsView>();
            var serializedStats = new SerializedObject(statsView);
            serializedStats.FindProperty("_pointsText").objectReferenceValue = pointsTextComp;
            serializedStats.FindProperty("_entriesText").objectReferenceValue = entriesTextComp;
            serializedStats.ApplyModifiedProperties();

            return statistics;
        }

        private static GameObject CreateNavigation(Transform parent)
        {
            Debug.Log("🔄 Создаем Navigation...");

            // Основной объект
            var navigation = CreateUIObject("Navigation", parent);
            var navRect = navigation.GetComponent<RectTransform>();
            navRect.anchorMin = new Vector2(0, 1);
            navRect.anchorMax = new Vector2(1, 1);
            navRect.pivot = new Vector2(0.5f, 1);
            navRect.offsetMin = new Vector2(0, 0);
            navRect.offsetMax = new Vector2(0, 0);
            navRect.sizeDelta = new Vector2(0, 0);
            var layoutElement = navigation.AddComponent<LayoutElement>();
            layoutElement.flexibleHeight = 1;
            layoutElement.minHeight = 120;

            // Добавляем деревянную панель
            var woodPanel = CreateUIObject("WoodenPanel", navigation.transform);
            var woodPanelRect = woodPanel.GetComponent<RectTransform>();
            SetFullStretch(woodPanelRect);
            var woodPanelImage = woodPanel.AddComponent<Image>();
            woodPanelImage.color = WarmWoodDark;
            woodPanelImage.sprite = Resources.Load<Sprite>("UI/RoundedPanel") ?? CreateRoundedRectSprite(15);
            woodPanelImage.type = Image.Type.Sliced;
            woodPanelImage.pixelsPerUnitMultiplier = 1f;

            // Добавляем текстуру панели
            var panelTexture = CreateUIObject("PanelTexture", woodPanel.transform);
            var panelTextureRect = panelTexture.GetComponent<RectTransform>();
            SetFullStretch(panelTextureRect);
            panelTextureRect.offsetMin = new Vector2(5, 5);
            panelTextureRect.offsetMax = new Vector2(-5, -5);
            var panelTextureImage = panelTexture.AddComponent<Image>();
            panelTextureImage.color = new Color(1f, 1f, 1f, 0.05f);
            panelTextureImage.sprite = Resources.Load<Sprite>("UI/WoodTexture") ?? CreateWoodTextureSprite();
            panelTextureImage.type = Image.Type.Tiled;

            // Добавляем контейнер для кнопок
            var buttonsContainer = CreateUIObject("ButtonsContainer", panelTexture.transform);
            var buttonsContainerRect = buttonsContainer.GetComponent<RectTransform>();
            SetFullStretch(buttonsContainerRect);
            buttonsContainerRect.offsetMin = new Vector2(10, 10);
            buttonsContainerRect.offsetMax = new Vector2(-10, -10);

            var buttonLayout = buttonsContainer.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 20; // меньше расстояние для более узких экранов
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = false;
            buttonLayout.childForceExpandWidth = false;
            buttonLayout.padding = new RectOffset(5, 5, 5, 5); // меньше отступы

            // Создаем кнопки
            var logEmotionBtn = CreateButton("LogEmotionButton", buttonsContainer.transform, "Новая\nэмоция",
                Resources.Load<Sprite>("UI/Icons/EmotionPlusIcon") ?? CreateEmotionPlusIconSprite());

            var historyBtn = CreateButton("HistoryButton", buttonsContainer.transform, "История",
                Resources.Load<Sprite>("UI/Icons/HistoryIcon") ?? CreateHistoryIconSprite());

            var friendsBtn = CreateButton("FriendsButton", buttonsContainer.transform, "Друзья",
                Resources.Load<Sprite>("UI/Icons/FriendsIcon") ?? CreateFriendsIconSprite());

            var settingsBtn = CreateButton("SettingsButton", buttonsContainer.transform, "Настройки",
                Resources.Load<Sprite>("UI/Icons/SettingsIcon") ?? CreateSettingsIconSprite());

            var workshopBtn = CreateButton("WorkshopButton", buttonsContainer.transform, "Мастерская",
                Resources.Load<Sprite>("UI/Icons/WorkshopIcon") ?? CreateWorkshopIconSprite());

            // Настройка размеров кнопок - уменьшены для портретного режима
            foreach (var btn in new[] { logEmotionBtn, historyBtn, friendsBtn, settingsBtn, workshopBtn })
            {
                var btnRect = btn.GetComponent<RectTransform>();
                btnRect.sizeDelta = new Vector2(150, 80); // меньше кнопки для узких экранов
            }

            // Настройка компонента
            var navComponent = navigation.AddComponent<NavigationComponent>();
            var serializedNav = new SerializedObject(navComponent);
            serializedNav.FindProperty("_logEmotionButton").objectReferenceValue = logEmotionBtn.GetComponent<Button>();
            serializedNav.FindProperty("_historyButton").objectReferenceValue = historyBtn.GetComponent<Button>();
            serializedNav.FindProperty("_friendsButton").objectReferenceValue = friendsBtn.GetComponent<Button>();
            serializedNav.FindProperty("_settingsButton").objectReferenceValue = settingsBtn.GetComponent<Button>();
            serializedNav.FindProperty("_workshopButton").objectReferenceValue = workshopBtn.GetComponent<Button>();

            // Связываем иконки
            var logEmotionIcon = logEmotionBtn.transform.Find("WoodenPanel/InnerPanel/IconContainer").GetComponent<Image>();
            var historyIcon = historyBtn.transform.Find("WoodenPanel/InnerPanel/IconContainer").GetComponent<Image>();
            var friendsIcon = friendsBtn.transform.Find("WoodenPanel/InnerPanel/IconContainer").GetComponent<Image>();
            var settingsIcon = settingsBtn.transform.Find("WoodenPanel/InnerPanel/IconContainer").GetComponent<Image>();
            var workshopIcon = workshopBtn.transform.Find("WoodenPanel/InnerPanel/IconContainer").GetComponent<Image>();

            serializedNav.FindProperty("_logEmotionIcon").objectReferenceValue = logEmotionIcon;
            serializedNav.FindProperty("_historyIcon").objectReferenceValue = historyIcon;
            serializedNav.FindProperty("_friendsIcon").objectReferenceValue = friendsIcon;
            serializedNav.FindProperty("_settingsIcon").objectReferenceValue = settingsIcon;
            serializedNav.FindProperty("_workshopIcon").objectReferenceValue = workshopIcon;

            // Связываем тексты
            var logEmotionText = logEmotionBtn.transform.Find("WoodenPanel/InnerPanel/Text").GetComponent<TextMeshProUGUI>();
            var historyText = historyBtn.transform.Find("WoodenPanel/InnerPanel/Text").GetComponent<TextMeshProUGUI>();
            var friendsText = friendsBtn.transform.Find("WoodenPanel/InnerPanel/Text").GetComponent<TextMeshProUGUI>();
            var settingsText = settingsBtn.transform.Find("WoodenPanel/InnerPanel/Text").GetComponent<TextMeshProUGUI>();
            var workshopText = workshopBtn.transform.Find("WoodenPanel/InnerPanel/Text").GetComponent<TextMeshProUGUI>();

            serializedNav.FindProperty("_logEmotionText").objectReferenceValue = logEmotionText;
            serializedNav.FindProperty("_historyText").objectReferenceValue = historyText;
            serializedNav.FindProperty("_friendsText").objectReferenceValue = friendsText;
            serializedNav.FindProperty("_settingsText").objectReferenceValue = settingsText;
            serializedNav.FindProperty("_workshopText").objectReferenceValue = workshopText;

            serializedNav.ApplyModifiedProperties();

            return navigation;
        }

        private static GameObject CreateButton(string name, Transform parent, string text, Sprite icon = null)
        {
            // Создаем кнопку с деревянным стилем
            var buttonObj = CreateUIObject(name, parent);

            // Деревянная панель (фон кнопки)
            var woodenPanel = CreateUIObject("WoodenPanel", buttonObj.transform);
            var woodenPanelRect = woodenPanel.GetComponent<RectTransform>();
            SetFullStretch(woodenPanelRect);
            var buttonBgImage = woodenPanel.AddComponent<Image>();
            buttonBgImage.color = WoodDarkBrown;
            buttonBgImage.sprite = Resources.Load<Sprite>("UI/RoundedPanel") ?? CreateRoundedRectSprite(12);
            buttonBgImage.type = Image.Type.Sliced;
            buttonBgImage.pixelsPerUnitMultiplier = 1f;

            // Добавляем тень для объема
            var shadow = CreateUIObject("Shadow", woodenPanel.transform);
            var shadowRect = shadow.GetComponent<RectTransform>();
            SetFullStretch(shadowRect);
            shadowRect.offsetMin = new Vector2(-3, -3);
            shadowRect.offsetMax = new Vector2(3, 3);
            shadowRect.SetSiblingIndex(0);
            var shadowImage = shadow.AddComponent<Image>();
            shadowImage.color = new Color(0.1f, 0.1f, 0.1f, 0.3f);
            shadowImage.sprite = Resources.Load<Sprite>("UI/RoundedPanel") ?? CreateRoundedRectSprite(15);
            shadowImage.type = Image.Type.Sliced;
            shadowImage.pixelsPerUnitMultiplier = 1f;

            // Внутренняя панель (фон для текста)
            var innerPanel = CreateUIObject("InnerPanel", woodenPanel.transform);
            var innerPanelRect = innerPanel.GetComponent<RectTransform>();
            innerPanelRect.anchorMin = new Vector2(0, 0);
            innerPanelRect.anchorMax = new Vector2(1, 1);
            innerPanelRect.offsetMin = new Vector2(4, 4);
            innerPanelRect.offsetMax = new Vector2(-4, -4);
            var innerPanelImage = innerPanel.AddComponent<Image>();
            innerPanelImage.color = WarmWoodMedium;
            innerPanelImage.sprite = Resources.Load<Sprite>("UI/RoundedPanel") ?? CreateRoundedRectSprite(10);
            innerPanelImage.type = Image.Type.Sliced;
            innerPanelImage.pixelsPerUnitMultiplier = 1f;

            // Добавляем компонент кнопки
            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonBgImage;

            // Настраиваем цвета кнопки
            var normalColor = buttonBgImage.color;

            var highlightedColor = new Color(
                Mathf.Min(normalColor.r * 1.2f, 1f),
                Mathf.Min(normalColor.g * 1.2f, 1f),
                Mathf.Min(normalColor.b * 1.2f, 1f),
                normalColor.a
            );

            var pressedColor = new Color(
                normalColor.r * 0.8f,
                normalColor.g * 0.8f,
                normalColor.b * 0.8f,
                normalColor.a
            );

            var colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = highlightedColor;
            colors.pressedColor = pressedColor;
            colors.selectedColor = highlightedColor;
            colors.disabledColor = new Color(normalColor.r, normalColor.g, normalColor.b, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            // Иконка
            var iconContainer = CreateUIObject("IconContainer", innerPanel.transform);
            var iconContainerRect = iconContainer.GetComponent<RectTransform>();
            iconContainerRect.anchorMin = new Vector2(0.5f, 1);
            iconContainerRect.anchorMax = new Vector2(0.5f, 1);
            iconContainerRect.pivot = new Vector2(0.5f, 1);
            iconContainerRect.sizeDelta = new Vector2(30, 30);
            iconContainerRect.anchoredPosition = new Vector2(0, -5);

            var iconImage = iconContainer.AddComponent<Image>();
            iconImage.color = TextDark;

            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.preserveAspect = true;
            }

            // Добавляем текст
            var textObj = CreateTextObject("Text", innerPanel.transform, text, 16);
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 0.7f);
            textRect.offsetMin = new Vector2(2, 2);
            textRect.offsetMax = new Vector2(-2, 0);
            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.color = TextDark;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;

            // Добавляем анимацию нажатия
            var clickAnimation = buttonObj.AddComponent<ButtonClickAnimation>();

            return buttonObj;
        }

        private static GameObject CreateTextObject(string name, Transform parent, string text, int fontSize)
        {
            var textObj = CreateUIObject(name, parent);
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            
            // Более гибкая адаптация размера шрифта
            // Если запрошенный размер >= 28, используем 28 (большой заголовок)
            // Если >= 18, используем 20 (обычный заголовок)
            // Если >= 14, используем 16 (подзаголовок)
            // Иначе используем 12 (обычный текст)
            if (fontSize >= 28)
                tmp.fontSize = 28;
            else if (fontSize >= 18)
                tmp.fontSize = 20;
            else if (fontSize >= 14)
                tmp.fontSize = 16;
            else
                tmp.fontSize = 12;
                
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            tmp.color = TextDark;
            if (fontSize >= 18) tmp.fontStyle = FontStyles.Bold;
            return textObj;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));

            if (parent != null)
                go.transform.SetParent(parent, false);

            return go;
        }

        private static void SetFullStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetStretchWidth(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0, height);
        }

        private static void SaveAsPrefab(GameObject root)
        {
            // Создаем необходимые директории
            string directory = System.IO.Path.GetDirectoryName(PREFAB_PATH);

            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // Сохраняем префаб
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            AssetDatabase.Refresh();
        }

        private static Sprite CreateRoundedRectSprite(float radius)
        {
            // Создаем текстуру для скругленного прямоугольника
            int size = 128;
            Texture2D texture = new Texture2D(size, size);

            // Заполняем текстуру прозрачным цветом
            Color[] colors = new Color[size * size];

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }

            texture.SetPixels(colors);

            // Заполняем белым цветом с учетом скругленных углов
            Color white = Color.white;
            float radiusSquared = radius * radius;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Проверяем, находится ли пиксель в пределах скругленного прямоугольника
                    if (IsInsideRoundedRect(x, y, size, size, radius))
                    {
                        texture.SetPixel(x, y, white);
                    }
                }
            }

            texture.Apply();

            // Создаем спрайт из текстуры
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
        }

        private static bool IsInsideRoundedRect(int x, int y, int width, int height, float radius)
        {
            // Проверяем, находится ли точка внутри прямоугольника с учетом скругленных углов

            // Координаты относительно центра
            float nx = x - width / 2.0f;
            float ny = y - height / 2.0f;

            // Размеры внутреннего прямоугольника (без учета скруглений)
            float innerWidth = width - 2 * radius;
            float innerHeight = height - 2 * radius;

            // Если точка находится внутри внутреннего прямоугольника, то это внутри
            if (Mathf.Abs(nx) <= innerWidth / 2.0f || Mathf.Abs(ny) <= innerHeight / 2.0f)
                return true;

            // Проверяем, находится ли точка в скругленном углу
            float dx = Mathf.Abs(nx) - innerWidth / 2.0f;
            float dy = Mathf.Abs(ny) - innerHeight / 2.0f;

            return dx * dx + dy * dy <= radius * radius;
        }

        private static Sprite CreateJarSprite()
        {
            // Создаем текстуру для банки
            int width = 128;
            int height = 256;
            Texture2D texture = new Texture2D(width, height);

            // Заполняем текстуру прозрачным цветом
            Color[] colors = new Color[width * height];

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }

            texture.SetPixels(colors);

            // Рисуем форму банки
            Color white = Color.white;
            int neckWidth = width / 3;
            int neckHeight = height / 8;
            int bodyWidth = width - 10;
            int bodyHeight = height - neckHeight - 20;
            int bodyStartY = 10;
            int neckStartY = bodyStartY + bodyHeight;

            // Рисуем тело банки
            for (int y = bodyStartY; y < bodyStartY + bodyHeight; y++)
            {
                // Сужение к верху для банки
                float factor = 1.0f - (y - bodyStartY) / (float)bodyHeight * 0.2f;
                int currentWidth = (int)(bodyWidth * factor);
                int startX = (width - currentWidth) / 2;

                for (int x = startX; x < startX + currentWidth; x++)
                {
                    texture.SetPixel(x, y, white);
                }
            }

            // Рисуем горлышко
            int neckStartX = (width - neckWidth) / 2;

            for (int y = neckStartY; y < neckStartY + neckHeight; y++)
            {
                for (int x = neckStartX; x < neckStartX + neckWidth; x++)
                {
                    texture.SetPixel(x, y, white);
                }
            }

            // Рисуем крышку
            int capWidth = neckWidth + 10;
            int capHeight = 10;
            int capStartX = (width - capWidth) / 2;
            int capStartY = neckStartY + neckHeight;

            for (int y = capStartY; y < capStartY + capHeight; y++)
            {
                for (int x = capStartX; x < capStartX + capWidth; x++)
                {
                    texture.SetPixel(x, y, white);
                }
            }

            texture.Apply();

            // Создаем спрайт из текстуры
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private static Sprite CreateCircleSprite()
        {
            // Создаем текстуру для круга
            int size = 128;
            Texture2D texture = new Texture2D(size, size);

            // Заполняем текстуру прозрачным цветом
            Color[] colors = new Color[size * size];

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }

            texture.SetPixels(colors);

            // Рисуем круг
            Color white = Color.white;
            int radius = size / 2 - 2;
            int centerX = size / 2;
            int centerY = size / 2;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (IsInCircle(x, y, centerX, centerY, radius))
                    {
                        texture.SetPixel(x, y, white);
                    }
                }
            }

            texture.Apply();

            // Создаем спрайт из текстуры
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private static bool IsInCircle(int x, int y, int centerX, int centerY, int radius)
        {
            int dx = x - centerX;
            int dy = y - centerY;
            int distanceSquared = dx * dx + dy * dy;
            return distanceSquared <= radius * radius;
        }

        private static Sprite CreateCoinSprite()
        {
            // Создаем текстуру для монеты
            int size = 128;
            Texture2D texture = new Texture2D(size, size);

            // Заполняем текстуру прозрачным цветом
            Color[] colors = new Color[size * size];

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }

            texture.SetPixels(colors);

            // Рисуем круг монеты
            Color gold = new Color(1f, 0.8f, 0.2f, 1f);
            Color goldDark = new Color(0.9f, 0.7f, 0.1f, 1f);

            int radius = size / 2 - 2;
            int centerX = size / 2;
            int centerY = size / 2;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (IsInCircle(x, y, centerX, centerY, radius))
                    {
                        // Создаем градиент от центра к краям
                        float dist = Mathf.Sqrt(Mathf.Pow(x - centerX, 2) + Mathf.Pow(y - centerY, 2)) / radius;
                        Color pixelColor = Color.Lerp(gold, goldDark, dist);
                        texture.SetPixel(x, y, pixelColor);
                    }
                }
            }

            // Рисуем знак доллара (или другой символ) в центре
            int symbolThickness = 6;
            int symbolHeight = size / 2;

            // Вертикальная линия
            for (int y = centerY - symbolHeight / 2; y < centerY + symbolHeight / 2; y++)
            {
                for (int x = centerX - symbolThickness / 2; x < centerX + symbolThickness / 2; x++)
                {
                    if (IsInCircle(x, y, centerX, centerY, radius))
                    {
                        texture.SetPixel(x, y, Color.white);
                    }
                }
            }

            texture.Apply();

            // Создаем спрайт из текстуры
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private static Sprite CreateNoteSprite()
        {
            // Создаем текстуру для записи/заметки
            int width = 128;
            int height = 128;
            Texture2D texture = new Texture2D(width, height);

            // Заполняем текстуру прозрачным цветом
            Color[] colors = new Color[width * height];

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }

            texture.SetPixels(colors);

            // Рисуем лист бумаги
            Color paper = new Color(0.95f, 0.95f, 0.9f, 1f);
            Color line = new Color(0.3f, 0.3f, 0.5f, 0.5f);

            int margin = 10;

            // Заполняем лист
            for (int y = margin; y < height - margin; y++)
            {
                for (int x = margin; x < width - margin; x++)
                {
                    texture.SetPixel(x, y, paper);
                }
            }

            // Рисуем линии на листе
            int lineCount = 5;
            int lineSpacing = (height - 2 * margin) / (lineCount + 1);

            for (int i = 1; i <= lineCount; i++)
            {
                int lineY = margin + i * lineSpacing;

                for (int x = margin * 2; x < width - margin * 2; x++)
                {
                    texture.SetPixel(x, lineY, line);
                }
            }

            // Рисуем скрепку сверху
            int clipWidth = 10;
            int clipHeight = 30;
            int clipX = width / 2 - clipWidth / 2;
            int clipY = height - margin - clipHeight / 2;

            Color clipColor = new Color(0.7f, 0.7f, 0.7f, 1f);

            for (int y = clipY - clipHeight / 2; y < clipY + clipHeight / 2; y++)
            {
                for (int x = clipX; x < clipX + clipWidth; x++)
                {
                    if (y > height - margin) continue;

                    texture.SetPixel(x, y, clipColor);
                }
            }

            texture.Apply();

            // Создаем спрайт из текстуры
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private static Sprite CreateCornerFoldSprite()
        {
            // Создаем текстуру для загнутого угла
            int size = 128;
            Texture2D texture = new Texture2D(size, size);

            // Заполняем текстуру прозрачным цветом
            Color[] colors = new Color[size * size];

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }

            texture.SetPixels(colors);

            // Цвета для угла
            Color cornerLight = new Color(0.9f, 0.85f, 0.75f, 1f);
            Color cornerShadow = new Color(0.7f, 0.65f, 0.6f, 1f);

            // Рисуем треугольник загнутого угла
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Определяем, находится ли точка внутри треугольника
                    if (x + y < size)
                    {
                        // Создаем градиент от края к центру
                        float distFromDiagonal = Mathf.Abs(x + y - size) / (float)size;
                        Color pixelColor = Color.Lerp(cornerShadow, cornerLight, distFromDiagonal);

                        texture.SetPixel(x, y, pixelColor);
                    }
                }
            }

            // Рисуем линию загиба
            for (int i = 0; i < size; i++)
            {
                int x = i;
                int y = size - i;

                if (x >= 0 && x < size && y >= 0 && y < size)
                {
                    // Делаем линию толще
                    for (int t = -1; t <= 1; t++)
                    {
                        for (int s = -1; s <= 1; s++)
                        {
                            int px = x + t;
                            int py = y + s;

                            if (px >= 0 && px < size && py >= 0 && py < size)
                            {
                                texture.SetPixel(px, py, cornerShadow);
                            }
                        }
                    }
                }
            }

            texture.Apply();

            // Создаем спрайт из текстуры
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private static Sprite CreatePaperTextureSprite()
        {
            // Создаем текстуру для бумаги
            int width = 256;
            int height = 256;
            Texture2D texture = new Texture2D(width, height);

            // Заполняем текстуру базовым цветом бумаги
            Color paperBase = new Color(0.98f, 0.96f, 0.9f, 1f);
            Color paperShadow = new Color(0.95f, 0.92f, 0.85f, 1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Создаем шум для имитации текстуры бумаги
                    float noiseValue = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);

                    // Делаем некоторые области темнее для эффекта текстуры
                    float factor = noiseValue * 0.2f;

                    Color pixelColor = Color.Lerp(paperBase, paperShadow, factor);
                    texture.SetPixel(x, y, pixelColor);
                }
            }

            texture.Apply();

            // Создаем спрайт из текстуры
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private static Sprite CreateWoodTextureSprite()
        {
            // Создаем текстуру для дерева
            int width = 256;
            int height = 256;
            Texture2D texture = new Texture2D(width, height);

            // Заполняем текстуру базовым цветом дерева
            Color woodBase = new Color(0.7f, 0.5f, 0.3f, 1f);
            Color woodDark = new Color(0.6f, 0.4f, 0.25f, 1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Создаем шум для имитации текстуры дерева
                    float noiseValue = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);

                    // Создаем полосы как на деревянной текстуре
                    float stripeNoise = Mathf.PerlinNoise(x * 0.01f, y * 0.1f) * 0.5f + 0.5f;

                    // Комбинируем шумы для получения текстуры дерева
                    float combined = Mathf.Lerp(noiseValue, stripeNoise, 0.7f);

                    Color pixelColor = Color.Lerp(woodBase, woodDark, combined * 0.5f);
                    texture.SetPixel(x, y, pixelColor);
                }
            }

            texture.Apply();

            // Создаем спрайт из текстуры
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        // Метод для рисования линии на текстуре
        private static void DrawLine(Texture2D texture, int x1, int y1, int x2, int y2, int thickness, Color color)
        {
            int dx = x2 - x1;
            int dy = y2 - y1;
            int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));

            if (steps == 0)
                return;

            float xIncrement = dx / (float)steps;
            float yIncrement = dy / (float)steps;

            float x = x1;
            float y = y1;

            for (int i = 0; i <= steps; i++)
            {
                // Рисуем точку с учетом толщины
                for (int tx = -thickness / 2; tx <= thickness / 2; tx++)
                {
                    for (int ty = -thickness / 2; ty <= thickness / 2; ty++)
                    {
                        int px = Mathf.RoundToInt(x) + tx;
                        int py = Mathf.RoundToInt(y) + ty;

                        // Проверяем, что точка внутри текстуры
                        if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                        {
                            texture.SetPixel(px, py, color);
                        }
                    }
                }

                x += xIncrement;
                y += yIncrement;
            }
        }

        private static Sprite CreateEmotionPlusIconSprite()
        {
            // Создаем текстуру для иконки добавления эмоции
            int size = 64;
            Texture2D texture = new Texture2D(size, size);

            // Заполняем текстуру прозрачным цветом
            Color[] colors = new Color[size * size];

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }

            texture.SetPixels(colors);

            // Цвета для иконки
            Color mainColor = new Color(1f, 0.6f, 0.2f, 1f); // Оранжевый

            // Рисуем круг (эмоцию)
            int radius = size / 3;
            int centerX = size / 2;
            int centerY = size / 2;

            for (int y = centerY - radius; y < centerY + radius; y++)
            {
                for (int x = centerX - radius; x < centerX + radius; x++)
                {
                    if (IsInCircle(x, y, centerX, centerY, radius))
                    {
                        texture.SetPixel(x, y, mainColor);
                    }
                }
            }

            // Рисуем плюс внутри круга
            int plusThickness = size / 12;
            int plusLength = radius - 2;

            // Горизонтальная линия
            for (int y = centerY - plusThickness / 2; y < centerY + plusThickness / 2; y++)
            {
                for (int x = centerX - plusLength; x < centerX + plusLength; x++)
                {
                    if (x >= 0 && x < size && y >= 0 && y < size && IsInCircle(x, y, centerX, centerY, radius))
                    {
                        texture.SetPixel(x, y, Color.white);
                    }
                }
            }

            // Вертикальная линия
            for (int y = centerY - plusLength; y < centerY + plusLength; y++)
            {
                for (int x = centerX - plusThickness / 2; x < centerX + plusThickness / 2; x++)
                {
                    if (x >= 0 && x < size && y >= 0 && y < size && IsInCircle(x, y, centerX, centerY, radius))
                    {
                        texture.SetPixel(x, y, Color.white);
                    }
                }
            }

            texture.Apply();

            // Создаем спрайт из текстуры
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private static Sprite CreateHistoryIconSprite()
        {
            // Создаем текстуру для иконки истории (часы)
            int size = 64;
            Texture2D texture = new Texture2D(size, size);

            // Заполняем текстуру прозрачным цветом
            Color[] colors = new Color[size * size];

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }

            texture.SetPixels(colors);

            // Цвета для иконки
            Color mainColor = new Color(0.3f, 0.6f, 1f, 1f); // Голубой

            // Рисуем контур часов
            int radius = size / 3;
            int centerX = size / 2;
            int centerY = size / 2;
            int thickness = 3;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt(Mathf.Pow(x - centerX, 2) + Mathf.Pow(y - centerY, 2));

                    if (dist <= radius && dist >= radius - thickness)
                    {
                        texture.SetPixel(x, y, mainColor);
                    }
                }
            }

            // Рисуем стрелки
            int hourHandLength = radius / 2;
            int minuteHandLength = radius - 4;

            // Часовая стрелка (направлена на 10 часов)
            DrawLine(texture, centerX, centerY,
                (int)(centerX - hourHandLength * 0.7f),
                (int)(centerY + hourHandLength * 0.7f),
                2, mainColor);

            // Минутная стрелка (направлена на 2 часа)
            DrawLine(texture, centerX, centerY,
                (int)(centerX + minuteHandLength * 0.7f),
                (int)(centerY + minuteHandLength * 0.7f),
                2, mainColor);

            // Центральная точка
            for (int y = centerY - 2; y <= centerY + 2; y++)
            {
                for (int x = centerX - 2; x <= centerX + 2; x++)
                {
                    if (IsInCircle(x, y, centerX, centerY, 2))
                    {
                        texture.SetPixel(x, y, mainColor);
                    }
                }
            }

            texture.Apply();

            // Создаем спрайт из текстуры
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private static Sprite CreateFriendsIconSprite()
        {
            // Создаем текстуру для иконки друзей (силуэты людей)
            int size = 64;
            Texture2D texture = new Texture2D(size, size);

            // Заполняем текстуру прозрачным цветом
            Color[] colors = new Color[size * size];

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }

            texture.SetPixels(colors);

            // Цвета для иконки
            Color mainColor = new Color(0.2f, 0.8f, 0.4f, 1f); // Зеленый

            // Рисуем силуэты людей
            int headRadius = size / 10;
            int bodyHeight = size / 4;
            int shoulderWidth = size / 6;

            // Первый силуэт (слева)
            int figure1X = size / 2 - headRadius * 2;
            int figure1Y = size / 2 + headRadius;

            // Голова
            for (int y = figure1Y - headRadius; y < figure1Y + headRadius; y++)
            {
                for (int x = figure1X - headRadius; x < figure1X + headRadius; x++)
                {
                    if (IsInCircle(x, y, figure1X, figure1Y, headRadius))
                    {
                        texture.SetPixel(x, y, mainColor);
                    }
                }
            }

            // Тело
            for (int y = figure1Y - headRadius - bodyHeight; y < figure1Y - headRadius; y++)
            {
                for (int x = figure1X - 1; x < figure1X + 2; x++)
                {
                    texture.SetPixel(x, y, mainColor);
                }
            }

            // Плечи
            for (int y = figure1Y - headRadius - headRadius; y < figure1Y - headRadius; y++)
            {
                for (int x = figure1X - shoulderWidth / 2; x < figure1X + shoulderWidth / 2; x++)
                {
                    texture.SetPixel(x, y, mainColor);
                }
            }

            // Второй силуэт (справа)
            int figure2X = size / 2 + headRadius * 2;
            int figure2Y = size / 2 + headRadius;

            // Голова
            for (int y = figure2Y - headRadius; y < figure2Y + headRadius; y++)
            {
                for (int x = figure2X - headRadius; x < figure2X + headRadius; x++)
                {
                    if (IsInCircle(x, y, figure2X, figure2Y, headRadius))
                    {
                        texture.SetPixel(x, y, mainColor);
                    }
                }
            }

            // Тело
            for (int y = figure2Y - headRadius - bodyHeight; y < figure2Y - headRadius; y++)
            {
                for (int x = figure2X - 1; x < figure2X + 2; x++)
                {
                    texture.SetPixel(x, y, mainColor);
                }
            }

            // Плечи
            for (int y = figure2Y - headRadius - headRadius; y < figure2Y - headRadius; y++)
            {
                for (int x = figure2X - shoulderWidth / 2; x < figure2X + shoulderWidth / 2; x++)
                {
                    texture.SetPixel(x, y, mainColor);
                }
            }

            texture.Apply();

            // Создаем спрайт из текстуры
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private static Sprite CreateSettingsIconSprite()
        {
            // Создаем текстуру для иконки настроек (шестеренка)
            int size = 64;
            Texture2D texture = new Texture2D(size, size);

            // Заполняем текстуру прозрачным цветом
            Color[] colors = new Color[size * size];

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }

            texture.SetPixels(colors);

            // Цвета для иконки
            Color mainColor = new Color(0.8f, 0.8f, 0.85f, 1f); // Серебристый

            // Рисуем шестеренку
            int centerX = size / 2;
            int centerY = size / 2;
            int outerRadius = size / 3;
            int innerRadius = size / 5;
            int teethCount = 8;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float angle = Mathf.Atan2(y - centerY, x - centerX);
                    float distance = Mathf.Sqrt(Mathf.Pow(x - centerX, 2) + Mathf.Pow(y - centerY, 2));

                    // Выступы шестеренки
                    float toothAngle = (angle + Mathf.PI) * teethCount / (2 * Mathf.PI);
                    float toothSize = (Mathf.Abs(toothAngle % 1 - 0.5f) * 2) * 5;

                    if (distance <= outerRadius + toothSize && distance >= innerRadius)
                    {
                        texture.SetPixel(x, y, mainColor);
                    }
                }
            }

            // Центральное отверстие
            for (int y = centerY - innerRadius / 2; y < centerY + innerRadius / 2; y++)
            {
                for (int x = centerX - innerRadius / 2; x < centerX + innerRadius / 2; x++)
                {
                    if (IsInCircle(x, y, centerX, centerY, innerRadius / 2))
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            texture.Apply();

            // Создаем спрайт из текстуры
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private static Sprite CreateWorkshopIconSprite()
        {
            // Создаем текстуру для иконки мастерской (молоток и гаечный ключ)
            int size = 64;
            Texture2D texture = new Texture2D(size, size);

            // Заполняем текстуру прозрачным цветом
            Color[] colors = new Color[size * size];

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }

            texture.SetPixels(colors);

            // Цвета для иконки
            Color mainColor = new Color(0.9f, 0.5f, 0.2f, 1f); // Медный оттенок

            int centerX = size / 2;
            int centerY = size / 2;

            // Рисуем молоток
            int hammerHeadWidth = size / 5;
            int hammerHeadHeight = size / 8;
            int hammerHandleThickness = size / 16;
            int hammerHandleLength = size / 2;

            // Рукоятка молотка (наклонная линия)
            DrawLine(texture,
                centerX - hammerHandleLength / 4, centerY - hammerHandleLength / 3,
                centerX + hammerHandleLength / 4, centerY + hammerHandleLength / 3,
                hammerHandleThickness, mainColor);

            // Голова молотка
            for (int y = centerY + hammerHandleLength / 3 - hammerHeadHeight / 2;
                 y < centerY + hammerHandleLength / 3 + hammerHeadHeight / 2;
                 y++)
            {
                for (int x = centerX + hammerHandleLength / 4 - hammerHeadWidth / 2;
                     x < centerX + hammerHandleLength / 4 + hammerHeadWidth / 2;
                     x++)
                {
                    texture.SetPixel(x, y, mainColor);
                }
            }

            // Рисуем гаечный ключ
            int wrenchLength = size / 2;
            int wrenchThickness = size / 16;
            int wrenchHeadRadius = size / 8;

            // Рукоятка ключа (наклонная линия в другую сторону)
            DrawLine(texture,
                centerX + hammerHandleLength / 4, centerY - hammerHandleLength / 3,
                centerX - hammerHandleLength / 4, centerY + hammerHandleLength / 3,
                wrenchThickness, mainColor);

            // Головка ключа (кружок с отверстием)
            for (int y = centerY - hammerHandleLength / 3 - wrenchHeadRadius;
                 y < centerY - hammerHandleLength / 3 + wrenchHeadRadius;
                 y++)
            {
                for (int x = centerX + hammerHandleLength / 4 - wrenchHeadRadius;
                     x < centerX + hammerHandleLength / 4 + wrenchHeadRadius;
                     x++)
                {
                    float dist = Mathf.Sqrt(Mathf.Pow(x - (centerX + hammerHandleLength / 4), 2) +
                                            Mathf.Pow(y - (centerY - hammerHandleLength / 3), 2));

                    if (dist <= wrenchHeadRadius && dist >= wrenchHeadRadius / 2)
                    {
                        texture.SetPixel(x, y, mainColor);
                    }
                }
            }

            texture.Apply();

            // Создаем спрайт из текстуры
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        // Класс для обеспечения безопасной области UI в зависимости от устройства
        public class SafeArea : MonoBehaviour
        {
            private RectTransform _rectTransform;
            private Rect _safeArea;
            private Vector2 _minAnchor;
            private Vector2 _maxAnchor;

            private void Awake()
            {
                _rectTransform = GetComponent<RectTransform>();
                _safeArea = Screen.safeArea;
                _minAnchor = _safeArea.position;
                _maxAnchor = _minAnchor + _safeArea.size;

                // Конвертируем в нормализованные координаты анкоров
                _minAnchor.x /= Screen.width;
                _minAnchor.y /= Screen.height;
                _maxAnchor.x /= Screen.width;
                _maxAnchor.y /= Screen.height;

                // Применяем значения
                ApplySafeArea();
            }

            private void ApplySafeArea()
            {
                // Сохраняем текущие значения
                Vector2 anchorMin = _rectTransform.anchorMin;
                Vector2 anchorMax = _rectTransform.anchorMax;

                // Изменяем анкоры только если значения SafeArea отличаются от полного экрана
                if (_minAnchor.x > 0) anchorMin.x = _minAnchor.x;
                if (_minAnchor.y > 0) anchorMin.y = _minAnchor.y;
                if (_maxAnchor.x < 1) anchorMax.x = _maxAnchor.x;
                if (_maxAnchor.y < 1) anchorMax.y = _maxAnchor.y;

                // Установка новых значений
                _rectTransform.anchorMin = anchorMin;
                _rectTransform.anchorMax = anchorMax;
            }

            // Для обновления в редакторе (если нужно)
            private void OnValidate()
            {
                if (_rectTransform == null)
                    _rectTransform = GetComponent<RectTransform>();

                // В редакторе просто растягиваем на полный экран
                _rectTransform.anchorMin = Vector2.zero;
                _rectTransform.anchorMax = Vector2.one;
                _rectTransform.offsetMin = Vector2.zero;
                _rectTransform.offsetMax = Vector2.zero;
            }
        }
    }
}
#endif
