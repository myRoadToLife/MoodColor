using System.Threading.Tasks;
using Firebase;
using Firebase.Database;

namespace App.Develop.CommonServices.Firebase.Common
{
    /// <summary>
    /// Интерфейс для инициализации Firebase в соответствии с лучшими практиками
    /// </summary>
    public interface IFirebaseInitializer
    {
        /// <summary>
        /// Асинхронно инициализирует Firebase
        /// </summary>
        /// <returns>True, если инициализация прошла успешно</returns>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Получает состояние подключения к Firebase
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Событие изменения состояния подключения
        /// </summary>
        event System.Action<bool> ConnectionStateChanged;
    }
}