#define DISABLE_AUTO_ADDRESSABLES_IMPORT

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using Firebase.Auth;
using App.Develop.DI;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.ConfigsManagement;
using App.Develop.CommonServices.UI;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.Firebase.Database.Services;
using App.Develop.CommonServices.Firebase.Database.Validators;
using App.Develop.CommonServices.Firebase.Auth.Services;
using App.Develop.CommonServices.GameSystem;
using App.Develop.CommonServices.CoroutinePerformer;
using App.Develop.CommonServices.DataManagement;
using App.Develop.CommonServices.Firebase.Auth;
using App.Develop.CommonServices.Firebase.Common.Cache;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.CommonServices.LoadingScreen;
using App.Develop.CommonServices.Networking;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.CommonServices.Social;
using App.Develop.Scenes.PersonalAreaScene.Infrastructure;
using App.Develop.Scenes.PersonalAreaScene.Settings;
using App.Develop.Scenes.PersonalAreaScene.UI.Components;
using App.Develop.Scenes.PersonalAreaScene.Handlers;
using App.Develop.Utils.Logging;
using UnityEngine.AddressableAssets;
using App.Develop.DI.Installers;
using App.Develop.Configs;

#if !DISABLE_AUTO_ADDRESSABLES_IMPORT
using UnityEngine.AddressableAssets;
#endif

namespace App.Develop.EntryPoint
{
    /// <summary>
    /// Точка входа в приложение. Отвечает за инициализацию всех основных сервисов.
    /// </summary>
    public class EntryPoint : MonoBehaviour
    {
        [SerializeField] private Bootstrap _appBootstrap;
        [SerializeField] private ApplicationConfig _applicationConfig;
        
        private DIContainer _projectContainer;
        private FirebaseApp _firebaseApp; // Храним ссылку на наш экземпляр Firebase
        private FirebaseDatabase _firebaseDatabase; // Храним ссылку на базу данных

        private void Awake()
        {
            MyLogger.Log("🚀 EntryPoint.Awake() вызван", MyLogger.LogCategory.Bootstrap);
            DontDestroyOnLoad(gameObject); // РАСКОММЕНТИРОВАНО для исправления
            InitializeApplication();
        }

        /// <summary>
        /// Основной метод инициализации приложения
        /// </summary>
        private async void InitializeApplication()
        {
            try
            {
                MyLogger.Log("📦 Инициализация Addressables...", MyLogger.LogCategory.Bootstrap);
                await Addressables.InitializeAsync().Task;
                
                MyLogger.Log("⚙️ Настройка приложения...", MyLogger.LogCategory.Bootstrap);
                SetupAppSettings();
                _projectContainer = new DIContainer();
                InitializeSecureStorage(_projectContainer);
                
                MyLogger.Log("🔧 Регистрация основных сервисов...", MyLogger.LogCategory.Bootstrap);
                await RegisterCoreServices(_projectContainer);
                ShowInitialLoadingScreen();

                MyLogger.Log("🔥 Инициализация Firebase...", MyLogger.LogCategory.Bootstrap);
                if (!await InitFirebaseAsync())
                {
                    MyLogger.LogError("❌ Не удалось инициализировать Firebase", MyLogger.LogCategory.Bootstrap);
                    return;
                }

                MyLogger.Log("🔥 Регистрация Firebase сервисов...", MyLogger.LogCategory.Bootstrap);
                RegisterFirebaseServices();

                MyLogger.Log("🔄 Регистрация сервиса синхронизации с облаком...", MyLogger.LogCategory.Bootstrap);
                _projectContainer.RegisterAsSingle<ICloudSyncService>(c =>
                    new CloudSyncService(
                        c.Resolve<ISaveLoadService>(),
                        c.Resolve<IDatabaseService>()
                    )
                ).NonLazy();

                MyLogger.Log("👤 Регистрация PlayerDataProvider...", MyLogger.LogCategory.Bootstrap);
                _projectContainer.RegisterAsSingle(c =>
                    new PlayerDataProvider(
                        c.Resolve<ISaveLoadService>(),
                        c.Resolve<IConfigsProvider>(),
                        c.Resolve<IDatabaseService>()
                    )
                );

                MyLogger.Log("🎮 Регистрация игровой системы...", MyLogger.LogCategory.Bootstrap);
                RegisterGameSystem(_projectContainer);
                
                MyLogger.Log("📊 Инициализация контейнера и загрузка данных...", MyLogger.LogCategory.Bootstrap);
                await InitializeContainerAndLoadData();
                
                MyLogger.Log("🚀 Запуск Bootstrap...", MyLogger.LogCategory.Bootstrap);
                StartBootstrapProcess();
                
                MyLogger.Log("✅ Приложение инициализировано успешно", MyLogger.LogCategory.Bootstrap);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ КРИТИЧЕСКАЯ ОШИБКА инициализации: {ex}", MyLogger.LogCategory.Bootstrap);
                MyLogger.LogError($"❌ Stack trace: {ex.StackTrace}", MyLogger.LogCategory.Bootstrap);
            }
        }

