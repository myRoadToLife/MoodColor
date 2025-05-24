using System;
using System.Collections.Generic;
using UnityEngine;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Notifications
{
    /// <summary>
    /// Менеджер пользовательских настроек для уведомлений
    /// </summary>
    [Serializable]
    public class NotificationPreferences
    {
        public bool EnablePushNotifications = true;
        public bool EnableInGameNotifications = true;
        public bool EnableEmailNotifications = false;
        
        // Используем сериализуемый список для категорий
        public List<CategorySetting> CategorySettings = new List<CategorySetting>();
        
        // Временной диапазон для отправки уведомлений
        public int QuietHoursStart = 22; // 10 PM
        public int QuietHoursEnd = 8;    // 8 AM
        
        // Максимальное количество уведомлений в день
        public int MaxNotificationsPerDay = 5;
    }
    
    [Serializable]
    public class CategorySetting
    {
        public NotificationCategory Category;
        public bool Enabled = true;
        
        public CategorySetting(NotificationCategory category, bool enabled)
        {
            Category = category;
            Enabled = enabled;
        }
    }
    
    public class UserPreferencesManager
    {
        private const string PREFS_KEY = "notification_preferences";
        
        private NotificationPreferences _preferences = new NotificationPreferences();
        private Dictionary<NotificationCategory, bool> _categoryEnabledCache = new Dictionary<NotificationCategory, bool>();
        private Dictionary<string, int> _dailyNotificationCount = new Dictionary<string, int>();
        
        private bool _isInitialized = false;
        
        public void Initialize()
        {
            if (_isInitialized) return;
            
            MyLogger.Log("Initializing UserPreferencesManager", MyLogger.LogCategory.Default);
            
            LoadPreferences();
            InitializeDefaultCategorySettings();
            UpdateCategoryCache();
            
            _isInitialized = true;
            MyLogger.Log("UserPreferencesManager initialized successfully", MyLogger.LogCategory.Default);
        }
        
        private void InitializeDefaultCategorySettings()
        {
            // Если список настроек категорий пуст, заполняем его значениями по умолчанию
            if (_preferences.CategorySettings.Count == 0)
            {
                foreach (NotificationCategory category in Enum.GetValues(typeof(NotificationCategory)))
                {
                    _preferences.CategorySettings.Add(new CategorySetting(category, true));
                }
            }
        }
        
        private void UpdateCategoryCache()
        {
            _categoryEnabledCache.Clear();
            foreach (var setting in _preferences.CategorySettings)
            {
                _categoryEnabledCache[setting.Category] = setting.Enabled;
            }
        }
        
        public bool IsNotificationEnabled(NotificationCategory category)
        {
            if (!_preferences.EnablePushNotifications)
            {
                return false;
            }
            
            if (!_categoryEnabledCache.ContainsKey(category))
            {
                return true; // По умолчанию разрешено, если не указано иное
            }
            
            return _categoryEnabledCache[category];
        }
        
        public bool IsTimeWindowAllowed()
        {
            int currentHour = DateTime.Now.Hour;
            
            // Проверяем, находимся ли мы в тихое время
            if (_preferences.QuietHoursStart <= _preferences.QuietHoursEnd)
            {
                // Простой случай, например с 22 до 8
                return currentHour < _preferences.QuietHoursStart && currentHour >= _preferences.QuietHoursEnd;
            }
            else
            {
                // Сложный случай, когда период пересекает полночь, например с 22 до 8
                return !(currentHour >= _preferences.QuietHoursStart || currentHour < _preferences.QuietHoursEnd);
            }
        }
        
        public bool CanSendNotificationToday()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            
            if (!_dailyNotificationCount.ContainsKey(today))
            {
                _dailyNotificationCount[today] = 0;
                // Очищаем старые записи
                CleanupOldCounts();
            }
            
            return _dailyNotificationCount[today] < _preferences.MaxNotificationsPerDay;
        }
        
        public void RecordNotificationSent()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            
            if (!_dailyNotificationCount.ContainsKey(today))
            {
                _dailyNotificationCount[today] = 0;
            }
            
            _dailyNotificationCount[today]++;
        }
        
        private void CleanupOldCounts()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            List<string> keysToRemove = new List<string>();
            
            foreach (var key in _dailyNotificationCount.Keys)
            {
                if (key != today)
                {
                    keysToRemove.Add(key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _dailyNotificationCount.Remove(key);
            }
        }
        
        public void SetCategoryEnabled(NotificationCategory category, bool enabled)
        {
            bool found = false;
            
            for (int i = 0; i < _preferences.CategorySettings.Count; i++)
            {
                if (_preferences.CategorySettings[i].Category == category)
                {
                    _preferences.CategorySettings[i].Enabled = enabled;
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                _preferences.CategorySettings.Add(new CategorySetting(category, enabled));
            }
            
            UpdateCategoryCache();
            SavePreferences();
        }
        
        public void SetPushNotificationsEnabled(bool enabled)
        {
            _preferences.EnablePushNotifications = enabled;
            SavePreferences();
        }
        
        public void SetInGameNotificationsEnabled(bool enabled)
        {
            _preferences.EnableInGameNotifications = enabled;
            SavePreferences();
        }
        
        public void SetEmailNotificationsEnabled(bool enabled)
        {
            _preferences.EnableEmailNotifications = enabled;
            SavePreferences();
        }
        
        public void SetQuietHours(int start, int end)
        {
            _preferences.QuietHoursStart = Mathf.Clamp(start, 0, 23);
            _preferences.QuietHoursEnd = Mathf.Clamp(end, 0, 23);
            SavePreferences();
        }
        
        public void SetMaxNotificationsPerDay(int max)
        {
            _preferences.MaxNotificationsPerDay = Mathf.Max(0, max);
            SavePreferences();
        }
        
        public NotificationPreferences GetPreferences()
        {
            return _preferences;
        }
        
        private void LoadPreferences()
        {
            if (PlayerPrefs.HasKey(PREFS_KEY))
            {
                string json = PlayerPrefs.GetString(PREFS_KEY);
                try
                {
                    _preferences = JsonUtility.FromJson<NotificationPreferences>(json);
                    MyLogger.Log("Notification preferences loaded from PlayerPrefs", MyLogger.LogCategory.Default);
                }
                catch (Exception e)
                {
                    MyLogger.LogError($"Failed to load notification preferences: {e.Message}", MyLogger.LogCategory.Default);
                    _preferences = new NotificationPreferences();
                }
            }
            else
            {
                _preferences = new NotificationPreferences();
                MyLogger.Log("Using default notification preferences", MyLogger.LogCategory.Default);
            }
        }
        
        private void SavePreferences()
        {
            try
            {
                string json = JsonUtility.ToJson(_preferences);
                PlayerPrefs.SetString(PREFS_KEY, json);
                PlayerPrefs.Save();
                MyLogger.Log("Notification preferences saved to PlayerPrefs", MyLogger.LogCategory.Default);
            }
            catch (Exception e)
            {
                MyLogger.LogError($"Failed to save notification preferences: {e.Message}", MyLogger.LogCategory.Default);
            }
        }

        /// <summary>
        /// Возвращает email пользователя для отправки уведомлений
        /// </summary>
        public string GetUserEmail()
        {
            // Приоритет 1: Проверяем сохраненное значение
            string savedEmail = PlayerPrefs.GetString("UserEmail", "");
            if (!string.IsNullOrEmpty(savedEmail))
            {
                return savedEmail;
            }
            
            // Приоритет 2: Пытаемся получить из системы авторизации
            // Пример интеграции с Firebase Auth:
            /*
            if (FirebaseAuth.DefaultInstance.CurrentUser != null && 
                !string.IsNullOrEmpty(FirebaseAuth.DefaultInstance.CurrentUser.Email))
            {
                return FirebaseAuth.DefaultInstance.CurrentUser.Email;
            }
            */
            
            // Временная логика для тестирования
            #if UNITY_EDITOR
            return "test@example.com";
            #else
            return "";
            #endif
        }

        /// <summary>
        /// Устанавливает email пользователя для отправки уведомлений
        /// </summary>
        public void SetUserEmail(string email)
        {
            PlayerPrefs.SetString("UserEmail", email);
            PlayerPrefs.Save();
        }
    }
} 