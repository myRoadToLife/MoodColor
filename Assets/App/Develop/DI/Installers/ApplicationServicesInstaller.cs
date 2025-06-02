using App.Develop.Configs;
using App.Develop.CommonServices.Networking;
using App.Develop.CommonServices.Social;
using App.Develop.CommonServices.CoroutinePerformer;
using App.Develop.DI;
using App.Develop.Utils.Logging;
using UnityEngine;

namespace App.Develop.DI.Installers
{
    /// <summary>
    /// Installer для регистрации сервисов приложения
    /// </summary>
    public class ApplicationServicesInstaller : IServiceInstaller
    {
        private readonly ApplicationConfig _applicationConfig;

        public string InstallerName => "Application Services";

        public ApplicationServicesInstaller(ApplicationConfig applicationConfig)
        {
            _applicationConfig = applicationConfig;
        }

        public void RegisterServices(DIContainer container)
        {
            MyLogger.Log($"🔧 Регистрация {InstallerName}...", MyLogger.LogCategory.Bootstrap);

            // Регистрируем конфигурацию приложения
            container.RegisterAsSingle<ApplicationConfig>(c => _applicationConfig).NonLazy();

            // Регистрируем ConnectivityManager
            container.RegisterAsSingle<ConnectivityManager>(c =>
                new ConnectivityManager(c.Resolve<ICoroutinePerformer>())
            ).NonLazy();

            MyLogger.Log($"✅ {InstallerName} зарегистрированы", MyLogger.LogCategory.Bootstrap);
        }
    }
}