        /// <summary>
        /// Настройка базовых параметров приложения
        /// </summary>
        private void SetupAppSettings()
        {
            if (_applicationConfig != null)
            {
                QualitySettings.vSyncCount = _applicationConfig.EnableVSync ? 1 : 0;
                Application.targetFrameRate = _applicationConfig.TargetFrameRate;
                
                MyLogger.Log($"⚙️ Настройки приложения: FPS={_applicationConfig.TargetFrameRate}, VSync={_applicationConfig.EnableVSync}", MyLogger.LogCategory.Bootstrap);
            }
            else
            {
                // Fallback значения
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 60;
                MyLogger.LogWarning("ApplicationConfig не назначен, используются значения по умолчанию", MyLogger.LogCategory.Bootstrap);
            }
        }

        /// <summary>
        /// Показывает загрузочный экран
        /// </summary>
        private void ShowInitialLoadingScreen()
        {
            ILoadingScreen loadingScreen = _projectContainer.Resolve<ILoadingScreen>();
            loadingScreen.Show();
        }

        /// <summary>
        /// Инициализирует Firebase и проверяет его доступность
        /// </summary>
        private async Task<bool> InitFirebaseAsync()
        {
            try
            {
                string databaseUrl = _applicationConfig?.DatabaseUrl ?? "https://moodcolor-3ac59-default-rtdb.firebaseio.com/";
                string firebaseAppName = _applicationConfig?.FirebaseAppName ?? "MoodColorApp";
                
                // Удаляем все существующие экземпляры с нашим именем, если они есть
                try
                {
                    var existingApp = FirebaseApp.GetInstance(firebaseAppName);
                    if (existingApp != null)
                    {
                        existingApp.Dispose();
                    }
                }
                catch (Exception)
                {
                    // Если приложение не найдено, это не ошибка
                }

                // Проверяем зависимости
                var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();
                await dependencyTask;

                var dependencyStatus = dependencyTask.Result;

                if (dependencyStatus != DependencyStatus.Available)
                {
                    MyLogger.LogError($"❌ Firebase зависимости недоступны: {dependencyStatus}", MyLogger.LogCategory.Bootstrap);
                    return false;
                }

                // Создаем кастомный экземпляр Firebase с нашим URL
                var options = new Firebase.AppOptions
                {
                    DatabaseUrl = new Uri(databaseUrl)
                };

                _firebaseApp = FirebaseApp.Create(options, firebaseAppName);

                // Создаем экземпляр базы данных с нашим Firebase App и URL
                _firebaseDatabase = FirebaseDatabase.GetInstance(_firebaseApp, databaseUrl);
                _firebaseDatabase.SetPersistenceEnabled(true);

                MyLogger.Log($"✅ Firebase инициализирован: {databaseUrl}", MyLogger.LogCategory.Bootstrap);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ ОШИБКА Firebase инициализации: {ex.Message}", MyLogger.LogCategory.Bootstrap);
                MyLogger.LogError($"❌ Firebase Stack trace: {ex.StackTrace}", MyLogger.LogCategory.Bootstrap);
                return false;
            }
        }

