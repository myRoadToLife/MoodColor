using System;
using System.Threading.Tasks;
using System.Linq;
using App.Develop.AppServices.Firebase.Auth;
using App.Develop.AppServices.Firebase.Auth.Services;
using App.Develop.AppServices.Firebase.Database.Services;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.ConfigsManagement;
using App.Develop.CommonServices.CoroutinePerformer;
using App.Develop.CommonServices.DataManagement;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.LoadingScreen;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.CommonServices.UI;
using App.Develop.DI;
using App.Develop.Scenes.PersonalAreaScene;
using App.Develop.Scenes.PersonalAreaScene.Settings;
using App.Develop.Scenes.PersonalAreaScene.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;
using App.Develop.UI.Panels;
using App.Develop.Scenes.PersonalAreaScene.UI.Components;

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
            DontDestroyOnLoad(gameObject);
            InitializeApplication();
        }

        /// <summary>
        /// Основной метод инициализации приложения
        /// </summary>
        private async void InitializeApplication()
        {
            try
            {
                // Настройка базовых параметров приложения
                SetupAppSettings();

                // Создание и настройка контейнера зависимостей
                _projectContainer = new DIContainer();
                RegisterCoreServices(_projectContainer);

                // Показываем загрузочный экран сразу после инициализации
                ShowInitialLoadingScreen();

                // Инициализация Firebase
                if (!await InitFirebaseAsync())
                {
                    Debug.LogError("❌ Не удалось инициализировать Firebase");
                    return;
                }

                // Регистрация сервисов, зависящих от Firebase
                RegisterFirebaseServices();
                
                // Инициализация контейнера и загрузка данных
                InitializeContainerAndLoadData();
                
                // Запуск основного процесса приложения
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
        }

        /// <summary>
        /// Инициализирует Firebase и проверяет его доступность
        /// </summary>
        private async Task<bool> InitFirebaseAsync()
        {
            try
            {
                Debug.Log("🔄 Инициализация Firebase...");
                
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
                Debug.Log($"✅ Создан экземпляр Firebase с именем {FIREBASE_APP_NAME} и URL: {DATABASE_URL}");
                
                // Создаем экземпляр базы данных с нашим Firebase App и URL
                _firebaseDatabase = FirebaseDatabase.GetInstance(_firebaseApp, DATABASE_URL);
                _firebaseDatabase.SetPersistenceEnabled(true);
                Debug.Log("✅ База данных Firebase инициализирована");
                
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
        }

        /// <summary>
        /// Инициализирует контейнер и загружает данные
        /// </summary>
        private void InitializeContainerAndLoadData()
        {
            RegisterPersonalAreaServices(_projectContainer);
            _projectContainer.Initialize();
            _projectContainer.Resolve<PlayerDataProvider>().Load();
        }

        /// <summary>
        /// Запускает основной процесс приложения
        /// </summary>
        private void StartBootstrapProcess()
        {
            _projectContainer.Resolve<ICoroutinePerformer>()
                .StartCoroutine(_appBootstrap.Run(_projectContainer));
        }

        /// <summary>
        /// Регистрирует основные сервисы в контейнере
        /// </summary>
        private void RegisterCoreServices(DIContainer container)
        {
            try
            {
                // Загрузчик ресурсов
                container.RegisterAsSingle(container => new ResourcesAssetLoader());

                // Исполнитель корутин
                container.RegisterAsSingle<ICoroutinePerformer>(container =>
                    CoroutinePerformerFactory.Create()
                );

                // Загрузочный экран
                container.RegisterAsSingle<ILoadingScreen>(container =>
                    Instantiate(container.Resolve<ResourcesAssetLoader>().LoadAsset<LoadingScreen>(AssetPaths.LoadingScreen))
                );

                // Загрузчик сцен
                container.RegisterAsSingle<ISceneLoader>(container => new SceneLoader());

                // Переключатель сцен
                container.RegisterAsSingle(container =>
                    new SceneSwitcher(
                        container.Resolve<ICoroutinePerformer>(),
                        container.Resolve<ILoadingScreen>(),
                        container.Resolve<ISceneLoader>(),
                        container
                    )
                );

                // Сервис сохранения/загрузки
                container.RegisterAsSingle<ISaveLoadService>(container =>
                    new SaveLoadService(new JsonSerializer(), new LocalDataRepository())
                );

                // Сервис конфигураций
                container.RegisterAsSingle(container =>
                    new ConfigsProviderService(container.Resolve<ResourcesAssetLoader>())
                ).NonLazy();

                // Провайдер данных игрока
                container.RegisterAsSingle(container =>
                    new PlayerDataProvider(
                        container.Resolve<ISaveLoadService>(),
                        container.Resolve<ConfigsProviderService>()
                    )
                );

                // Сервис эмоций
                container.RegisterAsSingle(container =>
                    new EmotionService(
                        container.Resolve<PlayerDataProvider>(),
                        container.Resolve<ConfigsProviderService>()
                    )
                ).NonLazy();

                // Менеджер панелей UI
                container.RegisterAsSingle(container =>
                    new PanelManager(
                        container.Resolve<ResourcesAssetLoader>(),
                        new MonoFactory(container)
                    )
                ).NonLazy();

                Debug.Log("✅ Базовые сервисы зарегистрированы успешно");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка регистрации базовых сервисов: {ex}");
                throw;
            }
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

                // Сервис базы данных
                container.RegisterAsSingle<DatabaseService>(container =>
                    new DatabaseService(
                        container.Resolve<DatabaseReference>()
                    )
                ).NonLazy();

                Debug.Log("✅ Firebase сервисы зарегистрированы");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка регистрации Firebase сервисов: {ex.Message}");
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

                // Хранилище учетных данных
                container.RegisterAsSingle<CredentialStorage>(container =>
                    new CredentialStorage("UltraSecretKey!🔥")
                ).NonLazy();

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

                Debug.Log("✅ Аутентификационные сервисы зарегистрированы");
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
        private void RegisterPersonalAreaServices(DIContainer container)
        {
            try
            {
                // Регистрируем SettingsManager для работы панели настроек
                container.RegisterAsSingle<ISettingsManager>(container =>
                {
                    var settingsManager = new SettingsManager();
                    settingsManager.Inject(container);
                    return settingsManager;
                }).NonLazy();

                // Сервис личного кабинета
                container.RegisterAsSingle<IPersonalAreaService>(container =>
                    new PersonalAreaService(
                        container.Resolve<EmotionService>()
                    )
                );
                
                // Регистрируем EmotionJarView с отложенным созданием
                container.RegisterAsSingle<EmotionJarView>(container =>
                {
                    // Создаем префаб только когда это действительно нужно
                    // Логика регистрации упрощена, чтобы избежать проблем на этапе загрузки
                    Debug.Log("⚠️ Создаем EmotionJarView через AssetPaths");
                    return Instantiate(container.Resolve<ResourcesAssetLoader>().LoadAsset<EmotionJarView>(AssetPaths.EmotionJarView));
                });

                Debug.Log("✅ Сервисы личного кабинета зарегистрированы");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка регистрации сервисов личного кабинета: {ex.Message}");
                throw;
            }
        }
        
    }
}
