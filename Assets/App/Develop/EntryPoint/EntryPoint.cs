using System;
using System.Threading.Tasks;
using App.Develop.AppServices.Firebase;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.ConfigsManagement;
using App.Develop.CommonServices.CoroutinePerformer;
using App.Develop.CommonServices.DataManagement;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.LoadingScreen;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using UnityEngine;
using Firebase;
using Firebase.Extensions;

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

            RegisterResourcesAssetLoader(_projectContainer);
            RegisterCoroutinePerformer(_projectContainer);
            RegisterLoadingScreen(_projectContainer);
            RegisterSceneLoader(_projectContainer);
            RegisterSceneSwitcher(_projectContainer);
            RegisterSaveLoadService(_projectContainer);
            RegisterPlayerDataProvider(_projectContainer);
            RegisterEmotionService(_projectContainer);
            RegisterConfigsProviderService(_projectContainer);
            RegisterFirestoreManager(_projectContainer);

            _projectContainer.Initialize();

            bool firebaseReady = await InitFirebaseAsync();

            if (firebaseReady)
            {
                Debug.Log("Запускаем Bootstrap после инициализации Firebase");

                _projectContainer.Resolve<ICoroutinePerformer>()
                    .StartPerformCoroutine(_appBootstrap.Run(_projectContainer));
            }
            else
            {
                Debug.LogError("Firebase не готов. Приложение не может продолжить работу.");
            }
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

            if (task.Result == DependencyStatus.Available)
            {
                Debug.Log("Firebase готов к работе!");
                return true;
            }

            Debug.LogError("Ошибка инициализации Firebase: " + task.Result);
            return false;
        }
        
        private void RegisterFirestoreManager(DIContainer container) =>
            container.RegisterAsSingle(di => new FirestoreManager()).NonLazy();
        
        private void RegisterConfigsProviderService(DIContainer container)
            => container.RegisterAsSingle(diContainer
                => new ConfigsProviderService(diContainer.Resolve<ResourcesAssetLoader>()));

        private void RegisterEmotionService(DIContainer container)
            => container.RegisterAsSingle(diContainer
                => new EmotionService(diContainer.Resolve<PlayerDataProvider>())).NonLazy();

        private void RegisterPlayerDataProvider(DIContainer container)
            => container.RegisterAsSingle(diContainer
                => new PlayerDataProvider(diContainer.Resolve<ISaveLoadService>(),
                    diContainer.Resolve<ConfigsProviderService>()));

        private void RegisterSceneLoader(DIContainer container)
            => container.RegisterAsSingle<ISceneLoader>(diContainer => new SceneLoader());

        private void RegisterResourcesAssetLoader(DIContainer container)
            => container.RegisterAsSingle(diContainer => new ResourcesAssetLoader());

        private void RegisterSaveLoadService(DIContainer projectContainer)
            => projectContainer.RegisterAsSingle<ISaveLoadService>(diContainer
                => new SaveLoadService(new JsonSerializer(), new LocalDataRepository()));

        private void RegisterSceneSwitcher(DIContainer container)
            => container.RegisterAsSingle(diContainer
                => new SceneSwitcher(
                    diContainer.Resolve<ICoroutinePerformer>(),
                    diContainer.Resolve<ILoadingScreen>(),
                    diContainer.Resolve<ISceneLoader>(),
                    diContainer));

        private void RegisterCoroutinePerformer(DIContainer container)
        {
            container.RegisterAsSingle<ICoroutinePerformer>(diContainer =>
            {
                ResourcesAssetLoader resourcesAssetLoader = diContainer.Resolve<ResourcesAssetLoader>();

                CoroutinePerformer coroutinePerformerPrefab =
                    resourcesAssetLoader.LoadResource<CoroutinePerformer>(AssetPaths.CoroutinePerformer);

                return Instantiate(coroutinePerformerPrefab);
            });
        }

        private void RegisterLoadingScreen(DIContainer container)
        {
            container.RegisterAsSingle<ILoadingScreen>(diContainer =>
            {
                ResourcesAssetLoader resourcesAssetLoader = diContainer.Resolve<ResourcesAssetLoader>();

                LoadingScreen loadingScreenPrefab =
                    resourcesAssetLoader.LoadResource<LoadingScreen>(AssetPaths.LoadingScreen);

                return Instantiate(loadingScreenPrefab);
            });
        }
    }
}
