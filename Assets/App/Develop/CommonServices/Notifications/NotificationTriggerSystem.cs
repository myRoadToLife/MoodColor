using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Notifications
{
    /// <summary>
    /// Система триггеров уведомлений, отвечающая за запуск уведомлений по расписанию
    /// </summary>
    public class NotificationTriggerSystem : IDisposable
    {
        public event Action<NotificationData> OnNotificationTriggered;
        
        private Dictionary<string, ScheduledNotification> _scheduledNotifications = new Dictionary<string, ScheduledNotification>();
        private bool _isInitialized = false;
        private Timer _checkTimer;
        
        private class ScheduledNotification
        {
            public NotificationData Data { get; set; }
            public DateTime ScheduledTime { get; set; }
            public bool IsTriggered { get; set; }
            
            public ScheduledNotification(NotificationData data, DateTime scheduledTime)
            {
                Data = data;
                ScheduledTime = scheduledTime;
                IsTriggered = false;
            }
        }
        
        public void Initialize()
        {
            if (_isInitialized) return;
            
            MyLogger.Log("Initializing NotificationTriggerSystem", MyLogger.LogCategory.Default);

            // Запускаем таймер, который будет проверять уведомления каждую секунду
            _checkTimer = new Timer(CheckScheduledNotificationsCallback, null, 0, 1000);
            
            _isInitialized = true;
            MyLogger.Log("NotificationTriggerSystem initialized successfully", MyLogger.LogCategory.Default);
        }

        public void Dispose()
        {
            _checkTimer?.Dispose();
        }
        
        public void AddScheduledNotification(NotificationData notification, DateTime scheduledTime)
        {
            if (scheduledTime < DateTime.Now)
            {
                MyLogger.LogWarning("Cannot schedule notification in the past", MyLogger.LogCategory.Default);
                return;
            }
            
            var scheduledNotification = new ScheduledNotification(notification, scheduledTime);
            _scheduledNotifications[notification.Id] = scheduledNotification;
            
            MyLogger.Log($"Scheduled notification '{notification.Title}' for {scheduledTime}", MyLogger.LogCategory.Default);
        }
        
        public void CancelScheduledNotification(string notificationId)
        {
            if (_scheduledNotifications.ContainsKey(notificationId))
            {
                _scheduledNotifications.Remove(notificationId);
                MyLogger.Log($"Cancelled scheduled notification with ID: {notificationId}", MyLogger.LogCategory.Default);
            }
        }
        
        public void CancelAllScheduledNotifications()
        {
            _scheduledNotifications.Clear();
            MyLogger.Log("Cancelled all scheduled notifications", MyLogger.LogCategory.Default);
        }
        
        private void CheckScheduledNotificationsCallback(object state)
        {
            var now = DateTime.Now;
            List<string> triggeredNotificationIds = new List<string>();
            List<NotificationData> triggeredNotifications = new List<NotificationData>();
            
            lock (_scheduledNotifications)
            {
                foreach (var kvp in _scheduledNotifications)
                {
                    var scheduledNotification = kvp.Value;
                    
                    if (!scheduledNotification.IsTriggered && scheduledNotification.ScheduledTime <= now)
                    {
                        scheduledNotification.IsTriggered = true;
                        
                        if (scheduledNotification.Data.ExpiresAt.HasValue && scheduledNotification.Data.ExpiresAt.Value < now)
                        {
                            // Если уведомление уже истекло, не отправляем его
                            triggeredNotificationIds.Add(kvp.Key);
                            continue;
                        }
                        
                        triggeredNotifications.Add(scheduledNotification.Data);
                        triggeredNotificationIds.Add(kvp.Key);
                    }
                }
                
                // Удаляем отработанные уведомления
                foreach (var id in triggeredNotificationIds)
                {
                    _scheduledNotifications.Remove(id);
                }
            }
            
            // Вызываем событие для каждого сработавшего уведомления
            // Делаем это вне блока lock, чтобы не блокировать другие операции
            foreach (var notification in triggeredNotifications)
            {
                OnNotificationTriggered?.Invoke(notification);
            }
        }
    }
}