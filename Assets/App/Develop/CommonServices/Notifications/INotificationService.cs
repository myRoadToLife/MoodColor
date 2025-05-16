using System.Threading.Tasks;

namespace App.Develop.CommonServices.Notifications
{
    /// <summary>
    /// Базовый интерфейс для всех сервисов уведомлений
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Инициализирует сервис уведомлений
        /// </summary>
        Task Initialize();
        
        /// <summary>
        /// Имя сервиса для идентификации
        /// </summary>
        string ServiceName { get; }
    }
} 