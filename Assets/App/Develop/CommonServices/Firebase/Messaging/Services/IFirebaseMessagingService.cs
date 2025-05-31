using System;
using System.Threading.Tasks;
using Firebase.Messaging;

namespace App.Develop.CommonServices.Firebase.Messaging.Services
{
    /// <summary>
    /// Интерфейс сервиса для работы с Firebase Cloud Messaging
    /// </summary>
    public interface IFirebaseMessagingService
    {
        /// <summary>
        /// Событие получения сообщения
        /// </summary>
        event Action<FirebaseMessage> OnMessageReceived;
        
        /// <summary>
        /// Событие получения токена регистрации
        /// </summary>
        event Action<string> OnTokenReceived;
        
        /// <summary>
        /// Получает текущий токен для устройства
        /// </summary>
        /// <returns>Токен FCM</returns>
        Task<string> GetTokenAsync();
        
        /// <summary>
        /// Подписывает пользователя на указанную тему
        /// </summary>
        /// <param name="topic">Тема для подписки</param>
        Task SubscribeToTopic(string topic);
        
        /// <summary>
        /// Отписывает пользователя от указанной темы
        /// </summary>
        /// <param name="topic">Тема для отписки</param>
        Task UnsubscribeFromTopic(string topic);
        
        /// <summary>
        /// Инициализирует сервис и подписывается на события Firebase Messaging
        /// </summary>
        void Initialize();
    }
} 