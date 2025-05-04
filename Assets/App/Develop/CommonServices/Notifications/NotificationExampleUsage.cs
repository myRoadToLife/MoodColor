using System;
using System.Collections;
using UnityEngine;
using System.IO;

namespace MoodColor.App.Develop.CommonServices.Notifications
{
    /// <summary>
    /// Пример использования системы уведомлений
    /// </summary>
    public class NotificationExampleUsage : MonoBehaviour
    {
        [SerializeField] private bool _initializeOnStart = true;
        
        [Header("Иконки для уведомлений Android")]
        [SerializeField] private Texture2D _smallIcon;
        [SerializeField] private Texture2D _largeIcon;
        
        private NotificationManager _notificationManager;
        
        private void Start()
        {
            if (_initializeOnStart)
            {
                Initialize();
            }
        }
        
        /// <summary>
        /// Инициализирует систему уведомлений
        /// </summary>
        public void Initialize()
        {
            // Создаем или получаем существующий NotificationManager
            _notificationManager = NotificationManager.CreateInstance();
            
            // Проверяем и генерируем иконки, если они нужны
            #if UNITY_ANDROID
            CreateNotificationIcons();
            #endif
            
            Debug.Log("NotificationExampleUsage initialized successfully");
        }
        
        /// <summary>
        /// Создает и сохраняет иконки для Android уведомлений
        /// </summary>
        private void CreateNotificationIcons()
        {
            #if UNITY_ANDROID && UNITY_EDITOR
            if (_smallIcon != null)
            {
                SaveIconToProject(_smallIcon, "notification_small_icon");
            }
            
            if (_largeIcon != null)
            {
                SaveIconToProject(_largeIcon, "notification_large_icon");
            }
            #endif
        }
        
        /// <summary>
        /// Сохраняет иконку в проект для использования в уведомлениях
        /// </summary>
        private void SaveIconToProject(Texture2D icon, string name)
        {
            #if UNITY_ANDROID && UNITY_EDITOR
            // Путь сохранения для Android
            string directoryPath = Application.dataPath + "/Plugins/Android/res/drawable";
            
            // Создаем директорию, если она не существует
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            
            // Сохраняем PNG
            byte[] bytes = icon.EncodeToPNG();
            File.WriteAllBytes(directoryPath + "/" + name + ".png", bytes);
            
            Debug.Log($"Icon saved to {directoryPath}/{name}.png");
            
            // Заставим Unity обновить Project View
            UnityEditor.AssetDatabase.Refresh();
            #endif
        }
        
        /// <summary>
        /// Отправляет тестовое push-уведомление
        /// </summary>
        public void SendTestPushNotification()
        {
            // Проверяем, что менеджер уведомлений создан
            if (_notificationManager == null)
            {
                Debug.LogError("NotificationManager not initialized");
                return;
            }
            
            // Создаем данные уведомления
            NotificationData notification = new NotificationData(
                "Тестовое уведомление",
                "Это тестовое push-уведомление",
                NotificationDeliveryType.Push,
                NotificationCategory.System
            );
            
            // Отправляем уведомление немедленно
            _notificationManager.SendImmediateNotification(notification);
        }
        
        /// <summary>
        /// Планирует отправку уведомления через указанное количество секунд
        /// </summary>
        public void ScheduleNotificationIn(float seconds)
        {
            // Проверяем, что менеджер уведомлений создан
            if (_notificationManager == null)
            {
                Debug.LogError("NotificationManager not initialized");
                return;
            }
            
            // Создаем данные уведомления
            NotificationData notification = new NotificationData(
                "Запланированное уведомление",
                $"Это уведомление было запланировано на {DateTime.Now.AddSeconds(seconds)}",
                NotificationDeliveryType.Push,
                NotificationCategory.Reminder
            );
            
            // Задаем время истечения через день
            notification.ExpiresAt = DateTime.Now.AddDays(1);
            
            // Планируем отправку через указанное количество секунд
            _notificationManager.ScheduleNotification(notification, DateTime.Now.AddSeconds(seconds));
            
            Debug.Log($"Notification scheduled for {seconds} seconds from now");
        }
        
        /// <summary>
        /// Тестирует различные категории уведомлений
        /// </summary>
        public void TestAllNotificationCategories()
        {
            StartCoroutine(SendCategoryNotificationsSequence());
        }
        
        private IEnumerator SendCategoryNotificationsSequence()
        {
            // Проверяем, что менеджер уведомлений создан
            if (_notificationManager == null)
            {
                Debug.LogError("NotificationManager not initialized");
                yield break;
            }
            
            // Отправляем уведомления каждой категории с интервалом
            foreach (NotificationCategory category in Enum.GetValues(typeof(NotificationCategory)))
            {
                NotificationData notification = new NotificationData(
                    $"Уведомление категории {category}",
                    $"Это тестовое уведомление категории {category}",
                    NotificationDeliveryType.Push,
                    category
                );
                
                _notificationManager.SendImmediateNotification(notification);
                
                // Ждем 2 секунды перед отправкой следующего
                yield return new WaitForSeconds(2f);
            }
            
            Debug.Log("All category notifications sent");
        }
        
        /// <summary>
        /// Отменяет все запланированные уведомления
        /// </summary>
        public void CancelAllNotifications()
        {
            // Проверяем, что менеджер уведомлений создан
            if (_notificationManager == null)
            {
                Debug.LogError("NotificationManager not initialized");
                return;
            }
            
            _notificationManager.CancelAllNotifications();
            Debug.Log("All notifications cancelled");
        }
    }
}