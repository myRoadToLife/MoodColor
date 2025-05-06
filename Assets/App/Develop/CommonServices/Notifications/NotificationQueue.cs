using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

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

        // Ссылка на NotificationManager
        private NotificationManager _notificationManager;
        
        public NotificationQueue(NotificationManager notificationManager, int processingInterval = 5000, int maxQueueSize = 100)
        {
            _notificationManager = notificationManager;
            _processingInterval = processingInterval;
            _maxQueueSize = maxQueueSize;
        }
        
        public void Initialize()
        {
            if (_isInitialized) return;
            
            Debug.Log("Initializing NotificationQueue");
            
            // Запускаем таймер для обработки очереди каждые 500 мс
            _processTimer = new Timer(ProcessQueueCallback, null, 0, 500);
            
            _isInitialized = true;
            Debug.Log("NotificationQueue initialized successfully");
        }

        public void Dispose()
        {
            _processTimer?.Dispose();
        }
        
        public void EnqueueNotification(NotificationData notification)
        {
            if (_notificationQueue.Count >= _maxQueueSize)
            {
                Debug.LogWarning($"Notification queue is full. Dropping notification: {notification.Title}");
                return;
            }
            
            lock (_notificationQueue)
            {
                _notificationQueue.Enqueue(notification);
            }
            Debug.Log($"Enqueued notification: {notification.Title}. Queue size: {_notificationQueue.Count}");
        }
        
        public void ClearQueue()
        {
            lock (_notificationQueue)
            {
                _notificationQueue.Clear();
            }
            Debug.Log("Notification queue cleared");
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
                        Debug.Log($"Processed queued notification: {notification.Title}. Remaining in queue: {_notificationQueue.Count}");
                    }
                    else
                    {
                        Debug.Log($"Skipped expired notification: {notification.Title}");
                    }
                }
                
                // Задерживаем обработку следующего уведомления
                Thread.Sleep(_processingInterval);
                _isProcessing = false;
            }
        }
    }
}