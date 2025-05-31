using System;
using System.Threading.Tasks;
using Firebase.Messaging;
using App.Develop.Utils.Logging;
using UnityEngine;

namespace App.Develop.CommonServices.Firebase.Messaging.Services
{
    /// <summary>
    /// Сервис для работы с Firebase Cloud Messaging
    /// </summary>
    public class FirebaseMessagingService : IFirebaseMessagingService
    {
        #region Private Fields
        private bool _isInitialized;
        #endregion

        #region Events
        /// <summary>
        /// Событие получения сообщения
        /// </summary>
        public event Action<FirebaseMessage> OnMessageReceived;
        
        /// <summary>
        /// Событие получения токена регистрации
        /// </summary>
        public event Action<string> OnTokenReceived;
        #endregion
        
        #region Constructor
        /// <summary>
        /// Создает новый экземпляр сервиса Firebase Cloud Messaging
        /// </summary>
        public FirebaseMessagingService()
        {
            MyLogger.Log("✅ FirebaseMessagingService создан", MyLogger.LogCategory.Firebase);
        }
        #endregion
        
        #region IFirebaseMessagingService Implementation
        /// <summary>
        /// Инициализирует сервис и подписывается на события Firebase Messaging
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;
            
            try
            {
                // Подписываемся на события Firebase Messaging
                FirebaseMessaging.TokenReceived += OnFirebaseTokenReceived;
                FirebaseMessaging.MessageReceived += OnFirebaseMessageReceived;
                
                MyLogger.Log("✅ FirebaseMessagingService инициализирован", MyLogger.LogCategory.Firebase);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка инициализации FirebaseMessagingService: {ex.Message}\n{ex.StackTrace}", MyLogger.LogCategory.Firebase);
            }
        }
        
        /// <summary>
        /// Получает текущий токен для устройства
        /// </summary>
        /// <returns>Токен FCM</returns>
        public async Task<string> GetTokenAsync()
        {
            if (!_isInitialized)
            {
                MyLogger.LogWarning("⚠️ [Messaging] Попытка получить токен FCM до инициализации сервиса", MyLogger.LogCategory.Firebase);
                return null;
            }
            
            try
            {
                string token = await FirebaseMessaging.GetTokenAsync();
                MyLogger.Log($"📱 [Messaging] Получен токен FCM: {token}", MyLogger.LogCategory.Firebase);
                return token;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [Messaging] Ошибка при получении токена FCM: {ex.Message}", MyLogger.LogCategory.Firebase);
                return null;
            }
        }
        
        /// <summary>
        /// Подписывает пользователя на указанную тему
        /// </summary>
        /// <param name="topic">Тема для подписки</param>
        public async Task SubscribeToTopic(string topic)
        {
            if (!_isInitialized || string.IsNullOrEmpty(topic))
            {
                MyLogger.LogWarning("⚠️ [Messaging] Попытка подписаться на тему до инициализации сервиса или пустая тема", MyLogger.LogCategory.Firebase);
                return;
            }
            
            try
            {
                await FirebaseMessaging.SubscribeAsync(topic);
                MyLogger.Log($"📱 [Messaging] Подписка на тему: {topic}", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [Messaging] Ошибка при подписке на тему {topic}: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }
        
        /// <summary>
        /// Отписывает пользователя от указанной темы
        /// </summary>
        /// <param name="topic">Тема для отписки</param>
        public async Task UnsubscribeFromTopic(string topic)
        {
            if (!_isInitialized || string.IsNullOrEmpty(topic))
            {
                MyLogger.LogWarning("⚠️ [Messaging] Попытка отписаться от темы до инициализации сервиса или пустая тема", MyLogger.LogCategory.Firebase);
                return;
            }
            
            try
            {
                await FirebaseMessaging.UnsubscribeAsync(topic);
                MyLogger.Log($"📱 [Messaging] Отписка от темы: {topic}", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [Messaging] Ошибка при отписке от темы {topic}: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }
        #endregion
        
        #region Event Handlers
        private void OnFirebaseTokenReceived(object sender, TokenReceivedEventArgs token)
        {
            string tokenStr = token.Token;
            MyLogger.Log($"📱 [Messaging] Получен новый токен FCM: {tokenStr}", MyLogger.LogCategory.Firebase);
            
            // Вызываем событие получения токена
            OnTokenReceived?.Invoke(tokenStr);
        }
        
        private void OnFirebaseMessageReceived(object sender, MessageReceivedEventArgs messageData)
        {
            FirebaseMessage message = messageData.Message;
            
            if (message == null)
            {
                MyLogger.LogWarning("⚠️ [Messaging] Получено сообщение, но оно null", MyLogger.LogCategory.Firebase);
                return;
            }
            
            // Логируем информацию о сообщении
            string notificationTitle = message.Notification?.Title ?? "Нет заголовка";
            string notificationBody = message.Notification?.Body ?? "Нет текста";
            string dataStr = "";
            
            if (message.Data != null && message.Data.Count > 0)
            {
                foreach (var pair in message.Data)
                {
                    dataStr += $"{pair.Key}={pair.Value}, ";
                }
                dataStr = dataStr.TrimEnd(',', ' ');
            }
            else
            {
                dataStr = "Нет данных";
            }
            
            MyLogger.Log($"📱 [Messaging] Получено сообщение:\nЗаголовок: {notificationTitle}\nТекст: {notificationBody}\nДанные: {dataStr}", 
                MyLogger.LogCategory.Firebase);
            
            // Вызываем событие получения сообщения
            OnMessageReceived?.Invoke(message);
        }
        #endregion
    }
}