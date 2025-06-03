using System;

namespace App.Develop.Scenes.PersonalAreaScene.Settings
{
    public interface ISettingsManager
    {
        /// <summary>
        /// Событие изменения настроек
        /// </summary>
        event Action<SettingsData> OnSettingsChanged;

        void SaveSettings();
        void LoadSettings();
        void ResetSettings();
        void SetNotifications(bool value);
        void SetSound(bool value);
        void SetTheme(ThemeType value);
        void SetLanguage(string value);
        void SetSelectedRegion(string regionName);
        SettingsData GetCurrentSettings();
    }
}
