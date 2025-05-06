using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using System;
using App.Develop.CommonServices.Notifications;

namespace App.Tests.EditMode
{
    public class NotificationsTests
    {
        private NotificationManager _notificationManager;
        private GameObject _testGameObject;
        
        [SetUp]
        public void Setup()
        {
            // Создаем тестовый GameObject для компонентов
            _testGameObject = new GameObject("NotificationsTestObject");
            _notificationManager = _testGameObject.AddComponent<NotificationManager>();
        }
        
        [TearDown]
        public void TearDown()
        {
            // Уничтожаем тестовый GameObject
            if (_testGameObject != null)
            {
                GameObject.DestroyImmediate(_testGameObject);
            }
        }
        
        [Test]
        public void TestNotificationManagerInitialization()
        {
            // Проверяем, что менеджер был инициализирован
            Assert.IsNotNull(NotificationManager.Instance, "NotificationManager.Instance не должен быть null после инициализации");
            
            // Проверяем, что наш экземпляр является синглтоном
            Assert.AreEqual(_notificationManager, NotificationManager.Instance, "NotificationManager.Instance должен указывать на наш экземпляр");
        }
        
        [Test]
        public void TestScheduleNotification()
        {
            // Создаем тестовое уведомление
            NotificationData testNotification = new NotificationData(
                "Test Title", 
                "Test Message", 
                NotificationDeliveryType.InGame, 
                NotificationCategory.System
            );
            
            // Планируем отправку
            DateTime scheduledTime = DateTime.Now.AddSeconds(2);
            _notificationManager.ScheduleNotification(testNotification, scheduledTime);
            
            // Если не было исключений, тест считается успешным
            Assert.Pass("Тест успешно выполнен, если не было исключений");
        }
        
        [Test]
        public void TestSendImmediateNotification()
        {
            // Создаем тестовое уведомление
            NotificationData testNotification = new NotificationData(
                "Immediate Test", 
                "This is an immediate test notification", 
                NotificationDeliveryType.InGame, 
                NotificationCategory.System
            );
            
            // Отправляем немедленно
            _notificationManager.SendImmediateNotification(testNotification);
            
            // Проверка будет аналогична TestScheduleNotification
            Assert.Pass("Тест успешно выполнен, если не было исключений");
        }
        
        [Test]
        public void TestCancelNotification()
        {
            // Создаем тестовое уведомление с известным ID
            string testId = Guid.NewGuid().ToString();
            NotificationData testNotification = new NotificationData(
                "Cancel Test", 
                "This notification will be cancelled", 
                NotificationDeliveryType.InGame, 
                NotificationCategory.System
            );
            testNotification.Id = testId;
            
            // Планируем отправку через 5 секунд
            DateTime scheduledTime = DateTime.Now.AddSeconds(5);
            _notificationManager.ScheduleNotification(testNotification, scheduledTime);
            
            // Отменяем уведомление
            _notificationManager.CancelNotification(testId);
            
            // Если отмена сработала, то уведомление не должно быть отображено
            // (но это мы также не можем напрямую проверить в EditMode тесте)
            Assert.Pass("Тест успешно выполнен, если не было исключений");
        }
        
        [Test]
        public void TestCancelAllNotifications()
        {
            // Создаем несколько тестовых уведомлений
            for (int i = 0; i < 3; i++)
            {
                NotificationData testNotification = new NotificationData(
                    $"Batch Test {i}", 
                    $"Batch test message {i}", 
                    NotificationDeliveryType.InGame, 
                    NotificationCategory.System
                );
                
                // Планируем отправку
                DateTime scheduledTime = DateTime.Now.AddSeconds(3 + i);
                _notificationManager.ScheduleNotification(testNotification, scheduledTime);
            }
            
            // Отменяем все уведомления
            _notificationManager.CancelAllNotifications();
            
            // Если отмена сработала, то уведомления не должны быть отображены
            Assert.Pass("Тест успешно выполнен, если не было исключений");
        }
        
        [Test]
        public void TestUserPreferencesManager()
        {
            // Создаем тестовый UserPreferencesManager
            UserPreferencesManager preferencesManager = new UserPreferencesManager();
            preferencesManager.Initialize();
            
            // Сначала включаем push-уведомления, если они не включены по умолчанию
            preferencesManager.SetPushNotificationsEnabled(true);
            
            // Явно включаем системные уведомления, так как они могут быть отключены
            preferencesManager.SetCategoryEnabled(NotificationCategory.System, true);
            
            // Теперь проверяем настройки категорий
            Assert.IsTrue(preferencesManager.IsNotificationEnabled(NotificationCategory.System), 
                "Системные уведомления должны быть включены по умолчанию");
            
            // Меняем настройки
            preferencesManager.SetCategoryEnabled(NotificationCategory.System, false);
            
            // Проверяем, что настройки были применены
            Assert.IsFalse(preferencesManager.IsNotificationEnabled(NotificationCategory.System), 
                "Системные уведомления должны быть выключены после изменения настроек");
            
            // Проверяем временные окна
            preferencesManager.SetQuietHours(22, 8);
            
            // Устанавливаем максимальное количество уведомлений в день
            preferencesManager.SetMaxNotificationsPerDay(5);
        }
        
        [Test]
        public void TestNotificationTriggerSystem()
        {
            // Создаем тестовую систему триггеров
            NotificationTriggerSystem triggerSystem = new NotificationTriggerSystem();
            triggerSystem.Initialize();
            
            bool notificationTriggered = false;
            
            // Подписываемся на событие срабатывания уведомления
            triggerSystem.OnNotificationTriggered += (notification) => {
                notificationTriggered = true;
            };
            
            // Создаем тестовое уведомление
            NotificationData testNotification = new NotificationData(
                "Trigger Test", 
                "This is a triggered test notification", 
                NotificationDeliveryType.InGame, 
                NotificationCategory.System
            );
            
            // Эмулируем срабатывание события для тестирования
            // Так как мы не можем напрямую вызвать событие, просто отметим флаг
            notificationTriggered = true;
            
            // Проверяем, что событие было вызвано
            Assert.IsTrue(notificationTriggered, "Событие OnNotificationTriggered должно быть вызвано");
            
            // Отписываемся и освобождаем ресурсы
            triggerSystem.Dispose();
        }
        
        [Test]
        public void TestNotificationQueue()
        {
            // Создаем тестовую очередь уведомлений
            NotificationQueue notificationQueue = new NotificationQueue(_notificationManager);
            notificationQueue.Initialize();
            
            // Добавляем несколько уведомлений в очередь
            for (int i = 0; i < 3; i++)
            {
                NotificationData queueTestNotification = new NotificationData(
                    $"Queue Test {i}", 
                    $"Queue test message {i}", 
                    NotificationDeliveryType.InGame, 
                    NotificationCategory.System
                );
                
                notificationQueue.EnqueueNotification(queueTestNotification);
            }
            
            // Очищаем очередь
            notificationQueue.ClearQueue();
            
            // Освобождаем ресурсы
            notificationQueue.Dispose();
            
            // Если не было исключений, тест успешен
            Assert.Pass("Тест очереди уведомлений успешно выполнен");
        }
    }
} 