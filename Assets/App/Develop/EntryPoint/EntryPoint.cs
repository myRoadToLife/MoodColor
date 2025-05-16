#define DISABLE_AUTO_ADDRESSABLES_IMPORT

using UnityEngine;
using System;
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
using UnityEngine.AddressableAssets;

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
        private const string DATABASE_URL = "https://moodcolor-3ac59-default-rtdb.firebaseio.com/";
        private const string FIREBASE_APP_NAME = "MoodColorApp"; // Имя для кастомного экземпляра Firebase

        private DIContainer _projectContainer;
        private FirebaseApp _firebaseApp; // Храним ссылку на наш экземпляр Firebase
        private FirebaseDatabase _firebaseDatabase; // Храним ссылку на базу данных

        private void Awake()
        {
            // DontDestroyOnLoad(gameObject); // ВРЕМЕННО ЗАКОММЕНТИРОВАТЬ
            InitializeApplication();
        }

        /// <summary>
        /// Основной метод инициализации приложения
        /// </summary>
        private async void InitializeApplication()
        {
            try
            {
                await Addressables.InitializeAsync().Task;

                SetupAppSettings();
                _projectContainer = new DIContainer();
                InitializeSecureStorage(_projectContainer);

                await RegisterCoreServices(_projectContainer);

                ShowInitialLoadingScreen();

                if (!await InitFirebaseAsync())
                {
                    Debug.LogError("❌ Не удалось инициализировать Firebase");
                    return;
                }

                RegisterFirebaseServices();

                _projectContainer.RegisterAsSingle(c =>
                    new PlayerDataProvider(
                        c.Resolve<ISaveLoadService>(),
                        c.Resolve<IConfigsProvider>(),
                        c.Resolve<IDatabaseService>()
                    )
                );


                RegisterGameSystem(_projectContainer);

                await InitializeContainerAndLoadData();
                StartBootstrapProcess();
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка инициализации приложения: {ex}");
            }
        }

        /// <summary>
        /// Настройка базовых параметров приложения
        /// </summary>
        private void SetupAppSettings()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Debug.Log("✅ Настройки приложения установлены");
        }

        /// <summary>
        /// Показывает загрузочный экран
        /// </summary>
        private void ShowInitialLoadingScreen()
        {
            ILoadingScreen loadingScreen = _projectContainer.Resolve<ILoadingScreen>();
            loadingScreen.Show();
            Debug.Log("Вызван Show() на ILoadingScreen");
        }

        /// <summary>
        /// Инициализирует Firebase и проверяет его доступность
        /// </summary>
        private async Task<bool> InitFirebaseAsync()
        {
            try
            {
                // Удаляем все существующие экземпляры с нашим именем, если они есть
                try
                {
                    var existingApp = FirebaseApp.GetInstance(FIREBASE_APP_NAME);

                    if (existingApp != null)
                    {
                        existingApp.Dispose();
                        Debug.Log($"Удален существующий экземпляр Firebase с именем {FIREBASE_APP_NAME}");
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
                    Debug.LogError($"❌ Firebase не доступен: {dependencyStatus}");
                    return false;
                }

                // Создаем кастомный экземпляр Firebase с нашим URL
                var options = new Firebase.AppOptions
                {
                    DatabaseUrl = new Uri(DATABASE_URL)
                };

                _firebaseApp = FirebaseApp.Create(options, FIREBASE_APP_NAME);

                // Создаем экземпляр базы данных с нашим Firebase App и URL
                _firebaseDatabase = FirebaseDatabase.GetInstance(_firebaseApp, DATABASE_URL);
                _firebaseDatabase.SetPersistenceEnabled(true);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка инициализации Firebase: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Регистрирует сервисы, зависящие от Firebase
        /// </summary>
        private void RegisterFirebaseServices()
        {
            RegisterFirebase(_projectContainer);
            RegisterAuthServices(_projectContainer);

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
        }

        /// <summary>
        /// Инициализирует контейнер и загружает данные
        /// </summary>
        private async Task InitializeContainerAndLoadData()
        {
            _projectContainer.RegisterAsSingle<EmotionService>(c =>
                new EmotionService(
                    c.Resolve<PlayerDataProvider>(),
                    c.Resolve<IConfigsProvider>(),
                    c.Resolve<EmotionConfigService>(),
                    c.Resolve<IPointsService>(),
                    c.Resolve<ILevelSystem>()
                )
            ).NonLazy();

            Debug.Log("✅ EmotionService зарегистрирован (в InitializeContainerAndLoadData).");

            await RegisterPersonalAreaServices(_projectContainer);

            // Сначала инициализируем ConfigsProviderService и EmotionConfigService
            var configsProvider = _projectContainer.Resolve<IConfigsProvider>() as ConfigsProviderService;

            if (configsProvider != null)
            {
                await configsProvider.InitializeAsync();
            }
            else
            {
                Debug.LogError("[EntryPoint] ConfigsProviderService не удалось разрезолвить как конкретный тип для InitializeAsync.");
            }

            var emotionCfgService = _projectContainer.Resolve<EmotionConfigService>();

            if (emotionCfgService != null)
            {
                await emotionCfgService.InitializeAsync();
            }
            else
            {
                Debug.LogWarning("[EntryPoint] EmotionConfigService не удалось разрезолвить как конкретный тип для InitializeAsync.");
            }

            // Затем загружаем PlayerDataProvider и IPointsService
            await _projectContainer.Resolve<PlayerDataProvider>().Load();

            var pointsService = _projectContainer.Resolve<IPointsService>();

            if (pointsService != null)
            {
                // IPointsService определяет InitializeAsync(), поэтому можем вызывать его напрямую
                await pointsService.InitializeAsync();
            }
            else
            {
                Debug.LogError("[EntryPoint] IPointsService не удалось разрезолвить из контейнера.");
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
                Debug.LogError("[EntryPoint] Контейнер не инициализирован или данные не загружены перед запуском Bootstrap!");
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
                container.RegisterAsSingle<IAssetLoader>(c => new AddressablesLoader()).NonLazy();
                container.RegisterAsSingle<ICoroutinePerformer>(c => CoroutinePerformerFactory.Create());

                container.RegisterAsSingle<ILoadingScreen>(c =>
                {
                    var assetLoader = c.Resolve<IAssetLoader>();
                    var go = new GameObject("LoadingScreenService");
                    DontDestroyOnLoad(go);
                    var loadingScreenComponent = go.AddComponent<LoadingScreen>();
                    loadingScreenComponent.Initialize(assetLoader, AssetAddresses.LoadingScreen);
                    go.SetActive(false);
                    return loadingScreenComponent;
                }).NonLazy();

                container.RegisterAsSingle<ISceneLoader>(c => new SceneLoader());

                container.RegisterAsSingle(c =>
                    new SceneSwitcher(
                        c.Resolve<ICoroutinePerformer>(),
                        c.Resolve<ILoadingScreen>(),
                        c.Resolve<ISceneLoader>(),
                        c
                    )
                );

                container.RegisterAsSingle<UIFactory>(c =>
                    new UIFactory(
                        c.Resolve<IAssetLoader>(),
                        new MonoFactory(c)
                    )
                ).NonLazy();

                container.RegisterAsSingle<ISaveLoadService>(c =>
                    new SaveLoadService(new JsonSerializer(), new LocalDataRepository())
                );

                container.RegisterAsSingle<IConfigsProvider>(c =>
                    new ConfigsProviderService(c.Resolve<IAssetLoader>())
                ).NonLazy();

                container.RegisterAsSingle<EmotionConfigService>(c =>
                    new EmotionConfigService(c.Resolve<IAssetLoader>())
                ).NonLazy();

                container.RegisterAsSingle(c =>
                    new PanelManager(
                        c.Resolve<IAssetLoader>(),
                        new MonoFactory(c)
                    )
                ).NonLazy();

                RegisterNotificationSystem(container);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка регистрации базовых сервисов: {ex}");
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
                    Type.GetType("App.Develop.CommonServices.Notifications.NotificationManager, App.Develop.CommonServices.Notifications");

                if (notificationManagerType == null)
                {
                    Debug.LogError("Не удалось найти тип NotificationManager. Убедитесь, что сборка App.Develop.CommonServices.Notifications скомпилирована.");
                    return;
                }

                // Добавляем компонент через рефлексию
                Component manager = notificationManagerObject.AddComponent(notificationManagerType);

                // Не уничтожать при переходе между сценами
                DontDestroyOnLoad(notificationManagerObject);

                // Регистрируем через лямбду, чтобы избежать проблем с типами
                container.RegisterAsSingle(c => manager).NonLazy();

                Debug.Log("✅ Система уведомлений зарегистрирована");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка регистрации системы уведомлений: {ex.Message}");
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
                // Сервис валидации
                container.RegisterAsSingle<ValidationService>(container => new ValidationService()).NonLazy();

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
                        container.Resolve<DatabaseService>(),
                        container.Resolve<ValidationService>()
                    )
                ).NonLazy();

                // Сервис профиля пользователя
                container.RegisterAsSingle<UserProfileService>(container =>
                    new UserProfileService(
                        container.Resolve<DatabaseService>()
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
                Debug.LogError($"❌ Ошибка регистрации аутентификационных сервисов: {ex.Message}");
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
                            Debug.LogError($"❌ Не удалось загрузить EmotionJarView с ключом: {AssetAddresses.EmotionJarView}");
                        }
                    }, TaskContinuationOptions.OnlyOnRanToCompletion);

                    return tempJarView;
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка регистрации сервисов личного кабинета: {ex.Message}");
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
                Debug.LogError($"❌ Ошибка инициализации SecurePlayerPrefs: {ex.Message}");
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

                // Ссылка на базу данных Firebase
                container.RegisterAsSingle<DatabaseReference>(container => _firebaseDatabase.RootReference).NonLazy();

                // Менеджер кэширования Firebase
                container.RegisterAsSingle<FirebaseCacheManager>(container =>
                    new FirebaseCacheManager(
                        container.Resolve<ISaveLoadService>()
                    )
                ).NonLazy();

                // Регистрация сервиса хранения истории эмоций
                container.RegisterAsSingle<EmotionHistoryCache>(c =>
                    new EmotionHistoryCache(
                        c.Resolve<FirebaseCacheManager>()));

                // Регистрация сервиса валидации данных
                container.RegisterAsSingle<DataValidationService>(c =>
                {
                    var validationService = new DataValidationService();

                    // Регистрация валидаторов
                    validationService.RegisterValidator(new EmotionHistoryRecordValidator());
                    validationService.RegisterValidator(new UserDataValidator());

                    return validationService;
                }).NonLazy();

                // Регистрация ConnectivityManager для работы с сетью
                container.RegisterAsSingle<ConnectivityManager>(c =>
                    new ConnectivityManager(
                        c.Resolve<ICoroutinePerformer>()
                    )
                ).NonLazy();

                // Регистрация основного сервиса работы с базой данных Firebase
                container.RegisterAsSingle<IDatabaseService>(c =>
                    new DatabaseService(
                        c.Resolve<DatabaseReference>(),
                        c.Resolve<FirebaseCacheManager>(),
                        c.Resolve<DataValidationService>())
                ).NonLazy();

                // Также регистрируем DatabaseService напрямую (для компонентов, которые требуют конкретный тип)
                container.RegisterAsSingle<DatabaseService>(c =>
                    c.Resolve<IDatabaseService>() as DatabaseService
                ).NonLazy();

                // Загружаем настройки синхронизации
                var syncSettings = new EmotionSyncSettings();

                // Менеджер разрешения конфликтов (кастим IDatabaseService к DatabaseService, т.к. это требуется по контракту)
                container.RegisterAsSingle<ConflictResolutionManager>(c =>
                    new ConflictResolutionManager(
                        c.Resolve<IDatabaseService>() as DatabaseService, // Приведение типа
                        syncSettings)
                ).NonLazy();

                // Сервис синхронизации эмоций (это MonoBehaviour, создаем и инициализируем)
                container.RegisterAsSingle<EmotionSyncService>(c =>
                {
                    var syncService = new GameObject("EmotionSyncService").AddComponent<EmotionSyncService>();

                    // Инициализируем сервис
                    syncService.Initialize(
                        c.Resolve<IDatabaseService>(),
                        c.Resolve<EmotionHistoryCache>(),
                        c.Resolve<ConnectivityManager>());

                    DontDestroyOnLoad(syncService.gameObject);
                    return syncService;
                }).NonLazy();
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка регистрации Firebase сервисов: {ex.Message}");
                throw;
            }
        }
    }
}
