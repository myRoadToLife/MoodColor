using System;

namespace App.Develop.CommonServices.Notifications
{
    /// <summary>
    /// Основной интерфейс менеджера уведомлений
    /// </summary>
    public interface INotificationManager
    {
        /// <summary>
        /// Планирует отправку уведомления
        /// </summary>
        void ScheduleNotification(NotificationData notification, DateTime scheduledTime);
        
        /// <summary>
        /// Отправляет уведомление немедленно
        /// </summary>
        void SendImmediateNotification(NotificationData notification);

        /// <summary>
        /// Отменяет запланированное уведомление по ID
        /// </summary>
        void CancelNotification(string notificationId);
        
        /// <summary>
        /// Отменяет все запланированные уведомления
        /// </summary>
        void CancelAllNotifications();
    }
} 