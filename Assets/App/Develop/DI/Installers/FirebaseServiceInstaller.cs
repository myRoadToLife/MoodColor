using System;
using System.Collections.Generic;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Firebase.Auth.Services;
using App.Develop.CommonServices.Firebase.Common.Cache;
using App.Develop.CommonServices.Firebase.Database.Services;
using App.Develop.CommonServices.Firebase.Analytics.Services;
using App.Develop.CommonServices.Firebase.Messaging.Services;
using App.Develop.CommonServices.Firebase.RemoteConfig.Services;
using App.Develop.CommonServices.Firebase;
using App.Develop.CommonServices.Firebase.Auth;
using App.Develop.DI;
using App.Develop.DI.Installers;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;
using App.Develop.Utils.Logging;

namespace App.Develop.DI.Installers
{
    /// <summary>
    /// Инсталлятор сервисов Firebase для внедрения зависимостей
    /// </summary>
    public class FirebaseServiceInstaller : IServiceRegistrator
    {
        /// <summary>
        /// Регистрирует все сервисы Firebase в контейнере
        /// </summary>
        /// <param name="container">Контейнер внедрения зависимостей</param>
        public void RegisterServices(DIContainer container)
        {
            try
            {
                MyLogger.Log("🔧 Регистрация Firebase Services...", MyLogger.LogCategory.Bootstrap);

                // Регистрируем сервисы базы данных
                RegisterDatabaseServices(container);

                // Регистрируем остальные сервисы
                RegisterAuthServices(container);
                RegisterAnalyticsServices(container);
                RegisterMessagingServices(container);
                RegisterRemoteConfigServices(container);

                // Регистрируем главный фасад в последнюю очередь,
                // так как он зависит от всех остальных сервисов
                RegisterFirebaseServiceFacade(container);

                MyLogger.Log("✅ Firebase Services зарегистрированы", MyLogger.LogCategory.Bootstrap);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при регистрации Firebase сервисов: {ex.Message}\n{ex.StackTrace}", MyLogger.LogCategory.Bootstrap);
                throw;
            }
        }

