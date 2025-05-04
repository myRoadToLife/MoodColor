using System;
using System.Threading.Tasks;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.DI;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

namespace App.Develop.Scenes.PersonalAreaScene.Settings
{
    #region Enums and Models
    
    public enum ThemeType
    {
        Default,
        Dark,
        Light
    }

    [Serializable]
    public class SettingsData
    {
        public bool notifications = true;
        public bool sound = true;
        public ThemeType theme = ThemeType.Default;
        public string language = "ru";
    }
    #endregion

    public class SettingsManager : IInjectable, ISettingsManager
    {
        #region Private Fields
        private FirebaseAuth _auth;
        private DatabaseReference _database;
        private SettingsData _currentSettings;
        private const string SETTINGS_PATH = "settings";
        private const string SETTINGS_KEY = "user_settings";
        #endregion

        #region Events
        public event Action<SettingsData> OnSettingsChanged;
        #endregion

        public SettingsManager()
        {
            _currentSettings = LoadLocalSettings();
        }

        public void Inject(DIContainer container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            
            try
            {
                _auth = container.Resolve<FirebaseAuth>();
                _database = container.Resolve<DatabaseReference>();
                
                LoadSettings();
                Debug.Log("✅ SettingsManager успешно инициализирован");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка инициализации SettingsManager: {ex.Message}");
                throw;
            }
        }

        #region Public Methods
        public void SetNotifications(bool enabled)
        {
            if (_currentSettings.notifications == enabled) return;
            _currentSettings.notifications = enabled;
            OnSettingsChanged?.Invoke(_currentSettings);
            SaveSettings();
        }

        public void SetSound(bool enabled)
        {
            if (_currentSettings.sound == enabled) return;
            _currentSettings.sound = enabled;
            OnSettingsChanged?.Invoke(_currentSettings);
            SaveSettings();
        }

        public void SetTheme(ThemeType theme)
        {
            if (_currentSettings.theme == theme) return;
            _currentSettings.theme = theme;
            OnSettingsChanged?.Invoke(_currentSettings);
            SaveSettings();
        }

        public void SetLanguage(string language)
        {
            if (_currentSettings.language == language) return;
            _currentSettings.language = language;
            OnSettingsChanged?.Invoke(_currentSettings);
            SaveSettings();
        }

        public SettingsData GetCurrentSettings() => _currentSettings;

        public void SaveSettings()
        {
            if (_auth.CurrentUser == null)
            {
                Debug.LogWarning("Пользователь не авторизован");
                return;
            }

            try
            {
                var json = JsonUtility.ToJson(_currentSettings);
                var userSettingsRef = _database
                    .Child("users")
                    .Child(_auth.CurrentUser.UserId)
                    .Child(SETTINGS_PATH);

                userSettingsRef.SetRawJsonValueAsync(json);
                SaveLocalSettings();
                Debug.Log("Настройки успешно сохранены");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка сохранения настроек: {ex.Message}");
            }
        }

        public void LoadSettings()
        {
            if (_auth.CurrentUser == null)
            {
                Debug.LogWarning("Пользователь не авторизован");
                return;
            }

            var userSettingsRef = _database
                .Child("users")
                .Child(_auth.CurrentUser.UserId)
                .Child(SETTINGS_PATH);

            userSettingsRef.GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"Ошибка загрузки настроек: {task.Exception}");
                    return;
                }

                if (!task.IsCompleted)
                {
                    Debug.LogWarning("Загрузка настроек не завершена");
                    return;
                }

                var snapshot = task.Result;
                if (!snapshot.Exists)
                {
                    Debug.Log("Настройки не найдены, используются значения по умолчанию");
                    SaveSettings();
                    return;
                }

                try
                {
                    var json = snapshot.GetRawJsonValue();
                    var settings = JsonUtility.FromJson<SettingsData>(json);
                    
                    if (settings != null)
                    {
                        _currentSettings = settings;
                        OnSettingsChanged?.Invoke(_currentSettings);
                        SaveLocalSettings();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Ошибка десериализации настроек: {ex.Message}");
                }
            });
        }

        public void ResetSettings()
        {
            _currentSettings = new SettingsData();
            OnSettingsChanged?.Invoke(_currentSettings);
            SaveSettings();
        }
        #endregion

        #region Private Methods
        private SettingsData LoadLocalSettings()
        {
            var json = PlayerPrefs.GetString(SETTINGS_KEY, string.Empty);
            
            if (string.IsNullOrEmpty(json))
            {
                return new SettingsData();
            }

            try
            {
                return JsonUtility.FromJson<SettingsData>(json);
            }
            catch
            {
                return new SettingsData();
            }
        }

        private void SaveLocalSettings()
        {
            var json = JsonUtility.ToJson(_currentSettings);
            PlayerPrefs.SetString(SETTINGS_KEY, json);
            PlayerPrefs.Save();
        }
        #endregion
    }
} 