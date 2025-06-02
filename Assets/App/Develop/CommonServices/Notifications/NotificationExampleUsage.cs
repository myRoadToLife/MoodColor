using System;
using System.Collections;
using UnityEngine;
using System.IO;
using App.Develop.Utils.Logging;
using App.Develop.DI;

namespace App.Develop.CommonServices.Notifications
{
    /// <summary>
    /// Пример использования системы уведомлений в приложении
    /// Обновлен для работы с новой архитектурой NotificationCoordinator
    /// </summary>
    public class NotificationExampleUsage : MonoBehaviour
    {
        [SerializeField] private bool _initializeOnStart = true;
        
        [Header("Иконки для уведомлений Android")]
        [SerializeField] private Texture2D _smallIcon;
        [SerializeField] private Texture2D _largeIcon;
        
        private INotificationManager _notificationManager;
        
        private void Start()
        {
            if (_initializeOnStart)
            {
                Initialize();
            }
        }
        
        /// <summary>
        /// Инициализирует систему уведомлений через DI контейнер
        /// </summary>
        public void Initialize()
        {
            // Получаем NotificationManager из DI контейнера
            try
            {
                // Предполагаем, что DI контейнер доступен глобально
                // В реальном проекте лучше инжектировать зависимости через конструктор или метод
                var container = FindObjectOfType<MonoBehaviour>().GetComponent<DIContainer>();
                if (container == null)
                {
                    // Альтернативный способ получения контейнера
                    // Можно использовать ServiceLocator или другой механизм доступа к DI
                    MyLogger.LogError("DI Container not found. Cannot initialize NotificationExampleUsage.", MyLogger.LogCategory.Default);
                    return;
                }
                
                _notificationManager = container.Resolve<INotificationManager>();
                
                if (_notificationManager == null)
                {
                    MyLogger.LogError("INotificationManager not found in DI container", MyLogger.LogCategory.Default);
                    return;
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Failed to resolve INotificationManager: {ex.Message}", MyLogger.LogCategory.Default);
                return;
            }
            
            // Проверяем и генерируем иконки, если они нужны
            #if UNITY_ANDROID
            CreateNotificationIcons();
            #endif
            
            MyLogger.Log("NotificationExampleUsage initialized successfully with new architecture", MyLogger.LogCategory.Default);
        }
        
        /// <summary>
        /// Альтернативный метод инициализации с прямой инъекцией зависимости
        /// Предпочтительный способ для использования в архитектуре с DI
        /// </summary>
        public void Initialize(INotificationManager notificationManager)
        {
            _notificationManager = notificationManager ?? throw new ArgumentNullException(nameof(notificationManager));
            
            // Проверяем и генерируем иконки, если они нужны
            #if UNITY_ANDROID
            CreateNotificationIcons();
            #endif
            
            MyLogger.Log("NotificationExampleUsage initialized with injected dependency", MyLogger.LogCategory.Default);
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
            string directoryPath = Application.dataPath + "/Plugins/Android/FirebaseApp.androidlib/res/drawable";
            
            // Создаем директорию, если она не существует
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            
            // Сохраняем PNG
            byte[] bytes = icon.EncodeToPNG();
            File.WriteAllBytes(directoryPath + "/" + name + ".png", bytes);
            
            MyLogger.Log($"Icon saved to {directoryPath}/{name}.png", MyLogger.LogCategory.Default);
            
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
                MyLogger.LogError("NotificationManager not initialized", MyLogger.LogCategory.Default);
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
                MyLogger.LogError("NotificationManager not initialized", MyLogger.LogCategory.Default);
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
            
            MyLogger.Log($"Notification scheduled for {seconds} seconds from now", MyLogger.LogCategory.Default);
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
                MyLogger.LogError("NotificationManager not initialized", MyLogger.LogCategory.Default);
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
            
            MyLogger.Log("All category notifications sent", MyLogger.LogCategory.Default);
        }
        
        /// <summary>
        /// Отменяет все запланированные уведомления
        /// </summary>
        public void CancelAllNotifications()
        {
            // Проверяем, что менеджер уведомлений создан
            if (_notificationManager == null)
            {
                MyLogger.LogError("NotificationManager not initialized", MyLogger.LogCategory.Default);
                return;
            }
            
            _notificationManager.CancelAllNotifications();
            MyLogger.Log("All notifications cancelled", MyLogger.LogCategory.Default);
        }
    }
}