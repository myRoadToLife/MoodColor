using System;
using System.Threading.Tasks;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Интерфейс для управления соединением с Firebase
    /// </summary>
    public interface IConnectionManager : IDisposable
    {
        /// <summary>
        /// Текущее состояние соединения
        /// </summary>
        bool IsConnected { get; }
        
        /// <summary>
        /// Включен ли режим сохранения соединения
        /// </summary>
        bool KeepConnection { get; set; }
        
        /// <summary>
        /// Интервал проверки соединения в секундах
        /// </summary>
        float ConnectionCheckInterval { get; set; }
        
        /// <summary>
        /// Включено ли офлайн кэширование
        /// </summary>
        bool IsOfflineCachingEnabled { get; set; }
        
        /// <summary>
        /// Время бездействия в секундах, после которого отключается персистентность (для экономии ресурсов)
        /// </summary>
        float DisablePersistenceAfterInactivitySeconds { get; set; }
        
        /// <summary>
        /// Добавляет слушателя состояния соединения
        /// </summary>
        /// <param name="listener">Функция обратного вызова</param>
        void AddConnectionStateListener(Action<bool> listener);
        
        /// <summary>
        /// Удаляет слушателя состояния соединения
        /// </summary>
        /// <param name="listener">Функция обратного вызова</param>
        void RemoveConnectionStateListener(Action<bool> listener);
        
        /// <summary>
        /// Проверяет соединение с Firebase
        /// </summary>
        /// <returns>true, если соединение активно, иначе false</returns>
        Task<bool> CheckConnection();
        
        /// <summary>
        /// Проверяет, нужно ли выполнить проверку соединения
        /// </summary>
        /// <returns>true, если требуется проверка</returns>
        bool ShouldCheckConnection();
        
        /// <summary>
        /// Регистрирует активность пользователя
        /// </summary>
        void RegisterActivity();
        
        /// <summary>
        /// Устанавливает приоритет для сетевого или кэшированного доступа
        /// </summary>
        /// <param name="prioritizeNetwork">true для приоритета сети, false для приоритета кэша</param>
        void SetNetworkPriority(bool prioritizeNetwork);
        
        /// <summary>
        /// Устанавливает тайм-аут для сетевых операций
        /// </summary>
        /// <param name="timeoutMs">Тайм-аут в миллисекундах</param>
        void SetNetworkTimeout(long timeoutMs);
        
        /// <summary>
        /// Устанавливает размер кэша
        /// </summary>
        /// <param name="sizeBytes">Размер кэша в байтах</param>
        void SetCacheSize(long sizeBytes);
    }
} 