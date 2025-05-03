#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace App.Develop.AppServices.Firebase.Tests.Editor
{
    /// <summary>
    /// Редактор для создания UI панели тестирования нагрузки Firebase
    /// </summary>
    public class FirebaseLoadTestUIGenerator : EditorWindow
    {
        #region Private Fields
        private GameObject m_CanvasObject;
        private string m_PanelName = "FirebaseLoadTestPanel";
        private FirebaseLoadTestManager m_TestManager;
        #endregion

        #region Window Methods
        [MenuItem("Tools/Firebase/Create Load Test UI")]
        public static void ShowWindow()
        {
            GetWindow<FirebaseLoadTestUIGenerator>("Firebase Test UI Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Firebase Load Test UI Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            m_PanelName = EditorGUILayout.TextField("Panel Name", m_PanelName);
            m_TestManager = EditorGUILayout.ObjectField("Test Manager", m_TestManager, typeof(FirebaseLoadTestManager), true) as FirebaseLoadTestManager;
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Этот инструмент создаст панель UI со всеми необходимыми элементами для тестирования нагрузки Firebase.", MessageType.Info);
            EditorGUILayout.Space();

            if (GUILayout.Button("Создать UI панель"))
            {
                GenerateUIPanel();
            }
        }
        #endregion

        #region Generation Methods
        private void GenerateUIPanel()
        {
            // Проверяем, есть ли Canvas на сцене
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                // Создаем новый Canvas
                m_CanvasObject = new GameObject("Canvas");
                canvas = m_CanvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                m_CanvasObject.AddComponent<CanvasScaler>();
                m_CanvasObject.AddComponent<GraphicRaycaster>();
            }
            else
            {
                m_CanvasObject = canvas.gameObject;
            }

            // Создаем панель
            GameObject panelObject = new GameObject(m_PanelName);
            RectTransform panelRectTransform = panelObject.AddComponent<RectTransform>();
            panelObject.AddComponent<CanvasRenderer>();
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            // Устанавливаем родителя и настройки RectTransform
            panelObject.transform.SetParent(m_CanvasObject.transform, false);
            panelRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panelRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panelRectTransform.pivot = new Vector2(0.5f, 0.5f);
            panelRectTransform.sizeDelta = new Vector2(600, 500);
            panelRectTransform.anchoredPosition = Vector2.zero;
            
            // Добавляем компонент FirebaseLoadTestUI
            FirebaseLoadTestUI testUI = panelObject.AddComponent<FirebaseLoadTestUI>();
            
            // Создаем заголовок
            GameObject titleObject = CreateTextObject("TitleText", panelObject.transform, "Firebase Load Test", 24);
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 50);
            titleRect.anchoredPosition = new Vector2(0, -25);
            
            // Создаем статус текст
            GameObject statusObject = CreateTextObject("StatusText", panelObject.transform, "Status: Ready", 18);
            RectTransform statusRect = statusObject.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 1);
            statusRect.anchorMax = new Vector2(1, 1);
            statusRect.pivot = new Vector2(0.5f, 1);
            statusRect.sizeDelta = new Vector2(0, 30);
            statusRect.anchoredPosition = new Vector2(0, -80);
            
            // Создаем прогресс-бар
            GameObject sliderObject = new GameObject("ProgressSlider");
            RectTransform sliderRect = sliderObject.AddComponent<RectTransform>();
            sliderObject.transform.SetParent(panelObject.transform, false);
            sliderRect.anchorMin = new Vector2(0.1f, 0.7f);
            sliderRect.anchorMax = new Vector2(0.9f, 0.75f);
            sliderRect.anchoredPosition = Vector2.zero;
            sliderRect.sizeDelta = Vector2.zero;
            
            Slider progressSlider = sliderObject.AddComponent<Slider>();
            
            // Background для слайдера
            GameObject sliderBg = new GameObject("Background");
            Image sliderBgImage = sliderBg.AddComponent<Image>();
            RectTransform sliderBgRect = sliderBg.GetComponent<RectTransform>();
            sliderBg.transform.SetParent(sliderObject.transform, false);
            sliderBgRect.anchorMin = Vector2.zero;
            sliderBgRect.anchorMax = Vector2.one;
            sliderBgRect.sizeDelta = Vector2.zero;
            sliderBgImage.color = new Color(0.2f, 0.2f, 0.2f, 1);
            
            // Fill для слайдера
            GameObject sliderFill = new GameObject("Fill");
            Image sliderFillImage = sliderFill.AddComponent<Image>();
            RectTransform sliderFillRect = sliderFill.GetComponent<RectTransform>();
            sliderFill.transform.SetParent(sliderObject.transform, false);
            sliderFillRect.anchorMin = Vector2.zero;
            sliderFillRect.anchorMax = new Vector2(0, 1);
            sliderFillRect.sizeDelta = Vector2.zero;
            sliderFillImage.color = new Color(0.2f, 0.8f, 0.2f, 1);
            
            progressSlider.targetGraphic = sliderBgImage;
            progressSlider.fillRect = sliderFillRect;
            progressSlider.interactable = false;
            
            // Создаем результаты текст
            GameObject resultsObject = CreateTextObject("ResultsText", panelObject.transform, "Successful: 0\nFailed: 0\nAverage response time: 0.000s\nMaximum response time: 0.000s", 16);
            RectTransform resultsRect = resultsObject.GetComponent<RectTransform>();
            resultsRect.anchorMin = new Vector2(0.1f, 0.4f);
            resultsRect.anchorMax = new Vector2(0.9f, 0.65f);
            resultsRect.pivot = new Vector2(0.5f, 0.5f);
            resultsRect.anchoredPosition = Vector2.zero;
            resultsRect.sizeDelta = Vector2.zero;
            
            // Создаем кнопки
            GameObject startButtonObject = CreateButton("StartTestButton", panelObject.transform, "Start Test", new Vector2(0.2f, 0.25f), new Vector2(0.4f, 0.35f));
            GameObject abortButtonObject = CreateButton("AbortTestButton", panelObject.transform, "Abort Test", new Vector2(0.6f, 0.25f), new Vector2(0.8f, 0.35f));
            GameObject exportButtonObject = CreateButton("ExportResultsButton", panelObject.transform, "Export Results", new Vector2(0.4f, 0.15f), new Vector2(0.6f, 0.25f));
            
            // Присваиваем ссылки к FirebaseLoadTestUI
            SerializedObject serializedTestUI = new SerializedObject(testUI);
            
            SerializedProperty testManagerProp = serializedTestUI.FindProperty("m_TestManager");
            SerializedProperty startButtonProp = serializedTestUI.FindProperty("m_StartTestButton");
            SerializedProperty abortButtonProp = serializedTestUI.FindProperty("m_AbortTestButton");
            SerializedProperty exportButtonProp = serializedTestUI.FindProperty("m_ExportResultsButton");
            SerializedProperty progressSliderProp = serializedTestUI.FindProperty("m_ProgressSlider");
            SerializedProperty statusTextProp = serializedTestUI.FindProperty("m_StatusText");
            SerializedProperty resultsTextProp = serializedTestUI.FindProperty("m_ResultsText");
            
            testManagerProp.objectReferenceValue = m_TestManager;
            startButtonProp.objectReferenceValue = startButtonObject.GetComponent<Button>();
            abortButtonProp.objectReferenceValue = abortButtonObject.GetComponent<Button>();
            exportButtonProp.objectReferenceValue = exportButtonObject.GetComponent<Button>();
            progressSliderProp.objectReferenceValue = progressSlider;
            statusTextProp.objectReferenceValue = statusObject.GetComponent<TextMeshProUGUI>();
            resultsTextProp.objectReferenceValue = resultsObject.GetComponent<TextMeshProUGUI>();
            
            serializedTestUI.ApplyModifiedProperties();
            
            // Выбираем созданный объект
            Selection.activeGameObject = panelObject;
            EditorUtility.DisplayDialog("UI Generator", "Firebase Load Test UI успешно создан!", "OK");
        }

        private GameObject CreateTextObject(string name, Transform parent, string text, int fontSize)
        {
            GameObject textObject = new GameObject(name);
            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textObject.transform.SetParent(parent, false);
            
            TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            
            return textObject;
        }

        private GameObject CreateButton(string name, Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject buttonObject = new GameObject(name);
            RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonObject.transform.SetParent(parent, false);
            
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.sizeDelta = Vector2.zero;
            buttonRect.anchoredPosition = Vector2.zero;
            
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1);
            
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.5f, 0.5f, 0.5f, 1);
            colors.pressedColor = new Color(0.2f, 0.2f, 0.2f, 1);
            button.colors = colors;
            
            // Создаем текст для кнопки
            GameObject textObject = CreateTextObject("Text", buttonObject.transform, text, 16);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            return buttonObject;
        }
        #endregion
    }
}
#endif 