using App.Develop.DI;
using App.Develop.Utils.Events;
using App.Develop.Utils.Logging;

namespace App.Develop.DI.Installers
{
    /// <summary>
    /// Installer для регистрации системы событий
    /// </summary>
    public class EventsInstaller : IServiceInstaller
    {
        public string InstallerName => "Events System";

        public void RegisterServices(DIContainer container)
        {
            MyLogger.Log($"🔧 Регистрация {InstallerName}...", MyLogger.LogCategory.Bootstrap);

            // Регистрируем глобальную шину событий
            container.RegisterAsSingle<EventBus>(c => new EventBus()).NonLazy();

            MyLogger.Log($"✅ {InstallerName} зарегистрирована", MyLogger.LogCategory.Bootstrap);
        }
    }
} 