namespace App.Develop.Scenes.PersonalAreaScene.Settings
{
    public interface ISettingsManager
    {
        void SaveSettings();
        void LoadSettings();
        void ResetSettings();
        void SetNotifications(bool value);
        void SetSound(bool value);
        void SetTheme(ThemeType value);
        void SetLanguage(string value);
        SettingsData GetCurrentSettings();
    }
}
