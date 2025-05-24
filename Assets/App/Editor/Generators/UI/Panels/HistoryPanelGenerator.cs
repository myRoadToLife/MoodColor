using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using System.IO;
using App.Develop.Scenes.PersonalAreaScene.UI; // Добавлено для HistoryPanelController
using App.Editor.Generators.UI.Core; // Для UIComponentGenerator
using App.Develop.Scenes.PersonalAreaScene.Panels.HistoryPanel; // <--- ДОБАВЛЯЕМ ЭТО
using App.Develop.Utils.Logging;

namespace App.Editor.Generators.UI.Panels
{
    public static class HistoryPanelGenerator
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

        // Стили для элемента истории
        private static Color _itemBackgroundColor = new Color(0.9f, 0.88f, 0.85f, 1f); // Светло-бежевый
        private static Color _itemSeparatorColor = new Color(0.7f, 0.65f, 0.6f, 1f);
        private static Vector2 _emotionIconSize = new Vector2(40, 40);
        private static Color _itemTextColor = new Color(0.25f, 0.15f, 0.1f, 1f);
        private static float _itemDateFontSize = 18f;
        private static float _itemTimeFontSize = 16f;
        private static float _historyItemHeight = 60f;
        private const string HistoryItemPrefabName = "HistoryItemEntry";

        // Стили для кнопок и попапа (адаптировано из LogEmotionPanelGenerator)
        private static Color _buttonTextColor = new Color(0.2f, 0.1f, 0.05f, 1f);

        private static float _buttonFontSize = 20f;

        // private static Vector2 _buttonSize = new Vector2(180, 50); // Не используется напрямую для этой панели, но оставим для консистентности
        private static Vector2 _closeButtonSize = new Vector2(60, 60); // Размер для кнопки "X"
        private static Vector3 _buttonPressedScale = new Vector3(0.95f, 0.95f, 1f);
        private static Color _buttonSpriteTintColor = Color.white;

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
                MyLogger.EditorLogWarning($"[HistoryPanelGenerator] Текстура WoodenPlank.png не найдена в {TexturesFolder}");

            if (_brushyFont == null)
            {
                _brushyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Path.Combine(FontsFolder, "BrushyFont.asset"));

