using System.Threading.Tasks;
using App.Develop.AppServices.Auth;
using App.Develop.AppServices.Firebase;
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
using Firebase.Extensions;
using Firebase.Firestore;
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

            RegisterCoreServices(_projectContainer);
            RegisterFirebase(_projectContainer);
            RegisterAuthServices(_projectContainer);
            RegisterPersonalAreaServices(_projectContainer);

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

        private void RegisterPersonalAreaServices(DIContainer projectContainer)
        {
            projectContainer.RegisterAsSingle<IPersonalAreaService>(di =>
                new PersonalAreaService(di.Resolve<EmotionService>())
            ).NonLazy();
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

        private void RegisterCoreServices(DIContainer container)
        {
            container.RegisterAsSingle(_ => new ResourcesAssetLoader());

            container.RegisterAsSingle<ICoroutinePerformer>(di =>
                Instantiate(di.Resolve<ResourcesAssetLoader>().LoadResource<CoroutinePerformer>(AssetPaths.CoroutinePerformer))
            );

            container.RegisterAsSingle<ILoadingScreen>(di =>
                Instantiate(di.Resolve<ResourcesAssetLoader>().LoadResource<LoadingScreen>(AssetPaths.LoadingScreen))
            );

            container.RegisterAsSingle<ISceneLoader>(_ => new SceneLoader());

            container.RegisterAsSingle(di =>
                new SceneSwitcher(
                    di.Resolve<ICoroutinePerformer>(),
                    di.Resolve<ILoadingScreen>(),
                    di.Resolve<ISceneLoader>(),
                    di
                )
            );

            container.RegisterAsSingle<ISaveLoadService>(_ =>
                new SaveLoadService(new JsonSerializer(), new LocalDataRepository()));

            container.RegisterAsSingle(di =>
                new ConfigsProviderService(di.Resolve<ResourcesAssetLoader>())
            );

            container.RegisterAsSingle(di =>
                new PlayerDataProvider(
                    di.Resolve<ISaveLoadService>(),
                    di.Resolve<ConfigsProviderService>()
                )
            );

            container.RegisterAsSingle(di =>
                new EmotionService(di.Resolve<PlayerDataProvider>())
            ).NonLazy();

            container.RegisterAsSingle(_ => new FirestoreManager()).NonLazy();

            container.RegisterAsSingle(di =>
                new PanelManager(
                    di.Resolve<ResourcesAssetLoader>(),
                    new MonoFactory(di)
                )
            ).NonLazy();
        }

        private void RegisterFirebase(DIContainer container)
        {
            container.RegisterAsSingle<FirebaseAuth>(_ => FirebaseAuth.DefaultInstance).NonLazy();
            container.RegisterAsSingle<FirebaseFirestore>(_ => FirebaseFirestore.DefaultInstance).NonLazy();
        }

        private void RegisterAuthServices(DIContainer container)
        {
            container.RegisterAsSingle(_ => new ValidationService()).NonLazy();
            container.RegisterAsSingle(_ => new CredentialStorage("UltraSecretKey!🔥")).NonLazy();

            container.RegisterAsSingle(di =>
                new AuthService(di.Resolve<FirebaseAuth>())
            ).NonLazy();

            container.RegisterAsSingle(di =>
                new UserProfileService(di.Resolve<FirebaseFirestore>())
            ).NonLazy();
        }
        
    }
}
