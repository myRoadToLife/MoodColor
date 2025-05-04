using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoodColor.App.Develop.CommonServices.Notifications
{
    public class NotificationQueue : MonoBehaviour
    {
        private Queue<NotificationData> _notificationQueue = new Queue<NotificationData>();
        private bool _isProcessing = false;
        private bool _isInitialized = false;
        
        // Время между отправкой уведомлений в очереди
        [SerializeField] private float _processingInterval = 5f;
        
        // Максимальное количество уведомлений в очереди
        [SerializeField] private int _maxQueueSize = 100;
        
        public void Initialize()
        {
            if (_isInitialized) return;
            
            Debug.Log("Initializing NotificationQueue");
            
            // Начинаем обработку очереди
            StartCoroutine(ProcessQueueRoutine());
            
            _isInitialized = true;
            Debug.Log("NotificationQueue initialized successfully");
        }
        
        public void EnqueueNotification(NotificationData notification)
        {
            if (_notificationQueue.Count >= _maxQueueSize)
            {
                Debug.LogWarning($"Notification queue is full. Dropping notification: {notification.Title}");
                return;
            }
            
            _notificationQueue.Enqueue(notification);
            Debug.Log($"Enqueued notification: {notification.Title}. Queue size: {_notificationQueue.Count}");
        }
        
        public void ClearQueue()
        {
            _notificationQueue.Clear();
            Debug.Log("Notification queue cleared");
        }
        
        private IEnumerator ProcessQueueRoutine()
        {
            while (true)
            {
                if (_notificationQueue.Count > 0 && !_isProcessing)
                {
                    _isProcessing = true;
                    
                    NotificationData notification = _notificationQueue.Dequeue();
                    
                    // Проверяем, не истекло ли время жизни уведомления
                    if (!notification.IsExpired())
                    {
                        // Отправляем уведомление через NotificationManager
                        NotificationManager.Instance.SendImmediateNotification(notification);
                        Debug.Log($"Processed queued notification: {notification.Title}. Remaining in queue: {_notificationQueue.Count}");
                    }
                    else
                    {
                        Debug.Log($"Skipped expired notification: {notification.Title}");
                    }
                    
                    // Ждем указанный интервал перед обработкой следующего
                    yield return new WaitForSeconds(_processingInterval);
                    _isProcessing = false;
                }
                
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}