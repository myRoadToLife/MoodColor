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
    }
} 