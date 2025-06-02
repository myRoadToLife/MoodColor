using System;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Notifications
{
    /// <summary>
    /// Координатор уведомлений, который управляет всеми типами уведомлений
    /// Заменяет NotificationManager без использования Singleton паттерна
    /// Обычный класс, получает зависимости через конструктор
    /// </summary>
    public class NotificationCoordinator : INotificationCoordinator
    {
        private readonly PushNotificationService _pushService;
        private readonly InGameNotificationService _inGameService;
        private readonly EmailNotificationService _emailService;
        private readonly NotificationQueue _notificationQueue;
        private readonly NotificationTriggerSystem _triggerSystem;
        private readonly UserPreferencesManager _preferencesManager;
        
        private bool _isInitialized = false;

        /// <summary>
        /// Конструктор с инъекцией зависимостей
        /// </summary>
        public NotificationCoordinator(
            PushNotificationService pushService,
            InGameNotificationService inGameService,
            EmailNotificationService emailService,
            NotificationQueue notificationQueue,
            NotificationTriggerSystem triggerSystem,
            UserPreferencesManager preferencesManager)
        {
            _pushService = pushService ?? throw new ArgumentNullException(nameof(pushService));
            _inGameService = inGameService ?? throw new ArgumentNullException(nameof(inGameService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _notificationQueue = notificationQueue ?? throw new ArgumentNullException(nameof(notificationQueue));
            _triggerSystem = triggerSystem ?? throw new ArgumentNullException(nameof(triggerSystem));
            _preferencesManager = preferencesManager ?? throw new ArgumentNullException(nameof(preferencesManager));
        }

        /// <summary>
        /// Инициализирует координатор уведомлений
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            
            MyLogger.Log("🔔 Инициализация NotificationCoordinator...", MyLogger.LogCategory.Bootstrap);
            
            try
            {
                // Инициализируем все сервисы
                _pushService.Initialize();
                _inGameService.Initialize();
                _emailService.Initialize();
                _preferencesManager.Initialize();
                _triggerSystem.Initialize();
                _notificationQueue.Initialize();
                
                // Подписываемся на события
                SubscribeToEvents();
                
                _isInitialized = true;
                MyLogger.Log("✅ NotificationCoordinator инициализирован успешно", MyLogger.LogCategory.Bootstrap);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка инициализации NotificationCoordinator: {ex.Message}", MyLogger.LogCategory.Bootstrap);
                throw;
            }
        }

        private void SubscribeToEvents()
        {
            // Подписываемся на события триггера
            _triggerSystem.OnNotificationTriggered += HandleNotificationTriggered;
        }

        /// <summary>
        /// Освобождает ресурсы
        /// </summary>
        public void Dispose()
        {
            UnsubscribeFromEvents();
            DisposeResources();
        }

        private void UnsubscribeFromEvents()
        {
            if (_triggerSystem != null)
                _triggerSystem.OnNotificationTriggered -= HandleNotificationTriggered;
        }

        private void DisposeResources()
        {
            _triggerSystem?.Dispose();
            _notificationQueue?.Dispose();
        }

        /// <summary>
        /// Обработчик события срабатывания триггера уведомления
        /// </summary>
        private void HandleNotificationTriggered(NotificationData notification)
        {
            try
            {
                // Проверка пользовательских настроек
                if (!_preferencesManager.IsNotificationEnabled(notification.Category))
                {
                    MyLogger.Log($"Уведомление категории {notification.Category} отключено в настройках пользователя", MyLogger.LogCategory.Default);
                    return;
                }
                
                // Проверка временного окна
                if (!_preferencesManager.IsTimeWindowAllowed())
                {
                    // Добавляем в очередь для отправки позже
                    _notificationQueue.EnqueueNotification(notification);
                    MyLogger.Log("Уведомление добавлено в очередь из-за ограничений временного окна", MyLogger.LogCategory.Default);
                    return;
                }
                
                // Отправка уведомления через соответствующий сервис
                SendNotificationThroughService(notification);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка обработки уведомления: {ex.Message}", MyLogger.LogCategory.Default);
            }
        }

        private void SendNotificationThroughService(NotificationData notification)
        {
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
                default:
                    MyLogger.LogWarning($"Неизвестный тип доставки уведомления: {notification.DeliveryType}", MyLogger.LogCategory.Default);
                    break;
            }
        }

        /// <summary>
        /// Отправляет email-уведомление пользователю
        /// </summary>
        private void SendEmailNotification(NotificationData notification)
        {
            // Получаем email пользователя из настроек
            string userEmail = _preferencesManager.GetUserEmail();
            
            if (string.IsNullOrEmpty(userEmail))
            {
                MyLogger.LogWarning("Невозможно отправить email-уведомление: email пользователя не установлен", MyLogger.LogCategory.Default);
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
            if (!_isInitialized)
            {
                MyLogger.LogError("NotificationCoordinator не инициализирован", MyLogger.LogCategory.Default);
                return;
            }
            
            _triggerSystem.AddScheduledNotification(notification, scheduledTime);
        }
        
        /// <summary>
        /// Отправляет уведомление немедленно
        /// </summary>
        public void SendImmediateNotification(NotificationData notification)
        {
            if (!_isInitialized)
            {
                MyLogger.LogError("NotificationCoordinator не инициализирован", MyLogger.LogCategory.Default);
                return;
            }
            
            HandleNotificationTriggered(notification);
        }

        /// <summary>
        /// Отменяет запланированное уведомление по ID
        /// </summary>
        public void CancelNotification(string notificationId)
        {
            if (!_isInitialized)
            {
                MyLogger.LogError("NotificationCoordinator не инициализирован", MyLogger.LogCategory.Default);
                return;
            }
            
            _triggerSystem.CancelScheduledNotification(notificationId);
            _pushService.CancelPushNotification(notificationId);
        }
        
        /// <summary>
        /// Отменяет все запланированные уведомления
        /// </summary>
        public void CancelAllNotifications()
        {
            if (!_isInitialized)
            {
                MyLogger.LogError("NotificationCoordinator не инициализирован", MyLogger.LogCategory.Default);
                return;
            }
            
            _triggerSystem.CancelAllScheduledNotifications();
            _pushService.CancelAllPushNotifications();
        }
        
        #endregion

        #region Public Properties для доступа к компонентам
        
        /// <summary>
        /// Менеджер пользовательских настроек
        /// </summary>
        public UserPreferencesManager PreferencesManager => _preferencesManager;
        
        /// <summary>
        /// Система триггеров уведомлений
        /// </summary>
        public NotificationTriggerSystem TriggerSystem => _triggerSystem;
        
        #endregion
    }
} 