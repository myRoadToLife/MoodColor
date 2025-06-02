using System;

namespace App.Develop.CommonServices.Notifications
{
    /// <summary>
    /// Интерфейс для координатора уведомлений
    /// Позволяет избежать циклических зависимостей
    /// </summary>
    public interface INotificationCoordinator : INotificationManager
    {
        /// <summary>
        /// Инициализирует координатор уведомлений
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Менеджер пользовательских настроек
        /// </summary>
        UserPreferencesManager PreferencesManager { get; }
        
        /// <summary>
        /// Система триггеров уведомлений
        /// </summary>
        NotificationTriggerSystem TriggerSystem { get; }
    }
} 