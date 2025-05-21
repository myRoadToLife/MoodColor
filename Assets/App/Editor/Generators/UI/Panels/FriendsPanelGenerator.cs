using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using System.IO;
using App.Develop.UI.Components; // Обновлены зависимости для новых компонентов
using App.Editor.Generators.UI.Core; // Для UIComponentGenerator

namespace App.Editor.Generators.UI.Panels
{
    public static class FriendsPanelGenerator
    {
        private const string TexturesFolder = "Assets/App/Resources/UI/Textures/";
        private const string FontsFolder = "Assets/App/Resources/UI/Fonts/";
        private const string PrefabSaveFolderPath = "Assets/App/Prefabs/Generated/UI/Panels/PersonalArea/";

        private static Sprite _woodenPlankSprite;
        private static TMP_FontAsset _brushyFont;
        
        private static Color _panelBackgroundColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        private static Color _titleContainerColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        private static Color _titleTextColor = new Color(0.2f, 0.1f, 0.05f, 1f); 
        private static float _titleFontSize = 24f;

        // Стили для кнопок и попапа
        private static Color _buttonTextColor = new Color(0.2f, 0.1f, 0.05f, 1f);
        private static float _buttonFontSize = 20f;
        private static Vector2 _buttonSize = new Vector2(220, 60);
        private static Vector2 _tabButtonSize = new Vector2(150, 50);
        private static Vector2 _closeButtonSize = new Vector2(60, 60);
        private static Vector3 _buttonPressedScale = new Vector3(0.95f, 0.95f, 1f);
        private static Color _buttonSpriteTintColor = Color.white;

        private static ColorBlock GetDefaultButtonColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            colors.fadeDuration = 0.1f;
            return colors;
        }

        private static void LoadResources()
        {
            if (_woodenPlankSprite == null)
                _woodenPlankSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(TexturesFolder, "WoodenPlank.png"));
            if (_woodenPlankSprite == null) 
                Debug.LogWarning($"[FriendsPanelGenerator] Текстура WoodenPlank.png не найдена в {TexturesFolder}");

