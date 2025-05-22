using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;
using App.Develop.Scenes.PersonalAreaScene.UI;
using App.Develop.CommonServices.AssetManagement;
using App.Editor.Generators.UI.Core;

namespace App.Editor.Generators.UI.Panels
{
    public static class FriendSearchPanelGenerator
    {
        private const string PrefabSavePath = "Assets/App/Prefabs/Generated/UI/Panels/PersonalArea/";
        private const string AddressableSavePath = "Assets/App/Addressables/UI/Panels/";
        
        public static void GenerateFriendSearchPanel()
        {
            // Создаем основу панели
            GameObject panelRoot = new GameObject("FriendSearchPanel");
            
            // Добавляем Canvas и настраиваем его
            Canvas canvas = panelRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            
            // Добавляем CanvasScaler для корректного масштабирования UI
            CanvasScaler scaler = panelRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            
            // Добавляем GraphicRaycaster для обработки кликов
            panelRoot.AddComponent<GraphicRaycaster>();
            
            // Настраиваем RectTransform
            RectTransform rootRect = panelRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;
            rootRect.anchoredPosition = Vector2.zero;
            
            // Создаем фон панели
            GameObject background = CreateBackground(panelRoot.transform);
            
            // Создаем заголовок панели
            GameObject titleBar = CreateTitleBar(panelRoot.transform, "Поиск друзей");
            
            // Создаем кнопку закрытия
            GameObject closeButton = CreateCloseButton(panelRoot.transform);
            
            // Создаем контейнер для поиска
            GameObject searchContainer = CreateSearchContainer(panelRoot.transform);
            
            // Создаем контейнер для результатов
            GameObject resultsContainer = CreateResultsContainer(panelRoot.transform);
            
            // Создаем сообщение об отсутствии результатов
            GameObject noResultsMessage = CreateNoResultsMessage(resultsContainer.transform);
            
            // Создаем индикатор загрузки
            GameObject loadingIndicator = CreateLoadingIndicator(panelRoot.transform);
            
            // Создаем панель сообщений
            GameObject popupPanel = CreatePopupPanel(panelRoot.transform);
            
            // Добавляем компонент контроллера
            FriendSearchPanelController controller = panelRoot.AddComponent<FriendSearchPanelController>();
            
            // Находим необходимые компоненты для привязки
            TMP_InputField searchInputField = searchContainer.transform.Find("SearchInputField").GetComponent<TMP_InputField>();
            Button searchButton = searchContainer.transform.Find("SearchButton").GetComponent<Button>();
            Transform searchResultsContainer = resultsContainer.transform.Find("ScrollView/Viewport/Content");
            TMP_Text popupText = popupPanel.transform.Find("PopupText").GetComponent<TMP_Text>();
            
            // Настраиваем контроллер через SerializedObject
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("_searchInputField").objectReferenceValue = searchInputField;
            serializedController.FindProperty("_searchButton").objectReferenceValue = searchButton;
            serializedController.FindProperty("_closeButton").objectReferenceValue = closeButton.GetComponent<Button>();
            serializedController.FindProperty("_searchResultsContainer").objectReferenceValue = searchResultsContainer;
            serializedController.FindProperty("_popupPanel").objectReferenceValue = popupPanel;
            serializedController.FindProperty("_popupText").objectReferenceValue = popupText;
            serializedController.FindProperty("_loadingIndicator").objectReferenceValue = loadingIndicator;
            serializedController.FindProperty("_noResultsMessage").objectReferenceValue = noResultsMessage;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            
            // Сохраняем префаб
            EnsureDirectoryExists(PrefabSavePath);
            GameObject prefabAsset = SavePrefab(panelRoot, PrefabSavePath, "FriendSearchPanel");
            
            // Сохраняем копию для Addressable
            EnsureDirectoryExists(AddressableSavePath);
            string addressablePrefabPath = Path.Combine(AddressableSavePath, "UIPanel_FriendSearch.prefab");
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(prefabAsset), addressablePrefabPath);
            
