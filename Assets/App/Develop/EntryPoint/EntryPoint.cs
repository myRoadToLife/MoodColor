using System;
using System.Threading.Tasks;
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
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;

namespace App.Develop.EntryPoint
{
    /// <summary>
    /// Точка входа в приложение. Отвечает за инициализацию всех основных сервисов.
    /// </summary>
    public class EntryPoint : MonoBehaviour
    {
        [SerializeField] private Bootstrap _appBootstrap;
        private DIContainer _projectContainer;

        private async void Start()
        {
            try
            {
                await InitializeApplication();
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Критическая ошибка при запуске: {ex}");
            }
        }

        /// <summary>
        /// Основной метод инициализации приложения
        /// </summary>
        private async Task InitializeApplication()
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
                Debug.LogError("❌ Firebase не готов. Приложение не может продолжить работу.");
                return;
            }

            // Регистрация сервисов, зависящих от Firebase
            RegisterFirebaseServices();
            
            // Инициализация контейнера и загрузка данных
            InitializeContainerAndLoadData();
            
            // Запуск основного процесса приложения
            StartBootstrapProcess();
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
                var task = FirebaseApp.CheckAndFixDependenciesAsync();
                await task;

                var result = task.Result;
                if (result == DependencyStatus.Available)
                {
                    Debug.Log("✅ Firebase инициализирован успешно");
                    return true;
                }
                
                Debug.LogError($"❌ Firebase не доступен: {result}");
                return false;
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
                .StartPerformCoroutine(_appBootstrap.Run(_projectContainer));
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
                    Instantiate(container.Resolve<ResourcesAssetLoader>().LoadResource<CoroutinePerformer>(AssetPaths.CoroutinePerformer))
                );

                // Загрузочный экран
                container.RegisterAsSingle<ILoadingScreen>(container =>
                    Instantiate(container.Resolve<ResourcesAssetLoader>().LoadResource<LoadingScreen>(AssetPaths.LoadingScreen))
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
                    new EmotionService(container.Resolve<PlayerDataProvider>())
                ).NonLazy();

                // Менеджер панелей UI
                container.RegisterAsSingle(container =>
                    new PanelManager(
                        container.Resolve<ResourcesAssetLoader>(),
                        new MonoFactory(container)
                    )
                ).NonLazy();

                Debug.Log("✅ Основные сервисы зарегистрированы");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка регистрации основных сервисов: {ex.Message}");
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
                // Сервис аутентификации Firebase
                container.RegisterAsSingle<FirebaseAuth>(container => FirebaseAuth.DefaultInstance).NonLazy();

                // Ссылка на базу данных Firebase
                container.RegisterAsSingle<DatabaseReference>(container =>
                {
                    string databaseUrl = "https://moodcolor-3ac59-default-rtdb.firebaseio.com/";
                    var database = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance, databaseUrl);
                    return database.RootReference;
                }).NonLazy();

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
        /// Регистрирует сервисы личного кабинета в контейнере
        /// </summary>
        private void RegisterPersonalAreaServices(DIContainer container)
        {
            try
            {
                container.RegisterAsSingle<IPersonalAreaService>(container =>
                    new PersonalAreaService(
                        container.Resolve<EmotionService>()
                    )
                ).NonLazy();

                Debug.Log("✅ PersonalAreaService зарегистрирован");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка регистрации PersonalAreaService: {ex.Message}");
                throw;
            }
        }
    }
}
