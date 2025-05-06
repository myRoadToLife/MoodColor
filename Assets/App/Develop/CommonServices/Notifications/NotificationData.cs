using System;

namespace App.Develop.CommonServices.Notifications
{
    /// <summary>
    /// Перечисление типов доставки уведомлений
    /// </summary>
    public enum NotificationDeliveryType
    {
        Push,       // Push-уведомление на устройство
        InGame,     // Внутриигровое уведомление
        Email       // Email-уведомление
    }
    
    /// <summary>
    /// Категории уведомлений
    /// </summary>
    public enum NotificationCategory
    {
        System,         // Системные уведомления
        Reminder,       // Напоминания
        Activity,       // Активность в приложении
        Achievement,    // Достижения
        Promotion,      // Акции и предложения
        Update          // Обновления
    }
    
    /// <summary>
    /// Приоритеты уведомлений
    /// </summary>
    public enum NotificationPriority
    {
        Low,        // Низкий приоритет
        Normal,     // Нормальный приоритет
        High,       // Высокий приоритет
        Critical    // Критический приоритет
    }
    
    /// <summary>
    /// Класс данных уведомления, содержащий всю информацию о нем
    /// </summary>
    [Serializable]
    public class NotificationData
    {
        // Основные данные уведомления
        public string Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string DeepLink { get; set; }
        
        // Метаданные
        public NotificationDeliveryType DeliveryType { get; set; }
        public NotificationCategory Category { get; set; }
        public NotificationPriority Priority { get; set; }
        
        // Данные для группировки
        public string GroupId { get; set; }
        
        // Дополнительные данные в формате JSON
        public string ExtraData { get; set; }
        
        // Время создания и время истечения
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        
        // Флаги состояния
        public bool IsRead { get; set; }
        public bool IsDismissed { get; set; }
        
        /// <summary>
        /// Конструктор с основными параметрами
        /// </summary>
        public NotificationData(string title, string message, NotificationDeliveryType deliveryType, NotificationCategory category)
        {
            Id = Guid.NewGuid().ToString();
            Title = title;
            Message = message;
            DeliveryType = deliveryType;
            Category = category;
            Priority = NotificationPriority.Normal;
            CreatedAt = DateTime.Now;
            IsRead = false;
            IsDismissed = false;
        }
        
        /// <summary>
        /// Проверяет, не истекло ли время жизни уведомления
        /// </summary>
        public bool IsExpired()
        {
            return ExpiresAt.HasValue && DateTime.Now > ExpiresAt.Value;
        }
    }
}