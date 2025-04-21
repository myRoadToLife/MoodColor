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

            var services = new ServiceCollection();

            RegisterCoreServices(services);
            RegisterFirebase(services);
            RegisterAuthServices(services);
            RegisterPersonalAreaServices(services);

            _projectContainer = services.Build();
            
            await _projectContainer.InitializeAsync();
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

        private void RegisterPersonalAreaServices(ServiceCollection services)
        {
            services.AddSingleton<IPersonalAreaService, PersonalAreaService>();
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

        private void RegisterCoreServices(ServiceCollection services)
        {
            services.AddSingleton<ResourcesAssetLoader>();
            
            services.AddSingleton<ICoroutinePerformer>(di =>
                Instantiate(di.Resolve<ResourcesAssetLoader>().LoadResource<CoroutinePerformer>(AssetPaths.CoroutinePerformer))
            );
            
            services.AddSingleton<ILoadingScreen>(di =>
                Instantiate(di.Resolve<ResourcesAssetLoader>().LoadResource<LoadingScreen>(AssetPaths.LoadingScreen))
            );
            
            services.AddSingleton<ISceneLoader, SceneLoader>();
            
            services.AddSingleton(di =>
                new SceneSwitcher(
                    di.Resolve<ICoroutinePerformer>(),
                    di.Resolve<ILoadingScreen>(),
                    di.Resolve<ISceneLoader>(),
                    di
                )
            );
            
            services.AddSingleton<ISaveLoadService>(_ =>
                new SaveLoadService(new JsonSerializer(), new LocalDataRepository()));
            
            services.AddSingleton(di =>
                new ConfigsProviderService(di.Resolve<ResourcesAssetLoader>())
            );
            
            services.AddSingleton(di =>
                new PlayerDataProvider(
                    di.Resolve<ISaveLoadService>(),
                    di.Resolve<ConfigsProviderService>()
                )
            );
            
            services.AddSingleton<EmotionService>();
            
            services.AddSingleton<FirestoreManager>();
            
            services.AddSingleton(di =>
                new PanelManager(
                    di.Resolve<ResourcesAssetLoader>(),
                    new MonoFactory(di)
                )
            );
        }

        private void RegisterFirebase(ServiceCollection services)
        {
            services.AddSingleton(_ => FirebaseAuth.DefaultInstance);
            services.AddSingleton(_ => FirebaseFirestore.DefaultInstance);
        }

        private void RegisterAuthServices(ServiceCollection services)
        {
            services.AddSingleton(_ => new ValidationService());
            services.AddSingleton(_ => new CredentialStorage("UltraSecretKey!🔥"));
            
            services.AddSingleton<AuthService>();
            
            services.AddSingleton<UserProfileService>();
        }
    }
}
