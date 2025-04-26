namespace App.Develop.Scenes.PersonalAreaScene.Settings
{
    public interface ISettingsManager
    {
        void SaveSettings();
        void LoadSettings();
        void ResetSettings();
    }
} 