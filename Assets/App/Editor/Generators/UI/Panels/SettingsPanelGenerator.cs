using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using System.IO;
// using App.Develop.Scenes.PersonalAreaScene.UI; // TODO: Add controller if needed
using App.Editor.Generators.UI.Core; // Для UIComponentGenerator
using App.Develop.Utils.Logging;

namespace App.Editor.Generators.UI.Panels
{
    public static class SettingsPanelGenerator
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

        private static void LoadResources()
        {
            if (_woodenPlankSprite == null)
                _woodenPlankSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(TexturesFolder, "WoodenPlank.png"));
            if (_woodenPlankSprite == null) 
                MyLogger.EditorLogWarning($"[SettingsPanelGenerator] Текстура WoodenPlank.png не найдена в {TexturesFolder}");

            if (_brushyFont == null)
            {
                _brushyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Path.Combine(FontsFolder, "BrushyFont.asset"));
                if (_brushyFont == null) 
                    _brushyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Path.Combine(FontsFolder, "BrushyFont.ttf"));
                if (_brushyFont == null) 
                    MyLogger.EditorLogWarning($"[SettingsPanelGenerator] TMP_FontAsset BrushyFont (.asset или .ttf) не найден в {FontsFolder}.");
            }
        }

        [MenuItem("MoodColor/Generate/UI Panels/Personal Area/Settings Panel")]
        public static void CreateSettingsPanelPrefab()
        {
            LoadResources();

            string panelName = "SettingsPanel";
            string title = "Настройки";

            GameObject panelRoot = UIComponentGenerator.CreateBasePanelRoot(panelName, RenderMode.ScreenSpaceOverlay, 10, new Vector2(1080, 1920));

            Transform contentContainer = UIComponentGenerator.CreateBasePanelVisuals(
                panelRoot, title, _brushyFont, _titleTextColor, _titleFontSize,
                _panelBackgroundColor, _titleContainerColor, 
                null, Image.Type.Simple, // panelBackgroundSprite
                null, Image.Type.Simple  // titleContainerSprite
            ).transform;

            // TODO: Создать специфичный контент для SettingsPanel
            GameObject placeholderContent = new GameObject("PlaceholderSettingsContent");
            placeholderContent.transform.SetParent(contentContainer, false);
            RectTransform placeholderRect = placeholderContent.AddComponent<RectTransform>();
            placeholderRect.anchorMin = new Vector2(0.1f, 0.1f); 
            placeholderRect.anchorMax = new Vector2(0.9f, 0.9f);
            placeholderRect.pivot = new Vector2(0.5f, 0.5f); 
            placeholderRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI placeholderText = placeholderContent.AddComponent<TextMeshProUGUI>();
            placeholderText.text = "Содержимое панели Настройки будет здесь.";
            placeholderText.alignment = TextAlignmentOptions.Center;
            if(_brushyFont != null) placeholderText.font = _brushyFont;
            placeholderText.fontSize = 20f;
            placeholderText.color = _titleTextColor;

            // TODO: Добавить и настроить контроллер, если он нужен для этой панели

            UIComponentGenerator.SavePrefab(panelRoot, PrefabSaveFolderPath, panelName);
            
            if (!Application.isPlaying)
            {
                 GameObject.DestroyImmediate(panelRoot);
            }

            MyLogger.EditorLog($"[SettingsPanelGenerator] Префаб {panelName} создан в {Path.Combine(PrefabSaveFolderPath, panelName + ".prefab")}");
        }
    }
} 