using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using System.IO;
using App.Develop.Scenes.PersonalAreaScene.UI; // Для LogEmotionPanelController
using App.Editor.Generators.UI.Core; // Для UIComponentGenerator
using App.Develop.Utils.Logging;

namespace App.Editor.Generators.UI.Panels
{
    public static class LogEmotionPanelGenerator
    {
        // Пути к ресурсам (могут быть вынесены в общий конфигурационный класс для генераторов)
        private const string TexturesFolder = "Assets/App/Resources/UI/Textures/";
        private const string FontsFolder = "Assets/App/Resources/UI/Fonts/";
        private const string PrefabSaveFolderPath = "Assets/App/Prefabs/Generated/UI/Panels/PersonalArea/";

        // Стили (можно также вынести или сделать более гибкими)
        private static Sprite _woodenPlankSprite;
        private static TMP_FontAsset _brushyFont;
        
        private static Color _panelBackgroundColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        private static Color _titleContainerColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        private static Color _titleTextColor = new Color(0.2f, 0.1f, 0.05f, 1f); // Темно-коричневый
        private static float _titleFontSize = 24f;

        private static Color _buttonTextColor = new Color(0.2f, 0.1f, 0.05f, 1f); // Темно-коричневый
        private static float _buttonFontSize = 20f;
        private static Vector2 _buttonSize = new Vector2(180, 50);
        private static Vector3 _buttonPressedScale = new Vector3(0.95f, 0.95f, 1f);
        private static Color _buttonSpriteTintColor = Color.white;

        private static Color _popupBgColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        private static Color _popupTextColor = Color.white;
        private static float _popupFontSize = 18f;

        private static void LoadResources()
        {
            if (_woodenPlankSprite == null)
                _woodenPlankSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(TexturesFolder, "WoodenPlank.png"));
            if (_woodenPlankSprite == null) 
                MyLogger.EditorLogWarning($"[LogEmotionPanelGenerator] Текстура WoodenPlank.png не найдена в {TexturesFolder}");