        /// <summary>
        /// Регистрирует сервисы, зависящие от Firebase
        /// </summary>
        private void RegisterFirebaseServices()
        {
            RegisterFirebase(_projectContainer);
            // RegisterAuthServices(_projectContainer); // Уже регистрируется в FirebaseServiceInstaller

            // Регистрация социального сервиса
            _projectContainer.RegisterAsSingle<ISocialService>(container =>
            {
                var socialServiceObject = new GameObject("FirebaseSocialService");
                DontDestroyOnLoad(socialServiceObject);
                var socialService = socialServiceObject.AddComponent<FirebaseSocialService>();

                // Инициализируем сервис с правильными экземплярами Firebase
                socialService.Initialize(
                    container.Resolve<FirebaseDatabase>(),
                    container.Resolve<FirebaseAuth>()
                );

                return socialService;
            }).NonLazy();

            // Подписываемся на изменение состояния аутентификации для обновления UserId в DatabaseService
            var authStateService = _projectContainer.Resolve<IAuthStateService>();
            var databaseService = _projectContainer.Resolve<IDatabaseService>();

            if (authStateService != null && databaseService != null)
            {
                authStateService.AuthStateChanged += (user) =>
                {
                    if (user != null)
                    {
                        MyLogger.Log($"🔑 [AUTH-STATE] 👤 UserID = {user.UserId}. Обновляем DatabaseService.", MyLogger.LogCategory.Firebase);
                        databaseService.UpdateUserId(user.UserId);
                    }
                    else
                    {
                        MyLogger.Log("🔑 [AUTH-STATE] ❌ User is null. Очищаем UserId в DatabaseService.", MyLogger.LogCategory.Firebase);
                        databaseService.UpdateUserId(null); // Очищаем UserId при выходе пользователя
                    }
                };
                MyLogger.Log("🔑 [AUTH-STATE] ✅ Успешно подписались на AuthStateChanged для обновления UserId в DatabaseService.", MyLogger.LogCategory.Firebase);
            }
            else
            {
                MyLogger.LogError("🔑 [AUTH-STATE] ⛔ Не удалось подписаться на AuthStateChanged: authStateService или databaseService is null.", MyLogger.LogCategory.Firebase);
            }
        }

