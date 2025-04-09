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

namespace App.Develop.EntryPoint
{
    //Тут проводим все глобальные регистрации для старта работы приложения
    public class EntryPoint : MonoBehaviour
    {
        [SerializeField] private Bootstrap _appBootstrap;

        private void Awake()
        {
            SetupAppSettings();

            DIContainer projectContainer = new DIContainer();
            //Регистрация сервисов на целый проект
            //Аналог global context из популярных DI
            //Самый родительский контейнер
            RegisterResourcesAssetLoader(projectContainer);
            RegisterCoroutinePerformer(projectContainer);

            RegisterLoadingScreen(projectContainer);
            RegisterSceneLoader(projectContainer);
            RegisterSceneSwitcher(projectContainer);

            RegisterSaveLoadService(projectContainer);
            RegisterPlayerDataProvider(projectContainer);

            RegisterEmotionService(projectContainer);
            RegisterConfigsProviderService(projectContainer);


            projectContainer.Initialize();

            projectContainer.Resolve<ICoroutinePerformer>().StartPerformCoroutine(_appBootstrap.Run(projectContainer));
        }

        private void SetupAppSettings()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }

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

        private void RegisterSceneLoader(DIContainer container) =>
            container.RegisterAsSingle<ISceneLoader>(diContainer => new SceneLoader());

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