            if (_brushyFont == null)
            {
                _brushyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Path.Combine(FontsFolder, "BrushyFont.asset"));
                if (_brushyFont == null) 
                    _brushyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Path.Combine(FontsFolder, "BrushyFont.ttf"));
                if (_brushyFont == null) 
                    MyLogger.EditorLogWarning($"[LogEmotionPanelGenerator] TMP_FontAsset BrushyFont (.asset или .ttf) не найден в {FontsFolder}.");
            }
        }

        private static ColorBlock GetDefaultButtonColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white; // Используется если нет спрайта или для tint 
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f); 
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);   
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            colors.fadeDuration = 0.1f;
            return colors;
        }

        [MenuItem("MoodColor/Generate/UI Panels/Personal Area/Log Emotion Panel")]
        public static void CreateLogEmotionPanelPrefab()
        {
            LoadResources();

            string panelName = "LogEmotionPanel";
            string title = "Запись эмоций";

            // 1. Создаем корневой объект панели
            GameObject panelRoot = UIComponentGenerator.CreateBasePanelRoot(panelName, RenderMode.ScreenSpaceOverlay, 10, new Vector2(1080, 1920));

            // 2. Создаем визуал базовой панели (фон, заголовок, контейнер контента)
            // Передаем null для спрайтов фона панели и заголовка, если они просто цветные
            Transform contentContainer = UIComponentGenerator.CreateBasePanelVisuals(
                panelRoot, title, _brushyFont, _titleTextColor, _titleFontSize,
                _panelBackgroundColor, _titleContainerColor, 
                null, Image.Type.Simple, // panelBackgroundSprite
                null, Image.Type.Simple  // titleContainerSprite
            ).transform;

            // 3. Создаем специфичный контент для LogEmotionPanel
            // EmotionSelector (placeholder)
            GameObject emotionSelector = new GameObject("EmotionSelector");
            emotionSelector.transform.SetParent(contentContainer, false);
            RectTransform emotionSelectorRect = emotionSelector.AddComponent<RectTransform>();
            emotionSelectorRect.anchorMin = new Vector2(0.1f, 0.6f); 
            emotionSelectorRect.anchorMax = new Vector2(0.9f, 0.9f);
            emotionSelectorRect.pivot = new Vector2(0.5f, 0.5f); 
            emotionSelectorRect.sizeDelta = Vector2.zero;
            // TODO: Заполнить EmotionSelector реальными элементами

            // Button Container
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

            // Кнопки
            GameObject saveButton = UIComponentGenerator.CreateStyledButton(
                "SaveButton", "Сохранить", buttonContainer.transform,
                _brushyFont, _buttonTextColor, _buttonFontSize,
                _woodenPlankSprite, Image.Type.Sliced, _buttonSpriteTintColor,
                GetDefaultButtonColors(), _buttonSize, _buttonPressedScale
            );

            GameObject cancelButton = UIComponentGenerator.CreateStyledButton(
                "CancelButton", "Отмена", buttonContainer.transform,
                _brushyFont, _buttonTextColor, _buttonFontSize,
                _woodenPlankSprite, Image.Type.Sliced, _buttonSpriteTintColor,
                GetDefaultButtonColors(), _buttonSize, _buttonPressedScale
            );
            
            // 4. Создаем Popup Panel
            GameObject popupPanel = UIComponentGenerator.CreatePopupPanel(
                panelRoot.transform, // Parent to the panel root so it overlays everything
                "Сообщение по умолчанию", 
                _brushyFont, _popupTextColor, _popupFontSize, 
                _popupBgColor
            );

            // 5. Добавляем и настраиваем контроллер
            LogEmotionPanelController controller = panelRoot.AddComponent<LogEmotionPanelController>();
            SerializedObject serializedController = new SerializedObject(controller);
            
            // Используем TryGetComponent для безопасности
            Button saveBtnComp = null;
            if (saveButton) saveButton.TryGetComponent(out saveBtnComp);
            serializedController.FindProperty("_saveButton").objectReferenceValue = saveBtnComp;

            Button cancelBtnComp = null;
            if (cancelButton) cancelButton.TryGetComponent(out cancelBtnComp);
            serializedController.FindProperty("_cancelButton").objectReferenceValue = cancelBtnComp;
            
            serializedController.FindProperty("_popupPanel").objectReferenceValue = popupPanel;
            TMP_Text popupTextComp = null;
            if (popupPanel) 
            {
                Transform popupTextTransform = popupPanel.transform.Find("PopupText");
                if (popupTextTransform) popupTextTransform.TryGetComponent(out popupTextComp);                    
            }
            serializedController.FindProperty("_popupText").objectReferenceValue = popupTextComp;
            
            serializedController.ApplyModifiedPropertiesWithoutUndo(); // Важно для Editor скриптов

            // 6. Сохраняем префаб
            UIComponentGenerator.SavePrefab(panelRoot, PrefabSaveFolderPath, panelName);
            
            // 7. Опционально: уничтожаем созданный GameObject со сцены, если он не нужен
            // GameObject.DestroyImmediate(panelRoot); 
            // Это нужно, если генератор вызывается на активной сцене и создает временные объекты.
            // Если ShowWindow() для EditorWindow, то объекты могут оставаться до закрытия окна или их ручного удаления.
            // Для MenuItem, который просто выполняет действие, лучше уничтожать.
            // Но так как SavePrefab уже сохранил, можно и оставить для просмотра, если это удобно.
            // Если метод вызывается из OnGUI EditorWindow, то уничтожение может быть нежелательным сразу.
            // Для статического метода, вызываемого через MenuItem, лучше уничтожать временный объект.
            if (!Application.isPlaying) // Уничтожаем только если не в Play Mode
            {
                 GameObject.DestroyImmediate(panelRoot);
            }

            MyLogger.EditorLog($"[LogEmotionPanelGenerator] Префаб {panelName} создан в {Path.Combine(PrefabSaveFolderPath, panelName + ".prefab")}");
        }
    }
} 