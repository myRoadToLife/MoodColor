using System;
using System.Threading.Tasks;
using App.Develop.CommonServices.Social;
using App.Develop.CommonServices.UI;
using App.Develop.DI;
using App.Develop.Utils.Logging;
using UnityEngine;

namespace App.Develop.CommonServices.Social
{
    /// <summary>
    /// Обработчик уведомлений о запросах в друзья
    /// </summary>
    public class FriendRequestNotificationHandler : MonoBehaviour, IInjectable
    {
        #region Private Fields
        private ISocialService _socialService;
        private NotificationManager _notificationManager;
        #endregion
        
        #region Unity Lifecycle
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        #endregion
        
        #region Initialization
        public void Inject(DIContainer container)
        {
            try
            {
                _socialService = container.Resolve<ISocialService>();
                if (_socialService == null)
                {
                    MyLogger.LogError("❌ FriendRequestNotificationHandler: Не удалось получить ISocialService!");
                    return;
                }
                
                _notificationManager = container.Resolve<NotificationManager>();
                if (_notificationManager == null)
                {
                    MyLogger.LogError("❌ FriendRequestNotificationHandler: Не удалось получить NotificationManager!");
                    return;
                }
                
                // Подписываемся на события уведомлений
                SubscribeToEvents();
                
                MyLogger.Log("✅ FriendRequestNotificationHandler успешно инициализирован");
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка инициализации FriendRequestNotificationHandler: {ex.Message}");
            }
        }
        
        private void SubscribeToEvents()
        {
            if (_socialService != null)
            {
                _socialService.OnNotificationReceived += HandleNotification;
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (_socialService != null)
            {
                _socialService.OnNotificationReceived -= HandleNotification;
            }
        }
        #endregion
        
        #region Notification Handling
        /// <summary>
        /// Обрабатывает полученное уведомление
        /// </summary>
        private void HandleNotification(SocialNotification notification)
        {
            if (notification == null) return;
            
            switch (notification.Type)
            {
                case NotificationType.FriendRequest:
                    HandleFriendRequest(notification);
                    break;
                    
                case NotificationType.FriendAccepted:
                    HandleFriendAccepted(notification);
                    break;
                    
                case NotificationType.EmotionReaction:
                    HandleEmotionReaction(notification);
                    break;
            }
        }
        
        /// <summary>
        /// Обрабатывает уведомление о запросе в друзья
        /// </summary>
        private void HandleFriendRequest(SocialNotification notification)
        {
            if (notification?.Data == null || !notification.Data.TryGetValue("senderId", out string senderId))
                return;
            
            // Создаем и показываем уведомление с кнопками принять/отклонить
            var notificationConfig = new NotificationConfig
            {
                Title = "Новый запрос в друзья",
                Message = notification.Message,
                Duration = 10f,
                ShowAcceptButton = true,
                ShowDeclineButton = true,
                OnAccept = () => AcceptFriendRequest(senderId),
                OnDecline = () => DeclineFriendRequest(senderId)
            };
            
            _notificationManager.ShowNotification(notificationConfig);
        }
        
        /// <summary>
        /// Обрабатывает уведомление о принятии запроса в друзья
        /// </summary>
        private void HandleFriendAccepted(SocialNotification notification)
        {
            // Показываем информационное уведомление
            var notificationConfig = new NotificationConfig
            {
                Title = "Запрос в друзья принят",
                Message = notification.Message,
                Duration = 5f
            };
            
            _notificationManager.ShowNotification(notificationConfig);
        }
        
        /// <summary>
        /// Обрабатывает уведомление о реакции на эмоцию
        /// </summary>
        private void HandleEmotionReaction(SocialNotification notification)
        {
            // Получаем данные о реакции
            if (notification?.Data == null || 
                !notification.Data.TryGetValue("emotionId", out string emotionId) ||
                !notification.Data.TryGetValue("reactionType", out string reactionTypeStr))
                return;
            
            if (!Enum.TryParse(reactionTypeStr, out ReactionType reactionType))
                return;
            
            string reactionName = GetReactionName(reactionType);
            
            // Показываем информационное уведомление
            var notificationConfig = new NotificationConfig
            {
                Title = "Новая реакция на эмоцию",
                Message = $"Кто-то отреагировал на вашу эмоцию: {reactionName}",
                Duration = 5f
            };
            
            _notificationManager.ShowNotification(notificationConfig);
        }
        #endregion
        
        #region Action Handlers
        /// <summary>
        /// Принимает запрос в друзья
        /// </summary>
        private async void AcceptFriendRequest(string senderId)
        {
            try
            {
                bool success = await _socialService.AcceptFriendRequest(senderId);
                
                if (success)
                {
                    // Показываем уведомление об успешном принятии запроса
                    _notificationManager.ShowNotification(new NotificationConfig
                    {
                        Title = "Запрос принят",
                        Message = "Пользователь добавлен в друзья",
                        Duration = 3f
                    });
                }
                else
                {
                    // Показываем уведомление об ошибке
                    _notificationManager.ShowNotification(new NotificationConfig
                    {
                        Title = "Ошибка",
                        Message = "Не удалось принять запрос в друзья",
                        Duration = 3f
                    });
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при принятии запроса в друзья: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Отклоняет запрос в друзья
        /// </summary>
        private async void DeclineFriendRequest(string senderId)
        {
            try
            {
                bool success = await _socialService.RemoveFriend(senderId);
                
                if (success)
                {
                    // Показываем уведомление об успешном отклонении запроса
                    _notificationManager.ShowNotification(new NotificationConfig
                    {
                        Title = "Запрос отклонен",
                        Message = "Запрос в друзья отклонен",
                        Duration = 3f
                    });
                }
                else
                {
                    // Показываем уведомление об ошибке
                    _notificationManager.ShowNotification(new NotificationConfig
                    {
                        Title = "Ошибка",
                        Message = "Не удалось отклонить запрос в друзья",
                        Duration = 3f
                    });
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при отклонении запроса в друзья: {ex.Message}");
            }
        }
        #endregion
        
        #region Helper Methods
        /// <summary>
        /// Возвращает русское название типа реакции
        /// </summary>
        private string GetReactionName(ReactionType reactionType)
        {
            switch (reactionType)
            {
                case ReactionType.Like:
                    return "Нравится";
                case ReactionType.Support:
                    return "Поддержка";
                case ReactionType.Hug:
                    return "Обнимаю";
                case ReactionType.Celebrate:
                    return "Поздравляю";
                default:
                    return "Неизвестная реакция";
            }
        }
        #endregion
    }
    
    /// <summary>
    /// Конфигурация для отображения уведомления
    /// </summary>
    public class NotificationConfig
    {
        /// <summary>
        /// Заголовок уведомления
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// Текст уведомления
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Длительность показа уведомления в секундах
        /// </summary>
        public float Duration { get; set; } = 5f;
        
        /// <summary>
        /// Показывать ли кнопку принятия
        /// </summary>
        public bool ShowAcceptButton { get; set; }
        
        /// <summary>
        /// Показывать ли кнопку отклонения
        /// </summary>
        public bool ShowDeclineButton { get; set; }
        
        /// <summary>
        /// Действие при нажатии на кнопку принятия
        /// </summary>
        public Action OnAccept { get; set; }
        
        /// <summary>
        /// Действие при нажатии на кнопку отклонения
        /// </summary>
        public Action OnDecline { get; set; }
    }
} 