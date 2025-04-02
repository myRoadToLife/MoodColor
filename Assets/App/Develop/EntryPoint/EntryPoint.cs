using App.Develop.CommonServices.AssetManagment;
using App.Develop.CommonServices.CoroutinePerformer;
using App.Develop.CommonServices.LoadingScreen;
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

            projectContainer.Resolve<ICoroutinePerformer>().StartPerformCoroutine(_appBootstrap.Run(projectContainer));
        }


        private void SetupAppSettings()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }

        private void RegisterResourcesAssetLoader(DIContainer container)
            => container.RegisterAsSingle(diContainer => new ResourcesAssetLoader());

        private void RegisterCoroutinePerformer(DIContainer container)
        {
            container.RegisterAsSingle<ICoroutinePerformer>(diContainer =>
            {
                ResourcesAssetLoader resourcesAssetLoader = diContainer.Resolve<ResourcesAssetLoader>();
                CoroutinePerformer coroutinePerformerPrefab =
                    resourcesAssetLoader.LoadResource<CoroutinePerformer>("Infrastructure/CoroutinePerformer");

                return Instantiate(coroutinePerformerPrefab);
            });
        }
        
        private void RegisterLoadingScreen(DIContainer container)
        {
            container.RegisterAsSingle<ILoadingScreen>(diContainer =>
            {
                ResourcesAssetLoader resourcesAssetLoader = diContainer.Resolve<ResourcesAssetLoader>();
                LoadingScreen loadingScreenPrefab =
                    resourcesAssetLoader.LoadResource<LoadingScreen>("Infrastructure/LoadingScreen");

                return Instantiate(loadingScreenPrefab);
            });
        }
    }
}