            // Настраиваем Addressable
            AddressableSetup.SetupFriendSearchPanelAddressable();
            
            // Уничтожаем временный объект
            Object.DestroyImmediate(panelRoot);
            
            Debug.Log("✅ FriendSearchPanel успешно сгенерирована и настроена как Addressable ресурс");
        }
        
        private static GameObject CreateBackground(Transform parent)
        {
            GameObject background = new GameObject("Background");
            background.transform.SetParent(parent, false);
            
            RectTransform rect = background.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            
            Image image = background.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            
            return background;
        }
        
        private static GameObject CreateTitleBar(Transform parent, string title)
        {
            GameObject titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(parent, false);
            
            RectTransform rect = titleBar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.sizeDelta = new Vector2(0, 80);
            rect.anchoredPosition = new Vector2(0, -40);
            
            Image image = titleBar.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            GameObject titleText = new GameObject("TitleText");
            titleText.transform.SetParent(titleBar.transform, false);
            
            RectTransform textRect = titleText.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.sizeDelta = new Vector2(-100, -20);
            
            TextMeshProUGUI tmp = titleText.AddComponent<TextMeshProUGUI>();
            tmp.text = title;
            tmp.fontSize = 32;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            
            return titleBar;
        }
        
        private static GameObject CreateCloseButton(Transform parent)
        {
            GameObject closeButton = new GameObject("CloseButton");
            closeButton.transform.SetParent(parent, false);
            
            RectTransform rect = closeButton.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.sizeDelta = new Vector2(60, 60);
            rect.anchoredPosition = new Vector2(-30, -30);
            
            Image image = closeButton.AddComponent<Image>();
            image.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            
            Button button = closeButton.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.8f, 0.2f, 0.2f, 1f);
            colors.highlightedColor = new Color(1f, 0.3f, 0.3f, 1f);
            colors.pressedColor = new Color(0.7f, 0.1f, 0.1f, 1f);
            button.colors = colors;
            
            GameObject buttonText = new GameObject("Text");
            buttonText.transform.SetParent(closeButton.transform, false);
            
