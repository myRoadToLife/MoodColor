using System;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif
#if UNITY_IOS
using Unity.Notifications.iOS;
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

        public void Initialize()
        {
            if (_isInitialized) return;
            
            Debug.Log("Initializing PushNotificationService");

#if UNITY_ANDROID
            InitializeAndroid();
#endif

            _isInitialized = true;
            Debug.Log("PushNotificationService initialized successfully");
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

        public void SendPushNotification(NotificationData notificationData)
        {
            if (!_isInitialized)
            {
                Debug.LogError("PushNotificationService not initialized");
                return;
            }
            
            Debug.Log($"Sending push notification: {notificationData.Title}");

#if UNITY_ANDROID
            SendAndroidNotification(notificationData);
#else
            Debug.LogWarning("Push notifications are only supported on mobile platforms");
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
            
            Debug.Log($"Android notification sent with ID: {id}");
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

        public void CancelPushNotification(string notificationId)
        {
            if (!_isInitialized)
            {
                Debug.LogError("PushNotificationService not initialized");
                return;
            }
            
            Debug.Log($"Cancelling push notification with ID: {notificationId}");

#if UNITY_ANDROID
            if (int.TryParse(notificationId, out int id))
            {
                AndroidNotificationCenter.CancelScheduledNotification(id);
                AndroidNotificationCenter.CancelDisplayedNotification(id);
            }
#endif
        }

        public void CancelAllPushNotifications()
        {
            if (!_isInitialized)
            {
                Debug.LogError("PushNotificationService not initialized");
                return;
            }
            
            Debug.Log("Cancelling all push notifications");

#if UNITY_ANDROID
            AndroidNotificationCenter.CancelAllScheduledNotifications();
            AndroidNotificationCenter.CancelAllDisplayedNotifications();
#endif
        }
    }
} 