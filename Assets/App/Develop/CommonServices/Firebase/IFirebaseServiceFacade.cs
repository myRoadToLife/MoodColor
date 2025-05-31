using System;
using App.Develop.CommonServices.Firebase.Auth.Services;
using App.Develop.CommonServices.Firebase.Database.Services;
using App.Develop.CommonServices.Firebase.Analytics.Services;
using App.Develop.CommonServices.Firebase.Messaging.Services;
using App.Develop.CommonServices.Firebase.RemoteConfig.Services;

namespace App.Develop.CommonServices.Firebase
{
    /// <summary>
    /// Интерфейс главного фасада для сервисов Firebase, обеспечивающий единую точку доступа
    /// ко всем сервисам Firebase в приложении
    /// </summary>
    public interface IFirebaseServiceFacade : IDisposable
    {
        /// <summary>
        /// Сервис Firebase Database
        /// </summary>
        IDatabaseService Database { get; }
        
        /// <summary>
        /// Сервис аутентификации Firebase
        /// </summary>
        IAuthService Auth { get; }
        
        /// <summary>
        /// Сервис состояния аутентификации Firebase
        /// </summary>
        IAuthStateService AuthState { get; }
        
        /// <summary>
        /// Сервис Firebase Analytics
        /// </summary>
        IFirebaseAnalyticsService Analytics { get; }
        
        /// <summary>
        /// Сервис Firebase Cloud Messaging
        /// </summary>
        IFirebaseMessagingService Messaging { get; }
        
        /// <summary>
        /// Сервис Firebase Remote Config
        /// </summary>
        IFirebaseRemoteConfigService RemoteConfig { get; }
    }
} 