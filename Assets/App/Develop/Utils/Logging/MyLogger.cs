using UnityEngine;

namespace App.Develop.Utils.Logging
{
    /// <summary>
    /// Утилита для логирования с возможностью отключения логов в продакшене
    /// </summary>
    public static class MyLogger
    {
        // Приватные поля для свойств
        private static bool _isDebugLoggingEnabled = false;
        private static bool _isWarningLoggingEnabled = true;
        private static bool _isErrorLoggingEnabled = true;

        public enum LogCategory
        {
            Default,
            Sync,
            UI,
            Network,
            Firebase,
            Editor,
            Gameplay,
            Bootstrap,
            Emotion,
            // ... добавляй свои
        }

        private static System.Collections.Generic.Dictionary<LogCategory, bool> _categoryEnabled = new System.Collections.Generic.Dictionary<LogCategory, bool>
        {
            { LogCategory.Default, false },     // Отключено по умолчанию
            { LogCategory.Sync, true },         // Включено - важно для отладки
            { LogCategory.UI, false },          // Отключено по умолчанию
            { LogCategory.Network, true },      // Включено - важно для отладки
            { LogCategory.Firebase, true },     // Включено - важно для отладки
            { LogCategory.Editor, false },      // Отключено по умолчанию
            { LogCategory.Gameplay, false },    // Отключено по умолчанию
            { LogCategory.Bootstrap, true },    // Включено - важно для отладки
            { LogCategory.Emotion, true },      // Включено - важно для отладки
        };

        public static void SetCategoryEnabled(LogCategory category, bool enabled)
        {
            _categoryEnabled[category] = enabled;
        }

        public static bool IsCategoryEnabled(LogCategory category)
        {
            return _categoryEnabled.TryGetValue(category, out var enabled) && enabled;
        }

        /// <summary>
        /// Логирует информационное сообщение (только в режиме разработки или в development build)
        /// </summary>
        public static void Log(string message, LogCategory category = LogCategory.Default)
        {
            if (_isDebugLoggingEnabled && IsCategoryEnabled(category))
                Debug.Log($"[{category}] {message}");
        }

        /// <summary>
        /// Логирует информационное сообщение с указанием контекста (только в режиме разработки или в development build)
        /// </summary>
        public static void Log(string message, Object context, LogCategory category = LogCategory.Default)
        {
            if (_isDebugLoggingEnabled && IsCategoryEnabled(category))
                Debug.Log($"[{category}] {message}", context);
        }

        /// <summary>
        /// Логирует предупреждение (только в режиме разработки или в development build)
        /// </summary>
        public static void LogWarning(string message, LogCategory category = LogCategory.Default)
        {
            if (_isWarningLoggingEnabled && IsCategoryEnabled(category))
                Debug.LogWarning($"[{category}] {message}");
        }

        /// <summary>
        /// Логирует предупреждение с указанием контекста (только в режиме разработки или в development build)
        /// </summary>
        public static void LogWarning(string message, Object context, LogCategory category = LogCategory.Default)
        {
            if (_isWarningLoggingEnabled && IsCategoryEnabled(category))
                Debug.LogWarning($"[{category}] {message}", context);
        }

        /// <summary>
        /// Логирует ошибку (работает всегда, даже в релизных сборках)
        /// </summary>
        public static void LogError(string message, LogCategory category = LogCategory.Default)
        {
            if (_isErrorLoggingEnabled && IsCategoryEnabled(category))
                Debug.LogError($"[{category}] {message}");
        }

        /// <summary>
        /// Логирует ошибку с указанием контекста (работает всегда, даже в релизных сборках)
        /// </summary>
        public static void LogError(string message, Object context, LogCategory category = LogCategory.Default)
        {
            if (_isErrorLoggingEnabled && IsCategoryEnabled(category))
                Debug.LogError($"[{category}] {message}", context);
        }

        /// <summary>
        /// Логирует исключение 
        /// </summary>
        public static void LogException(System.Exception exception, LogCategory category = LogCategory.Default)
        {
            if (_isErrorLoggingEnabled && IsCategoryEnabled(category))
                Debug.LogException(exception);
        }

#if UNITY_EDITOR
        public static void EditorLog(string message)
        {
            Debug.Log(message);
        }

        public static void EditorLog(string message, Object context)
        {
            Debug.Log(message, context);
        }

        public static void EditorLogWarning(string message)
        {
            Debug.LogWarning(message);
        }

        public static void EditorLogWarning(string message, Object context)
        {
            Debug.LogWarning(message, context);
        }

        public static void EditorLogError(string message)
        {
            Debug.LogError(message);
        }

        public static void EditorLogError(string message, Object context)
        {
            Debug.LogError(message, context);
        }
#endif

        public static bool IsDebugLoggingEnabled
        {
            get => _isDebugLoggingEnabled;
            set => _isDebugLoggingEnabled = value;
        }

        public static bool IsWarningLoggingEnabled
        {
            get => _isWarningLoggingEnabled;
            set => _isWarningLoggingEnabled = value;
        }

        public static bool IsErrorLoggingEnabled
        {
            get => _isErrorLoggingEnabled;
            set => _isErrorLoggingEnabled = value;
        }
    }
} 