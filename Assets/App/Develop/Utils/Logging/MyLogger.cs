using UnityEngine;
using System.Collections.Generic;

namespace App.Develop.Utils.Logging
{
    /// <summary>
    /// Простая система логирования с контролем категорий
    /// Используйте MyLogger.Log(message, LogCategory.UI) для логирования
    /// </summary>
    public static class MyLogger
    {
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
            ClearHistory,
            Regional,
            Session
        }

        #region Настройки логирования

        /// <summary>
        /// Включить/отключить Debug.Log сообщения
        /// </summary>
        public static bool IsDebugLoggingEnabled = true;

        /// <summary>
        /// Включить/отключить Debug.LogWarning сообщения
        /// </summary>
        public static bool IsWarningLoggingEnabled = true;

        /// <summary>
        /// Включить/отключить Debug.LogError сообщения
        /// </summary>
        public static bool IsErrorLoggingEnabled = true;

        /// <summary>
        /// Словарь для включения/отключения конкретных категорий
        /// </summary>
        private static readonly Dictionary<LogCategory, bool> _categoryEnabled = new Dictionary<LogCategory, bool>
        {
            { LogCategory.Default, true },
            { LogCategory.Sync, false },
            { LogCategory.UI, false },
            { LogCategory.Network, false },
            { LogCategory.Firebase, true },
            { LogCategory.Editor, true },
            { LogCategory.Gameplay, false },
            { LogCategory.Bootstrap, true },
            { LogCategory.Emotion, false },
            { LogCategory.ClearHistory, false },
            { LogCategory.Regional, true },
            { LogCategory.Session, false }
        };

        #endregion

        #region Основные методы логирования

        /// <summary>
        /// Логирует информационное сообщение
        /// </summary>
        public static void Log(string message, LogCategory category = LogCategory.Default)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (IsDebugLoggingEnabled && IsCategoryEnabled(category))
                Debug.Log($"[{category}] {message}");
#endif
        }

        /// <summary>
        /// Логирует информационное сообщение с контекстом
        /// </summary>
        public static void Log(string message, Object context, LogCategory category = LogCategory.Default)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (IsDebugLoggingEnabled && IsCategoryEnabled(category))
                Debug.Log($"[{category}] {message}", context);
#endif
        }

        /// <summary>
        /// Логирует предупреждение
        /// </summary>
        public static void LogWarning(string message, LogCategory category = LogCategory.Default)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (IsWarningLoggingEnabled && IsCategoryEnabled(category))
                Debug.LogWarning($"[{category}] {message}");
#endif
        }

        /// <summary>
        /// Логирует предупреждение с контекстом
        /// </summary>
        public static void LogWarning(string message, Object context, LogCategory category = LogCategory.Default)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (IsWarningLoggingEnabled && IsCategoryEnabled(category))
                Debug.LogWarning($"[{category}] {message}", context);
