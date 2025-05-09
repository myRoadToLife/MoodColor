using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using System.IO;
using App.Develop.Scenes.PersonalAreaScene.UI; // Добавлено для HistoryPanelController
using App.Editor.Generators.UI.Core; // Для UIComponentGenerator

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
                Debug.LogWarning($"[HistoryPanelGenerator] Текстура WoodenPlank.png не найдена в {TexturesFolder}");

            if (_brushyFont == null)
            {
                _brushyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Path.Combine(FontsFolder, "BrushyFont.asset"));
                if (_brushyFont == null) 
                    _brushyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Path.Combine(FontsFolder, "BrushyFont.ttf"));
                if (_brushyFont == null) 
                    Debug.LogWarning($"[HistoryPanelGenerator] TMP_FontAsset BrushyFont (.asset или .ttf) не найден в {FontsFolder}.");
            }
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
            // Например, ScrollView для списка записей
            GameObject placeholderContent = new GameObject("PlaceholderHistoryContent");
            placeholderContent.transform.SetParent(contentContainer, false);
            RectTransform placeholderRect = placeholderContent.AddComponent<RectTransform>();
            placeholderRect.anchorMin = new Vector2(0.1f, 0.1f); 
            placeholderRect.anchorMax = new Vector2(0.9f, 0.9f);
            placeholderRect.pivot = new Vector2(0.5f, 0.5f); 
            placeholderRect.sizeDelta = Vector2.zero;
            // Добавьте сюда TextMeshPro с сообщением "История записей будет здесь"
            TextMeshProUGUI placeholderText = placeholderContent.AddComponent<TextMeshProUGUI>();
            placeholderText.text = "Содержимое Истории записей будет здесь.";
            placeholderText.alignment = TextAlignmentOptions.Center;
            if(_brushyFont != null) placeholderText.font = _brushyFont;
            placeholderText.fontSize = 20f;
            placeholderText.color = _titleTextColor;

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
            
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            UIComponentGenerator.SavePrefab(panelRoot, PrefabSaveFolderPath, panelName);
            
            if (!Application.isPlaying)
            {
                 GameObject.DestroyImmediate(panelRoot);
            }

            Debug.Log($"[HistoryPanelGenerator] Префаб {panelName} создан в {Path.Combine(PrefabSaveFolderPath, panelName + ".prefab")}");
        }
    }
} 