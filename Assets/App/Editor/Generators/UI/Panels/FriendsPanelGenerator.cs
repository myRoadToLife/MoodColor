using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using System.IO;
using App.Develop.Scenes.PersonalAreaScene.UI; // Для FriendsPanelController и FriendItemView
using App.Editor.Generators.UI.Core; // Для UIComponentGenerator
using App.Develop.Utils.Logging;

namespace App.Editor.Generators.UI.Panels
{
    public static class FriendsPanelGenerator
    {
        private const string TexturesFolder = "Assets/App/Resources/UI/Textures/";
        private const string FontsFolder = "Assets/App/Resources/UI/Fonts/";
        private const string PrefabSaveFolderPath = "Assets/App/Prefabs/Generated/UI/Panels/PersonalArea/";
        private const string AddressableSavePath = "Assets/App/Addressables/UI/Panels/";

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

        // Стили для попапа
        private static Color _popupBgColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        private static Color _popupTextColor = Color.white;
        private static float _popupFontSize = 18f;

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
                throw new System.IO.FileNotFoundException($"[FriendsPanelGenerator] Текстура WoodenPlank.png не найдена в {TexturesFolder}");

            if (_brushyFont == null)
            {
                _brushyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Path.Combine(FontsFolder, "BrushyFont.asset"));
                if (_brushyFont == null)
                    _brushyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Path.Combine(FontsFolder, "BrushyFont.ttf"));
                if (_brushyFont == null)
                    throw new System.IO.FileNotFoundException($"[FriendsPanelGenerator] TMP_FontAsset BrushyFont (.asset или .ttf) не найден в {FontsFolder}.");
            }
        }

        public static void CreateFriendsPanelPrefab()
        {
            LoadResources();

            string panelName = "FriendsPanel";
            string title = "Друзья";

            // Создаем корневой элемент с Canvas и CanvasScaler
            GameObject panelRoot = UIComponentGenerator.CreateBasePanelRoot(panelName, RenderMode.ScreenSpaceOverlay, 10, new Vector2(1080, 1920), 0.5f);

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

            // Создаем кнопки - добавление друга и обновление списка
            GameObject buttonsContainer = new GameObject("ButtonsContainer");
            buttonsContainer.transform.SetParent(contentContainer, false);
            RectTransform buttonsContainerRect = buttonsContainer.AddComponent<RectTransform>();
            buttonsContainerRect.anchorMin = new Vector2(0, 1);
            buttonsContainerRect.anchorMax = new Vector2(1, 1);
            buttonsContainerRect.pivot = new Vector2(0.5f, 1);
            buttonsContainerRect.anchoredPosition = new Vector2(0, -70);
            buttonsContainerRect.sizeDelta = new Vector2(-40, 50);

            HorizontalLayoutGroup buttonsLayout = buttonsContainer.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 20;
            buttonsLayout.padding = new RectOffset(20, 20, 0, 0);
            buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonsLayout.childControlWidth = false;
            buttonsLayout.childControlHeight = false;

            GameObject addFriendButton = UIComponentGenerator.CreateStyledButton(
                "AddFriendButton", "Добавить друга", buttonsContainer.transform,
                _brushyFont, _buttonTextColor, _buttonFontSize - 4,
                _woodenPlankSprite, Image.Type.Sliced, _buttonSpriteTintColor,
                GetDefaultButtonColors(), new Vector2(180, 40), _buttonPressedScale
            );

            GameObject refreshButton = UIComponentGenerator.CreateStyledButton(
                "RefreshButton", "Обновить", buttonsContainer.transform,
                _brushyFont, _buttonTextColor, _buttonFontSize - 4,
                _woodenPlankSprite, Image.Type.Sliced, _buttonSpriteTintColor,
                GetDefaultButtonColors(), new Vector2(120, 40), _buttonPressedScale
            );

            // Создаем контейнер для списка друзей
            GameObject friendsListContainer = new GameObject("FriendsListContainer");
            friendsListContainer.transform.SetParent(contentContainer, false);
            RectTransform friendsListContainerRect = friendsListContainer.AddComponent<RectTransform>();
            friendsListContainerRect.anchorMin = new Vector2(0, 0);
            friendsListContainerRect.anchorMax = new Vector2(1, 1);
            friendsListContainerRect.offsetMin = new Vector2(20, 20);
            friendsListContainerRect.offsetMax = new Vector2(-20, -130);

            // Создаем ScrollView для списка друзей
            GameObject scrollView = CreateScrollView(friendsListContainer.transform, "FriendsScrollView");

            // Создаем сообщение об отсутствии друзей
            GameObject emptyListMessage = new GameObject("EmptyListMessage");
            emptyListMessage.transform.SetParent(friendsListContainer.transform, false);
            RectTransform emptyListMessageRect = emptyListMessage.AddComponent<RectTransform>();
            emptyListMessageRect.anchorMin = new Vector2(0.5f, 0.5f);
            emptyListMessageRect.anchorMax = new Vector2(0.5f, 0.5f);
            emptyListMessageRect.sizeDelta = new Vector2(400, 100);
            emptyListMessageRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI emptyListMessageText = emptyListMessage.AddComponent<TextMeshProUGUI>();
            emptyListMessageText.text = "У вас еще нет друзей";
            emptyListMessageText.font = _brushyFont;
            emptyListMessageText.fontSize = _buttonFontSize;
            emptyListMessageText.color = _titleTextColor;
            emptyListMessageText.alignment = TextAlignmentOptions.Center;

            // Создаем индикатор загрузки
            GameObject loadingIndicator = CreateLoadingIndicator(contentContainer);

            // Создаем попап
            GameObject popupPanel = UIComponentGenerator.CreatePopupPanel(
                panelRoot.transform,
                "Сообщение по умолчанию",
                _brushyFont, _popupTextColor, _popupFontSize,
                _popupBgColor
            );
            if (popupPanel) popupPanel.SetActive(false); // Скрываем по умолчанию

            // Добавляем компонент FriendsPanelController
            FriendsPanelController friendsPanelComponent = panelRoot.AddComponent<FriendsPanelController>();

            // Настраиваем компонент FriendsPanelController через SerializedObject
            SerializedObject serializedPanel = new SerializedObject(friendsPanelComponent);

            // Назначаем кнопки
            serializedPanel.FindProperty("_closeButton").objectReferenceValue = closeButton.GetComponent<Button>();
            serializedPanel.FindProperty("_addFriendButton").objectReferenceValue = addFriendButton.GetComponent<Button>();
            serializedPanel.FindProperty("_refreshButton").objectReferenceValue = refreshButton.GetComponent<Button>();

            // Назначаем контейнеры
            serializedPanel.FindProperty("_friendsListContainer").objectReferenceValue = scrollView.transform.Find("Viewport/Content");
            serializedPanel.FindProperty("_emptyListMessage").objectReferenceValue = emptyListMessage;
            serializedPanel.FindProperty("_loadingIndicator").objectReferenceValue = loadingIndicator;

            // Назначаем попап
            serializedPanel.FindProperty("_popupPanel").objectReferenceValue = popupPanel;
            serializedPanel.FindProperty("_popupText").objectReferenceValue = popupPanel.transform.Find("PopupText")?.GetComponent<TMP_Text>();

            // Создаём шаблон префаба для списка друзей
            Transform friendsContent = scrollView.transform.Find("Viewport/Content");
            GameObject friendItemTemplate = CreateFriendItemTemplate(friendsContent);
            friendItemTemplate.SetActive(false);
            serializedPanel.FindProperty("_friendItemPrefab").objectReferenceValue = friendItemTemplate.GetComponent<FriendItemView>();

            serializedPanel.ApplyModifiedPropertiesWithoutUndo();

            // CanvasScaler уже настроен в UIComponentGenerator.CreateBasePanelRoot

            // Сохраняем префаб
            UIComponentGenerator.SavePrefab(panelRoot, PrefabSaveFolderPath, panelName);

            // Сохраняем копию для Addressable
            EnsureDirectoryExists(AddressableSavePath);
            string addressablePrefabPath = Path.Combine(AddressableSavePath, "UIPanel_Friends.prefab");
            string originalPath = Path.Combine(PrefabSaveFolderPath, $"{panelName}.prefab");
            AssetDatabase.CopyAsset(originalPath, addressablePrefabPath);

            // Настраиваем Addressable
            AddressableSetup.SetupFriendsPanelAddressable();

            if (!Application.isPlaying)
            {
                GameObject.DestroyImmediate(panelRoot);
            }

            MyLogger.EditorLog($"[FriendsPanelGenerator] Префаб {panelName} создан в {Path.Combine(PrefabSaveFolderPath, panelName + ".prefab")}");
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        private static GameObject CreateScrollView(Transform parent, string name)
        {
            GameObject scrollView = new GameObject(name);
            scrollView.transform.SetParent(parent, false);
            RectTransform scrollViewRect = scrollView.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = Vector2.zero;
            scrollViewRect.anchorMax = Vector2.one;
            scrollViewRect.sizeDelta = Vector2.zero;
            scrollViewRect.offsetMin = new Vector2(10, 10); // Добавляем отступы от краев
            scrollViewRect.offsetMax = new Vector2(-10, -10);

            // Добавляем фон для скролл-вью
            Image scrollViewBg = scrollView.AddComponent<Image>();
            scrollViewBg.color = new Color(0.95f, 0.95f, 0.95f, 0.2f);
            scrollViewBg.raycastTarget = false; // Чтобы не блокировал события

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
            viewportImage.color = new Color(1, 1, 1, 0.1f);

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
            contentLayout.spacing = 15; // Увеличиваем расстояние между элементами
            contentLayout.padding = new RectOffset(15, 15, 15, 15); // Увеличиваем отступы
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            // ContentSizeFitter для автоматического изменения размера контента
            ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Добавляем полосу прокрутки
            GameObject scrollbar = new GameObject("Scrollbar");
            scrollbar.transform.SetParent(scrollView.transform, false);
            RectTransform scrollbarRect = scrollbar.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.pivot = new Vector2(1, 1);
            scrollbarRect.sizeDelta = new Vector2(15, 0); // Ширина полосы прокрутки
            scrollbarRect.anchoredPosition = Vector2.zero;

            Image scrollbarImage = scrollbar.AddComponent<Image>();
            scrollbarImage.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);

            Scrollbar scrollbarComponent = scrollbar.AddComponent<Scrollbar>();
            scrollbarComponent.direction = Scrollbar.Direction.BottomToTop;

            // Создаем ползунок для полосы прокрутки
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(scrollbar.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = Vector2.zero;

            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);

            scrollbarComponent.handleRect = handleRect;
            scrollbarComponent.targetGraphic = handleImage;

            // Настраиваем ScrollRect
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontalScrollbar = null;
            scrollRect.verticalScrollbar = scrollbarComponent;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 20; // Увеличиваем чувствительность
            scrollRect.movementType = ScrollRect.MovementType.Elastic; // Добавляем эластичность
            scrollRect.elasticity = 0.1f; // Настраиваем эластичность
            scrollRect.inertia = true; // Включаем инерцию
            scrollRect.decelerationRate = 0.135f; // Настраиваем скорость замедления

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

        private static GameObject CreateFriendItemTemplate(Transform parent)
        {
            GameObject friendItemTemplate = new GameObject("FriendItemTemplate");
            friendItemTemplate.transform.SetParent(parent, false);
            RectTransform friendItemTemplateRect = friendItemTemplate.AddComponent<RectTransform>();
            friendItemTemplateRect.anchorMin = Vector2.zero;
            friendItemTemplateRect.anchorMax = Vector2.one;
            friendItemTemplateRect.sizeDelta = new Vector2(0, 80); // Высота элемента

            // Добавляем FriendItemView
            FriendItemView friendItemView = friendItemTemplate.AddComponent<FriendItemView>();

            // Добавляем фон и настраиваем layout
            Image itemBg = friendItemTemplate.AddComponent<Image>();
            itemBg.color = new Color(0.92f, 0.92f, 0.92f, 1f);

            var layout = friendItemTemplate.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            // Аватар
            GameObject avatarImage = new GameObject("AvatarImage");
            avatarImage.transform.SetParent(friendItemTemplate.transform, false);
            avatarImage.AddComponent<RectTransform>().sizeDelta = new Vector2(60, 60);
            avatarImage.AddComponent<Image>();
            var avatarLE = avatarImage.AddComponent<LayoutElement>();
            avatarLE.preferredWidth = 60; avatarLE.preferredHeight = 60;

            // Имя
            GameObject nameText = new GameObject("NameText");
            nameText.transform.SetParent(friendItemTemplate.transform, false);
            nameText.AddComponent<RectTransform>();
            var nameTextComp = nameText.AddComponent<TextMeshProUGUI>();
            nameTextComp.text = "Имя";
            nameTextComp.alignment = TextAlignmentOptions.MidlineLeft;
            nameTextComp.font = _brushyFont;
            nameTextComp.fontSize = _buttonFontSize - 2;
            nameTextComp.color = _buttonTextColor;
            var nameLE = nameText.AddComponent<LayoutElement>();
            nameLE.preferredWidth = 120; nameLE.flexibleWidth = 1;

            // Индикатор онлайн-статуса
            GameObject statusIndicator = new GameObject("OnlineStatusIndicator");
            statusIndicator.transform.SetParent(friendItemTemplate.transform, false);
            statusIndicator.AddComponent<RectTransform>().sizeDelta = new Vector2(20, 20);
            var statusIndicatorImage = statusIndicator.AddComponent<Image>();
            statusIndicatorImage.color = Color.green; // По умолчанию "онлайн"
            var statusLE = statusIndicator.AddComponent<LayoutElement>();
            statusLE.preferredWidth = 20;

            // Кнопка просмотра профиля
            GameObject viewProfileButton = new GameObject("ViewProfileButton");
            viewProfileButton.transform.SetParent(friendItemTemplate.transform, false);
            viewProfileButton.AddComponent<RectTransform>();
            var viewBtnImage = viewProfileButton.AddComponent<Image>();
            viewBtnImage.sprite = _woodenPlankSprite;
            viewBtnImage.type = Image.Type.Sliced;
            var viewBtn = viewProfileButton.AddComponent<Button>();
            viewBtn.colors = GetDefaultButtonColors();
            var viewLE = viewProfileButton.AddComponent<LayoutElement>();
            viewLE.preferredWidth = 90; viewLE.preferredHeight = 40;

            GameObject viewBtnText = new GameObject("Text");
            viewBtnText.transform.SetParent(viewProfileButton.transform, false);
            RectTransform viewBtnTextRect = viewBtnText.AddComponent<RectTransform>();
            viewBtnTextRect.anchorMin = Vector2.zero;
            viewBtnTextRect.anchorMax = Vector2.one;
            viewBtnTextRect.sizeDelta = Vector2.zero;
            var viewBtnTextComp = viewBtnText.AddComponent<TextMeshProUGUI>();
            viewBtnTextComp.text = "Профиль";
            viewBtnTextComp.alignment = TextAlignmentOptions.Center;
            viewBtnTextComp.font = _brushyFont;
            viewBtnTextComp.fontSize = _buttonFontSize - 4;
            viewBtnTextComp.color = _buttonTextColor;

            // Кнопка удаления
            GameObject removeButton = new GameObject("RemoveButton");
            removeButton.transform.SetParent(friendItemTemplate.transform, false);
            var removeBtnImage = removeButton.AddComponent<Image>();
            removeBtnImage.sprite = _woodenPlankSprite;
            removeBtnImage.type = Image.Type.Sliced;
            var removeButton3D = removeButton.AddComponent<Button>();
            removeButton3D.colors = GetDefaultButtonColors();
            var removeLE = removeButton.AddComponent<LayoutElement>();
            removeLE.preferredWidth = 90; removeLE.preferredHeight = 40;
            GameObject removeBtnText = new GameObject("Text");
            removeBtnText.transform.SetParent(removeButton.transform, false);
            RectTransform removeBtnTextRect = removeBtnText.AddComponent<RectTransform>();
            removeBtnTextRect.anchorMin = Vector2.zero;
            removeBtnTextRect.anchorMax = Vector2.one;
            removeBtnTextRect.sizeDelta = Vector2.zero;
            var removeBtnTextComp = removeBtnText.AddComponent<TextMeshProUGUI>();
            removeBtnTextComp.text = "Удалить";
            removeBtnTextComp.alignment = TextAlignmentOptions.Center;
            removeBtnTextComp.font = _brushyFont;
            removeBtnTextComp.fontSize = _buttonFontSize - 4;
            removeBtnTextComp.color = _buttonTextColor;

            // Настроим SerializedObject для FriendItemView
            SerializedObject serializedItemView = new SerializedObject(friendItemView);
            serializedItemView.FindProperty("_userNameText").objectReferenceValue = nameTextComp;
            serializedItemView.FindProperty("_avatarImage").objectReferenceValue = avatarImage.GetComponent<Image>();
            serializedItemView.FindProperty("_onlineStatusIndicator").objectReferenceValue = statusIndicator.GetComponent<Image>();
            serializedItemView.FindProperty("_viewProfileButton").objectReferenceValue = viewProfileButton.GetComponent<Button>();
            serializedItemView.FindProperty("_removeFriendButton").objectReferenceValue = removeButton.GetComponent<Button>();
            serializedItemView.ApplyModifiedPropertiesWithoutUndo();

            return friendItemTemplate;
        }
    }
}