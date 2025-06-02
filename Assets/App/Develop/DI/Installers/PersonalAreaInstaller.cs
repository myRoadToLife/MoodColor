using App.Develop.DI;
using App.Develop.Scenes.PersonalAreaScene.UI;
using App.Develop.Scenes.PersonalAreaScene.Infrastructure;
using App.Develop.CommonServices.UI;
using App.Develop.CommonServices.Firebase.Database.Services;
using App.Develop.CommonServices.GameSystem;
using App.Develop.Utils.Logging;

namespace App.Develop.DI.Installers
{
    /// <summary>
    /// Installer для регистрации MVP компонентов личного кабинета
    /// </summary>
    public class PersonalAreaInstaller : IServiceInstaller
    {
        public string InstallerName => "Personal Area MVP";

        public void RegisterServices(DIContainer container)
        {
            MyLogger.Log($"🔧 Регистрация {InstallerName}...", MyLogger.LogCategory.Bootstrap);

            // Регистрируем менеджеры
            RegisterManagers(container);
            
            // Регистрируем Presenter (будет создан когда понадобится View)
            RegisterPresenter(container);

            MyLogger.Log($"✅ {InstallerName} зарегистрированы", MyLogger.LogCategory.Bootstrap);
        }

        private void RegisterManagers(DIContainer container)
        {
            // PersonalAreaProfileManager
            container.RegisterAsSingle<PersonalAreaProfileManager>(c =>
                new PersonalAreaProfileManager(
                    null, // View будет установлен позже
                    c.Resolve<IDatabaseService>()
                )
            );

            // PersonalAreaStatisticsManager  
            container.RegisterAsSingle<PersonalAreaStatisticsManager>(c =>
                new PersonalAreaStatisticsManager(
                    null, // View будет установлен позже
                    c.Resolve<IPointsService>()
                )
            );
        }

        private void RegisterPresenter(DIContainer container)
        {
            // PersonalAreaPresenter
            container.RegisterAsSingle<PersonalAreaPresenter>(c =>
                new PersonalAreaPresenter(
                    null, // View будет установлен позже
                    c.Resolve<PanelManager>(),
                    c.Resolve<IPersonalAreaService>()
                )
            );
        }
    }
} 