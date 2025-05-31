using UnityEngine;
using System.Collections.Generic;
using System;

namespace App.Develop.Utils.Logging
{
    [System.Serializable]
    public class CategorySettingEntry
    {
        public MyLogger.LogCategory category;
        public bool enabled = true; // Default to true for easier setup in Inspector
    }

    /// <summary>
    /// Configures the static MyLogger settings from the Unity Inspector.
    /// Place this script on a GameObject in your scene (e.g., a GameManager or a dedicated LoggerConfig object).
    /// Ensure its Awake() method runs before other scripts that use MyLogger if you need immediate configuration.
    /// You can adjust Script Execution Order settings if necessary.
    /// </summary>
    [DefaultExecutionOrder(-999)] // Гарантируем самый ранний запуск
    public class MyLoggerConfigurator : MonoBehaviour
    {
        private static MyLoggerConfigurator _instance;
        public static MyLoggerConfigurator Instance => _instance;

        [Header("Global Log Levels")]
        [Tooltip("Enable/Disable Debug.Log calls via MyLogger.Log")]
        [SerializeField] private bool _enableDebugLogging = true; // Включаем для отладки
        [Tooltip("Enable/Disable Debug.LogWarning calls via MyLogger.LogWarning")]
        [SerializeField] private bool _enableWarningLogging = true;
        [Tooltip("Enable/Disable Debug.LogError calls via MyLogger.LogError")]
        [SerializeField] private bool _enableErrorLogging = true;

        [Header("Category Log Levels")]
        [Tooltip("Configure which log categories are enabled. Categories not listed here will retain their default state defined in MyLogger.")]
        [SerializeField] private List<CategorySettingEntry> _categorySettings = new List<CategorySettingEntry>
        {
            new CategorySettingEntry { category = MyLogger.LogCategory.Firebase, enabled = false },
            new CategorySettingEntry { category = MyLogger.LogCategory.Bootstrap, enabled = false },
            new CategorySettingEntry { category = MyLogger.LogCategory.Sync, enabled = false },
            new CategorySettingEntry { category = MyLogger.LogCategory.ClearHistory, enabled = false },
            new CategorySettingEntry { category = MyLogger.LogCategory.UI, enabled = false },
            new CategorySettingEntry { category = MyLogger.LogCategory.Emotion, enabled = false },
            new CategorySettingEntry { category = MyLogger.LogCategory.Default, enabled = false }
        };

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[MyLoggerConfigurator] Инициализация...");

                // ВРЕМЕННО: Принудительно включаем все логи для диагностики
                MyLogger.IsDebugLoggingEnabled = true;
                MyLogger.IsWarningLoggingEnabled = true;
                MyLogger.IsErrorLoggingEnabled = true;
                foreach (MyLogger.LogCategory category in Enum.GetValues(typeof(MyLogger.LogCategory)))
                {
                    MyLogger.SetCategoryEnabled(category, true);
                }
                Debug.Log("[MyLoggerConfigurator] ВРЕМЕННО: Все логи принудительно включены для диагностики.");

                ApplyLogSettings(); // Теперь применяем настройки из инспектора (они могут снова что-то отключить)
            }
            else
            {
                Debug.Log("[MyLoggerConfigurator] Дубликат уничтожен");
                Destroy(gameObject);
            }
        }

        void OnValidate()
        {
            if (Application.isPlaying && _instance == this)
            {
                Debug.Log("[MyLoggerConfigurator] Применяю изменения из инспектора");
                ApplyLogSettings();
            }
        }

        /// <summary>
        /// Applies the configured log settings to the static MyLogger.
        /// Can also be called manually if needed, e.g., after changing settings at runtime via another script.
        /// </summary>
        [ContextMenu("Apply Log Settings Now")]
        public void ApplyLogSettings()
        {
            MyLogger.IsDebugLoggingEnabled = _enableDebugLogging;
            MyLogger.IsWarningLoggingEnabled = _enableWarningLogging;
            MyLogger.IsErrorLoggingEnabled = _enableErrorLogging;

            foreach (var setting in _categorySettings)
            {
                MyLogger.SetCategoryEnabled(setting.category, setting.enabled);
            }
            
            // Логируем только факт применения настроек без спама
            Debug.Log($"[MyLoggerConfigurator] Настройки логгера применены: Debug={_enableDebugLogging}, Warning={_enableWarningLogging}, Error={_enableErrorLogging}");
        }

        [ContextMenu("Enable Firebase Debug Mode")]
        public void EnableFirebaseDebugMode()
        {
            MyLogger.EnableFirebaseDebugMode();
            UpdateCategorySettingsFromMyLogger();
            Debug.Log("[MyLoggerConfigurator] Включен режим отладки Firebase/синхронизации");
        }

        [ContextMenu("Enable UI Debug Mode")]
        public void EnableUIDebugMode()
        {
            MyLogger.EnableUIDebugMode();
            UpdateCategorySettingsFromMyLogger();
            Debug.Log("[MyLoggerConfigurator] Включен режим отладки UI");
        }

        [ContextMenu("Disable All Debug Logs")]
        public void DisableAllDebugLogs()
        {
            MyLogger.DisableAllDebugLogs();
            UpdateCategorySettingsFromMyLogger();
            Debug.Log("[MyLoggerConfigurator] Все отладочные логи отключены");
        }

        /// <summary>
        /// Обновляет настройки категорий в инспекторе на основе текущего состояния MyLogger
        /// </summary>
        private void UpdateCategorySettingsFromMyLogger()
        {
            foreach (var setting in _categorySettings)
            {
                setting.enabled = MyLogger.IsCategoryEnabled(setting.category);
            }
        }
    }
} 