            RectTransform textRect = buttonText.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI tmp = buttonText.AddComponent<TextMeshProUGUI>();
            tmp.text = "X";
            tmp.fontSize = 32;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            
            return closeButton;
        }
        
        private static GameObject CreateSearchContainer(Transform parent)
        {
            GameObject container = new GameObject("SearchContainer");
            container.transform.SetParent(parent, false);
            
            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.sizeDelta = new Vector2(-60, 60);
            rect.anchoredPosition = new Vector2(0, -100);
            
            // Создаем поле ввода
            GameObject inputField = new GameObject("SearchInputField");
            inputField.transform.SetParent(container.transform, false);
            
            RectTransform inputRect = inputField.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0, 0);
            inputRect.anchorMax = new Vector2(0.7f, 1);
            inputRect.sizeDelta = Vector2.zero;
            
            Image inputImage = inputField.AddComponent<Image>();
            inputImage.color = new Color(1, 1, 1, 0.9f);
            
            TMP_InputField tmpInput = inputField.AddComponent<TMP_InputField>();
            
            // Текстовый компонент для поля ввода
            GameObject textArea = new GameObject("Text");
            textArea.transform.SetParent(inputField.transform, false);
            
            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = new Vector2(0, 0);
            textAreaRect.anchorMax = new Vector2(1, 1);
            textAreaRect.sizeDelta = new Vector2(-20, -10);
            textAreaRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI textComponent = textArea.AddComponent<TextMeshProUGUI>();
            textComponent.color = Color.black;
            textComponent.fontSize = 24;
            
            // Плейсхолдер
            GameObject placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(inputField.transform, false);
            
            RectTransform placeholderRect = placeholder.AddComponent<RectTransform>();
            placeholderRect.anchorMin = new Vector2(0, 0);
            placeholderRect.anchorMax = new Vector2(1, 1);
            placeholderRect.sizeDelta = new Vector2(-20, -10);
            placeholderRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
            placeholderText.text = "Введите имя пользователя...";
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            placeholderText.fontSize = 24;
            
            tmpInput.textComponent = textComponent;
            tmpInput.placeholder = placeholderText;
            
            // Создаем кнопку поиска
            GameObject searchButton = new GameObject("SearchButton");
            searchButton.transform.SetParent(container.transform, false);
            
            RectTransform buttonRect = searchButton.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.75f, 0);
            buttonRect.anchorMax = new Vector2(1, 1);
            buttonRect.sizeDelta = Vector2.zero;
            
            Image buttonImage = searchButton.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 1f, 1f);
            
            Button button = searchButton.AddComponent<Button>();
            ColorBlock buttonColors = button.colors;
            buttonColors.normalColor = new Color(0.2f, 0.6f, 1f, 1f);
            buttonColors.highlightedColor = new Color(0.3f, 0.7f, 1f, 1f);
            buttonColors.pressedColor = new Color(0.1f, 0.5f, 0.9f, 1f);
            button.colors = buttonColors;
            
            GameObject buttonText = new GameObject("Text");
            buttonText.transform.SetParent(searchButton.transform, false);
            
            RectTransform buttonTextRect = buttonText.AddComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI buttonTmp = buttonText.AddComponent<TextMeshProUGUI>();
            buttonTmp.text = "Поиск";
            buttonTmp.fontSize = 24;
            buttonTmp.color = Color.white;
            buttonTmp.alignment = TextAlignmentOptions.Center;
            
            return container;
        }
        
        private static GameObject CreateResultsContainer(Transform parent)
        {
            GameObject container = new GameObject("ResultsContainer");
            container.transform.SetParent(parent, false);
            
            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(30, 100);
            rect.offsetMax = new Vector2(-30, -170);
            
            // Создаем ScrollView
            GameObject scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(container.transform, false);
            
            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.sizeDelta = Vector2.zero;
            
            Image scrollImage = scrollView.AddComponent<Image>();
            scrollImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            
            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
            
            // Создаем Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.05f);
            
            // Добавляем маску
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            
            // Создаем Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            contentRect.pivot = new Vector2(0.5f, 1f);
            
            // Добавляем VerticalLayoutGroup
            VerticalLayoutGroup verticalLayout = content.AddComponent<VerticalLayoutGroup>();
            verticalLayout.spacing = 10;
            verticalLayout.padding = new RectOffset(10, 10, 10, 10);
            verticalLayout.childAlignment = TextAnchor.UpperCenter;
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = false;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;
            
            // Добавляем ContentSizeFitter
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Настраиваем ScrollRect
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 10;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            
            // Создаем полосу прокрутки
            GameObject scrollbar = new GameObject("Scrollbar");
            scrollbar.transform.SetParent(scrollView.transform, false);
            
            RectTransform scrollbarRect = scrollbar.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.sizeDelta = new Vector2(20, 0);
            scrollbarRect.anchoredPosition = new Vector2(-10, 0);
            
            Image scrollbarImage = scrollbar.AddComponent<Image>();
            scrollbarImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            
            Scrollbar scrollbarComponent = scrollbar.AddComponent<Scrollbar>();
            scrollbarComponent.direction = Scrollbar.Direction.BottomToTop;
            
            // Создаем ползунок
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(scrollbar.transform, false);
            
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = new Vector2(1, 0.2f);
            handleRect.sizeDelta = Vector2.zero;
            handleRect.anchoredPosition = Vector2.zero;
            
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            
            scrollbarComponent.handleRect = handleRect;
            scrollbarComponent.targetGraphic = handleImage;
            
            scroll.verticalScrollbar = scrollbarComponent;
            
            return container;
        }
        
        private static GameObject CreateNoResultsMessage(Transform parent)
        {
            GameObject message = new GameObject("NoResultsMessage");
            message.transform.SetParent(parent, false);
            
            RectTransform rect = message.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(400, 80);
            rect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI tmp = message.AddComponent<TextMeshProUGUI>();
            tmp.text = "Нет результатов";
            tmp.fontSize = 30;
            tmp.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            
            return message;
        }
        
        private static GameObject CreateLoadingIndicator(Transform parent)
        {
            GameObject indicator = new GameObject("LoadingIndicator");
            indicator.transform.SetParent(parent, false);
            
            RectTransform rect = indicator.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(200, 80);
            rect.anchoredPosition = Vector2.zero;
            
            Image image = indicator.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            GameObject text = new GameObject("Text");
            text.transform.SetParent(indicator.transform, false);
            
            RectTransform textRect = text.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI tmp = text.AddComponent<TextMeshProUGUI>();
            tmp.text = "Загрузка...";
            tmp.fontSize = 24;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            
            // По умолчанию скрываем индикатор
            indicator.SetActive(false);
            
            return indicator;
        }
        
        private static GameObject CreatePopupPanel(Transform parent)
        {
            GameObject popup = new GameObject("PopupPanel");
            popup.transform.SetParent(parent, false);
            
            RectTransform rect = popup.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(400, 200);
            rect.anchoredPosition = Vector2.zero;
            
            Image image = popup.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            
            GameObject popupText = new GameObject("PopupText");
            popupText.transform.SetParent(popup.transform, false);
            
            RectTransform textRect = popupText.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 0.7f);
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(20, 20);
            textRect.offsetMax = new Vector2(-20, 0);
            
            TextMeshProUGUI tmp = popupText.AddComponent<TextMeshProUGUI>();
            tmp.text = "Сообщение";
            tmp.fontSize = 24;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            
            // Создаем кнопку ОК
            GameObject okButton = new GameObject("OkButton");
            okButton.transform.SetParent(popup.transform, false);
            
            RectTransform okRect = okButton.AddComponent<RectTransform>();
            okRect.anchorMin = new Vector2(0.5f, 0);
            okRect.anchorMax = new Vector2(0.5f, 0);
            okRect.sizeDelta = new Vector2(150, 50);
            okRect.anchoredPosition = new Vector2(0, 30);
            
            Image okImage = okButton.AddComponent<Image>();
            okImage.color = new Color(0.3f, 0.6f, 1f, 1f);
            
            Button okButtonComponent = okButton.AddComponent<Button>();
            ColorBlock okColors = okButtonComponent.colors;
            okColors.normalColor = new Color(0.3f, 0.6f, 1f, 1f);
            okColors.highlightedColor = new Color(0.4f, 0.7f, 1f, 1f);
            okColors.pressedColor = new Color(0.2f, 0.5f, 0.9f, 1f);
            okButtonComponent.colors = okColors;
            
            // Добавляем обработчик для закрытия попапа
            okButtonComponent.onClick.AddListener(() => { popup.SetActive(false); });
            
            GameObject okText = new GameObject("Text");
            okText.transform.SetParent(okButton.transform, false);
            
            RectTransform okTextRect = okText.AddComponent<RectTransform>();
            okTextRect.anchorMin = Vector2.zero;
            okTextRect.anchorMax = Vector2.one;
            okTextRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI okTmp = okText.AddComponent<TextMeshProUGUI>();
            okTmp.text = "OK";
            okTmp.fontSize = 24;
            okTmp.color = Color.white;
            okTmp.alignment = TextAlignmentOptions.Center;
            
            // По умолчанию скрываем попап
            popup.SetActive(false);
            
            return popup;
        }
        
        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
        
        private static GameObject SavePrefab(GameObject obj, string path, string name)
        {
            string assetPath = Path.Combine(path, name + ".prefab");
            
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
            
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            GameObject prefabAsset;
            
            if (existingPrefab != null)
            {
                prefabAsset = PrefabUtility.SaveAsPrefabAsset(obj, assetPath);
                Debug.Log($"Обновлен префаб по пути: {assetPath}");
            }
            else
            {
                prefabAsset = PrefabUtility.SaveAsPrefabAsset(obj, assetPath);
                Debug.Log($"Создан новый префаб по пути: {assetPath}");
            }
            
            return prefabAsset;
        }
    }
} 