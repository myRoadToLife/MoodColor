using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace App.Develop.CommonServices.Firebase.Common
{
    /// <summary>
    /// Интерфейс для управления операциями Firebase в offline режиме
    /// </summary>
    public interface IOfflineManager
    {
        /// <summary>
        /// Выполняет операцию с учетом состояния подключения
        /// </summary>
        /// <param name="operation">Операция для выполнения</param>
        /// <returns>True, если операция выполнена успешно</returns>
        Task<bool> ExecuteOperationAsync(IDatabaseOperation operation);

        /// <summary>
        /// Добавляет операцию в очередь для выполнения при восстановлении подключения
        /// </summary>
        /// <param name="operation">Операция для постановки в очередь</param>
        void QueueOperation(IDatabaseOperation operation);

        /// <summary>
        /// Проверяет состояние подключения
        /// </summary>
        bool IsOnline { get; }

        /// <summary>
        /// Количество операций в очереди
        /// </summary>
        int QueuedOperationsCount { get; }

        /// <summary>
        /// Событие изменения состояния подключения
        /// </summary>
        event Action<bool> ConnectionStateChanged;

        /// <summary>
        /// Событие обработки очереди операций
        /// </summary>
        event Action<int> QueueProcessed;

        /// <summary>
        /// Очищает очередь операций
        /// </summary>
        void ClearQueue();

        /// <summary>
        /// Обновляет состояние подключения
        /// </summary>
        /// <param name="isConnected">Состояние подключения</param>
        void UpdateConnectionState(bool isConnected);
    }

    /// <summary>
    /// Интерфейс для операций базы данных
    /// </summary>
    public interface IDatabaseOperation
    {
        /// <summary>
        /// Выполняет операцию
        /// </summary>
        Task<bool> ExecuteAsync();

        /// <summary>
        /// Описание операции для логирования
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Приоритет операции (чем выше число, тем важнее)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Идентификатор операции для предотвращения дублирования
        /// </summary>
        string OperationId { get; }
    }
}