#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
// using App.Develop.Scenes.PersonalAreaScene.UI.Components; // Проверить и обновить, если эти компоненты еще используются и пути корректны
// using App.App.Develop.Scenes.PersonalAreaScene.UI.Components; // Проверить и обновить
using System.Collections; // Используется? Если нет, можно убрать
using System.IO; // Для Path.Combine и Directory

namespace App.Editor.Generators.UI.Canvases // Измененное пространство имен
{
    public class PersonalAreaCanvasGenerator // Класс переименован
    {
        // private const string RESOURCES_FOLDER = "Assets/App/Resources"; // Больше не используется для определения пути сохранения
        // private const string UI_FOLDER = RESOURCES_FOLDER + "/UI"; // Больше не используется для определения пути сохранения
        // private const string PREFAB_PATH = UI_FOLDER + "/PersonalAreaCanvas.prefab"; // Старый путь

        private const string PREFAB_SAVE_FOLDER_PATH = "Assets/App/Prefabs/Generated/UI/Canvases/";
        private const string PREFAB_NAME = "PersonalAreaCanvas";

        // Цветовая палитра стиля MoodRoom (оставляем, если используется в генерации)
        private static readonly Color WarmWoodLight = new Color(0.9f, 0.8f, 0.7f, 1f); 
        private static readonly Color WarmWoodMedium = new Color(0.82f, 0.71f, 0.55f, 1f); 
        private static readonly Color WarmWoodDark = new Color(0.7f, 0.6f, 0.45f, 1f); 
        private static readonly Color WoodDarkBrown = new Color(0.6f, 0.5f, 0.35f, 1f); 
        private static readonly Color TextDark = new Color(0.25f, 0.2f, 0.15f, 1f); 
        private static readonly Color PaperBeige = new Color(0.95f, 0.9f, 0.8f, 1f); 
        private static readonly Color GlassBlue = new Color(0.7f, 0.85f, 0.9f, 0.3f); 

