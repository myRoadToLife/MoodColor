using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Notifications
{
    /// <summary>
    /// Очередь уведомлений, которая хранит и обрабатывает отложенные уведомления
    /// </summary>
    public class NotificationQueue : IDisposable
    {
        private Queue<NotificationData> _notificationQueue = new Queue<NotificationData>();
        private bool _isProcessing = false;
        private bool _isInitialized = false;
        private Timer _processTimer;
        
        // Время между отправкой уведомлений в очереди (в миллисекундах)
        private int _processingInterval = 5000;
        
        // Максимальное количество уведомлений в очереди
        private int _maxQueueSize = 100;

        // Ссылка на INotificationManager
        private INotificationManager _notificationManager;
        
        public NotificationQueue(INotificationManager notificationManager, int processingInterval = 5000, int maxQueueSize = 100)
        {
            _notificationManager = notificationManager;
            _processingInterval = processingInterval;
            _maxQueueSize = maxQueueSize;
        }
        
        public void Initialize()
        {
            if (_isInitialized) return;
            
            MyLogger.Log("Initializing NotificationQueue", MyLogger.LogCategory.Default);
            
            // Запускаем таймер для обработки очереди каждые 500 мс
            _processTimer = new Timer(ProcessQueueCallback, null, 0, 500);
            
            _isInitialized = true;
            MyLogger.Log("NotificationQueue initialized successfully", MyLogger.LogCategory.Default);
        }

        public void Dispose()
        {
            _processTimer?.Dispose();
        }
        
        public void EnqueueNotification(NotificationData notification)
        {
            if (_notificationQueue.Count >= _maxQueueSize)
            {
                MyLogger.LogWarning($"Notification queue is full. Dropping notification: {notification.Title}", MyLogger.LogCategory.Default);
                return;
            }
            
            lock (_notificationQueue)
            {
                _notificationQueue.Enqueue(notification);
            }
            MyLogger.Log($"Enqueued notification: {notification.Title}. Queue size: {_notificationQueue.Count}", MyLogger.LogCategory.Default);
        }
        
        public void ClearQueue()
        {
            lock (_notificationQueue)
            {
                _notificationQueue.Clear();
            }
            MyLogger.Log("Notification queue cleared", MyLogger.LogCategory.Default);
        }
        
        private void ProcessQueueCallback(object state)
        {
            if (_notificationQueue.Count > 0 && !_isProcessing)
            {
                _isProcessing = true;
                
                NotificationData notification = null;
                
                lock (_notificationQueue)
                {
                    if (_notificationQueue.Count > 0)
                    {
                        notification = _notificationQueue.Dequeue();
                    }
                }
                
                if (notification != null)
                {
                    // Проверяем, не истекло ли время жизни уведомления
                    if (!notification.IsExpired())
                    {
                        // Отправляем уведомление через NotificationManager
                        _notificationManager.SendImmediateNotification(notification);
                        MyLogger.Log($"Processed queued notification: {notification.Title}. Remaining in queue: {_notificationQueue.Count}", MyLogger.LogCategory.Default);
                    }
                    else
                    {
                        MyLogger.Log($"Skipped expired notification: {notification.Title}", MyLogger.LogCategory.Default);
                    }
                }
                
                // Задерживаем обработку следующего уведомления
                Thread.Sleep(_processingInterval);
                _isProcessing = false;
            }
        }
    }
}