            if (_brushyFont == null)
            {
                _brushyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Path.Combine(FontsFolder, "BrushyFont.asset"));
                if (_brushyFont == null) 
                    _brushyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Path.Combine(FontsFolder, "BrushyFont.ttf"));
                if (_brushyFont == null) 
                    Debug.LogWarning($"[FriendsPanelGenerator] TMP_FontAsset BrushyFont (.asset или .ttf) не найден в {FontsFolder}.");
            }
        }

        [MenuItem("MoodColor/Generate/UI Panels/Personal Area/Friends Panel")]
        public static void CreateFriendsPanelPrefab()
        {
            LoadResources();

            string panelName = "FriendsPanel";
            string title = "Друзья";

            GameObject panelRoot = UIComponentGenerator.CreateBasePanelRoot(panelName, RenderMode.ScreenSpaceOverlay, 10, new Vector2(1080, 1920));

            // Создаем основные визуальные элементы панели
            Transform contentContainer = UIComponentGenerator.CreateBasePanelVisuals(
                panelRoot, title, _brushyFont, _titleTextColor, _titleFontSize,
                _panelBackgroundColor, _titleContainerColor, 
                null, Image.Type.Simple,
                null, Image.Type.Simple
            ).transform;

            // Создаем кнопку "Закрыть" (X)
            GameObject closeButton = UIComponentGenerator.CreateStyledButton(
                "CloseButton", "X", panelRoot.transform,
                _brushyFont, _buttonTextColor, _buttonFontSize + 4,
                _woodenPlankSprite, Image.Type.Sliced, _buttonSpriteTintColor,
                GetDefaultButtonColors(), _closeButtonSize, _buttonPressedScale
            );
            RectTransform closeButtonRect = closeButton.GetComponent<RectTransform>();
            closeButtonRect.anchorMin = new Vector2(1, 1);
            closeButtonRect.anchorMax = new Vector2(1, 1);
            closeButtonRect.pivot = new Vector2(1, 1);
            closeButtonRect.anchoredPosition = new Vector2(-15, -15);

            // Создаем табы для навигации
            GameObject tabContainer = new GameObject("TabContainer");
            tabContainer.transform.SetParent(contentContainer, false);
            RectTransform tabContainerRect = tabContainer.AddComponent<RectTransform>();
            tabContainerRect.anchorMin = new Vector2(0, 1);
            tabContainerRect.anchorMax = new Vector2(1, 1);
            tabContainerRect.pivot = new Vector2(0.5f, 1);
            tabContainerRect.anchoredPosition = new Vector2(0, -60);
            tabContainerRect.sizeDelta = new Vector2(0, 60);
            
            HorizontalLayoutGroup tabLayout = tabContainer.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 10;
            tabLayout.padding = new RectOffset(20, 20, 0, 0);
            tabLayout.childAlignment = TextAnchor.MiddleCenter;
            tabLayout.childControlWidth = false;
            tabLayout.childControlHeight = false;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childForceExpandHeight = false;

            // Создаем кнопки табов
            GameObject friendsTabButton = UIComponentGenerator.CreateStyledButton(
                "FriendsTabButton", "Друзья", tabContainer.transform,
                _brushyFont, _buttonTextColor, _buttonFontSize - 4,
                _woodenPlankSprite, Image.Type.Sliced, _buttonSpriteTintColor,
                GetDefaultButtonColors(), _tabButtonSize, _buttonPressedScale
            );

            GameObject requestsTabButton = UIComponentGenerator.CreateStyledButton(
                "RequestsTabButton", "Запросы", tabContainer.transform,
                _brushyFont, _buttonTextColor, _buttonFontSize - 4,
                _woodenPlankSprite, Image.Type.Sliced, _buttonSpriteTintColor,
                GetDefaultButtonColors(), _tabButtonSize, _buttonPressedScale
            );

            GameObject searchTabButton = UIComponentGenerator.CreateStyledButton(
                "SearchTabButton", "Поиск", tabContainer.transform,
                _brushyFont, _buttonTextColor, _buttonFontSize - 4,
                _woodenPlankSprite, Image.Type.Sliced, _buttonSpriteTintColor,
                GetDefaultButtonColors(), _tabButtonSize, _buttonPressedScale
            );

            // Создаем контейнеры для контента табов
            GameObject tabContentContainer = new GameObject("TabContentContainer");
            tabContentContainer.transform.SetParent(contentContainer, false);
            RectTransform tabContentContainerRect = tabContentContainer.AddComponent<RectTransform>();
            tabContentContainerRect.anchorMin = new Vector2(0, 0);
            tabContentContainerRect.anchorMax = new Vector2(1, 1);
            tabContentContainerRect.pivot = new Vector2(0.5f, 0.5f);
            tabContentContainerRect.anchoredPosition = new Vector2(0, -30);
            tabContentContainerRect.sizeDelta = new Vector2(-40, -120);

            // Создаем контент для таба "Друзья"
            GameObject friendsTab = CreateFriendsTabContent(tabContentContainer.transform);
            
            // Создаем контент для таба "Запросы"
            GameObject requestsTab = CreateRequestsTabContent(tabContentContainer.transform);
            
            // Создаем контент для таба "Поиск"
            GameObject searchTab = CreateSearchTabContent(tabContentContainer.transform);

            // Создаем индикатор загрузки
            GameObject loadingIndicator = CreateLoadingIndicator(contentContainer);

            // Добавляем компоненты FriendsPanel и генераторы
            FriendsPanel friendsPanelComponent = panelRoot.AddComponent<FriendsPanel>();
            
            // Добавляем генераторы элементов
            FriendsListGenerator friendsListGenerator = friendsTab.AddComponent<FriendsListGenerator>();
            friendsListGenerator.Initialize(null); // Будет инициализировано в рантайме
            
            FriendRequestsGenerator friendRequestsGenerator = requestsTab.AddComponent<FriendRequestsGenerator>();
            friendRequestsGenerator.Initialize(null); // Будет инициализировано в рантайме
            
            FriendSearchGenerator friendSearchGenerator = searchTab.AddComponent<FriendSearchGenerator>();
            friendSearchGenerator.Initialize(null); // Будет инициализировано в рантайме

            // Настраиваем компонент FriendsPanel через SerializedObject
            SerializedObject serializedPanel = new SerializedObject(friendsPanelComponent);
            
            // Назначаем кнопки табов
            serializedPanel.FindProperty("_friendsTabButton").objectReferenceValue = friendsTabButton.GetComponent<Button>();
            serializedPanel.FindProperty("_requestsTabButton").objectReferenceValue = requestsTabButton.GetComponent<Button>();
            serializedPanel.FindProperty("_searchTabButton").objectReferenceValue = searchTabButton.GetComponent<Button>();
            
            // Назначаем контейнеры табов
            serializedPanel.FindProperty("_friendsTab").objectReferenceValue = friendsTab;
            serializedPanel.FindProperty("_requestsTab").objectReferenceValue = requestsTab;
            serializedPanel.FindProperty("_searchTab").objectReferenceValue = searchTab;
            
            // Назначаем другие элементы
            serializedPanel.FindProperty("_loadingIndicator").objectReferenceValue = loadingIndicator;
            serializedPanel.FindProperty("_closeButton").objectReferenceValue = closeButton.GetComponent<Button>();
            
            // Для поискового таба
            var searchInput = searchTab.transform.Find("SearchContainer/SearchInputField")?.GetComponent<TMP_InputField>();
            var searchButton = searchTab.transform.Find("SearchContainer/SearchButton")?.GetComponent<Button>();
            
            if (searchInput != null && searchButton != null)
            {
                serializedPanel.FindProperty("_searchInputField").objectReferenceValue = searchInput;
                serializedPanel.FindProperty("_searchButton").objectReferenceValue = searchButton;
            }
            
            serializedPanel.ApplyModifiedPropertiesWithoutUndo();
            
            // Настройка генераторов
            SerializedObject serializedFriendsGenerator = new SerializedObject(friendsListGenerator);
            serializedFriendsGenerator.FindProperty("_friendsPanel").objectReferenceValue = friendsPanelComponent;
            serializedFriendsGenerator.FindProperty("_itemContainer").objectReferenceValue = friendsTab.transform.Find("ScrollView/Viewport/Content");
            serializedFriendsGenerator.FindProperty("_itemPrefab").objectReferenceValue = null; // Будет задано вручную
            serializedFriendsGenerator.FindProperty("_noItemsMessage").objectReferenceValue = friendsTab.transform.Find("NoFriendsText")?.gameObject;
            serializedFriendsGenerator.ApplyModifiedPropertiesWithoutUndo();
            
            SerializedObject serializedRequestsGenerator = new SerializedObject(friendRequestsGenerator);
            serializedRequestsGenerator.FindProperty("_friendsPanel").objectReferenceValue = friendsPanelComponent;
            serializedRequestsGenerator.FindProperty("_itemContainer").objectReferenceValue = requestsTab.transform.Find("ScrollView/Viewport/Content");
            serializedRequestsGenerator.FindProperty("_itemPrefab").objectReferenceValue = null; // Будет задано вручную
            serializedRequestsGenerator.FindProperty("_noItemsMessage").objectReferenceValue = requestsTab.transform.Find("NoRequestsText")?.gameObject;
            serializedRequestsGenerator.ApplyModifiedPropertiesWithoutUndo();
            
            SerializedObject serializedSearchGenerator = new SerializedObject(friendSearchGenerator);
            serializedSearchGenerator.FindProperty("_friendsPanel").objectReferenceValue = friendsPanelComponent;
            serializedSearchGenerator.FindProperty("_itemContainer").objectReferenceValue = searchTab.transform.Find("ScrollView/Viewport/Content");
            serializedSearchGenerator.FindProperty("_itemPrefab").objectReferenceValue = null; // Будет задано вручную
            serializedSearchGenerator.FindProperty("_noItemsMessage").objectReferenceValue = searchTab.transform.Find("NoResultsText")?.gameObject;
            serializedSearchGenerator.ApplyModifiedPropertiesWithoutUndo();

            // --- Автоматическая генерация и назначение itemPrefab ---
            var friendItemPrefab = CreateOrFindFriendItemPrefab();
            var friendRequestItemPrefab = CreateOrFindFriendRequestItemPrefab();
            var friendSearchItemPrefab = CreateOrFindFriendSearchItemPrefab();

            serializedFriendsGenerator.FindProperty("_itemPrefab").objectReferenceValue = friendItemPrefab;
            serializedRequestsGenerator.FindProperty("_itemPrefab").objectReferenceValue = friendRequestItemPrefab;
            serializedSearchGenerator.FindProperty("_itemPrefab").objectReferenceValue = friendSearchItemPrefab;
            serializedFriendsGenerator.ApplyModifiedPropertiesWithoutUndo();
            serializedRequestsGenerator.ApplyModifiedPropertiesWithoutUndo();
            serializedSearchGenerator.ApplyModifiedPropertiesWithoutUndo();

            // Сохраняем префаб
            UIComponentGenerator.SavePrefab(panelRoot, PrefabSaveFolderPath, panelName);
            
            if (!Application.isPlaying)
            {
                 GameObject.DestroyImmediate(panelRoot);
            }

            Debug.Log($"[FriendsPanelGenerator] Префаб {panelName} создан в {Path.Combine(PrefabSaveFolderPath, panelName + ".prefab")}");

            // --- Вставить после создания panelRoot ---
            var canvas = panelRoot.GetComponent<Canvas>();
            var scaler = panelRoot.GetComponent<CanvasScaler>() ?? panelRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private static GameObject CreateFriendsTabContent(Transform parent)
        {
            GameObject friendsTab = new GameObject("FriendsTab");
            friendsTab.transform.SetParent(parent, false);
            RectTransform friendsTabRect = friendsTab.AddComponent<RectTransform>();
            friendsTabRect.anchorMin = Vector2.zero;
            friendsTabRect.anchorMax = Vector2.one;
            friendsTabRect.sizeDelta = Vector2.zero;
            
            // Текст "Нет друзей"
            GameObject noFriendsText = new GameObject("NoFriendsText");
            noFriendsText.transform.SetParent(friendsTab.transform, false);
            RectTransform noFriendsTextRect = noFriendsText.AddComponent<RectTransform>();
            noFriendsTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            noFriendsTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            noFriendsTextRect.sizeDelta = new Vector2(400, 100);
            noFriendsTextRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI noFriendsTextComp = noFriendsText.AddComponent<TextMeshProUGUI>();
            noFriendsTextComp.text = "У вас еще нет друзей";
            noFriendsTextComp.font = _brushyFont;
            noFriendsTextComp.fontSize = _buttonFontSize;
            noFriendsTextComp.color = _titleTextColor;
            noFriendsTextComp.alignment = TextAlignmentOptions.Center;
            
            // Скроллящийся список друзей
            GameObject scrollView = CreateScrollView(friendsTab.transform, "ScrollView");
            
            return friendsTab;
        }

        private static GameObject CreateRequestsTabContent(Transform parent)
        {
            GameObject requestsTab = new GameObject("RequestsTab");
            requestsTab.transform.SetParent(parent, false);
            RectTransform requestsTabRect = requestsTab.AddComponent<RectTransform>();
            requestsTabRect.anchorMin = Vector2.zero;
            requestsTabRect.anchorMax = Vector2.one;
            requestsTabRect.sizeDelta = Vector2.zero;
            
            // Текст "Нет запросов"
            GameObject noRequestsText = new GameObject("NoRequestsText");
            noRequestsText.transform.SetParent(requestsTab.transform, false);
            RectTransform noRequestsTextRect = noRequestsText.AddComponent<RectTransform>();
            noRequestsTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            noRequestsTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            noRequestsTextRect.sizeDelta = new Vector2(400, 100);
            noRequestsTextRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI noRequestsTextComp = noRequestsText.AddComponent<TextMeshProUGUI>();
            noRequestsTextComp.text = "У вас нет запросов в друзья";
            noRequestsTextComp.font = _brushyFont;
            noRequestsTextComp.fontSize = _buttonFontSize;
            noRequestsTextComp.color = _titleTextColor;
            noRequestsTextComp.alignment = TextAlignmentOptions.Center;
            
            // Скроллящийся список запросов
            GameObject scrollView = CreateScrollView(requestsTab.transform, "ScrollView");
            
            return requestsTab;
        }

        private static GameObject CreateSearchTabContent(Transform parent)
        {
            GameObject searchTab = new GameObject("SearchTab");
            searchTab.transform.SetParent(parent, false);
            RectTransform searchTabRect = searchTab.AddComponent<RectTransform>();
            searchTabRect.anchorMin = Vector2.zero;
            searchTabRect.anchorMax = Vector2.one;
            searchTabRect.sizeDelta = Vector2.zero;
            
            // Контейнер для поиска
            GameObject searchContainer = new GameObject("SearchContainer");
            searchContainer.transform.SetParent(searchTab.transform, false);
            RectTransform searchContainerRect = searchContainer.AddComponent<RectTransform>();
            searchContainerRect.anchorMin = new Vector2(0, 1);
            searchContainerRect.anchorMax = new Vector2(1, 1);
            searchContainerRect.pivot = new Vector2(0.5f, 1);
            searchContainerRect.sizeDelta = new Vector2(0, 60);
            searchContainerRect.anchoredPosition = new Vector2(0, 0);
            
            HorizontalLayoutGroup searchLayout = searchContainer.AddComponent<HorizontalLayoutGroup>();
            searchLayout.spacing = 10;
            searchLayout.padding = new RectOffset(20, 20, 10, 10);
            searchLayout.childAlignment = TextAnchor.MiddleCenter;
            searchLayout.childControlWidth = false;
            searchLayout.childControlHeight = false;
            searchLayout.childForceExpandWidth = false;
            searchLayout.childForceExpandHeight = false;
            
            // Поле ввода для поиска
            GameObject searchInputField = new GameObject("SearchInputField");
            searchInputField.transform.SetParent(searchContainer.transform, false);
            RectTransform searchInputRect = searchInputField.AddComponent<RectTransform>();
            searchInputRect.sizeDelta = new Vector2(300, 40);
            
            Image searchInputBg = searchInputField.AddComponent<Image>();
            searchInputBg.color = Color.white;
            searchInputBg.sprite = _woodenPlankSprite;
            searchInputBg.type = Image.Type.Sliced;
            
            TMP_InputField inputField = searchInputField.AddComponent<TMP_InputField>();
            
            // Создаем текстовое поле для placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(searchInputField.transform, false);
            RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = new Vector2(-20, -10);
            placeholderRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholderText.text = "Введите имя пользователя...";
            placeholderText.font = _brushyFont;
            placeholderText.fontSize = _buttonFontSize - 4;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1);
            placeholderText.alignment = TextAlignmentOptions.Left;
            
            // Создаем текстовое поле для текста ввода
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(searchInputField.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = new Vector2(-20, -10);
            textRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI inputText = textObj.AddComponent<TextMeshProUGUI>();
            inputText.font = _brushyFont;
            inputText.fontSize = _buttonFontSize - 4;
            inputText.color = _buttonTextColor;
            inputText.alignment = TextAlignmentOptions.Left;
            
            // Настраиваем InputField
            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;
            inputField.caretColor = _buttonTextColor;
            inputField.selectionColor = new Color(0.2f, 0.8f, 1, 0.5f);
            
            // Кнопка поиска
            GameObject searchButton = UIComponentGenerator.CreateStyledButton(
                "SearchButton", "Поиск", searchContainer.transform,
                _brushyFont, _buttonTextColor, _buttonFontSize - 4,
                _woodenPlankSprite, Image.Type.Sliced, _buttonSpriteTintColor,
                GetDefaultButtonColors(), new Vector2(120, 40), _buttonPressedScale
            );
            
            // Текст "Нет результатов"
            GameObject noResultsText = new GameObject("NoResultsText");
            noResultsText.transform.SetParent(searchTab.transform, false);
            RectTransform noResultsTextRect = noResultsText.AddComponent<RectTransform>();
            noResultsTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            noResultsTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            noResultsTextRect.sizeDelta = new Vector2(400, 100);
            noResultsTextRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI noResultsTextComp = noResultsText.AddComponent<TextMeshProUGUI>();
            noResultsTextComp.text = "Нет результатов поиска";
            noResultsTextComp.font = _brushyFont;
            noResultsTextComp.fontSize = _buttonFontSize;
            noResultsTextComp.color = _titleTextColor;
            noResultsTextComp.alignment = TextAlignmentOptions.Center;
            
            // Скроллящийся список результатов
            GameObject scrollView = CreateScrollView(searchTab.transform, "ScrollView");
            
            // Устанавливаем якорь скроллвью ниже контейнера поиска
            RectTransform scrollViewRect = scrollView.GetComponent<RectTransform>();
            scrollViewRect.anchorMin = new Vector2(0, 0);
            scrollViewRect.anchorMax = new Vector2(1, 1);
            scrollViewRect.offsetMin = new Vector2(0, 0);
            scrollViewRect.offsetMax = new Vector2(0, -60); // Отступ сверху под searchContainer
            
            return searchTab;
        }

        private static GameObject CreateScrollView(Transform parent, string name)
        {
            GameObject scrollView = new GameObject(name);
            scrollView.transform.SetParent(parent, false);
            RectTransform scrollViewRect = scrollView.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = Vector2.zero;
            scrollViewRect.anchorMax = Vector2.one;
            scrollViewRect.sizeDelta = Vector2.zero;
            
            ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
            
            // Создаем viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;
            
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(1, 1, 1, 0.05f);
            
            // Маска для viewport
            Mask viewportMask = viewport.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            
            // Создаем контент
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0); // Высота будет задаваться динамически
            
            // Настраиваем VerticalLayoutGroup для контента
            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 10;
            contentLayout.padding = new RectOffset(10, 10, 10, 10);
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            
            // ContentSizeFitter для автоматического изменения размера контента
            ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Настраиваем ScrollRect
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 15;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            
            return scrollView;
        }

        private static GameObject CreateLoadingIndicator(Transform parent)
        {
            GameObject loadingIndicator = new GameObject("LoadingIndicator");
            loadingIndicator.transform.SetParent(parent, false);
            RectTransform loadingRect = loadingIndicator.AddComponent<RectTransform>();
            loadingRect.anchorMin = new Vector2(0.5f, 0.5f);
            loadingRect.anchorMax = new Vector2(0.5f, 0.5f);
            loadingRect.sizeDelta = new Vector2(100, 100);
            
            Image loadingImage = loadingIndicator.AddComponent<Image>();
            loadingImage.color = Color.white;
            
            // Для простоты используем тот же спрайт, но в реальном проекте нужен спрайт индикатора загрузки
            loadingImage.sprite = _woodenPlankSprite;
            loadingImage.type = Image.Type.Simple;
            
            // Создаем текст "Загрузка..."
            GameObject loadingText = new GameObject("LoadingText");
            loadingText.transform.SetParent(loadingIndicator.transform, false);
            RectTransform loadingTextRect = loadingText.AddComponent<RectTransform>();
            loadingTextRect.anchorMin = Vector2.zero;
            loadingTextRect.anchorMax = Vector2.one;
            loadingTextRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI loadingTextComp = loadingText.AddComponent<TextMeshProUGUI>();
            loadingTextComp.text = "Загрузка...";
            loadingTextComp.font = _brushyFont;
            loadingTextComp.fontSize = _buttonFontSize - 4;
            loadingTextComp.color = _buttonTextColor;
            loadingTextComp.alignment = TextAlignmentOptions.Center;
            
            // Добавляем анимацию вращения (просто игровой объект, в редакторе анимация не будет работать)
            loadingIndicator.AddComponent<RectTransform>();
            
            return loadingIndicator;
        }

        private static void EnsurePrefabFolderExists()
        {
            string[] parts = PrefabSaveFolderPath.TrimEnd('/').Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static GameObject CreateOrFindFriendItemPrefab()
        {
            EnsurePrefabFolderExists();
            string path = Path.Combine(PrefabSaveFolderPath, "FriendItem.prefab");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) return prefab;
            GameObject friendItem = new GameObject("FriendItem");
            RectTransform itemRect = friendItem.AddComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(400, 80);
            Image itemBg = friendItem.AddComponent<Image>();
            itemBg.color = new Color(0.92f, 0.92f, 0.92f, 1f);
            var layout = friendItem.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10,10,10,10);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            // Аватар
            GameObject avatarImage = new GameObject("AvatarImage");
            avatarImage.transform.SetParent(friendItem.transform, false);
            avatarImage.AddComponent<RectTransform>().sizeDelta = new Vector2(60, 60);
            avatarImage.AddComponent<Image>();
            var avatarLE = avatarImage.AddComponent<LayoutElement>();
            avatarLE.preferredWidth = 60; avatarLE.preferredHeight = 60;
            // Имя
            GameObject nameText = new GameObject("NameText");
            nameText.transform.SetParent(friendItem.transform, false);
            nameText.AddComponent<RectTransform>();
            var nameTextComp = nameText.AddComponent<TextMeshProUGUI>();
            nameTextComp.text = "Имя";
            nameTextComp.alignment = TextAlignmentOptions.MidlineLeft;
            var nameLE = nameText.AddComponent<LayoutElement>();
            nameLE.preferredWidth = 120; nameLE.flexibleWidth = 1;
            // Статус
            GameObject statusText = new GameObject("StatusText");
            statusText.transform.SetParent(friendItem.transform, false);
            statusText.AddComponent<RectTransform>();
            var statusTextComp = statusText.AddComponent<TextMeshProUGUI>();
            statusTextComp.text = "Онлайн";
            statusTextComp.alignment = TextAlignmentOptions.MidlineLeft;
            var statusLE = statusText.AddComponent<LayoutElement>();
            statusLE.preferredWidth = 80;
            // Кнопка удаления
            GameObject removeButton = new GameObject("RemoveButton");
            removeButton.transform.SetParent(friendItem.transform, false);
            removeButton.AddComponent<RectTransform>();
            removeButton.AddComponent<Image>();
            removeButton.AddComponent<Button>();
            var removeLE = removeButton.AddComponent<LayoutElement>();
            removeLE.preferredWidth = 90; removeLE.preferredHeight = 40;
            GameObject removeBtnText = new GameObject("Text");
            removeBtnText.transform.SetParent(removeButton.transform, false);
            removeBtnText.AddComponent<RectTransform>();
            var removeBtnTextComp = removeBtnText.AddComponent<TextMeshProUGUI>();
            removeBtnTextComp.text = "Удалить";
            removeBtnTextComp.alignment = TextAlignmentOptions.Center;
            // Сохраняем
            var prefabObj = PrefabUtility.SaveAsPrefabAsset(friendItem, path);
            GameObject.DestroyImmediate(friendItem);
            AssetDatabase.SaveAssets();
            return prefabObj;
        }

        private static GameObject CreateOrFindFriendRequestItemPrefab()
        {
            EnsurePrefabFolderExists();
            string path = Path.Combine(PrefabSaveFolderPath, "FriendRequestItem.prefab");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) return prefab;
            GameObject item = new GameObject("FriendRequestItem");
            item.AddComponent<RectTransform>().sizeDelta = new Vector2(400, 80);
            item.AddComponent<Image>().color = new Color(0.92f, 0.92f, 0.92f, 1f);
            var layout = item.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10,10,10,10);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            GameObject avatarImage = new GameObject("AvatarImage");
            avatarImage.transform.SetParent(item.transform, false);
            avatarImage.AddComponent<RectTransform>().sizeDelta = new Vector2(60, 60);
            avatarImage.AddComponent<Image>();
            var avatarLE = avatarImage.AddComponent<LayoutElement>();
            avatarLE.preferredWidth = 60; avatarLE.preferredHeight = 60;
            GameObject nameText = new GameObject("NameText");
            nameText.transform.SetParent(item.transform, false);
            nameText.AddComponent<RectTransform>();
            var nameTextComp = nameText.AddComponent<TextMeshProUGUI>();
            nameTextComp.text = "Имя";
            nameTextComp.alignment = TextAlignmentOptions.MidlineLeft;
            var nameLE = nameText.AddComponent<LayoutElement>();
            nameLE.preferredWidth = 120; nameLE.flexibleWidth = 1;
            GameObject acceptButton = new GameObject("AcceptButton");
            acceptButton.transform.SetParent(item.transform, false);
            acceptButton.AddComponent<RectTransform>();
            acceptButton.AddComponent<Image>();
            acceptButton.AddComponent<Button>();
            var acceptLE = acceptButton.AddComponent<LayoutElement>();
            acceptLE.preferredWidth = 90; acceptLE.preferredHeight = 40;
            GameObject acceptBtnText = new GameObject("Text");
            acceptBtnText.transform.SetParent(acceptButton.transform, false);
            acceptBtnText.AddComponent<RectTransform>();
            var acceptBtnTextComp = acceptBtnText.AddComponent<TextMeshProUGUI>();
            acceptBtnTextComp.text = "Принять";
            acceptBtnTextComp.alignment = TextAlignmentOptions.Center;
            GameObject declineButton = new GameObject("DeclineButton");
            declineButton.transform.SetParent(item.transform, false);
            declineButton.AddComponent<RectTransform>();
            declineButton.AddComponent<Image>();
            declineButton.AddComponent<Button>();
            var declineLE = declineButton.AddComponent<LayoutElement>();
            declineLE.preferredWidth = 90; declineLE.preferredHeight = 40;
            GameObject declineBtnText = new GameObject("Text");
            declineBtnText.transform.SetParent(declineButton.transform, false);
            declineBtnText.AddComponent<RectTransform>();
            var declineBtnTextComp = declineBtnText.AddComponent<TextMeshProUGUI>();
            declineBtnTextComp.text = "Отклонить";
            declineBtnTextComp.alignment = TextAlignmentOptions.Center;
            var prefabObj = PrefabUtility.SaveAsPrefabAsset(item, path);
            GameObject.DestroyImmediate(item);
            AssetDatabase.SaveAssets();
            return prefabObj;
        }

        private static GameObject CreateOrFindFriendSearchItemPrefab()
        {
            EnsurePrefabFolderExists();
            string path = Path.Combine(PrefabSaveFolderPath, "FriendSearchItem.prefab");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) return prefab;
            GameObject item = new GameObject("FriendSearchItem");
            item.AddComponent<RectTransform>().sizeDelta = new Vector2(400, 80);
            item.AddComponent<Image>().color = new Color(0.92f, 0.92f, 0.92f, 1f);
            var layout = item.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10,10,10,10);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            GameObject avatarImage = new GameObject("AvatarImage");
            avatarImage.transform.SetParent(item.transform, false);
            avatarImage.AddComponent<RectTransform>().sizeDelta = new Vector2(60, 60);
            avatarImage.AddComponent<Image>();
            var avatarLE = avatarImage.AddComponent<LayoutElement>();
            avatarLE.preferredWidth = 60; avatarLE.preferredHeight = 60;
            GameObject nameText = new GameObject("NameText");
            nameText.transform.SetParent(item.transform, false);
            nameText.AddComponent<RectTransform>();
            var nameTextComp = nameText.AddComponent<TextMeshProUGUI>();
            nameTextComp.text = "Имя";
            nameTextComp.alignment = TextAlignmentOptions.MidlineLeft;
            var nameLE = nameText.AddComponent<LayoutElement>();
            nameLE.preferredWidth = 120; nameLE.flexibleWidth = 1;
            GameObject addButton = new GameObject("AddButton");
            addButton.transform.SetParent(item.transform, false);
            addButton.AddComponent<RectTransform>();
            addButton.AddComponent<Image>();
            addButton.AddComponent<Button>();
            var addLE = addButton.AddComponent<LayoutElement>();
            addLE.preferredWidth = 90; addLE.preferredHeight = 40;
            GameObject addBtnText = new GameObject("Text");
            addBtnText.transform.SetParent(addButton.transform, false);
            addBtnText.AddComponent<RectTransform>();
            var addBtnTextComp = addBtnText.AddComponent<TextMeshProUGUI>();
            addBtnTextComp.text = "Добавить";
            addBtnTextComp.alignment = TextAlignmentOptions.Center;
            var prefabObj = PrefabUtility.SaveAsPrefabAsset(item, path);
            GameObject.DestroyImmediate(item);
            AssetDatabase.SaveAssets();
            return prefabObj;
        }
    }
} 