        /// <summary>
        /// Регистрирует сервисы базы данных Firebase
        /// </summary>
        private void RegisterDatabaseServices(DIContainer container)
        {
            try
            {
                // Регистрируем специализированные сервисы через фабричные методы
                container.RegisterAsSingle<IUserProfileDatabaseService>(c =>
                    new UserProfileDatabaseService(
                        c.Resolve<DatabaseReference>(),
                        c.Resolve<FirebaseCacheManager>(),
                        c.Resolve<DataValidationService>()
                    )
                ).NonLazy();

                container.RegisterAsSingle<IJarDatabaseService>(c =>
                    new JarDatabaseService(
                        c.Resolve<DatabaseReference>(),
                        c.Resolve<FirebaseCacheManager>(),
                        c.Resolve<DataValidationService>()
                    )
                ).NonLazy();

                container.RegisterAsSingle<IGameDataDatabaseService>(c =>
                    new GameDataDatabaseService(
                        c.Resolve<DatabaseReference>(),
                        c.Resolve<FirebaseCacheManager>(),
                        c.Resolve<DataValidationService>()
                    )
                ).NonLazy();

                container.RegisterAsSingle<ISessionManagementService>(c =>
                    new SessionManagementService(
                        c.Resolve<DatabaseReference>(),
                        c.Resolve<FirebaseCacheManager>(),
                        c.Resolve<DataValidationService>()
                    )
                ).NonLazy();

                container.RegisterAsSingle<IBackupDatabaseService>(c =>
                    new BackupDatabaseService(
                        c.Resolve<DatabaseReference>(),
                        c.Resolve<FirebaseCacheManager>(),
                        c.Resolve<DataValidationService>()
                    )
                ).NonLazy();

                container.RegisterAsSingle<IEmotionDatabaseService>(c =>
                    new EmotionDatabaseService(
                        c.Resolve<DatabaseReference>(),
                        c.Resolve<FirebaseCacheManager>(),
                        c.Resolve<DataValidationService>()
                    )
                ).NonLazy();

                container.RegisterAsSingle<IRegionalDatabaseService>(c =>
                    new RegionalDatabaseService(
                        c.Resolve<DatabaseReference>(),
                        c.Resolve<FirebaseCacheManager>(),
                        c.Resolve<DataValidationService>()
                    )
                ).NonLazy();

                // Регистрируем фасад, который реализует старый интерфейс
                container.RegisterAsSingle<IDatabaseService>(c =>
                    new DatabaseServiceFacade(
                        c.Resolve<DatabaseReference>(),
                        c.Resolve<FirebaseCacheManager>(),
                        c.Resolve<DataValidationService>(),
                        (UserProfileDatabaseService)c.Resolve<IUserProfileDatabaseService>(),
                        (JarDatabaseService)c.Resolve<IJarDatabaseService>(),
                        (GameDataDatabaseService)c.Resolve<IGameDataDatabaseService>(),
                        (SessionManagementService)c.Resolve<ISessionManagementService>(),
                        (BackupDatabaseService)c.Resolve<IBackupDatabaseService>(),
                        (EmotionDatabaseService)c.Resolve<IEmotionDatabaseService>(),
                        (RegionalDatabaseService)c.Resolve<IRegionalDatabaseService>()
                    )
                ).NonLazy();

                // Регистрируем EmotionHistoryCache
                container.RegisterAsSingle<EmotionHistoryCache>(c =>
                    new EmotionHistoryCache(c.Resolve<FirebaseCacheManager>())
                ).NonLazy();

                MyLogger.Log("✅ Database Services успешно зарегистрированы", MyLogger.LogCategory.Bootstrap);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при регистрации Database сервисов: {ex.Message}", MyLogger.LogCategory.Bootstrap);
                throw;
            }
        }

        /// <summary>
        /// Регистрирует сервисы аутентификации Firebase
        /// </summary>
        private void RegisterAuthServices(DIContainer container)
        {
            try
            {
                // Сервис валидации
                container.RegisterAsSingle<ValidationService>(c => new ValidationService()).NonLazy();

                // Создаем сервисы аутентификации через фабричные методы
                container.RegisterAsSingle<IAuthService>(c =>
                    new AuthService(
                        c.Resolve<FirebaseAuth>(),
                        c.Resolve<IDatabaseService>(),
                        c.Resolve<ValidationService>()
                    )
                ).NonLazy();

                // AuthStateService создаем после регистрации authService
                container.RegisterAsSingle<IAuthStateService>(c =>
                    new AuthStateService(
                        c.Resolve<FirebaseAuth>(),
                        c.Resolve<IAuthService>()
                    )
                ).NonLazy();

                // Регистрируем AuthManager как обычный сервис, а не MonoBehaviour
                container.RegisterAsSingle<IAuthManager>(c =>
                {
                    var authManager = new AuthManager();
                    authManager.Inject(c);
                    return authManager;
                }).NonLazy();

                // Сервис профиля пользователя
                container.RegisterAsSingle<UserProfileService>(c =>
                    new UserProfileService(
                        c.Resolve<IDatabaseService>()
                    )
                ).NonLazy();

                MyLogger.Log("✅ Auth Services успешно зарегистрированы", MyLogger.LogCategory.Bootstrap);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при регистрации Auth сервисов: {ex.Message}", MyLogger.LogCategory.Bootstrap);
                throw;
            }
        }

        /// <summary>
        /// Регистрирует сервисы Firebase Analytics
        /// </summary>
        private void RegisterAnalyticsServices(DIContainer container)
        {
            try
            {
                var analyticsService = new FirebaseAnalyticsService();
                container.RegisterAsSingle<IFirebaseAnalyticsService>(c => analyticsService).NonLazy();

                MyLogger.Log("✅ Analytics Service успешно зарегистрирован", MyLogger.LogCategory.Bootstrap);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при регистрации Analytics сервиса: {ex.Message}", MyLogger.LogCategory.Bootstrap);
                throw;
            }
        }

