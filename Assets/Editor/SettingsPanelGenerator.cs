using App.Develop.AppServices.Settings;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Editor
{
    public class SettingsPanelGenerator : MonoBehaviour
    {
        [MenuItem("Tools/🛠 Generate SettingsPanel")]
        public static void GenerateSettingsPanel()
        {
            GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvasGO.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);

            GameObject settingsPanel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(CanvasGroup));
            settingsPanel.transform.SetParent(canvasGO.transform);
            settingsPanel.AddComponent<AccountSettingsManager>();

            RectTransform rt = settingsPanel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = settingsPanel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            // Popup Panel
            GameObject popupPanel = CreatePanel("PopupPanel", settingsPanel.transform);
            TMP_Text popupText = CreateText("PopupText", popupPanel.transform, "Сообщение...", 22);
            popupPanel.SetActive(false);

            // Buttons
            CreateButton("LogoutButton", settingsPanel.transform, "Выйти");
            CreateButton("DeleteButton", settingsPanel.transform, "Удалить аккаунт");

            // Confirm Delete Panel
            GameObject confirmPanel = CreatePanel("ConfirmDeletePanel", settingsPanel.transform);
            CreateText("InfoText", confirmPanel.transform, "Введите пароль для подтверждения", 20);
            CreateTMPInput("PasswordConfirmInput", confirmPanel.transform);
            CreateButton("ConfirmButton", confirmPanel.transform, "Подтвердить");
            CreateButton("CancelButton", confirmPanel.transform, "Отмена");
            confirmPanel.SetActive(false);

            Debug.Log("✅ SettingsPanel создан успешно!");
        }

        private static GameObject CreatePanel(string name, Transform parent)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(600, 300);
            return panel;
        }

        private static TMP_Text CreateText(string name, Transform parent, string text, int fontSize)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TMP_Text tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(600, 60);
            return tmp;
        }

        private static TMP_InputField CreateTMPInput(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(600, 60);
            return go.GetComponent<TMP_InputField>();
        }

        private static Button CreateButton(string name, Transform parent, string text)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Button), typeof(Image));
            btnObj.transform.SetParent(parent, false);
            btnObj.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.9f, 0.8f);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(600, 80);

            GameObject txt = new GameObject("Text", typeof(TextMeshProUGUI));
            txt.transform.SetParent(btnObj.transform, false);
            TMP_Text tmp = txt.GetComponent<TMP_Text>();
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btnObj.GetComponent<Button>();
        }
    }
}