        /// <summary>
        /// Инициализирует контейнер и загружает данные
        /// </summary>
        private async Task InitializeContainerAndLoadData()
        {
            // Сначала инициализируем ConfigsProviderService и EmotionConfigService
            var configsProvider = _projectContainer.Resolve<IConfigsProvider>() as ConfigsProviderService;

            if (configsProvider != null)
            {
                await configsProvider.InitializeAsync();
            }
            else
            {
                MyLogger.LogError("[EntryPoint] ConfigsProviderService не удалось разрезолвить как конкретный тип для InitializeAsync.", MyLogger.LogCategory.Bootstrap);
            }

            var emotionCfgService = _projectContainer.Resolve<EmotionConfigService>();

            if (emotionCfgService != null)
            {
                await emotionCfgService.InitializeAsync();
            }
            else
            {
                MyLogger.LogWarning("[EntryPoint] EmotionConfigService не удалось разрезолвить как конкретный тип для InitializeAsync.", MyLogger.LogCategory.Bootstrap);
            }

            // Затем загружаем PlayerDataProvider ПЕРЕД созданием EmotionService
            var playerDataProviderInstance = _projectContainer.Resolve<PlayerDataProvider>();
            await playerDataProviderInstance.Load(); // Явный вызов Load

            // ТЕПЕРЬ регистрируем EmotionService после загрузки PlayerDataProvider
            _projectContainer.RegisterAsSingle<IEmotionService>(c =>
                new EmotionService(
                    // Используем уже созданный и загруженный экземпляр
                    c.Resolve<PlayerDataProvider>(),
                    c.Resolve<IConfigsProvider>(),
                    c.Resolve<EmotionConfigService>(),
                    c.Resolve<EmotionHistoryCache>(),
                    c.Resolve<IPointsService>(),
                    c.Resolve<ILevelSystem>()
                )
            ).NonLazy();
            _projectContainer.RegisterAsSingle<EmotionService>(c => 
                (EmotionService)c.Resolve<IEmotionService>() // Получаем уже созданный IEmotionService
            ).NonLazy();

                            // MyLogger.Log("✅ EmotionService зарегистрирован (в InitializeContainerAndLoadData).", MyLogger.LogCategory.Bootstrap);
            
            // Загружаем PointsService ПОСЛЕ EmotionService
            var pointsService = _projectContainer.Resolve<IPointsService>();

            if (pointsService != null)
            {
                // IPointsService определяет InitializeAsync(), поэтому можем вызывать его напрямую
                await pointsService.InitializeAsync();
            }
            else
            {
                MyLogger.LogError("[EntryPoint] IPointsService не удалось разрезолвить из контейнера.", MyLogger.LogCategory.Bootstrap);
            }

            await RegisterPersonalAreaServices(_projectContainer);

            // Инициализируем Firebase синхронизацию для EmotionService ПОСЛЕ регистрации всех сервисов
            MyLogger.Log("🔗 [EntryPoint] Начинаем инициализацию Firebase синхронизации для EmotionService...", MyLogger.LogCategory.ClearHistory);
            
            var emotionService = _projectContainer.Resolve<EmotionService>();
            var databaseService = _projectContainer.Resolve<IDatabaseService>();
            var syncService = _projectContainer.Resolve<EmotionSyncService>();
            var connectivityManager = _projectContainer.Resolve<ConnectivityManager>();
            
            MyLogger.Log($"🔍 [EntryPoint] Проверка сервисов: emotionService!=null={emotionService != null}, databaseService!=null={databaseService != null}, syncService!=null={syncService != null}, connectivityManager!=null={connectivityManager != null}", MyLogger.LogCategory.ClearHistory);
            
            if (emotionService != null && databaseService != null && syncService != null && connectivityManager != null)
            {
                MyLogger.Log("🔗 [EntryPoint] Все сервисы найдены, вызываем InitializeFirebaseSync...", MyLogger.LogCategory.ClearHistory);
                emotionService.InitializeFirebaseSync(databaseService, syncService, connectivityManager);
                
                // Запускаем синхронизацию ТОЛЬКО если пользователь аутентифицирован
                MyLogger.Log($"🔍 [EntryPoint] Проверка аутентификации: databaseService.IsAuthenticated={databaseService.IsAuthenticated}", MyLogger.LogCategory.ClearHistory);
                if (databaseService.IsAuthenticated)
                {
                    MyLogger.Log("🔗 [EntryPoint] Пользователь аутентифицирован, запускаем синхронизацию...", MyLogger.LogCategory.ClearHistory);
                    
                    // Сначала загружаем историю из Firebase
                    MyLogger.Log("📥 [EntryPoint] Загружаем историю из Firebase...", MyLogger.LogCategory.ClearHistory);
                    try
                    {
                        bool syncSuccess = await emotionService.ForceSyncWithFirebase();
                        if (syncSuccess)
                        {
                            MyLogger.Log("✅ [EntryPoint] История успешно загружена из Firebase", MyLogger.LogCategory.ClearHistory);
                        }
                        else
                        {
                            MyLogger.LogWarning("⚠️ [EntryPoint] Не удалось загрузить историю из Firebase", MyLogger.LogCategory.ClearHistory);
                        }
                    }
                    catch (Exception ex)
                    {
                        MyLogger.LogError($"❌ [EntryPoint] Ошибка при загрузке истории из Firebase: {ex.Message}", MyLogger.LogCategory.ClearHistory);
                    }
                    
                    // Затем запускаем обычную синхронизацию для отправки локальных изменений
                    emotionService.StartSync();
                    MyLogger.Log("✅ [EntryPoint] Firebase синхронизация для EmotionService инициализирована и запущена", MyLogger.LogCategory.ClearHistory);
                }
                else
                {
                    MyLogger.LogWarning("⚠️ [EntryPoint] Firebase синхронизация инициализирована, но не запущена: пользователь не аутентифицирован", MyLogger.LogCategory.ClearHistory);
                }
            }
            else
            {
                MyLogger.LogError("❌ [EntryPoint] Не удалось инициализировать Firebase синхронизацию для EmotionService", MyLogger.LogCategory.ClearHistory);
            }
        }

