using System;
using App.Develop.CommonServices.Firebase.Auth.Services;
using App.Develop.CommonServices.Firebase.Database.Services;
using App.Develop.CommonServices.Firebase.Analytics.Services;
using App.Develop.CommonServices.Firebase.Messaging.Services;
using App.Develop.CommonServices.Firebase.RemoteConfig.Services;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Firebase
{
    /// <summary>
    /// Главный фасад для сервисов Firebase, обеспечивающий единую точку доступа
    /// ко всем сервисам Firebase в приложении
    /// </summary>
    public class FirebaseServiceFacade : IFirebaseServiceFacade
    {
        #region Private Fields
        private readonly IDatabaseService _databaseService;
        private readonly IAuthService _authService;
        private readonly IAuthStateService _authStateService;
        private readonly IFirebaseAnalyticsService _analyticsService;
        private readonly IFirebaseMessagingService _messagingService;
        private readonly IFirebaseRemoteConfigService _remoteConfigService;
        #endregion

        #region Properties
        /// <summary>
        /// Сервис Firebase Database
        /// </summary>
        public IDatabaseService Database => _databaseService;
        
        /// <summary>
        /// Сервис аутентификации Firebase
        /// </summary>
        public IAuthService Auth => _authService;
        
        /// <summary>
        /// Сервис состояния аутентификации Firebase
        /// </summary>
        public IAuthStateService AuthState => _authStateService;
        
        /// <summary>
        /// Сервис Firebase Analytics
        /// </summary>
        public IFirebaseAnalyticsService Analytics => _analyticsService;
        
        /// <summary>
        /// Сервис Firebase Cloud Messaging
        /// </summary>
        public IFirebaseMessagingService Messaging => _messagingService;
        
        /// <summary>
        /// Сервис Firebase Remote Config
        /// </summary>
        public IFirebaseRemoteConfigService RemoteConfig => _remoteConfigService;
        #endregion

        #region Constructor
        /// <summary>
        /// Создает новый экземпляр фасада сервисов Firebase
        /// </summary>
        /// <param name="databaseService">Сервис базы данных Firebase</param>
        /// <param name="authService">Сервис аутентификации Firebase</param>
        /// <param name="authStateService">Сервис состояния аутентификации Firebase</param>
        /// <param name="analyticsService">Сервис Firebase Analytics</param>
        /// <param name="messagingService">Сервис Firebase Cloud Messaging</param>
        /// <param name="remoteConfigService">Сервис Firebase Remote Config</param>
        public FirebaseServiceFacade(
            IDatabaseService databaseService,
            IAuthService authService,
            IAuthStateService authStateService,
            IFirebaseAnalyticsService analyticsService,
            IFirebaseMessagingService messagingService,
            IFirebaseRemoteConfigService remoteConfigService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _authStateService = authStateService ?? throw new ArgumentNullException(nameof(authStateService));
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _remoteConfigService = remoteConfigService ?? throw new ArgumentNullException(nameof(remoteConfigService));
            
            // Настраиваем связи между сервисами при необходимости
            SetupServiceConnections();
            
            MyLogger.Log("✅ FirebaseServiceFacade инициализирован", MyLogger.LogCategory.Firebase);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Настраивает связи между сервисами
        /// </summary>
        private void SetupServiceConnections()
        {
            // Подписываемся на изменение состояния аутентификации для обновления UserId в Database
            if (_authStateService != null && _databaseService != null)
            {
                _authStateService.AuthStateChanged += user =>
                {
                    if (user != null)
                    {
                        MyLogger.Log($"🔑 [FirebaseServiceFacade] Обновляем UserId в сервисах: {user.UserId}", MyLogger.LogCategory.Firebase);
                        _databaseService.UpdateUserId(user.UserId);
                        
                        if (_analyticsService != null)
                        {
                            _analyticsService.SetUserId(user.UserId);
                        }
                    }
                    else
                    {
                        MyLogger.Log("🔑 [FirebaseServiceFacade] Очищаем UserId в сервисах", MyLogger.LogCategory.Firebase);
                        _databaseService.UpdateUserId(null);
                        
                        if (_analyticsService != null)
                        {
                            _analyticsService.SetUserId(null);
                        }
                    }
                };
            }
        }
        #endregion

        #region IDisposable Implementation
        /// <summary>
        /// Освобождает ресурсы всех сервисов
        /// </summary>
        public void Dispose()
        {
            try
            {
                MyLogger.Log("Disposing FirebaseServiceFacade...", MyLogger.LogCategory.Firebase);
                
                // Освобождаем ресурсы сервисов, которые реализуют IDisposable
                (_databaseService as IDisposable)?.Dispose();
                (_authService as IDisposable)?.Dispose();
                (_authStateService as IDisposable)?.Dispose();
                (_analyticsService as IDisposable)?.Dispose();
                (_messagingService as IDisposable)?.Dispose();
                (_remoteConfigService as IDisposable)?.Dispose();
                
                MyLogger.Log("✅ FirebaseServiceFacade: все ресурсы освобождены", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при освобождении ресурсов FirebaseServiceFacade: {ex.Message}\n{ex.StackTrace}", MyLogger.LogCategory.Firebase);
            }
        }
        #endregion
    }
} 