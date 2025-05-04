using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoodColor.App.Develop.CommonServices.Notifications
{
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
    
    public class UserPreferencesManager : MonoBehaviour
    {
        private const string PREFS_KEY = "notification_preferences";
        
        [SerializeField] private NotificationPreferences _preferences = new NotificationPreferences();
        private Dictionary<NotificationCategory, bool> _categoryEnabledCache = new Dictionary<NotificationCategory, bool>();
        private Dictionary<string, int> _dailyNotificationCount = new Dictionary<string, int>();
        
        private bool _isInitialized = false;
        
        public void Initialize()
        {
            if (_isInitialized) return;
            
            Debug.Log("Initializing UserPreferencesManager");
            
            LoadPreferences();
            InitializeDefaultCategorySettings();
            UpdateCategoryCache();
            
            _isInitialized = true;
            Debug.Log("UserPreferencesManager initialized successfully");
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
                    Debug.Log("Notification preferences loaded from PlayerPrefs");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load notification preferences: {e.Message}");
                    _preferences = new NotificationPreferences();
                }
            }
            else
            {
                _preferences = new NotificationPreferences();
                Debug.Log("Using default notification preferences");
            }
        }
        
        private void SavePreferences()
        {
            try
            {
                string json = JsonUtility.ToJson(_preferences);
                PlayerPrefs.SetString(PREFS_KEY, json);
                PlayerPrefs.Save();
                Debug.Log("Notification preferences saved to PlayerPrefs");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save notification preferences: {e.Message}");
            }
        }
    }
} 