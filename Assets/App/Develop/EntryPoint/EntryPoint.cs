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
using App.Develop.DI;
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
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            _projectContainer = new DIContainer();

            // Core services (без UI‑контроллера)
            _projectContainer.RegisterAsSingle(_ => new ResourcesAssetLoader());
            _projectContainer.RegisterAsSingle<ICoroutinePerformer>(di =>
                Instantiate(di.Resolve<ResourcesAssetLoader>()
                    .LoadResource<CoroutinePerformer>(AssetPaths.CoroutinePerformer))
            );
            _projectContainer.RegisterAsSingle<ILoadingScreen>(di =>
                Instantiate(di.Resolve<ResourcesAssetLoader>()
                    .LoadResource<LoadingScreen>(AssetPaths.LoadingScreen))
            );
            _projectContainer.RegisterAsSingle<ISceneLoader>(_ => new SceneLoader());
            _projectContainer.RegisterAsSingle(_ =>
                new SceneSwitcher(
                    _projectContainer.Resolve<ICoroutinePerformer>(),
                    _projectContainer.Resolve<ILoadingScreen>(),
                    _projectContainer.Resolve<ISceneLoader>(),
                    _projectContainer
                )
            );
            _projectContainer.RegisterAsSingle<ISaveLoadService>(_ =>
                new SaveLoadService(new JsonSerializer(), new LocalDataRepository()));
            _projectContainer.RegisterAsSingle(_ =>
                new PlayerDataProvider(
                    _projectContainer.Resolve<ISaveLoadService>(),
                    _projectContainer.Resolve<ConfigsProviderService>()
                )
            );
            _projectContainer.RegisterAsSingle(_ =>
                new EmotionService(_projectContainer.Resolve<PlayerDataProvider>())
            ).NonLazy();
            _projectContainer.RegisterAsSingle(_ =>
                new ConfigsProviderService(_projectContainer.Resolve<ResourcesAssetLoader>())
            );
            _projectContainer.RegisterAsSingle(_ => new FirestoreManager()).NonLazy();

            // Firebase core
            _projectContainer.RegisterAsSingle<FirebaseAuth>(_ => FirebaseAuth.DefaultInstance).NonLazy();
            _projectContainer.RegisterAsSingle<FirebaseFirestore>(_ => FirebaseFirestore.DefaultInstance).NonLazy();

            // Auth services
            _projectContainer.RegisterAsSingle<ValidationService>(_ => new ValidationService()).NonLazy();
            _projectContainer.RegisterAsSingle<CredentialStorage>(_ =>
                new CredentialStorage("UltraSecretKey!🔥")
            ).NonLazy();
            _projectContainer.RegisterAsSingle<AuthService>(di =>
                new AuthService(di.Resolve<FirebaseAuth>())
            ).NonLazy();
            _projectContainer.RegisterAsSingle<UserProfileService>(di =>
                new UserProfileService(di.Resolve<FirebaseFirestore>())
            ).NonLazy();

            _projectContainer.Initialize();

            var deps = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (deps == DependencyStatus.Available)
            {
                _projectContainer.Resolve<ICoroutinePerformer>()
                    .StartPerformCoroutine(_appBootstrap.Run(_projectContainer));
            }
            else
            {
                Debug.LogError($"Firebase dependency error: {deps}");
            }
        }
    }
}