        /// <summary>
        /// Запускает основной процесс приложения
        /// </summary>
        private void StartBootstrapProcess()
        {
            _projectContainer.Resolve<ICoroutinePerformer>()
                .StartCoroutine(InitializeAndRunBootstrap());
        }

        private System.Collections.IEnumerator InitializeAndRunBootstrap()
        {
            // Проверяем, что контейнер успешно инициализирован и данные загружены
            // (это уже должно было произойти в InitializeApplication)
            if (_projectContainer == null)
            {
                MyLogger.LogError("[EntryPoint] Контейнер не инициализирован или данные не загружены перед запуском Bootstrap!", MyLogger.LogCategory.Bootstrap);
                yield break;
            }

            yield return _appBootstrap.Run(_projectContainer);
        }

        /// <summary>
        /// Регистрирует основные сервисы в контейнере
        /// </summary>
        private async Task RegisterCoreServices(DIContainer container)
        {
            try
            {
                MyLogger.Log("🔧 Регистрация сервисов через installer'ы...", MyLogger.LogCategory.Bootstrap);
                
                // Создаем менеджер installer'ов
                var installerManager = new ServiceInstallerManager();
                
                // Добавляем installer'ы в правильном порядке
                installerManager.AddInstaller(new CoreServicesInstaller());
                installerManager.AddInstaller(new UIServicesInstaller());
                installerManager.AddInstaller(new NotificationInstaller());
                installerManager.AddInstaller(new EventsInstaller());
                // installerManager.AddInstaller(new PersonalAreaInstaller()); // Временно отключено
                
                // Добавляем ApplicationServicesInstaller если конфигурация доступна
                if (_applicationConfig != null)
                {
                    installerManager.AddInstaller(new ApplicationServicesInstaller(_applicationConfig));
                }
                
                // Регистрируем все сервисы
                installerManager.RegisterAllServices(container);
                
                // Регистрируем дополнительные сервисы, которые пока не перенесены в installer'ы
                RegisterAdditionalServices(container);
                
                MyLogger.Log("✅ Все основные сервисы зарегистрированы", MyLogger.LogCategory.Bootstrap);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка регистрации базовых сервисов: {ex}", MyLogger.LogCategory.Bootstrap);
                throw;
            }
        }

        /// <summary>
        /// Регистрирует дополнительные сервисы, которые пока не перенесены в installer'ы
        /// </summary>
        private void RegisterAdditionalServices(DIContainer container)
        {
            try
            {
                // Регистрируем EmotionSyncService как GameObject компонент
                container.RegisterAsSingle<EmotionSyncService>(c =>
                {
                    var syncServiceObject = new GameObject("EmotionSyncService");
                    DontDestroyOnLoad(syncServiceObject);
                    var syncService = syncServiceObject.AddComponent<EmotionSyncService>();
                    return syncService;
                }).NonLazy();
                
                MyLogger.Log("✅ Дополнительные сервисы зарегистрированы", MyLogger.LogCategory.Bootstrap);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка регистрации дополнительных сервисов: {ex.Message}", MyLogger.LogCategory.Bootstrap);
                throw;
            }
        }

        /// <summary>
        /// Регистрирует систему уведомлений в контейнере
        /// </summary>
        private void RegisterNotificationSystem(DIContainer container)
        {
            try
            {
                // Создаем GameObject для менеджера уведомлений
                GameObject notificationManagerObject = new GameObject("NotificationManager");

                // Получаем тип по имени через рефлексию, так как не все файлы могли быть полностью скомпилированы
                Type notificationManagerType =
                    Type.GetType("App.Develop.CommonServices.Notifications.NotificationManager, Assembly-CSharp");

                if (notificationManagerType == null)
                {
                    MyLogger.LogError("Не удалось найти тип NotificationManager. Убедитесь, что класс скомпилирован в основной сборке Assembly-CSharp.", MyLogger.LogCategory.Bootstrap);
                    return;
                }

                // Добавляем компонент через рефлексию
                Component manager = notificationManagerObject.AddComponent(notificationManagerType);

                // Не уничтожать при переходе между сценами
                DontDestroyOnLoad(notificationManagerObject);

                // Регистрируем через лямбду, чтобы избежать проблем с типами
                container.RegisterAsSingle(c => manager).NonLazy();

                // MyLogger.Log("✅ Система уведомлений зарегистрирована", MyLogger.LogCategory.Bootstrap);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка регистрации системы уведомлений: {ex.Message}", MyLogger.LogCategory.Bootstrap);
                throw;
            }
        }

