using UnityEngine;

namespace App.Develop.Utils.Logging
{
    /// <summary>
    /// Утилита для логирования с возможностью отключения логов в продакшене
    /// </summary>
    public static class Logger
    {
        // Установите в false, чтобы отключить все логи, кроме ошибок
        private static bool _isDebugLoggingEnabled =
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            true;
#else
            false;
#endif

        // Установите в false, чтобы отключить предупреждения
        private static bool _isWarningLoggingEnabled =
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            true;
#else
            false;
#endif

        // Установите в false, чтобы отключить все ошибки
        private static bool _isErrorLoggingEnabled = true;

        /// <summary>
        /// Логирует информационное сообщение (только в режиме разработки или в development build)
        /// </summary>
        public static void Log(string message)
        {
            if (_isDebugLoggingEnabled)
            {
                Debug.Log(message);
            }
        }

        /// <summary>
        /// Логирует информационное сообщение с указанием контекста (только в режиме разработки или в development build)
        /// </summary>
        public static void Log(string message, Object context)
        {
            if (_isDebugLoggingEnabled)
            {
                Debug.Log(message, context);
            }
        }

        /// <summary>
        /// Логирует предупреждение (только в режиме разработки или в development build)
        /// </summary>
        public static void LogWarning(string message)
        {
            if (_isWarningLoggingEnabled)
            {
                Debug.LogWarning(message);
            }
        }

        /// <summary>
        /// Логирует предупреждение с указанием контекста (только в режиме разработки или в development build)
        /// </summary>
        public static void LogWarning(string message, Object context)
        {
            if (_isWarningLoggingEnabled)
            {
                Debug.LogWarning(message, context);
            }
        }

        /// <summary>
        /// Логирует ошибку (работает всегда, даже в релизных сборках)
        /// </summary>
        public static void LogError(string message)
        {
            if (_isErrorLoggingEnabled)
            {
                Debug.LogError(message);
            }
        }

        /// <summary>
        /// Логирует ошибку с указанием контекста (работает всегда, даже в релизных сборках)
        /// </summary>
        public static void LogError(string message, Object context)
        {
            if (_isErrorLoggingEnabled)
            {
                Debug.LogError(message, context);
            }
        }

        /// <summary>
        /// Логирует исключение 
        /// </summary>
        public static void LogException(System.Exception exception)
        {
            if (_isErrorLoggingEnabled)
            {
                Debug.LogException(exception);
            }
        }
    }
} 