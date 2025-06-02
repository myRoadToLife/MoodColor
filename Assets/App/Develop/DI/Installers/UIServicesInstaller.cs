using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.ConfigsManagement;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.UI;
using App.Develop.DI;
using App.Develop.Utils.Logging;

namespace App.Develop.DI.Installers
{
    /// <summary>
    /// Installer для регистрации UI сервисов
    /// </summary>
    public class UIServicesInstaller : IServiceInstaller
    {
        public string InstallerName => "UI Services";

        public void RegisterServices(DIContainer container)
        {
            MyLogger.Log($"🔧 Регистрация {InstallerName}...", MyLogger.LogCategory.Bootstrap);

            // UI Factory
            container.RegisterAsSingle<UIFactory>(c =>
                new UIFactory(
                    c.Resolve<IAssetLoader>(),
                    new MonoFactory(c)
                )
            ).NonLazy();

            // Panel Manager
            container.RegisterAsSingle(c =>
                new PanelManager(
                    c.Resolve<IAssetLoader>(),
                    new MonoFactory(c)
                )
            ).NonLazy();

            // Emotion Config Service
            container.RegisterAsSingle<EmotionConfigService>(c =>
                new EmotionConfigService(c.Resolve<IAssetLoader>())
            ).NonLazy();

            MyLogger.Log($"✅ {InstallerName} зарегистрированы", MyLogger.LogCategory.Bootstrap);
        }
    }
} 