        /// <summary>
        /// Регистрирует сервисы аутентификации в контейнере
        /// </summary>
        private void RegisterAuthServices(DIContainer container)
        {
            try
            {
                // Сервис валидации уже регистрируется в FirebaseServiceInstaller
                // container.RegisterAsSingle<ValidationService>(container => new ValidationService()).NonLazy();

                // CredentialStorage уже зарегистрирован в InitializeSecureStorage
                // Оставим эту часть в комментариях для понимания изменений
                /*
                // Хранилище учетных данных
                container.RegisterAsSingle<CredentialStorage>(container =>
                    new CredentialStorage("UltraSecretKey!🔥")
                ).NonLazy();
                */

                // Сервис аутентификации
                container.RegisterAsSingle<IAuthService>(container =>
                    new AuthService(
                        container.Resolve<FirebaseAuth>(),
                        container.Resolve<IDatabaseService>(),
                        container.Resolve<ValidationService>()
                    )
                ).NonLazy();

                // Сервис профиля пользователя
                container.RegisterAsSingle<UserProfileService>(container =>
                    new UserProfileService(
                        container.Resolve<IDatabaseService>()
                    )
                ).NonLazy();

                // Сервис отслеживания состояния аутентификации
                container.RegisterAsSingle<IAuthStateService>(container =>
                    new AuthStateService(
                        container.Resolve<FirebaseAuth>(),
                        container.Resolve<IAuthService>()
                    )
                ).NonLazy();

                // Регистрируем AuthManager как обычный сервис, а не MonoBehaviour
                container.RegisterAsSingle<IAuthManager>(container =>
                {
                    var authManager = new AuthManager();
                    authManager.Inject(container);
                    return authManager;
                }).NonLazy();
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка регистрации аутентификационных сервисов: {ex.Message}", MyLogger.LogCategory.Bootstrap);
                throw;
            }
        }

        /// <summary>
        /// Регистрирует сервисы для личного кабинета
        /// </summary>
        private async Task RegisterPersonalAreaServices(DIContainer container)
        {
            try
            {
                // Регистрируем SettingsManager для работы панели настроек
                container.RegisterAsSingle<ISettingsManager>(c =>
                {
                    var settingsManager = new SettingsManager();
                    settingsManager.Inject(c);
                    return settingsManager;
                }).NonLazy();

                // Сервис личного кабинета
                container.RegisterAsSingle<IPersonalAreaService>(c =>
                    new PersonalAreaService(
                        c.Resolve<EmotionService>(),
                        c.Resolve<IPointsService>()
                    )
                );

                // Регистрируем EmotionJarView с использованием IAssetLoader асинхронно
                container.RegisterAsSingle<EmotionJarView>(c =>
                {
                    var assetLoader = c.Resolve<IAssetLoader>();

                    // Создадим временную заглушку, которая будет возвращена немедленно
                    var tempJarObject = new GameObject("TempEmotionJarView");
                    var tempJarView = tempJarObject.AddComponent<EmotionJarView>();

                    // Начинаем асинхронную загрузку
                    var loadTask = assetLoader.LoadAssetAsync<EmotionJarView>(AssetAddresses.EmotionJarView);

                    // После завершения загрузки, заменим временный объект на настоящий
                    loadTask.ContinueWith(t =>
                    {
                        if (t.Result != null)
                        {
                            var realJarView = Instantiate(t.Result);

                            // Уничтожим временный объект
                            if (tempJarObject != null)
                                Destroy(tempJarObject);
                        }
                        else
                        {
                            MyLogger.LogError($"❌ Не удалось загрузить EmotionJarView с ключом: {AssetAddresses.EmotionJarView}", MyLogger.LogCategory.Bootstrap);
                        }
                    }, TaskContinuationOptions.OnlyOnRanToCompletion);

                    return tempJarView;
                });
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка регистрации сервисов личного кабинета: {ex.Message}", MyLogger.LogCategory.Bootstrap);
                throw;
            }
        }

