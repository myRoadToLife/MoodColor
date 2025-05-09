using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using System.IO;
using App.Develop.Scenes.PersonalAreaScene.UI;
using App.Develop.Scenes.PersonalAreaScene.Settings;

namespace App.Editor
{
    public class PanelsPrefabGenerator : EditorWindow
    {
        private static string resourceFolder = "Assets/App/Resources/UI/Panels";
        private static Color backgroundColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        private static Color titleBackgroundColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        private static Color buttonColor = new Color(0.6f, 0.8f, 1f, 1f);

        [MenuItem("Tools/Create UI Panels")]
        public static void ShowWindow()
        {
            GetWindow<PanelsPrefabGenerator>("Панели UI");
        }

        private void OnGUI()
        {
            GUILayout.Label("Генератор панелей для личного кабинета", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Создать все панели"))
            {
                CreateAllPanels();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Создать панель записи эмоций"))
            {
                CreateLogEmotionPanel();
            }
            
            if (GUILayout.Button("Создать панель истории"))
            {
                CreateHistoryPanel();
            }
            
            if (GUILayout.Button("Создать панель друзей"))
            {
                CreateFriendsPanel();
            }
            
            if (GUILayout.Button("Создать панель настроек"))
            {
                CreateSettingsPanel();
            }
            
            if (GUILayout.Button("Создать панель мастерской"))
            {
                CreateWorkshopPanel();
            }
        }

        private static void CreateAllPanels()
        {
            CreateLogEmotionPanel();
            CreateHistoryPanel();
            CreateFriendsPanel();
            CreateSettingsPanel();
            CreateWorkshopPanel();
        }

        private static void CreateLogEmotionPanel()
        {
            string panelName = "LogEmotionPanel";
            string title = "Запись эмоций";
            
            // Создаем базовую панель
            GameObject panel = CreateBasePanelPrefab(panelName, title);
            
            // Получаем контент контейнер
            Transform contentContainer = panel.transform.Find("Content");
            
            // Создаем контейнер для выбора эмоции
            GameObject emotionSelector = new GameObject("EmotionSelector");
            emotionSelector.transform.SetParent(contentContainer, false);
            RectTransform emotionSelectorRect = emotionSelector.AddComponent<RectTransform>();
            emotionSelectorRect.anchorMin = new Vector2(0.1f, 0.6f);
            emotionSelectorRect.anchorMax = new Vector2(0.9f, 0.9f);
            emotionSelectorRect.pivot = new Vector2(0.5f, 0.5f);
            emotionSelectorRect.sizeDelta = Vector2.zero;
            
            // Создаем кнопки управления
            GameObject buttonContainer = new GameObject("ButtonContainer");
            buttonContainer.transform.SetParent(contentContainer, false);
            RectTransform buttonContainerRect = buttonContainer.AddComponent<RectTransform>();
            HorizontalLayoutGroup buttonLayout = buttonContainer.AddComponent<HorizontalLayoutGroup>();
            buttonContainerRect.anchorMin = new Vector2(0.1f, 0.1f);
            buttonContainerRect.anchorMax = new Vector2(0.9f, 0.2f);
            buttonContainerRect.pivot = new Vector2(0.5f, 0.5f);
            buttonContainerRect.sizeDelta = Vector2.zero;
            buttonLayout.spacing = 20;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            
            // Создаем кнопку "Сохранить"
            GameObject saveButton = CreateButton("SaveButton", "Сохранить", buttonContainer.transform);
            
            // Создаем кнопку "Отмена"
            GameObject cancelButton = CreateButton("CancelButton", "Отмена", buttonContainer.transform);
            
            // Добавляем скрипт контроллера
            var controller = panel.AddComponent<LogEmotionPanelController>();
            
            // Настраиваем SerializeField через SerializedObject
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("_saveButton").objectReferenceValue = saveButton.GetComponent<Button>();
            serializedController.FindProperty("_cancelButton").objectReferenceValue = cancelButton.GetComponent<Button>();
            
            // Добавляем pop-up сообщение
            GameObject popupPanel = CreatePopupPanel(panel.transform);
            serializedController.FindProperty("_popupPanel").objectReferenceValue = popupPanel;
            serializedController.FindProperty("_popupText").objectReferenceValue = popupPanel.transform.Find("PopupText").GetComponent<TMP_Text>();
            serializedController.ApplyModifiedProperties();
            
            SaveAsPrefab(panel, panelName);
        }

        private static void CreateHistoryPanel()
        {
            string panelName = "HistoryPanel";
            string title = "История эмоций";
            
            // Создаем базовую панель
            GameObject panel = CreateBasePanelPrefab(panelName, title);
            
            // Получаем контент контейнер
            Transform contentContainer = panel.transform.Find("Content");
            
            // Создаем скроллируемую область для истории
            GameObject scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(contentContainer, false);
            RectTransform scrollViewRect = scrollView.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = new Vector2(0.1f, 0.2f);
            scrollViewRect.anchorMax = new Vector2(0.9f, 0.9f);
            scrollViewRect.sizeDelta = Vector2.zero;
            ScrollRect scrollRectComponent = scrollView.AddComponent<ScrollRect>();
            Image scrollViewImage = scrollView.AddComponent<Image>();
            scrollViewImage.color = new Color(1, 1, 1, 0.1f);
            
            // Создаем viewport для scroll view
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.pivot = new Vector2(0.5f, 0.5f);
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = Color.clear;
            Mask viewportMask = viewport.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            
            // Создаем контент для viewport
            GameObject historyContent = new GameObject("HistoryContent");
            historyContent.transform.SetParent(viewport.transform, false);
            RectTransform historyContentRect = historyContent.AddComponent<RectTransform>();
            historyContentRect.anchorMin = new Vector2(0, 1);
            historyContentRect.anchorMax = new Vector2(1, 1);
            historyContentRect.pivot = new Vector2(0.5f, 1);
            historyContentRect.sizeDelta = new Vector2(0, 300); // Высота контента
            VerticalLayoutGroup historyLayout = historyContent.AddComponent<VerticalLayoutGroup>();
            historyLayout.padding = new RectOffset(10, 10, 10, 10);
            historyLayout.spacing = 10;
            ContentSizeFitter contentFitter = historyContent.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Настраиваем scroll rect
            scrollRectComponent.content = historyContentRect;
            scrollRectComponent.viewport = viewportRect;
            scrollRectComponent.horizontal = false;
            scrollRectComponent.vertical = true;
            
            // Добавляем кнопку "Закрыть"
            GameObject closeButton = CreateButton("CloseButton", "Закрыть", contentContainer);
            RectTransform closeButtonRect = closeButton.GetComponent<RectTransform>();
            closeButtonRect.anchorMin = new Vector2(0.5f, 0.05f);
            closeButtonRect.anchorMax = new Vector2(0.5f, 0.15f);
            closeButtonRect.anchoredPosition = Vector2.zero;
            
            // Добавляем скрипт контроллера
            var controller = panel.AddComponent<HistoryPanelController>();
            
            // Настраиваем SerializeField через SerializedObject
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("_closeButton").objectReferenceValue = closeButton.GetComponent<Button>();
            
            // Добавляем pop-up сообщение
            GameObject popupPanel = CreatePopupPanel(panel.transform);
            serializedController.FindProperty("_popupPanel").objectReferenceValue = popupPanel;
            serializedController.FindProperty("_popupText").objectReferenceValue = popupPanel.transform.Find("PopupText").GetComponent<TMP_Text>();
            serializedController.ApplyModifiedProperties();
            
            SaveAsPrefab(panel, panelName);
        }

        private static void CreateFriendsPanel()
        {
            string panelName = "FriendsPanel";
            string title = "Друзья";
            
            // Создаем базовую панель
            GameObject panel = CreateBasePanelPrefab(panelName, title);
            
            // Получаем контент контейнер
            Transform contentContainer = panel.transform.Find("Content");
            
            // Создаем скроллируемую область для списка друзей
            GameObject scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(contentContainer, false);
            RectTransform scrollViewRect = scrollView.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = new Vector2(0.1f, 0.2f);
            scrollViewRect.anchorMax = new Vector2(0.9f, 0.9f);
            scrollViewRect.sizeDelta = Vector2.zero;
            ScrollRect scrollRectComponent = scrollView.AddComponent<ScrollRect>();
            Image scrollViewImage = scrollView.AddComponent<Image>();
            scrollViewImage.color = new Color(1, 1, 1, 0.1f);
            
            // Создаем viewport для scroll view
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.pivot = new Vector2(0.5f, 0.5f);
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = Color.clear;
            Mask viewportMask = viewport.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            
            // Создаем контент для viewport
            GameObject friendsContent = new GameObject("FriendsContent");
            friendsContent.transform.SetParent(viewport.transform, false);
            RectTransform friendsContentRect = friendsContent.AddComponent<RectTransform>();
            friendsContentRect.anchorMin = new Vector2(0, 1);
            friendsContentRect.anchorMax = new Vector2(1, 1);
            friendsContentRect.pivot = new Vector2(0.5f, 1);
            friendsContentRect.sizeDelta = new Vector2(0, 300); // Высота контента
            VerticalLayoutGroup friendsLayout = friendsContent.AddComponent<VerticalLayoutGroup>();
            friendsLayout.padding = new RectOffset(10, 10, 10, 10);
            friendsLayout.spacing = 10;
            ContentSizeFitter contentFitter = friendsContent.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Настраиваем scroll rect
            scrollRectComponent.content = friendsContentRect;
            scrollRectComponent.viewport = viewportRect;
            scrollRectComponent.horizontal = false;
            scrollRectComponent.vertical = true;
            
            // Создаем контейнер для кнопок
            GameObject buttonContainer = new GameObject("ButtonContainer");
            buttonContainer.transform.SetParent(contentContainer, false);
            RectTransform buttonContainerRect = buttonContainer.AddComponent<RectTransform>();
            HorizontalLayoutGroup buttonLayout = buttonContainer.AddComponent<HorizontalLayoutGroup>();
            buttonContainerRect.anchorMin = new Vector2(0.1f, 0.05f);
            buttonContainerRect.anchorMax = new Vector2(0.9f, 0.15f);
            buttonContainerRect.sizeDelta = Vector2.zero;
            buttonLayout.spacing = 20;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            
            // Добавляем кнопку "Добавить друга"
            GameObject addFriendButton = CreateButton("AddFriendButton", "Добавить друга", buttonContainer.transform);
            
            // Добавляем кнопку "Закрыть"
            GameObject closeButton = CreateButton("CloseButton", "Закрыть", buttonContainer.transform);
            
            // Добавляем скрипт контроллера
            var controller = panel.AddComponent<FriendsPanelController>();
            
            // Настраиваем SerializeField через SerializedObject
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("_closeButton").objectReferenceValue = closeButton.GetComponent<Button>();
            serializedController.FindProperty("_addFriendButton").objectReferenceValue = addFriendButton.GetComponent<Button>();
            
            // Добавляем pop-up сообщение
            GameObject popupPanel = CreatePopupPanel(panel.transform);
            serializedController.FindProperty("_popupPanel").objectReferenceValue = popupPanel;
            serializedController.FindProperty("_popupText").objectReferenceValue = popupPanel.transform.Find("PopupText").GetComponent<TMP_Text>();
            serializedController.ApplyModifiedProperties();
            
            SaveAsPrefab(panel, panelName);
        }

        private static void CreateSettingsPanel()
        {
            string panelName = "SettingsPanel";
            string title = "Настройки";
            
            // Создаем базовую панель
            GameObject panel = CreateBasePanelPrefab(panelName, title);
            
            // Получаем контент контейнер
            Transform contentContainer = panel.transform.Find("Content");
            
            // Создаем контейнер для настроек
            GameObject settingsContainer = new GameObject("SettingsContainer");
            settingsContainer.transform.SetParent(contentContainer, false);
            RectTransform settingsContainerRect = settingsContainer.AddComponent<RectTransform>();
            settingsContainerRect.anchorMin = new Vector2(0.1f, 0.2f);
            settingsContainerRect.anchorMax = new Vector2(0.9f, 0.9f);
            settingsContainerRect.sizeDelta = Vector2.zero;
            VerticalLayoutGroup settingsLayout = settingsContainer.AddComponent<VerticalLayoutGroup>();
            settingsLayout.padding = new RectOffset(10, 10, 10, 10);
            settingsLayout.spacing = 15;
            settingsLayout.childAlignment = TextAnchor.UpperCenter;
            
            // Создаем переключатель уведомлений
            GameObject notificationsToggle = CreateToggle("NotificationsToggle", "Уведомления", settingsContainer.transform);
            
            // Создаем переключатель звука
            GameObject soundToggle = CreateToggle("SoundToggle", "Звук", settingsContainer.transform);
            
            // Создаем переключатель музыки
            GameObject musicToggle = CreateToggle("MusicToggle", "Музыка", settingsContainer.transform);
            
            // Создаем переключатель вибрации
            GameObject vibrationToggle = CreateToggle("VibrationToggle", "Вибрация", settingsContainer.transform);
            
            // Создаем выпадающий список тем
            GameObject themeDropdown = CreateDropdown("ThemeDropdown", "Тема", new string[] { "По умолчанию", "Светлая", "Тёмная" }, settingsContainer.transform);
            
            // Создаем выпадающий список языков
            GameObject languageDropdown = CreateDropdown("LanguageDropdown", "Язык", new string[] { "Русский", "English" }, settingsContainer.transform);
            
            // Создаем контейнер для кнопок
            GameObject buttonContainer = new GameObject("ButtonContainer");
            buttonContainer.transform.SetParent(contentContainer, false);
            RectTransform buttonContainerRect = buttonContainer.AddComponent<RectTransform>();
            HorizontalLayoutGroup buttonLayout = buttonContainer.AddComponent<HorizontalLayoutGroup>();
            buttonContainerRect.anchorMin = new Vector2(0.1f, 0.05f);
            buttonContainerRect.anchorMax = new Vector2(0.9f, 0.15f);
            buttonContainerRect.sizeDelta = Vector2.zero;
            buttonLayout.spacing = 20;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            
            // Добавляем кнопку "Сохранить"
            GameObject saveButton = CreateButton("SaveButton", "Сохранить", buttonContainer.transform);
            
            // Добавляем кнопку "Сбросить"
            GameObject resetButton = CreateButton("ResetButton", "Сбросить", buttonContainer.transform);
            
            // Добавляем кнопку "Удалить аккаунт"
            GameObject deleteButton = CreateButton("DeleteAccountButton", "Удалить аккаунт", buttonContainer.transform);
            
            // Добавляем скрипт контроллера
            var controller = panel.AddComponent<SettingsPanelController>();
            
            // Настройка контроллера через SerializedObject
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("_soundToggle").objectReferenceValue = soundToggle.GetComponent<Toggle>();
            serializedController.FindProperty("_musicToggle").objectReferenceValue = musicToggle.GetComponent<Toggle>();
            serializedController.FindProperty("_vibrationToggle").objectReferenceValue = vibrationToggle.GetComponent<Toggle>();
            serializedController.FindProperty("_notificationsToggle").objectReferenceValue = notificationsToggle.GetComponent<Toggle>();
            serializedController.FindProperty("_languageDropdown").objectReferenceValue = languageDropdown.GetComponent<TMP_Dropdown>();
            serializedController.FindProperty("_themeDropdown").objectReferenceValue = themeDropdown.GetComponent<TMP_Dropdown>();
            serializedController.FindProperty("_saveButton").objectReferenceValue = saveButton.GetComponent<Button>();
            serializedController.FindProperty("_resetButton").objectReferenceValue = resetButton.GetComponent<Button>();
            serializedController.FindProperty("_deleteAccountButton").objectReferenceValue = deleteButton.GetComponent<Button>();
            
            // Добавляем pop-up сообщение
            GameObject popupPanel = CreatePopupPanel(panel.transform);
            serializedController.FindProperty("_popupPanel").objectReferenceValue = popupPanel;
            serializedController.FindProperty("_popupText").objectReferenceValue = popupPanel.transform.Find("PopupText").GetComponent<TMP_Text>();
            serializedController.ApplyModifiedProperties();
            
            SaveAsPrefab(panel, panelName);
        }

        private static void CreateWorkshopPanel()
        {
            string panelName = "WorkshopPanel";
            string title = "Мастерская";
            
            // Создаем базовую панель
            GameObject panel = CreateBasePanelPrefab(panelName, title);
            
            // Получаем контент контейнер
            Transform contentContainer = panel.transform.Find("Content");
            
            // Создаем скроллируемую область для проектов
            GameObject scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(contentContainer, false);
            RectTransform scrollViewRect = scrollView.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = new Vector2(0.1f, 0.2f);
            scrollViewRect.anchorMax = new Vector2(0.9f, 0.9f);
            scrollViewRect.sizeDelta = Vector2.zero;
            ScrollRect scrollRectComponent = scrollView.AddComponent<ScrollRect>();
            Image scrollViewImage = scrollView.AddComponent<Image>();
            scrollViewImage.color = new Color(1, 1, 1, 0.1f);
            
            // Создаем viewport для scroll view
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.pivot = new Vector2(0.5f, 0.5f);
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = Color.clear;
            Mask viewportMask = viewport.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            
            // Создаем контент для viewport
            GameObject workshopContent = new GameObject("WorkshopContent");
            workshopContent.transform.SetParent(viewport.transform, false);
            RectTransform workshopContentRect = workshopContent.AddComponent<RectTransform>();
            workshopContentRect.anchorMin = new Vector2(0, 1);
            workshopContentRect.anchorMax = new Vector2(1, 1);
            workshopContentRect.pivot = new Vector2(0.5f, 1);
            workshopContentRect.sizeDelta = new Vector2(0, 300); // Высота контента
            GridLayoutGroup workshopLayout = workshopContent.AddComponent<GridLayoutGroup>();
            workshopLayout.padding = new RectOffset(10, 10, 10, 10);
            workshopLayout.spacing = new Vector2(20, 20);
            workshopLayout.cellSize = new Vector2(150, 150);
            ContentSizeFitter contentFitter = workshopContent.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Настраиваем scroll rect
            scrollRectComponent.content = workshopContentRect;
            scrollRectComponent.viewport = viewportRect;
            scrollRectComponent.horizontal = false;
            scrollRectComponent.vertical = true;
            
            // Создаем контейнер для кнопок
            GameObject buttonContainer = new GameObject("ButtonContainer");
            buttonContainer.transform.SetParent(contentContainer, false);
            RectTransform buttonContainerRect = buttonContainer.AddComponent<RectTransform>();
            HorizontalLayoutGroup buttonLayout = buttonContainer.AddComponent<HorizontalLayoutGroup>();
            buttonContainerRect.anchorMin = new Vector2(0.1f, 0.05f);
            buttonContainerRect.anchorMax = new Vector2(0.9f, 0.15f);
            buttonContainerRect.sizeDelta = Vector2.zero;
            buttonLayout.spacing = 20;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            
            // Добавляем кнопку "Новый проект"
            GameObject newProjectButton = CreateButton("NewProjectButton", "Новый проект", buttonContainer.transform);
            
            // Добавляем кнопку "Закрыть"
            GameObject closeButton = CreateButton("CloseButton", "Закрыть", buttonContainer.transform);
            
            // Добавляем скрипт контроллера
            var controller = panel.AddComponent<WorkshopPanelController>();
            
            // Настраиваем SerializeField через SerializedObject
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("_closeButton").objectReferenceValue = closeButton.GetComponent<Button>();
            
            // Добавляем pop-up сообщение
            GameObject popupPanel = CreatePopupPanel(panel.transform);
            serializedController.FindProperty("_popupPanel").objectReferenceValue = popupPanel;
            serializedController.FindProperty("_popupText").objectReferenceValue = popupPanel.transform.Find("PopupText").GetComponent<TMP_Text>();
            serializedController.ApplyModifiedProperties();
            
            SaveAsPrefab(panel, panelName);
        }

        private static GameObject CreateBasePanelPrefab(string name, string titleText)
        {
            // Создаем корневой объект панели
            GameObject panel = new GameObject(name);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panel.AddComponent<CanvasGroup>();
            
            // Добавляем Canvas для корректного отображения поверх других UI элементов
            Canvas canvas = panel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;  // Устанавливаем более высокий порядок сортировки
            panel.AddComponent<CanvasScaler>();
            panel.AddComponent<GraphicRaycaster>();
            
            // Настраиваем RectTransform
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = Vector2.zero;
            
            // Добавляем задний фон
            GameObject background = new GameObject("Background");
            background.transform.SetParent(panel.transform, false);
            RectTransform backgroundRect = background.AddComponent<RectTransform>();
            Image backgroundImage = background.AddComponent<Image>();
            
            // Настраиваем фон
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            backgroundRect.sizeDelta = Vector2.zero;
            backgroundImage.color = backgroundColor;
            
            // Создаем контейнер для заголовка
            GameObject titleContainer = new GameObject("TitleContainer");
            titleContainer.transform.SetParent(panel.transform, false);
            RectTransform titleContainerRect = titleContainer.AddComponent<RectTransform>();
            Image titleContainerImage = titleContainer.AddComponent<Image>();
            
            // Настраиваем контейнер заголовка
            titleContainerRect.anchorMin = new Vector2(0, 1);
            titleContainerRect.anchorMax = new Vector2(1, 1);
            titleContainerRect.pivot = new Vector2(0.5f, 1f);
            titleContainerRect.sizeDelta = new Vector2(0, 50);
            titleContainerImage.color = titleBackgroundColor;
            
            // Создаем текст заголовка
            GameObject titleObject = new GameObject("Title");
            titleObject.transform.SetParent(titleContainer.transform, false);
            RectTransform titleRect = titleObject.AddComponent<RectTransform>();
            TextMeshProUGUI titleTextComponent = titleObject.AddComponent<TextMeshProUGUI>();
            
            // Настраиваем текст заголовка
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = Vector2.zero;
            titleTextComponent.text = titleText;
            titleTextComponent.fontSize = 24;
            titleTextComponent.alignment = TextAlignmentOptions.Center;
            titleTextComponent.color = Color.black;
            
            // Создаем контейнер для контента
            GameObject content = new GameObject("Content");
            content.transform.SetParent(panel.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            
            // Настраиваем контейнер контента
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(0, -50);
            contentRect.anchoredPosition = new Vector2(0, -25);
            
            return panel;
        }
        
        private static GameObject CreateButton(string name, string text, Transform parent)
        {
            GameObject button = new GameObject(name);
            button.transform.SetParent(parent, false);
            
            RectTransform buttonRect = button.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(120, 40);
            
            Image buttonImage = button.AddComponent<Image>();
            buttonImage.color = buttonColor;
            
            Button buttonComponent = button.AddComponent<Button>();
            ColorBlock colors = buttonComponent.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = new Color(buttonColor.r * 1.1f, buttonColor.g * 1.1f, buttonColor.b * 1.1f, 1f);
            colors.pressedColor = new Color(buttonColor.r * 0.9f, buttonColor.g * 0.9f, buttonColor.b * 0.9f, 1f);
            buttonComponent.colors = colors;
            
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(button.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 18;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.color = Color.black;
            
            return button;
        }
        
        private static GameObject CreatePopupPanel(Transform parent)
        {
            GameObject popupPanel = new GameObject("PopupPanel");
            popupPanel.transform.SetParent(parent, false);
            
            RectTransform popupRect = popupPanel.AddComponent<RectTransform>();
            popupRect.anchorMin = new Vector2(0.3f, 0.4f);
            popupRect.anchorMax = new Vector2(0.7f, 0.6f);
            popupRect.pivot = new Vector2(0.5f, 0.5f);
            popupRect.sizeDelta = Vector2.zero;
            
            Image popupImage = popupPanel.AddComponent<Image>();
            popupImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            GameObject textObj = new GameObject("PopupText");
            textObj.transform.SetParent(popupPanel.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(-20, -20);
            
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = "Сообщение";
            textComponent.fontSize = 18;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.color = Color.white;
            
            popupPanel.SetActive(false);
            
            return popupPanel;
        }

        private static void SaveAsPrefab(GameObject panel, string prefabName)
        {
            // Проверяем существование директории
            if (!Directory.Exists(resourceFolder))
            {
                Directory.CreateDirectory(resourceFolder);
            }
            
            // Путь для сохранения префаба
            string prefabPath = $"{resourceFolder}/{prefabName}.prefab";
            
            // Проверяем, существует ли уже префаб
            bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
            
            // Создаем или обновляем префаб
            GameObject prefab;
            if (prefabExists)
            {
                prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(panel, prefabPath, InteractionMode.UserAction);
                Debug.Log($"Префаб {prefabName} обновлен в {prefabPath}");
            }
            else
            {
                prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(panel, prefabPath, InteractionMode.UserAction);
                Debug.Log($"Префаб {prefabName} создан в {prefabPath}");
            }
            
            // Удаляем временный объект со сцены
            Object.DestroyImmediate(panel);
        }

        private static GameObject CreateToggle(string name, string label, Transform parent)
        {
            GameObject toggleObj = new GameObject(name);
            toggleObj.transform.SetParent(parent, false);
            
            RectTransform toggleRect = toggleObj.AddComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(0, 40);
            
            HorizontalLayoutGroup toggleLayout = toggleObj.AddComponent<HorizontalLayoutGroup>();
            toggleLayout.childAlignment = TextAnchor.MiddleLeft;
            toggleLayout.spacing = 10;
            
            // Создаем лейбл
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(toggleObj.transform, false);
            
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(100, 40);
            
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 18;
            labelText.alignment = TextAlignmentOptions.Left;
            labelText.color = Color.black;
            
            // Создаем toggle компонент
            GameObject toggleComponent = new GameObject("Toggle");
            toggleComponent.transform.SetParent(toggleObj.transform, false);
            
            RectTransform toggleCompRect = toggleComponent.AddComponent<RectTransform>();
            toggleCompRect.sizeDelta = new Vector2(40, 40);
            
            Image toggleImage = toggleComponent.AddComponent<Image>();
            toggleImage.color = Color.white;
            
            Toggle toggle = toggleComponent.AddComponent<Toggle>();
            toggle.isOn = true;
            
            // Создаем background для toggle
            GameObject background = new GameObject("Background");
            background.transform.SetParent(toggleComponent.transform, false);
            
            RectTransform backgroundRect = background.AddComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            backgroundRect.sizeDelta = Vector2.zero;
            
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = Color.white;
            
            // Создаем checkmark для toggle
            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(toggleComponent.transform, false);
            
            RectTransform checkmarkRect = checkmark.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0.1f, 0.1f);
            checkmarkRect.anchorMax = new Vector2(0.9f, 0.9f);
            checkmarkRect.pivot = new Vector2(0.5f, 0.5f);
            checkmarkRect.sizeDelta = Vector2.zero;
            
            Image checkmarkImage = checkmark.AddComponent<Image>();
            checkmarkImage.color = new Color(0.2f, 0.6f, 0.9f, 1);
            
            // Настраиваем toggle компонент
            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkmarkImage;
            
            return toggleObj;
        }
        
        private static GameObject CreateDropdown(string name, string label, string[] options, Transform parent)
        {
            GameObject dropdownObj = new GameObject(name);
            dropdownObj.transform.SetParent(parent, false);
            
            RectTransform dropdownRect = dropdownObj.AddComponent<RectTransform>();
            dropdownRect.sizeDelta = new Vector2(0, 50);
            
            HorizontalLayoutGroup dropdownLayout = dropdownObj.AddComponent<HorizontalLayoutGroup>();
            dropdownLayout.childAlignment = TextAnchor.MiddleLeft;
            dropdownLayout.spacing = 10;
            
            // Создаем лейбл
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(dropdownObj.transform, false);
            
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(100, 40);
            
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 18;
            labelText.alignment = TextAlignmentOptions.Left;
            labelText.color = Color.black;
            
            // Создаем dropdown компонент
            GameObject dropdownComponent = new GameObject("Dropdown");
            dropdownComponent.transform.SetParent(dropdownObj.transform, false);
            
            RectTransform dropdownCompRect = dropdownComponent.AddComponent<RectTransform>();
            dropdownCompRect.sizeDelta = new Vector2(200, 40);
            
            Image dropdownImage = dropdownComponent.AddComponent<Image>();
            dropdownImage.color = Color.white;
            
            TMP_Dropdown dropdown = dropdownComponent.AddComponent<TMP_Dropdown>();
            
            // Добавляем опции в dropdown
            dropdown.options.Clear();
            foreach (string option in options)
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData(option));
            }
            
            return dropdownObj;
        }
    }
} 