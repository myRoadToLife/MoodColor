using UnityEditor;
using UnityEngine;
using TMPro;
using App.Develop.AppServices.Settings;

public class SettingsPanelAutoBinder : MonoBehaviour
{
    [MenuItem("Tools/🧩 Auto-Bind AccountSettingsManager UI")]
    public static void AutoBind()
    {
        var manager = FindFirstObjectByType<AccountSettingsManager>();

        if (manager == null)
        {
            Debug.LogError("❌ AccountSettingsManager не найден в сцене.");
            return;
        }

        Transform root = manager.transform;

        manager.GetType().GetField("_passwordConfirmInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(manager, root.Find("ConfirmDeletePanel/PasswordConfirmInput")?.GetComponent<TMP_InputField>());

        manager.GetType().GetField("_confirmDeletePanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(manager, root.Find("ConfirmDeletePanel")?.gameObject);

        manager.GetType().GetField("_popupPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(manager, root.Find("PopupPanel")?.gameObject);

        manager.GetType().GetField("_popupText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(manager, root.Find("PopupPanel/PopupText")?.GetComponent<TMP_Text>());

        Debug.Log("✅ Привязка UI элементов завершена!");
    }
}
