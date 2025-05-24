using System;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using App.Develop.Utils.Logging;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif
#if UNITY_IOS
using Unity.Notifications.iOS;
using App.Develop.Utils.Logging;
#endif

namespace App.Develop.CommonServices.Notifications
{
    /// <summary>
    /// Сервис для работы с push-уведомлениями на устройствах
    /// </summary>
    public class PushNotificationService : MonoBehaviour, INotificationService
    {
        private const string CHANNEL_ID_DEFAULT = "mood_color_default";
        private const string CHANNEL_ID_REMINDER = "mood_color_reminder";
        private const string CHANNEL_ID_IMPORTANT = "mood_color_important";
        
        // Имена иконок для Android уведомлений
        private const string SMALL_ICON_NAME = "notification_small_icon";
        private const string LARGE_ICON_NAME = "notification_large_icon";
        
        private bool _isInitialized = false;

        /// <summary>
        /// Имя сервиса для идентификации
        /// </summary>
        public string ServiceName => "PushNotifications";

        public Task Initialize()
        {
            if (_isInitialized) return Task.CompletedTask;
            
            MyLogger.Log("Initializing PushNotificationService", MyLogger.LogCategory.Default);

#if UNITY_ANDROID
            InitializeAndroid();
#endif

            _isInitialized = true;
            MyLogger.Log("PushNotificationService initialized successfully", MyLogger.LogCategory.Default);
            
            return Task.CompletedTask;
        }

#if UNITY_ANDROID
        private void InitializeAndroid()
        {
            AndroidNotificationCenter.CancelAllDisplayedNotifications();
            AndroidNotificationCenter.CancelAllScheduledNotifications();
            
            var channelList = AndroidNotificationCenter.GetNotificationChannels();
            foreach (var channel in channelList)
            {
                AndroidNotificationCenter.DeleteNotificationChannel(channel.Id);
            }
            
            var defaultChannel = new AndroidNotificationChannel()
            {
                Id = CHANNEL_ID_DEFAULT,
                Name = "Общие уведомления",
                Description = "Канал для обычных уведомлений",
                Importance = Importance.Default
            };
            AndroidNotificationCenter.RegisterNotificationChannel(defaultChannel);
            
            var reminderChannel = new AndroidNotificationChannel()
            {
                Id = CHANNEL_ID_REMINDER,
                Name = "Напоминания",
                Description = "Канал для напоминаний",
                Importance = Importance.High,
                CanShowBadge = true,
                EnableVibration = true
            };
            AndroidNotificationCenter.RegisterNotificationChannel(reminderChannel);
            
            var importantChannel = new AndroidNotificationChannel()
            {
                Id = CHANNEL_ID_IMPORTANT,
                Name = "Важные уведомления",
                Description = "Канал для важных уведомлений",
                Importance = Importance.High,
                CanShowBadge = true,
                EnableLights = true,
                EnableVibration = true
            };
            AndroidNotificationCenter.RegisterNotificationChannel(importantChannel);
        }
#endif

        public async void SendPushNotification(NotificationData notificationData)
        {
            if (!_isInitialized)
            {
                await Initialize();
            }
            
            MyLogger.Log($"Sending push notification: {notificationData.Title}", MyLogger.LogCategory.Default);

#if UNITY_ANDROID
            SendAndroidNotification(notificationData);
#else
            MyLogger.LogWarning("Push notifications are only supported on mobile platforms", MyLogger.LogCategory.Default);
#endif
        }

#if UNITY_ANDROID
        private void SendAndroidNotification(NotificationData notificationData)
        {
            var channelId = GetAndroidChannelId(notificationData.Category, notificationData.Priority);
            
            var notification = new AndroidNotification
            {
                Title = notificationData.Title,
                Text = notificationData.Message,
                SmallIcon = SMALL_ICON_NAME,
                LargeIcon = LARGE_ICON_NAME,
                FireTime = DateTime.Now
            };
            
            if (notificationData.ExpiresAt.HasValue)
            {
                notification.Group = notificationData.GroupId;
            }
            
            if (!string.IsNullOrEmpty(notificationData.GroupId))
            {
                notification.Group = notificationData.GroupId;
            }
            
            switch (notificationData.Priority)
            {
                case NotificationPriority.Low:
                    notification.ShouldAutoCancel = true;
                    break;
                case NotificationPriority.High:
                case NotificationPriority.Critical:
                    notification.ShouldAutoCancel = false;
                    break;
            }
            
            int id = AndroidNotificationCenter.SendNotification(notification, channelId);
            
            MyLogger.Log($"Android notification sent with ID: {id}");
        }
        
        private string GetAndroidChannelId(NotificationCategory category, NotificationPriority priority)
        {
            if (category == NotificationCategory.Reminder)
            {
                return CHANNEL_ID_REMINDER;
            }
            
            if (priority == NotificationPriority.High || priority == NotificationPriority.Critical)
            {
                return CHANNEL_ID_IMPORTANT;
            }
            
            return CHANNEL_ID_DEFAULT;
        }
#endif

        public async void CancelPushNotification(string notificationId)
        {
            if (!_isInitialized)
            {
                await Initialize();
            }
            
            MyLogger.Log($"Cancelling push notification with ID: {notificationId}", MyLogger.LogCategory.Default);

#if UNITY_ANDROID
            if (int.TryParse(notificationId, out int id))
            {
                AndroidNotificationCenter.CancelScheduledNotification(id);
                AndroidNotificationCenter.CancelDisplayedNotification(id);
            }
#endif
        }

        public async void CancelAllPushNotifications()
        {
            if (!_isInitialized)
            {
                await Initialize();
            }
            
            MyLogger.Log("Cancelling all push notifications", MyLogger.LogCategory.Default);

#if UNITY_ANDROID
            AndroidNotificationCenter.CancelAllScheduledNotifications();
            AndroidNotificationCenter.CancelAllDisplayedNotifications();
#endif
        }
    }
} 