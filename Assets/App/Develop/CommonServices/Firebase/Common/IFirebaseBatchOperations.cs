using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace App.Develop.CommonServices.Firebase.Common
{
    /// <summary>
    /// Интерфейс для выполнения batch операций Firebase
    /// </summary>
    public interface IFirebaseBatchOperations
    {
        /// <summary>
        /// Выполняет множественное обновление данных в одной транзакции
        /// </summary>
        /// <param name="updates">Словарь с путями и значениями для обновления</param>
        /// <returns>True, если операция выполнена успешно</returns>
        Task<bool> UpdateMultipleRecordsAsync(Dictionary<string, object> updates);

        /// <summary>
        /// Создает множественные записи в разных узлах
        /// </summary>
        /// <param name="records">Словарь с путями и данными для создания</param>
        /// <returns>True, если операция выполнена успешно</returns>
        Task<bool> CreateMultipleRecordsAsync(Dictionary<string, object> records);

        /// <summary>
        /// Удаляет множественные записи
        /// </summary>
        /// <param name="paths">Список путей для удаления</param>
        /// <returns>True, если операция выполнена успешно</returns>
        Task<bool> DeleteMultipleRecordsAsync(List<string> paths);

        /// <summary>
        /// Выполняет атомарную операцию (все или ничего)
        /// </summary>
        /// <param name="batchOperation">Операция для выполнения</param>
        /// <returns>True, если операция выполнена успешно</returns>
        Task<bool> ExecuteAtomicOperationAsync(Func<Dictionary<string, object>, Task> batchOperation);

        /// <summary>
        /// Создает операцию для управления fan-out структурой данных
        /// </summary>
        /// <param name="fanOutOperations">Операции fan-out</param>
        /// <returns>True, если операция выполнена успешно</returns>
        Task<bool> ExecuteFanOutOperationAsync(IEnumerable<FanOutOperation> fanOutOperations);

        /// <summary>
        /// Получает множественные записи одним запросом
        /// </summary>
        /// <param name="paths">Пути для получения данных</param>
        /// <returns>Словарь с данными по каждому пути</returns>
        Task<Dictionary<string, object>> GetMultipleRecordsAsync(List<string> paths);

        /// <summary>
        /// Максимальное количество операций в одном batch (по умолчанию 500)
        /// </summary>
        int MaxBatchSize { get; set; }
    }

    /// <summary>
    /// Операция fan-out для нормализованной структуры данных
    /// </summary>
    public class FanOutOperation
    {
        /// <summary>
        /// Путь к данным
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Значение для записи
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Тип операции
        /// </summary>
        public FanOutOperationType OperationType { get; set; }

        /// <summary>
        /// Описание операции
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// Тип операции fan-out
    /// </summary>
    public enum FanOutOperationType
    {
        /// <summary>
        /// Установить значение
        /// </summary>
        Set,

        /// <summary>
        /// Обновить значение
        /// </summary>
        Update,

        /// <summary>
        /// Удалить значение
        /// </summary>
        Delete,

        /// <summary>
        /// Добавить к списку
        /// </summary>
        Push
    }

    /// <summary>
    /// Результат batch операции
    /// </summary>
    public class BatchOperationResult
    {
        /// <summary>
        /// Успешность выполнения операции
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Сообщение об ошибке (если есть)
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Количество успешно обработанных операций
        /// </summary>
        public int ProcessedOperations { get; set; }

        /// <summary>
        /// Общее количество операций
        /// </summary>
        public int TotalOperations { get; set; }

        /// <summary>
        /// Время выполнения операции
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }

        /// <summary>
        /// Неуспешные операции (путь и причина)
        /// </summary>
        public Dictionary<string, string> FailedOperations { get; set; } = new Dictionary<string, string>();
    }
}