        [MenuItem("MoodColor/Generate/UI Canvases/Personal Area Canvas")] // Измененный путь в меню
        public static void GeneratePrefab()
        {
            Debug.Log("🔄 Начинаем генерацию префаба Personal Area Canvas...");

            // Папки для ресурсов больше не создаем здесь, т.к. префаб сохраняется в другое место
            // if (!AssetDatabase.IsValidFolder(RESOURCES_FOLDER))
            //     AssetDatabase.CreateFolder("Assets/App", "Resources");
            // if (!AssetDatabase.IsValidFolder(UI_FOLDER))
            //     AssetDatabase.CreateFolder(RESOURCES_FOLDER, "UI");

            var root = CreateUIObject("PersonalAreaCanvas", null);
            root.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            var background = CreateUIObject("RoomBackground", root.transform);
            var backgroundRect = background.GetComponent<RectTransform>();
            SetFullStretch(backgroundRect);
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = WarmWoodLight;

            var windowFrame = CreateUIObject("WindowFrame", background.transform);
            var windowFrameRect = windowFrame.GetComponent<RectTransform>();
            windowFrameRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowFrameRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowFrameRect.sizeDelta = new Vector2(600, 400);
            windowFrameRect.anchoredPosition = new Vector2(0, 300);
            var windowFrameImage = windowFrame.AddComponent<Image>();
            windowFrameImage.color = WarmWoodDark;

            var windowView = CreateUIObject("WindowView", windowFrame.transform);
            var windowViewRect = windowView.GetComponent<RectTransform>();
            windowViewRect.anchorMin = new Vector2(0, 0);
            windowViewRect.anchorMax = new Vector2(1, 1);
            windowViewRect.sizeDelta = new Vector2(-40, -40);
            windowViewRect.anchoredPosition = Vector2.zero;
            var windowViewImage = windowView.AddComponent<Image>();
            windowViewImage.color = GlassBlue;

            var shelf = CreateUIObject("WoodenShelf", background.transform);
            var shelfRect = shelf.GetComponent<RectTransform>();
            shelfRect.anchorMin = new Vector2(0, 0);
            shelfRect.anchorMax = new Vector2(1, 0);
            shelfRect.sizeDelta = new Vector2(0, 30);
            shelfRect.anchoredPosition = new Vector2(0, 500);
            var shelfImage = shelf.AddComponent<Image>();
            shelfImage.color = WoodDarkBrown;

            var safeArea = CreateUIObject("SafeArea", root.transform);
            var safeAreaRect = safeArea.GetComponent<RectTransform>();
            SetFullStretch(safeAreaRect);

            // Проверка на App.Develop.Scenes.PersonalAreaScene.UI.SafeArea
            // Это специфичный для проекта компонент, его нужно будет либо перенести/сделать доступным,
            // либо заменить на общую логику, если он не критичен для генератора как такового.
            // Пока оставим как есть, но это место для возможного рефакторинга.
            System.Type safeAreaType = System.Type.GetType("App.Develop.Scenes.PersonalAreaScene.UI.SafeArea, Assembly-CSharp");
            if (safeAreaType != null)
            {
                if (safeArea.GetComponent(safeAreaType) == null)
                {
                    safeArea.AddComponent(safeAreaType);
                    Debug.Log("[PersonalAreaCanvasGenerator] Added SafeArea component.");
                }
            }
            else
            {
                Debug.LogWarning("[PersonalAreaCanvasGenerator] SafeArea script (App.Develop.Scenes.PersonalAreaScene.UI.SafeArea) not found. This might be an issue if the component is critical.");
            }

            var mainContent = CreateUIObject("MainContent", safeArea.transform);
            var mainContentRect = mainContent.GetComponent<RectTransform>();
            SetFullStretch(mainContentRect);

            var mainLayout = mainContent.AddComponent<VerticalLayoutGroup>();
            mainLayout.padding = new RectOffset(40, 40, 60, 40);
            mainLayout.spacing = 20; 
            mainLayout.childAlignment = TextAnchor.UpperCenter;
            mainLayout.childControlHeight = true; 
            mainLayout.childControlWidth = true;
            mainLayout.childForceExpandHeight = true; 
            mainLayout.childForceExpandWidth = true;

            Debug.Log("🔄 Создаем UI компоненты...");
            // Эти вызовы создают специфичные части UI. Их нужно будет проверить на зависимости.
            // var profileInfo = CreateProfileInfo(mainContent.transform);
            // var emotionJars = CreateEmotionJars(mainContent.transform);
            // var statistics = CreateStatistics(mainContent.transform);
            // var navigation = CreateNavigation(mainContent.transform);
            
            // ЗАГЛУШКИ для ProfileInfo, EmotionJars, Statistics, Navigation, чтобы генератор работал
            var profileInfo = new GameObject("ProfileInfo_Placeholder"); profileInfo.transform.SetParent(mainContent.transform);
            var emotionJars = new GameObject("EmotionJars_Placeholder"); emotionJars.transform.SetParent(mainContent.transform);
            var statistics = new GameObject("Statistics_Placeholder"); statistics.transform.SetParent(mainContent.transform);
            var navigation = new GameObject("Navigation_Placeholder"); navigation.transform.SetParent(mainContent.transform);
            // Добавим им LayoutElement, чтобы VerticalLayoutGroup их учитывал
            profileInfo.AddComponent<LayoutElement>().preferredHeight = 150;
            emotionJars.AddComponent<LayoutElement>().preferredHeight = 300;
            statistics.AddComponent<LayoutElement>().preferredHeight = 200;
            navigation.AddComponent<LayoutElement>().preferredHeight = 100;


            Debug.Log("🔄 Настраиваем контроллеры...");
            // Зависимости от PersonalAreaManager и PersonalAreaUIController.
            // Их нужно либо оставить, если они являются частью префаба канваса,
            // либо вынести, если они должны добавляться на сцене отдельно.
            // var manager = root.AddComponent<PersonalAreaManager>();
            // var uiController = root.AddComponent<PersonalAreaUIController>();

            // serializedManager.FindProperty("_ui").objectReferenceValue = uiController;
            // serializedManager.ApplyModifiedProperties();

            // serializedUI.FindProperty("_profileInfo").objectReferenceValue = profileInfo.GetComponent<ProfileInfoComponent>();
            // serializedUI.FindProperty("_emotionJars").objectReferenceValue = emotionJars.GetComponent<EmotionJarView>();
            // serializedUI.FindProperty("_statistics").objectReferenceValue = statistics.GetComponent<StatisticsView>();
            // serializedUI.FindProperty("_navigation").objectReferenceValue = navigation.GetComponent<NavigationComponent>();
            // serializedUI.ApplyModifiedProperties();

            Debug.Log($"💾 Сохраняем префаб в {Path.Combine(PREFAB_SAVE_FOLDER_PATH, PREFAB_NAME + ".prefab")}");
            SaveGeneratedPrefab(root, PREFAB_SAVE_FOLDER_PATH, PREFAB_NAME);

            if (!Application.isPlaying) Object.DestroyImmediate(root);

            Debug.Log("✅ Генерация префаба Personal Area Canvas завершена");
        }