#endif
        }

        /// <summary>
        /// Логирует ошибку (всегда работает, даже в релизе)
        /// </summary>
        public static void LogError(string message, LogCategory category = LogCategory.Default)
        {
            if (IsErrorLoggingEnabled && IsCategoryEnabled(category))
                Debug.LogError($"[{category}] {message}");
        }

        /// <summary>
        /// Логирует ошибку с контекстом (всегда работает, даже в релизе)
        /// </summary>
        public static void LogError(string message, Object context, LogCategory category = LogCategory.Default)
        {
            if (IsErrorLoggingEnabled && IsCategoryEnabled(category))
                Debug.LogError($"[{category}] {message}", context);
        }

        /// <summary>
        /// Логирует исключение (всегда работает, даже в релизе)
        /// </summary>
        public static void LogException(System.Exception exception, LogCategory category = LogCategory.Default)
        {
            if (IsErrorLoggingEnabled && IsCategoryEnabled(category))
                Debug.LogException(exception);
        }

        #endregion

        #region Управление категориями

        /// <summary>
        /// Включить/отключить конкретную категорию логирования
        /// </summary>
        public static void SetCategoryEnabled(LogCategory category, bool enabled)
        {
            _categoryEnabled[category] = enabled;
        }

        /// <summary>
        /// Проверить, включена ли категория
        /// </summary>
        public static bool IsCategoryEnabled(LogCategory category)
        {
            return _categoryEnabled.TryGetValue(category, out var enabled) && enabled;
        }

        /// <summary>
        /// Включить все категории
        /// </summary>
        public static void EnableAllCategories()
        {
            var keys = new List<LogCategory>(_categoryEnabled.Keys);
            foreach (var category in keys)
            {
                _categoryEnabled[category] = true;
            }
        }

        /// <summary>
        /// Отключить все категории
        /// </summary>
        public static void DisableAllCategories()
        {
            var keys = new List<LogCategory>(_categoryEnabled.Keys);
            foreach (var category in keys)
            {
                _categoryEnabled[category] = false;
            }
        }

        #endregion

        #region Быстрые профили

        /// <summary>
        /// Продакшн режим: только ошибки, минимум категорий
        /// </summary>
        public static void SetProductionMode()
        {
            IsDebugLoggingEnabled = false;
            IsWarningLoggingEnabled = true;
            IsErrorLoggingEnabled = true;

            DisableAllCategories();
            // Оставляем только критически важные категории
            SetCategoryEnabled(LogCategory.Bootstrap, true);
        }

        /// <summary>
        /// Режим разработки: основные категории для отладки
        /// </summary>
        public static void SetDevelopmentMode()
        {
            IsDebugLoggingEnabled = true;
            IsWarningLoggingEnabled = true;
            IsErrorLoggingEnabled = true;

            DisableAllCategories();
            // Включаем полезные для разработки категории
            SetCategoryEnabled(LogCategory.Bootstrap, true);
            SetCategoryEnabled(LogCategory.Firebase, true);
            SetCategoryEnabled(LogCategory.Regional, true);
            SetCategoryEnabled(LogCategory.Editor, true);
            SetCategoryEnabled(LogCategory.Session, true);
        }

        /// <summary>
        /// Отладочный режим: все логи включены
        /// </summary>
        public static void SetDebugMode()
        {
            IsDebugLoggingEnabled = true;
            IsWarningLoggingEnabled = true;
            IsErrorLoggingEnabled = true;

            EnableAllCategories();
        }

        /// <summary>
        /// Режим отладки Firebase и синхронизации
        /// </summary>
        public static void EnableFirebaseDebugMode()
        {
            IsDebugLoggingEnabled = true;
            IsWarningLoggingEnabled = true;
            IsErrorLoggingEnabled = true;

            DisableAllCategories();
            SetCategoryEnabled(LogCategory.Firebase, true);
            SetCategoryEnabled(LogCategory.Sync, true);
            SetCategoryEnabled(LogCategory.Bootstrap, true);
            SetCategoryEnabled(LogCategory.Session, true);
        }

        /// <summary>
        /// Режим отладки UI
        /// </summary>
        public static void EnableUIDebugMode()
        {
            IsDebugLoggingEnabled = true;
            IsWarningLoggingEnabled = true;
            IsErrorLoggingEnabled = true;

            DisableAllCategories();
            SetCategoryEnabled(LogCategory.UI, true);
            SetCategoryEnabled(LogCategory.Bootstrap, true);
        }

        /// <summary>
        /// Только операции с сессиями пользователя
        /// </summary>
        public static void EnableSessionDebugMode()
        {
            IsDebugLoggingEnabled = true;
            IsWarningLoggingEnabled = true;
            IsErrorLoggingEnabled = true;

            DisableAllCategories();
            SetCategoryEnabled(LogCategory.Session, true);
            SetCategoryEnabled(LogCategory.Firebase, true);
            SetCategoryEnabled(LogCategory.Bootstrap, true);
        }

        #endregion

#if UNITY_EDITOR
        /// <summary>
        /// Только для редактора - всегда логирует
        /// </summary>
        public static void EditorLog(string message)
        {
            Debug.Log($"[EDITOR] {message}");
        }

        /// <summary>
        /// Только для редактора - всегда логирует с контекстом
        /// </summary>
        public static void EditorLog(string message, Object context)
        {
            Debug.Log($"[EDITOR] {message}", context);
        }

        /// <summary>
        /// Только для редактора - всегда логирует предупреждение
        /// </summary>
        public static void EditorLogWarning(string message)
        {
            Debug.LogWarning($"[EDITOR] {message}");
        }

        /// <summary>
        /// Только для редактора - всегда логирует ошибку
        /// </summary>
        public static void EditorLogError(string message)
        {
            Debug.LogError($"[EDITOR] {message}");
        }
#endif

        #region Быстрая настройка при запуске

        /// <summary>
        /// Автоматическая настройка профиля при запуске игры
        /// Вызывайте в Awake() вашего стартового скрипта
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoSetupProfile()
        {
#if UNITY_EDITOR
            SetDevelopmentMode();
            Log("MyLogger: Development mode (Editor)", LogCategory.Bootstrap);
#elif DEVELOPMENT_BUILD
            SetDevelopmentMode();
            Log("MyLogger: Development mode (Dev Build)", LogCategory.Bootstrap);
#else
            SetProductionMode();
            Log("MyLogger: Production mode", LogCategory.Bootstrap);
#endif
        }

        #endregion
    }
}