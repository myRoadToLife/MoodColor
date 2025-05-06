using System;
using UnityEngine;
#if UNITY_ANDROID
#endif
#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

namespace App.Develop.CommonServices.Notifications
{
    /// <summary>
    /// Основной менеджер уведомлений, который координирует все типы уведомлений
    /// </summary>
    public class NotificationManager : MonoBehaviour, INotificationManager
    {
        #region Singleton

        public static NotificationManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }
        
        /// <summary>
        /// Создает GameObject с NotificationManager, если он еще не существует в сцене
        /// </summary>
        public static NotificationManager CreateInstance()
        {
            if (Instance != null)
                return Instance;
                
            GameObject notificationManagerObject = new GameObject("NotificationManager");
            NotificationManager manager = notificationManagerObject.AddComponent<NotificationManager>();
            return manager;
        }

        #endregion

        [SerializeField] private bool _initializeOnAwake = true;
        
        private PushNotificationService _pushService;
        private InGameNotificationService _inGameService;
        private EmailNotificationService _emailService;
        private NotificationQueue _notificationQueue;
        private NotificationTriggerSystem _triggerSystem;
        private UserPreferencesManager _preferencesManager;
        
        private bool _isInitialized = false;

        private void Initialize()
        {
            if (_isInitialized) return;
            
            Debug.Log("Initializing NotificationManager");
            
            // Создаем и инициализируем компоненты
            _pushService = gameObject.AddComponent<PushNotificationService>();
            _inGameService = gameObject.AddComponent<InGameNotificationService>();
            _emailService = gameObject.AddComponent<EmailNotificationService>();
            
            // Создаем компоненты, которые не требуют MonoBehaviour
            _preferencesManager = new UserPreferencesManager();
            _triggerSystem = new NotificationTriggerSystem();
            _notificationQueue = new NotificationQueue(this);
            
            // Последовательность инициализации
            _preferencesManager.Initialize();
            _pushService.Initialize();
            _inGameService.Initialize();
            _emailService.Initialize();
            _notificationQueue.Initialize();
            _triggerSystem.Initialize();
            
            // Подписываемся на события триггера
            _triggerSystem.OnNotificationTriggered += HandleNotificationTriggered;
            
            _isInitialized = true;
            Debug.Log("NotificationManager initialized successfully");
        }

        private void OnDestroy()
        {
            // Отписываемся от событий
            if (_triggerSystem != null)
                _triggerSystem.OnNotificationTriggered -= HandleNotificationTriggered;
            
            // Освобождаем ресурсы
            _triggerSystem?.Dispose();
            _notificationQueue?.Dispose();
        }

        /// <summary>
        /// Обработчик события срабатывания триггера уведомления
        /// </summary>
        private void HandleNotificationTriggered(NotificationData notification)
        {
            // Проверка пользовательских настроек
            if (!_preferencesManager.IsNotificationEnabled(notification.Category))
            {
                Debug.Log($"Notification of category {notification.Category} is disabled by user preferences");
                return;
            }
            
            // Проверка временного окна
            if (!_preferencesManager.IsTimeWindowAllowed())
            {
                // Добавляем в очередь для отправки позже
                _notificationQueue.EnqueueNotification(notification);
                Debug.Log($"Notification added to queue due to time window restrictions");
                return;
            }
            
            // Отправка уведомления через соответствующий сервис
            switch (notification.DeliveryType)
            {
                case NotificationDeliveryType.Push:
                    _pushService.SendPushNotification(notification);
                    break;
                case NotificationDeliveryType.InGame:
                    _inGameService.ShowNotification(notification);
                    break;
                case NotificationDeliveryType.Email:
                    SendEmailNotification(notification);
                    break;
            }
        }

        /// <summary>
        /// Отправляет email-уведомление пользователю
        /// </summary>
        private void SendEmailNotification(NotificationData notification)
        {
            // Получаем email пользователя из настроек или аккаунта
            string userEmail = _preferencesManager.GetUserEmail();
            
            if (string.IsNullOrEmpty(userEmail))
            {
                Debug.LogWarning("Cannot send email notification: user email is not set");
                return;
            }
            
            _emailService.SendEmailNotification(notification, userEmail);
        }
        
        #region INotificationManager Implementation
        
        /// <summary>
        /// Планирует отправку уведомления
        /// </summary>
        public void ScheduleNotification(NotificationData notification, DateTime scheduledTime)
        {
            _triggerSystem.AddScheduledNotification(notification, scheduledTime);
        }
        
        /// <summary>
        /// Отправляет уведомление немедленно
        /// </summary>
        public void SendImmediateNotification(NotificationData notification)
        {
            HandleNotificationTriggered(notification);
        }

        /// <summary>
        /// Отменяет запланированное уведомление по ID
        /// </summary>
        public void CancelNotification(string notificationId)
        {
            _triggerSystem.CancelScheduledNotification(notificationId);
            _pushService.CancelPushNotification(notificationId);
        }
        
        /// <summary>
        /// Отменяет все запланированные уведомления
        /// </summary>
        public void CancelAllNotifications()
        {
            _triggerSystem.CancelAllScheduledNotifications();
            _pushService.CancelAllPushNotifications();
        }
        
        #endregion
    }
    
    /// <summary>
    /// Фабрика для создания менеджера уведомлений
    /// </summary>
    public static class NotificationManagerFactory
    {
        /// <summary>
        /// Создает и возвращает экземпляр менеджера уведомлений
        /// </summary>
        public static INotificationManager Create()
        {
            // Проверяем существующий экземпляр
            if (NotificationManager.Instance != null)
                return NotificationManager.Instance;
                
            // Создаем новый экземпляр
            return NotificationManager.CreateInstance();
        }
    }
}