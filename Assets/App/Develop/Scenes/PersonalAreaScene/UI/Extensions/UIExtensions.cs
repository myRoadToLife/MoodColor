using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;

namespace App.Develop.Scenes.PersonalAreaScene.UI.Extensions
{
    public static class UIExtensions
    {
        public static void SetupButton(this Button button, UnityAction onClick)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
        }
        
        public static void SetupToggle(this Toggle toggle, UnityAction<bool> onValueChanged, bool defaultState = false)
        {
            if (toggle == null) return;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.isOn = defaultState;
            toggle.onValueChanged.AddListener(onValueChanged);
        }
        
        public static void SetupPasswordField(this TMP_InputField input, UnityAction<string> onValueChanged)
        {
            if (input == null) return;
            input.onValueChanged.RemoveAllListeners();
            input.onValueChanged.AddListener(onValueChanged);
            input.contentType = TMP_InputField.ContentType.Password;
            input.ForceLabelUpdate();
        }
    }
} 