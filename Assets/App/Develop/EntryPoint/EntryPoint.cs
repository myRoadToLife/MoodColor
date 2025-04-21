using System.Threading.Tasks;
using App.Develop.AppServices.Auth;
using App.Develop.AppServices.Firebase;
using App.Develop.AppServices.Firebase.Auth;
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
using UnityEngine;

namespace App.Develop.EntryPoint
{
    public class EntryPoint : MonoBehaviour
    {
        [SerializeField] private Bootstrap _appBootstrap;
        private DIContainer _projectContainer;

        private async void Start()
        {
            SetupAppSettings();

            _projectContainer = new DIContainer();
            
            RegisterCoreServices();
            RegisterPersonalAreaServices();

            _projectContainer.Initialize();
            _projectContainer.Resolve<PlayerDataProvider>().Load();

            if (await InitFirebaseAsync())
            {
                _projectContainer.Resolve<ICoroutinePerformer>()
                    .StartPerformCoroutine(_appBootstrap.Run(_projectContainer));
            }
            else
            {
                Debug.LogError("❌ Firebase не готов. Приложение не может продолжить работу.");
            }
        }

        private void RegisterPersonalAreaServices()
        {
            _projectContainer.RegisterAsSingle<IPersonalAreaService>(di =>
                new PersonalAreaService(
                    di.Resolve<EmotionService>()
                )
            );
        }

        private void SetupAppSettings()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }

        private async Task<bool> InitFirebaseAsync()
        {
            var task = FirebaseApp.CheckAndFixDependenciesAsync();
            await task;
            return task.Result == DependencyStatus.Available;
        }

        private void RegisterCoreServices()
        {
            // Базовые сервисы
            _projectContainer.RegisterAsSingle<ResourcesAssetLoader>(_ => new ResourcesAssetLoader());
            
            // Сервисы для работы с данными
            _projectContainer.RegisterAsSingle<ISaveLoadService>(_ =>
                new SaveLoadService(new JsonSerializer(), new LocalDataRepository()));

            _projectContainer.RegisterAsSingle(di =>
                new ConfigsProviderService(di.Resolve<ResourcesAssetLoader>())
            );

            // Firebase сервисы
            _projectContainer.RegisterAsSingle<FirebaseManager>(_ => new FirebaseManager());
            _projectContainer.RegisterAsSingle<ValidationService>(_ => new ValidationService());
            _projectContainer.RegisterAsSingle(_ => new CredentialStorage("UltraSecretKey!🔥"));
            _projectContainer.RegisterAsSingle(di => new AuthService(di.Resolve<FirebaseManager>()));
            _projectContainer.RegisterAsSingle(di => new UserProfileService(di.Resolve<FirebaseManager>()));

            // Сервисы для работы с данными игрока
            _projectContainer.RegisterAsSingle(di =>
                new PlayerDataProvider(
                    di.Resolve<ISaveLoadService>(),
                    di.Resolve<ConfigsProviderService>()
                )
            );

            _projectContainer.RegisterAsSingle(di => new EmotionService(di.Resolve<PlayerDataProvider>()));
            _projectContainer.RegisterAsSingle(di => new PersonalAreaService(di.Resolve<EmotionService>()));

            // UI и сценовые сервисы
            _projectContainer.RegisterAsSingle<ICoroutinePerformer>(di =>
                Instantiate(di.Resolve<ResourcesAssetLoader>().LoadResource<CoroutinePerformer>(AssetPaths.CoroutinePerformer))
            );

            _projectContainer.RegisterAsSingle<ILoadingScreen>(di =>
                Instantiate(di.Resolve<ResourcesAssetLoader>().LoadResource<LoadingScreen>(AssetPaths.LoadingScreen))
            );

            _projectContainer.RegisterAsSingle<ISceneLoader>(_ => new SceneLoader());

            _projectContainer.RegisterAsSingle(di =>
                new SceneSwitcher(
                    di.Resolve<ICoroutinePerformer>(),
                    di.Resolve<ILoadingScreen>(),
                    di.Resolve<ISceneLoader>(),
                    di
                )
            );

            _projectContainer.RegisterAsSingle(di =>
                new PanelManager(
                    di.Resolve<ResourcesAssetLoader>(),
                    new MonoFactory(di)
                )
            );
        }

        private void OnDestroy()
        {
            _projectContainer?.Dispose();
        }
    }
}