using UnityEngine;
using System.Collections.Generic;

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
        [SerializeField] private bool _enableDebugLogging = true;
        [Tooltip("Enable/Disable Debug.LogWarning calls via MyLogger.LogWarning")]
        [SerializeField] private bool _enableWarningLogging = true;
        [Tooltip("Enable/Disable Debug.LogError calls via MyLogger.LogError")]
        [SerializeField] private bool _enableErrorLogging = true;

        [Header("Category Log Levels")]
        [Tooltip("Configure which log categories are enabled. Categories not listed here will retain their default state defined in MyLogger.")]
        [SerializeField] private List<CategorySettingEntry> _categorySettings = new List<CategorySettingEntry>();

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[MyLoggerConfigurator] Инициализация...");
                ApplyLogSettings();
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

            Debug.Log($"[MyLoggerConfigurator] Применены глобальные настройки: Debug={_enableDebugLogging}, Warning={_enableWarningLogging}, Error={_enableErrorLogging}");

            foreach (var setting in _categorySettings)
            {
                MyLogger.SetCategoryEnabled(setting.category, setting.enabled);
                Debug.Log($"[MyLoggerConfigurator] Применены настройки категории: {setting.category} = {(setting.enabled ? "Включено" : "Выключено")}");
            }
            
            // Тестовые логи для проверки
            if (_enableDebugLogging)
                MyLogger.Log("Тестовый лог MyLogger", MyLogger.LogCategory.Default);
            if (_enableWarningLogging)
                MyLogger.LogWarning("Тестовое предупреждение MyLogger", MyLogger.LogCategory.Default);
            if (_enableErrorLogging)
                MyLogger.LogError("Тестовая ошибка MyLogger", MyLogger.LogCategory.Default);
        }
    }
} 