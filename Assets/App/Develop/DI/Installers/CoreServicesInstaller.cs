using System.Threading.Tasks;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.ConfigsManagement;
using App.Develop.CommonServices.CoroutinePerformer;
using App.Develop.CommonServices.DataManagement;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.LoadingScreen;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.CommonServices.UI;
using App.Develop.DI;
using App.Develop.Utils.Logging;

namespace App.Develop.DI.Installers
{
    /// <summary>
    /// Installer для регистрации основных сервисов приложения
    /// </summary>
    public class CoreServicesInstaller : IServiceInstaller
    {
        public string InstallerName => "Core Services";

        public void RegisterServices(DIContainer container)
        {
            MyLogger.Log($"🔧 Регистрация {InstallerName}...", MyLogger.LogCategory.Bootstrap);

            // Asset Management
            container.RegisterAsSingle<IAssetLoader>(c => new AddressablesLoader()).NonLazy();
            container.RegisterAsSingle<ISceneLoader>(c => new SceneLoader()).NonLazy();

            // Coroutine Performer
            container.RegisterAsSingle<ICoroutinePerformer>(c => 
            {
                var go = new UnityEngine.GameObject("[CoroutinePerformer]");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.hideFlags = UnityEngine.HideFlags.HideInHierarchy | UnityEngine.HideFlags.HideInInspector | UnityEngine.HideFlags.DontSave;
                return go.AddComponent<CoroutinePerformer>();
            }).NonLazy();

            // Loading Screen - создаем как MonoBehaviour
            container.RegisterAsSingle<ILoadingScreen>(c => 
            {
                var go = new UnityEngine.GameObject("LoadingScreen");
                UnityEngine.Object.DontDestroyOnLoad(go);
                var loadingScreen = go.AddComponent<LoadingScreen>();
                loadingScreen.Initialize(c.Resolve<IAssetLoader>(), AssetAddresses.LoadingScreen);
                return loadingScreen;
            }).NonLazy();

            // Scene Management
            container.RegisterAsSingle(c =>
                new SceneSwitcher(
                    c.Resolve<ICoroutinePerformer>(),
                    c.Resolve<ILoadingScreen>(),
                    c.Resolve<ISceneLoader>(),
                    c
                )
            );

            // Data Management
            container.RegisterAsSingle<ISaveLoadService>(c =>
                new SaveLoadService(new JsonSerializer(), new LocalDataRepository())
            );

            // Configs
            container.RegisterAsSingle<IConfigsProvider>(c =>
                new ConfigsProviderService(c.Resolve<IAssetLoader>())
            ).NonLazy();

            MyLogger.Log($"✅ {InstallerName} зарегистрированы", MyLogger.LogCategory.Bootstrap);
        }
    }
} 