        /// <summary>
        /// Регистрирует сервисы Firebase Cloud Messaging
        /// </summary>
        private void RegisterMessagingServices(DIContainer container)
        {
            try
            {
                var messagingService = new FirebaseMessagingService();
                container.RegisterAsSingle<IFirebaseMessagingService>(c => messagingService).NonLazy();

                // Инициализируем сервис
                messagingService.Initialize();

                MyLogger.Log("✅ Messaging Service успешно зарегистрирован", MyLogger.LogCategory.Bootstrap);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при регистрации Messaging сервиса: {ex.Message}", MyLogger.LogCategory.Bootstrap);
                throw;
            }
        }

        /// <summary>
        /// Регистрирует сервисы Firebase Remote Config
        /// </summary>
        private void RegisterRemoteConfigServices(DIContainer container)
        {
            try
            {
                var remoteConfigService = new FirebaseRemoteConfigService();
                container.RegisterAsSingle<IFirebaseRemoteConfigService>(c => remoteConfigService).NonLazy();

                // Инициализируем сервис с значениями по умолчанию
                var defaultValues = new Dictionary<string, object>
                {
                    // Здесь можно задать значения по умолчанию
                    { "sync_interval_seconds", 3600 },
                    { "analytics_enabled", true },
                    { "debug_logging_enabled", false }
                };

                remoteConfigService.Initialize(defaultValues);

                // Асинхронно загружаем конфигурацию после инициализации приложения
                // Только если это не режим разработки или если явно разрешено
                if (Application.isPlaying && ShouldFetchRemoteConfig())
                {
                    remoteConfigService.FetchAndActivateAsync().ConfigureAwait(false);
                }

                MyLogger.Log("✅ Remote Config Service успешно зарегистрирован", MyLogger.LogCategory.Bootstrap);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при регистрации Remote Config сервиса: {ex.Message}", MyLogger.LogCategory.Bootstrap);
                throw;
            }
        }

        /// <summary>
        /// Определяет, следует ли загружать Remote Config
        /// </summary>
        private bool ShouldFetchRemoteConfig()
        {
            // В режиме разработки можно отключить Remote Config для ускорения запуска
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            // Можно добавить дополнительную логику или настройки здесь
            // Например, проверить PlayerPrefs или конфигурационный файл
            bool disableRemoteConfigInDev = UnityEngine.PlayerPrefs.GetInt("DisableRemoteConfigInDev", 0) == 1;
            if (disableRemoteConfigInDev)
            {
                MyLogger.Log("🔧 [RemoteConfig] Отключен в режиме разработки", MyLogger.LogCategory.Bootstrap);
                return false;
            }
#endif

            return true;
        }

        /// <summary>
        /// Регистрирует главный фасад Firebase сервисов
        /// </summary>
        private void RegisterFirebaseServiceFacade(DIContainer container)
        {
            try
            {
                container.RegisterAsSingle<IFirebaseServiceFacade>(c =>
                    new FirebaseServiceFacade(
                        c.Resolve<IDatabaseService>(),
                        c.Resolve<IAuthService>(),
                        c.Resolve<IAuthStateService>(),
                        c.Resolve<IFirebaseAnalyticsService>(),
                        c.Resolve<IFirebaseMessagingService>(),
                        c.Resolve<IFirebaseRemoteConfigService>()
                    )
                ).NonLazy();

                MyLogger.Log("✅ Firebase Service Facade успешно зарегистрирован", MyLogger.LogCategory.Bootstrap);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при регистрации Firebase Service Facade: {ex.Message}", MyLogger.LogCategory.Bootstrap);
                throw;
            }
        }
    }
}