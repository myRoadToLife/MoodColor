using System;
using System.Collections.Generic;
using Firebase.Analytics;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Firebase.Analytics.Services
{
    /// <summary>
    /// Сервис для работы с Firebase Analytics
    /// </summary>
    public class FirebaseAnalyticsService : IFirebaseAnalyticsService
    {
        #region Private Fields
        private bool _isInitialized;
        #endregion

        #region Constructor
        /// <summary>
        /// Создает новый экземпляр сервиса Firebase Analytics
        /// </summary>
        public FirebaseAnalyticsService()
        {
            try
            {
                // Проверка доступности Firebase Analytics путем вызова безопасного метода
                // Если Firebase не инициализирован, будет выброшено исключение
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                _isInitialized = true;
                MyLogger.Log("✅ FirebaseAnalyticsService инициализирован", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                MyLogger.LogWarning($"⚠️ FirebaseAnalyticsService не может быть инициализирован: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }
        #endregion

        #region IFirebaseAnalyticsService Implementation
        /// <summary>
        /// Логирует событие с указанным именем
        /// </summary>
        /// <param name="eventName">Имя события</param>
        public void LogEvent(string eventName)
        {
            if (!_isInitialized || string.IsNullOrEmpty(eventName))
            {
                MyLogger.LogWarning($"⚠️ Firebase Analytics не инициализирован или пустое имя события. Событие {eventName} не отправлено", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                FirebaseAnalytics.LogEvent(eventName);
                MyLogger.Log($"📊 [Analytics] Событие: {eventName}", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [Analytics] Ошибка при логировании события {eventName}: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }

        /// <summary>
        /// Логирует событие с указанным именем и параметром
        /// </summary>
        /// <param name="eventName">Имя события</param>
        /// <param name="parameterName">Имя параметра</param>
        /// <param name="parameterValue">Значение параметра</param>
        public void LogEvent(string eventName, string parameterName, string parameterValue)
        {
            if (!_isInitialized || string.IsNullOrEmpty(eventName) || string.IsNullOrEmpty(parameterName))
            {
                MyLogger.LogWarning($"⚠️ Firebase Analytics не инициализирован или неверные параметры. Событие {eventName} не отправлено", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                FirebaseAnalytics.LogEvent(eventName, parameterName, parameterValue);
                MyLogger.Log($"📊 [Analytics] Событие: {eventName}, Параметр: {parameterName}={parameterValue}", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [Analytics] Ошибка при логировании события {eventName}: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }

        /// <summary>
        /// Логирует событие с указанным именем и параметром
        /// </summary>
        /// <param name="eventName">Имя события</param>
        /// <param name="parameterName">Имя параметра</param>
        /// <param name="parameterValue">Значение параметра</param>
        public void LogEvent(string eventName, string parameterName, double parameterValue)
        {
            if (!_isInitialized || string.IsNullOrEmpty(eventName) || string.IsNullOrEmpty(parameterName))
            {
                MyLogger.LogWarning($"⚠️ Firebase Analytics не инициализирован или неверные параметры. Событие {eventName} не отправлено", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                FirebaseAnalytics.LogEvent(eventName, parameterName, parameterValue);
                MyLogger.Log($"📊 [Analytics] Событие: {eventName}, Параметр: {parameterName}={parameterValue}", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [Analytics] Ошибка при логировании события {eventName}: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }

        /// <summary>
        /// Логирует событие с указанным именем и параметром
        /// </summary>
        /// <param name="eventName">Имя события</param>
        /// <param name="parameterName">Имя параметра</param>
        /// <param name="parameterValue">Значение параметра</param>
        public void LogEvent(string eventName, string parameterName, long parameterValue)
        {
            if (!_isInitialized || string.IsNullOrEmpty(eventName) || string.IsNullOrEmpty(parameterName))
            {
                MyLogger.LogWarning($"⚠️ Firebase Analytics не инициализирован или неверные параметры. Событие {eventName} не отправлено", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                FirebaseAnalytics.LogEvent(eventName, parameterName, parameterValue);
                MyLogger.Log($"📊 [Analytics] Событие: {eventName}, Параметр: {parameterName}={parameterValue}", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [Analytics] Ошибка при логировании события {eventName}: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }

        /// <summary>
        /// Логирует событие с указанным именем и параметрами
        /// </summary>
        /// <param name="eventName">Имя события</param>
        /// <param name="parameters">Параметры события</param>
        public void LogEvent(string eventName, Parameter[] parameters)
        {
            if (!_isInitialized || string.IsNullOrEmpty(eventName))
            {
                MyLogger.LogWarning($"⚠️ Firebase Analytics не инициализирован или пустое имя события. Событие {eventName} не отправлено", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                FirebaseAnalytics.LogEvent(eventName, parameters);
                
                string parametersStr = "";
                if (parameters != null && parameters.Length > 0)
                {
                    var paramList = new List<string>();
                    foreach (var p in parameters)
                    {
                        // В Firebase SDK для Unity Parameter не имеет свойств Name и Value,
                        // поэтому просто используем ToString или другой способ идентификации параметра
                        paramList.Add(p.ToString());
                    }
                    parametersStr = string.Join(", ", paramList);
                }
                
                MyLogger.Log($"📊 [Analytics] Событие: {eventName}, Параметры: {parametersStr}", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [Analytics] Ошибка при логировании события {eventName}: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }

        /// <summary>
        /// Устанавливает пользовательское свойство
        /// </summary>
        /// <param name="propertyName">Имя свойства</param>
        /// <param name="propertyValue">Значение свойства</param>
        public void SetUserProperty(string propertyName, string propertyValue)
        {
            if (!_isInitialized || string.IsNullOrEmpty(propertyName))
            {
                MyLogger.LogWarning($"⚠️ Firebase Analytics не инициализирован или пустое имя свойства. Свойство {propertyName} не установлено", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                FirebaseAnalytics.SetUserProperty(propertyName, propertyValue);
                MyLogger.Log($"📊 [Analytics] Свойство пользователя: {propertyName}={propertyValue}", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [Analytics] Ошибка при установке свойства пользователя {propertyName}: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }

        /// <summary>
        /// Устанавливает идентификатор пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        public void SetUserId(string userId)
        {
            if (!_isInitialized)
            {
                MyLogger.LogWarning($"⚠️ Firebase Analytics не инициализирован. ID пользователя не установлен", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                FirebaseAnalytics.SetUserId(userId);
                MyLogger.Log($"📊 [Analytics] ID пользователя установлен: {userId}", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [Analytics] Ошибка при установке ID пользователя: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }

        /// <summary>
        /// Устанавливает текущий экран
        /// </summary>
        /// <param name="screenName">Имя экрана</param>
        /// <param name="screenClass">Класс экрана</param>
        public void SetCurrentScreen(string screenName, string screenClass)
        {
            if (!_isInitialized || string.IsNullOrEmpty(screenName))
            {
                MyLogger.LogWarning($"⚠️ Firebase Analytics не инициализирован или пустое имя экрана. Экран {screenName} не установлен", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                // В Firebase SDK для Unity нужно использовать LogEvent с параметром screen_name
                // вместо SetCurrentScreen, который используется в Android SDK
                FirebaseAnalytics.LogEvent(
                    FirebaseAnalytics.EventScreenView,
                    new Parameter(FirebaseAnalytics.ParameterScreenName, screenName),
                    new Parameter(FirebaseAnalytics.ParameterScreenClass, screenClass ?? "")
                );
                
                MyLogger.Log($"📊 [Analytics] Текущий экран: {screenName} ({screenClass})", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [Analytics] Ошибка при установке текущего экрана {screenName}: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }

        /// <summary>
        /// Сбрасывает все данные аналитики
        /// </summary>
        public void ResetAnalyticsData()
        {
            if (!_isInitialized)
            {
                MyLogger.LogWarning("⚠️ Firebase Analytics не инициализирован. Данные не сброшены", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                FirebaseAnalytics.ResetAnalyticsData();
                MyLogger.Log("📊 [Analytics] данные аналитики сброшены", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [Analytics] Ошибка при сбросе данных аналитики: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }
        #endregion
    }
} 