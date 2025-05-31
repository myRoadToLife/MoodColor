using System.Collections.Generic;
using Firebase.Analytics;

namespace App.Develop.CommonServices.Firebase.Analytics.Services
{
    /// <summary>
    /// Интерфейс сервиса для работы с Firebase Analytics
    /// </summary>
    public interface IFirebaseAnalyticsService
    {
        /// <summary>
        /// Логирует событие с указанным именем
        /// </summary>
        /// <param name="eventName">Имя события</param>
        void LogEvent(string eventName);
        
        /// <summary>
        /// Логирует событие с указанным именем и параметром
        /// </summary>
        /// <param name="eventName">Имя события</param>
        /// <param name="parameterName">Имя параметра</param>
        /// <param name="parameterValue">Значение параметра</param>
        void LogEvent(string eventName, string parameterName, string parameterValue);
        
        /// <summary>
        /// Логирует событие с указанным именем и набором параметров
        /// </summary>
        /// <param name="eventName">Имя события</param>
        /// <param name="parameters">Список параметров события</param>
        void LogEvent(string eventName, params Parameter[] parameters);
        
        /// <summary>
        /// Устанавливает свойство пользователя
        /// </summary>
        /// <param name="propertyName">Имя свойства</param>
        /// <param name="propertyValue">Значение свойства</param>
        void SetUserProperty(string propertyName, string propertyValue);
        
        /// <summary>
        /// Устанавливает ID пользователя для аналитики
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        void SetUserId(string userId);
        
        /// <summary>
        /// Устанавливает текущий экран
        /// </summary>
        /// <param name="screenName">Имя экрана</param>
        /// <param name="screenClass">Класс экрана</param>
        void SetCurrentScreen(string screenName, string screenClass);
    }
} 