                if (_brushyFont == null)
                    _brushyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Path.Combine(FontsFolder, "BrushyFont.ttf"));

                if (_brushyFont == null)
                    MyLogger.EditorLogWarning($"[HistoryPanelGenerator] TMP_FontAsset BrushyFont (.asset или .ttf) не найден в {FontsFolder}.");
            }
        }

        private static GameObject CreateHistoryItemEntryPrefab(Transform parentForPrefabCreation)
        {
            GameObject itemRoot = new GameObject(HistoryItemPrefabName);
            itemRoot.transform.SetParent(parentForPrefabCreation); // Временно для создания, потом будет префаб
            RectTransform itemRect = itemRoot.AddComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, _historyItemHeight); // Ширина будет по родителю (VLG), высота фиксированная

            Image itemBg = itemRoot.AddComponent<Image>();
            itemBg.color = _itemBackgroundColor;

            if (_woodenPlankSprite != null) // Можно использовать другую текстуру или просто цвет
            {
                itemBg.sprite = _woodenPlankSprite; // Например, более светлый вариант доски
                itemBg.type = Image.Type.Sliced;
            }


            HorizontalLayoutGroup hlg = itemRoot.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 5, 5);
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlHeight = true;
            hlg.childControlWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childForceExpandWidth = false;

            LayoutElement itemLayout = itemRoot.AddComponent<LayoutElement>();
            itemLayout.minHeight = _historyItemHeight;
            itemLayout.preferredHeight = _historyItemHeight;
            itemLayout.flexibleWidth = 1; // Чтобы занимал всю ширину ScrollView

            // 1. Иконка/Цвет Эмоции
            GameObject emotionIconGO = new GameObject("EmotionIndicator");
            emotionIconGO.transform.SetParent(itemRoot.transform, false);
            Image emotionImage = emotionIconGO.AddComponent<Image>();
            // emotionImage.color = Color.clear; // Будет установлено в рантайме
            RectTransform iconRect = emotionIconGO.GetComponent<RectTransform>();
            iconRect.sizeDelta = _emotionIconSize;

            LayoutElement iconLayout = emotionIconGO.AddComponent<LayoutElement>();
            iconLayout.minWidth = _emotionIconSize.x;
            iconLayout.minHeight = _emotionIconSize.y;
            iconLayout.preferredWidth = _emotionIconSize.x;
            iconLayout.preferredHeight = _emotionIconSize.y;

            // 2. Контейнер для текста (Дата и Время)
            GameObject textContainer = new GameObject("TextContainer");
            textContainer.transform.SetParent(itemRoot.transform, false);
            /*RectTransform textContainerRect =*/
            textContainer.AddComponent<RectTransform>();
            VerticalLayoutGroup vlgTexts = textContainer.AddComponent<VerticalLayoutGroup>();
            vlgTexts.spacing = 2;
            vlgTexts.childAlignment = TextAnchor.UpperLeft;
            vlgTexts.childControlHeight = true;
            vlgTexts.childControlWidth = true;
            vlgTexts.childForceExpandHeight = false;
            vlgTexts.childForceExpandWidth = false;
            LayoutElement textContainerLayout = textContainer.AddComponent<LayoutElement>();
            textContainerLayout.flexibleWidth = 1; // Занимает оставшееся место

            // 2.1. Текст Даты
            GameObject dateTextGO = new GameObject("DateText");
            dateTextGO.transform.SetParent(textContainer.transform, false);
            TextMeshProUGUI dateText = dateTextGO.AddComponent<TextMeshProUGUI>();
            dateText.text = "ДД.ММ.ГГГГ";
            dateText.font = _brushyFont;
            dateText.color = _itemTextColor;
            dateText.fontSize = _itemDateFontSize;
            dateText.alignment = TextAlignmentOptions.Left; // Изменено на Left, если надо

            LayoutElement dateLayout = dateTextGO.AddComponent<LayoutElement>();
            dateLayout.preferredHeight = _itemDateFontSize * 1.2f; // Немного запаса

            // 2.2. Текст Времени
            GameObject timeTextGO = new GameObject("TimeText");
            timeTextGO.transform.SetParent(textContainer.transform, false);
            TextMeshProUGUI timeText = timeTextGO.AddComponent<TextMeshProUGUI>();
            timeText.text = "ЧЧ:ММ:СС";
            timeText.font = _brushyFont;
            timeText.color = _itemTextColor;
            timeText.fontSize = _itemTimeFontSize;
            timeText.alignment = TextAlignmentOptions.Left; // Изменено на Left, если надо

            LayoutElement timeLayout = timeTextGO.AddComponent<LayoutElement>();
            timeLayout.preferredHeight = _itemTimeFontSize * 1.2f;

            // 2.3. Текст Названия Эмоции (Опционально, но полезно)
            GameObject emotionNameTextGO = new GameObject("EmotionNameText");
            emotionNameTextGO.transform.SetParent(textContainer.transform, false);
            TextMeshProUGUI emotionNameText = emotionNameTextGO.AddComponent<TextMeshProUGUI>();
            emotionNameText.text = "Название Эмоции";
            emotionNameText.font = _brushyFont;
            emotionNameText.color = _itemTextColor;
            emotionNameText.fontSize = _itemTimeFontSize; // Можно такой же или чуть меньше даты
            emotionNameText.alignment = TextAlignmentOptions.Left;
            LayoutElement emotionNameLayout = emotionNameTextGO.AddComponent<LayoutElement>();
            emotionNameLayout.preferredHeight = _itemTimeFontSize * 1.2f;

            // Добавим разделитель внизу, если нужно (опционально)
            GameObject separator = new GameObject("Separator");
            separator.transform.SetParent(itemRoot.transform, false); // Ставим в конец, но HLG его не учитывает для размера
            RectTransform sepRect = separator.AddComponent<RectTransform>();
            Image sepImg = separator.AddComponent<Image>();
            sepImg.color = _itemSeparatorColor;

            // Расположим разделитель внизу элемента
            sepRect.anchorMin = new Vector2(0, 0);
            sepRect.anchorMax = new Vector2(1, 0);
            sepRect.pivot = new Vector2(0.5f, 0);
            sepRect.sizeDelta = new Vector2(0, 1); // Ширина по родителю, высота 1 пиксель
            sepRect.anchoredPosition = Vector2.zero;

            // Добавляем и настраиваем HistoryItemView
            HistoryItemView itemView = itemRoot.AddComponent<HistoryItemView>();

            if (itemView != null)
            {
                SerializedObject serializedItemView = new SerializedObject(itemView);

                Image indicator = emotionIconGO.GetComponent<Image>();

                if (indicator != null)
                    serializedItemView.FindProperty("_emotionIndicator").objectReferenceValue = indicator;
                else
                    MyLogger.EditorLogWarning($"[HistoryPanelGenerator] Не удалось найти Image на {emotionIconGO.name} для HistoryItemView._emotionIndicator");

                TextMeshProUGUI dateTMP = dateTextGO.GetComponent<TextMeshProUGUI>();

                if (dateTMP != null)
                    serializedItemView.FindProperty("_dateText").objectReferenceValue = dateTMP;
                else
                    MyLogger.EditorLogWarning($"[HistoryPanelGenerator] Не удалось найти TextMeshProUGUI на {dateTextGO.name} для HistoryItemView._dateText");

                TextMeshProUGUI timeTMP = timeTextGO.GetComponent<TextMeshProUGUI>();

                if (timeTMP != null)
                    serializedItemView.FindProperty("_timeText").objectReferenceValue = timeTMP;
                else
                    MyLogger.EditorLogWarning($"[HistoryPanelGenerator] Не удалось найти TextMeshProUGUI на {timeTextGO.name} для HistoryItemView._timeText");

                TextMeshProUGUI emotionNameTMP = emotionNameTextGO.GetComponent<TextMeshProUGUI>();

                if (emotionNameTMP != null)
                    serializedItemView.FindProperty("_emotionNameText").objectReferenceValue = emotionNameTMP;
                else
                    MyLogger.EditorLogWarning(
                        $"[HistoryPanelGenerator] Не удалось найти TextMeshProUGUI на {emotionNameTextGO.name} для HistoryItemView._emotionNameText");

                serializedItemView.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                MyLogger.EditorLogError($"[HistoryPanelGenerator] Не удалось добавить компонент HistoryItemView к {itemRoot.name}. " +
                               $"Убедитесь, что скрипт HistoryItemView.cs существует в проекте и не содержит ошибок компиляции.");
            }

            // Сохраняем как префаб
            string itemPrefabPath = Path.Combine(PrefabSaveFolderPath, "Components"); // Сохраняем в подпапку Components

            if (!Directory.Exists(itemPrefabPath))
            {
                Directory.CreateDirectory(itemPrefabPath);
            }

            UIComponentGenerator.SavePrefab(itemRoot, itemPrefabPath, HistoryItemPrefabName);

            if (parentForPrefabCreation.gameObject.scene.IsValid() && !Application.isPlaying) // Уничтожаем, если создавали в сцене для генерации префаба
            {
                GameObject.DestroyImmediate(itemRoot);
            }

            // return itemRoot; // Не возвращаем, т.к. он может быть уничтожен
            return AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(itemPrefabPath, HistoryItemPrefabName + ".prefab"));
        }

        [MenuItem("MoodColor/Generate/UI Panels/Personal Area/History Panel")]
        public static void CreateHistoryPanelPrefab()
        {
            LoadResources();

            string panelName = "HistoryPanel";
            string title = "История записей";

            GameObject panelRoot = UIComponentGenerator.CreateBasePanelRoot(panelName, RenderMode.ScreenSpaceOverlay, 10, new Vector2(1080, 1920));

            Transform contentContainer = UIComponentGenerator.CreateBasePanelVisuals(
                panelRoot, title, _brushyFont, _titleTextColor, _titleFontSize,
                _panelBackgroundColor, _titleContainerColor,
                null, Image.Type.Simple,
                null, Image.Type.Simple
            ).transform;

            // TODO: Создать специфичный контент для HistoryPanel
            // Убираем старый Placeholder
            Transform placeholder = contentContainer.Find("PlaceholderHistoryContent");

            if (placeholder != null)
            {
                GameObject.DestroyImmediate(placeholder.gameObject);
            }

            // Создаем ScrollView вручную
            GameObject scrollViewGO = new GameObject("HistoryScrollView");
            scrollViewGO.transform.SetParent(contentContainer, false);
            Image svImage = scrollViewGO.AddComponent<Image>(); // Фон для ScrollView
            // svImage.color = new Color(0.8f,0.8f,0.8f,1); // Пример цвета фона
            svImage.sprite = _woodenPlankSprite; // Используем доску для фона
            svImage.type = Image.Type.Sliced;


            ScrollRect scrollRect = scrollViewGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.scrollSensitivity = 20f;

            RectTransform scrollViewRect = scrollViewGO.GetComponent<RectTransform>();
            scrollViewRect.anchorMin = new Vector2(0.05f, 0.05f);
            scrollViewRect.anchorMax = new Vector2(0.95f, 0.9f); // Оставляем место под заголовок
            scrollViewRect.pivot = new Vector2(0.5f, 1f); // Верхний центр
            scrollViewRect.anchoredPosition = new Vector2(0, -60); // Сдвигаем вниз от заголовка
            scrollViewRect.sizeDelta = Vector2.zero; // Растягиваем по анкерам

            // Viewport
            GameObject viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollViewGO.transform, false);
            viewportGO.AddComponent<Image>(); // Маска требует Image
            viewportGO.AddComponent<Mask>().showMaskGraphic = false;
            RectTransform viewportRect = viewportGO.GetComponent<RectTransform>();
            viewportRect.pivot = new Vector2(0, 1); // UpperLeft
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero; // Растягивается по ScrollView

            // Content
            GameObject contentForScrollGO = new GameObject("Content");
            contentForScrollGO.transform.SetParent(viewportGO.transform, false);
            RectTransform contentForScrollRect = contentForScrollGO.AddComponent<RectTransform>();
            contentForScrollRect.pivot = new Vector2(0.5f, 1); // UpperCenter
            contentForScrollRect.anchorMin = new Vector2(0, 1); // Верхний край
            contentForScrollRect.anchorMax = new Vector2(1, 1); // Верхний край, растягивается по ширине
            contentForScrollRect.sizeDelta = new Vector2(0, 0); // Начальная высота 0, будет управляться ContentSizeFitter

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentForScrollRect;

            Transform contentForScroll = contentForScrollGO.transform;

            // Настраиваем контейнер для элементов истории (это Content у ScrollView)
            VerticalLayoutGroup historyItemsContainerVLG = contentForScroll.gameObject.AddComponent<VerticalLayoutGroup>();
            historyItemsContainerVLG.padding = new RectOffset(5, 5, 5, 5);
            historyItemsContainerVLG.spacing = 5;
            historyItemsContainerVLG.childAlignment = TextAnchor.UpperCenter;
            historyItemsContainerVLG.childControlWidth = true; // Элементы будут растягиваться по ширине
            historyItemsContainerVLG.childControlHeight = false; // Высота элементов будет браться из LayoutElement
            historyItemsContainerVLG.childForceExpandWidth = true;
            historyItemsContainerVLG.childForceExpandHeight = false;

            ContentSizeFitter contentSizeFitter = contentForScroll.gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Создаем префаб элемента истории, если его нет
            string itemPrefabFullPath = Path.Combine(PrefabSaveFolderPath, "Components", HistoryItemPrefabName + ".prefab");
            GameObject loadedItemPrefab = null;

            if (!File.Exists(itemPrefabFullPath))
            {
                loadedItemPrefab = CreateHistoryItemEntryPrefab(panelRoot.transform);
            }
            else
            {
                loadedItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(itemPrefabFullPath);
            }

            // Создаем кнопку "Закрыть" (X)
            GameObject closeButton = UIComponentGenerator.CreateStyledButton(
                "CloseButton", "X", panelRoot.transform,
                _brushyFont, _buttonTextColor, _buttonFontSize + 4, // Чуть больше для "X"
                _woodenPlankSprite, Image.Type.Sliced, _buttonSpriteTintColor,
                GetDefaultButtonColors(), _closeButtonSize, _buttonPressedScale
            );

            RectTransform closeButtonRect = closeButton.GetComponent<RectTransform>();
            closeButtonRect.anchorMin = new Vector2(1, 1);
            closeButtonRect.anchorMax = new Vector2(1, 1);
            closeButtonRect.pivot = new Vector2(1, 1);
            closeButtonRect.anchoredPosition = new Vector2(-15, -15);

            // Создаем Popup Panel
            GameObject popupPanel = UIComponentGenerator.CreatePopupPanel(
                panelRoot.transform,
                "Сообщение по умолчанию",
                _brushyFont, _popupTextColor, _popupFontSize,
                _popupBgColor
            );

            if (popupPanel) popupPanel.SetActive(false); // Скрываем по умолчанию

            // Добавляем и настраиваем контроллер
            HistoryPanelController controller = panelRoot.AddComponent<HistoryPanelController>();
            SerializedObject serializedController = new SerializedObject(controller);

            Button closeBtnComp = null;
            if (closeButton) closeButton.TryGetComponent(out closeBtnComp);
            serializedController.FindProperty("_closeButton").objectReferenceValue = closeBtnComp;

            serializedController.FindProperty("_popupPanel").objectReferenceValue = popupPanel;
            TMP_Text popupTextComp = null;

            if (popupPanel)
            {
                Transform popupTextTransform = popupPanel.transform.Find("PopupText");
                if (popupTextTransform) popupTextTransform.TryGetComponent(out popupTextComp);
            }

            serializedController.FindProperty("_popupText").objectReferenceValue = popupTextComp;

            // Привязываем контейнер для элементов истории к HistoryPanelController
            serializedController.FindProperty("_historyItemsContainer").objectReferenceValue = contentForScroll;

            // Попытка загрузить префаб элемента истории и присвоить его
            // Это необязательно, можно назначить вручную в инспекторе после генерации
            if (loadedItemPrefab != null)
            {
                serializedController.FindProperty("_historyItemPrefab").objectReferenceValue = loadedItemPrefab;
            }
            else
            {
                MyLogger.EditorLogWarning($"[HistoryPanelGenerator] Префаб {HistoryItemPrefabName} не найден или не создан по пути {itemPrefabFullPath}. " +
                                 $"Назначьте его вручную в HistoryPanelController после генерации.");
            }

            serializedController.ApplyModifiedPropertiesWithoutUndo();

            UIComponentGenerator.SavePrefab(panelRoot, PrefabSaveFolderPath, panelName);

            if (!Application.isPlaying)
            {
                GameObject.DestroyImmediate(panelRoot);
            }

            MyLogger.EditorLog($"[HistoryPanelGenerator] Префаб {panelName} создан в {Path.Combine(PrefabSaveFolderPath, panelName + ".prefab")}");
        }
    }
}
