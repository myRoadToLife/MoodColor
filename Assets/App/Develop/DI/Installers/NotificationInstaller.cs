using App.Develop.CommonServices.Notifications;
using App.Develop.DI;
using App.Develop.Utils.Logging;
using UnityEngine;

namespace App.Develop.DI.Installers
{
    /// <summary>
    /// Installer для регистрации системы уведомлений
    /// </summary>
    public class NotificationInstaller : IServiceInstaller
    {
        public string InstallerName => "Notification System";

        public void RegisterServices(DIContainer container)
        {
            MyLogger.Log($"🔧 Регистрация {InstallerName}...", MyLogger.LogCategory.Bootstrap);

            // Регистрируем сервисы уведомлений
            RegisterNotificationServices(container);
            
            // Регистрируем менеджеры и системы
            RegisterNotificationManagers(container);
            
            // Регистрируем координатор
            RegisterNotificationCoordinator(container);

            MyLogger.Log($"✅ {InstallerName} зарегистрирована", MyLogger.LogCategory.Bootstrap);
        }

        private void RegisterNotificationServices(DIContainer container)
        {
            // PushNotificationService (MonoBehaviour для корутин)
            container.RegisterAsSingle<PushNotificationService>(c =>
            {
                var pushObject = new GameObject("PushNotificationService");
                Object.DontDestroyOnLoad(pushObject);
                return pushObject.AddComponent<PushNotificationService>();
            }).NonLazy();

            // InGameNotificationService (MonoBehaviour для UI)
            container.RegisterAsSingle<InGameNotificationService>(c =>
            {
                var inGameObject = new GameObject("InGameNotificationService");
                Object.DontDestroyOnLoad(inGameObject);
                return inGameObject.AddComponent<InGameNotificationService>();
            }).NonLazy();

            // EmailNotificationService (обычный класс)
            container.RegisterAsSingle<EmailNotificationService>(c =>
                new EmailNotificationService()
            ).NonLazy();
        }

        private void RegisterNotificationManagers(DIContainer container)
        {
            // UserPreferencesManager (обычный класс)
            container.RegisterAsSingle<UserPreferencesManager>(c =>
                new UserPreferencesManager()
            ).NonLazy();

            // NotificationTriggerSystem (обычный класс)
            container.RegisterAsSingle<NotificationTriggerSystem>(c =>
                new NotificationTriggerSystem()
            ).NonLazy();

            // NotificationQueue (обычный класс, зависит от INotificationManager)
            container.RegisterAsSingle<NotificationQueue>(c =>
                new NotificationQueue(c.Resolve<INotificationManager>())
            );
        }

        private void RegisterNotificationCoordinator(DIContainer container)
        {
            // NotificationCoordinator (обычный класс с инъекцией зависимостей)
            container.RegisterAsSingle<INotificationManager>(c =>
            {
                var coordinator = new NotificationCoordinator(
                    c.Resolve<PushNotificationService>(),
                    c.Resolve<InGameNotificationService>(),
                    c.Resolve<EmailNotificationService>(),
                    c.Resolve<NotificationQueue>(),
                    c.Resolve<NotificationTriggerSystem>(),
                    c.Resolve<UserPreferencesManager>()
                );
                
                coordinator.Initialize();
                return coordinator;
            }).NonLazy();

            // Регистрируем также как конкретный тип для доступа к дополнительным методам
            container.RegisterAsSingle<NotificationCoordinator>(c =>
                (NotificationCoordinator)c.Resolve<INotificationManager>()
            ).NonLazy();
        }
    }
} 