        /// <summary>
        /// Инициализирует SecurePlayerPrefs до регистрации других сервисов
        /// </summary>
        private void InitializeSecureStorage(DIContainer container)
        {
            try
            {
                // Используем тип без полного квалификатора
                var credentialStorage = new CredentialStorage("UltraSecretKey!🔥");
                container.RegisterAsSingle<CredentialStorage>(c => credentialStorage).NonLazy();
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка инициализации SecurePlayerPrefs: {ex.Message}", MyLogger.LogCategory.Bootstrap);
                throw;
            }
        }

        /// <summary>
        /// Регистрирует сервисы игровой системы
        /// </summary>
        private void RegisterGameSystem(DIContainer container)
        {
            // Сервис очков
            container.RegisterAsSingle<IPointsService>(container =>
                new PointsService(
                    container.Resolve<PlayerDataProvider>()
                )
            ).NonLazy();

            // Система уровней
            container.RegisterAsSingle<ILevelSystem>(container =>
                new LevelSystem(
                    container.Resolve<PlayerDataProvider>(),
                    container.Resolve<IPointsService>()
                )
            ).NonLazy();

            // Сервис достижений (с зависимостью от системы уровней)
            container.RegisterAsSingle<IAchievementService>(container =>
                new AchievementService(
                    container.Resolve<PlayerDataProvider>(),
                    container.Resolve<IPointsService>(),
                    container.Resolve<ILevelSystem>()
                )
            ).NonLazy();
        }

        /// <summary>
        /// Регистрирует сервисы Firebase в контейнере
        /// </summary>
        private void RegisterFirebase(DIContainer container)
        {
            try
            {
                if (_firebaseApp == null)
                {
                    throw new InvalidOperationException("Firebase не инициализирован");
                }

                if (_firebaseDatabase == null)
                {
                    throw new InvalidOperationException("База данных Firebase не инициализирована");
                }

                // Сервис аутентификации Firebase
                container.RegisterAsSingle<FirebaseAuth>(container => FirebaseAuth.GetAuth(_firebaseApp)).NonLazy();

                // Регистрируем наш экземпляр FirebaseApp
                container.RegisterAsSingle<FirebaseApp>(container => _firebaseApp).NonLazy();

                // Регистрируем экземпляр FirebaseDatabase
                container.RegisterAsSingle<FirebaseDatabase>(container => _firebaseDatabase).NonLazy();

                // Регистрируем Firebase Database Reference
                container.RegisterAsSingle<DatabaseReference>(c => _firebaseDatabase.RootReference).NonLazy();
                
                // Регистрируем кэш-менеджер
                container.RegisterAsSingle<FirebaseCacheManager>(c => 
                    new FirebaseCacheManager(
                        c.Resolve<ISaveLoadService>()
                    )
                ).NonLazy();

                // Регистрируем сервис валидации данных
                container.RegisterAsSingle<DataValidationService>(c =>
                {
                    var validationService = new DataValidationService();
                    
                    // Регистрация валидаторов
                    validationService.RegisterValidator(new EmotionHistoryRecordValidator());
                    validationService.RegisterValidator(new UserDataValidator());
                    
                    return validationService;
                }).NonLazy();

                // Используем FirebaseServiceInstaller для регистрации всех Firebase сервисов
                var firebaseServiceInstaller = new FirebaseServiceInstaller();
                firebaseServiceInstaller.RegisterServices(container);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка регистрации Firebase сервисов: {ex.Message}", MyLogger.LogCategory.Bootstrap);
                throw;
            }
        }
    }
}