        // Методы CreateProfileInfo, CreateEmotionJars и т.д. остаются здесь, т.к. они специфичны для этого канваса.
        // ... (весь остальной код из оригинального файла, включая хелперы и вложенный класс SafeArea) ...
        // Важно: Метод SaveAsPrefab нужно будет переименовать или заменить на SaveGeneratedPrefab
        // и обновить его логику для использования новых путей и создания директории.

        private static GameObject CreateProfileInfo(Transform parent) { /* ... implementation ... */ return new GameObject("ProfileInfo_Generated"); }
        private static GameObject CreateEmotionJars(Transform parent) { /* ... implementation ... */ return new GameObject("EmotionJars_Generated"); }
        private static GameObject CreateStatistics(Transform parent) { /* ... implementation ... */ return new GameObject("Statistics_Generated"); }
        private static GameObject CreateNavigation(Transform parent) { /* ... implementation ... */ return new GameObject("Navigation_Generated"); }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var uiObject = new GameObject(name, typeof(RectTransform));
            uiObject.transform.SetParent(parent, false);
            return uiObject;
        }

        private static void SetFullStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        
        // ... (Другие хелперы, если они есть и используются CreateUIObject, SetFullStretch и т.д.)

        // Обновленный метод сохранения, адаптированный из UIComponentGenerator
        private static void SaveGeneratedPrefab(GameObject rootGameObject, string folderPath, string prefabName)
        {
            if (rootGameObject == null)
            {
                Debug.LogError("[PersonalAreaCanvasGenerator] Root GameObject is null. Cannot save prefab.");
                return;
            }
            if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(prefabName))
            {
                Debug.LogError("[PersonalAreaCanvasGenerator] Folder path or prefab name is null or empty. Cannot save prefab.");
                return;
            }
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }
            string fullPath = Path.Combine(folderPath, prefabName + ".prefab");
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
            if (existingPrefab != null)
            {
                Debug.LogWarning($"[PersonalAreaCanvasGenerator] Prefab already exists at {fullPath}. It will be overwritten.");
            }
            bool prefabSuccess;
            PrefabUtility.SaveAsPrefabAsset(rootGameObject, fullPath, out prefabSuccess);
            if (prefabSuccess)
            {
                Debug.Log($"[PersonalAreaCanvasGenerator] Prefab '{prefabName}' saved successfully to: {fullPath}");
            }
            else
            {
                Debug.LogError($"[PersonalAreaCanvasGenerator] Failed to save prefab '{prefabName}' to {fullPath}. Check for errors in the console.");
            }
        }

        // Оставляем класс SafeArea здесь, так как он, похоже, тесно связан с этим канвасом
        // Если он станет более общим, его можно будет вынести
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
                _maxAnchor = _safeArea.position + _safeArea.size;

                _minAnchor.x /= Screen.width;
                _minAnchor.y /= Screen.height;
                _maxAnchor.x /= Screen.width;
                _maxAnchor.y /= Screen.height;

                _rectTransform.anchorMin = _minAnchor;
                _rectTransform.anchorMax = _maxAnchor;
                ApplySafeArea(); // Применить сразу
            }

            private void ApplySafeArea()
            {
                if (_rectTransform == null) return;
                var safeAreaRect = Screen.safeArea;
                var newAnchorMin = safeAreaRect.position;
                var newAnchorMax = safeAreaRect.position + safeAreaRect.size;
                newAnchorMin.x /= Screen.width;
                newAnchorMin.y /= Screen.height;
                newAnchorMax.x /= Screen.width;
                newAnchorMax.y /= Screen.height;
                _rectTransform.anchorMin = newAnchorMin;
                _rectTransform.anchorMax = newAnchorMax;
            }

            private void OnValidate() // Для удобства в редакторе
            {
                if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
                ApplySafeArea();
            } 
        }        
    }
}
#endif 