using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace App.Develop.CommonServices.Firebase.Common
{
    /// <summary>
    /// Интерфейс для мониторинга производительности Firebase операций
    /// </summary>
    public interface IFirebasePerformanceMonitor
    {
        /// <summary>
        /// Отслеживает выполнение операции и собирает метрики
        /// </summary>
        /// <typeparam name="T">Тип результата операции</typeparam>
        /// <param name="operationName">Название операции</param>
        /// <param name="operation">Операция для выполнения</param>
        /// <returns>Результат операции</returns>
        Task<T> TrackOperationAsync<T>(string operationName, Func<Task<T>> operation);

        /// <summary>
        /// Отслеживает выполнение операции без возвращаемого значения
        /// </summary>
        /// <param name="operationName">Название операции</param>
        /// <param name="operation">Операция для выполнения</param>
        /// <returns>True, если операция выполнена успешно</returns>
        Task<bool> TrackOperationAsync(string operationName, Func<Task> operation);

        /// <summary>
        /// Получает статистику производительности операций
        /// </summary>
        /// <param name="operationName">Название операции (null для всех операций)</param>
        /// <returns>Статистика производительности</returns>
        PerformanceStats GetStats(string operationName = null);

        /// <summary>
        /// Сбрасывает статистику производительности
        /// </summary>
        void ResetStats();

        /// <summary>
        /// Событие превышения времени выполнения операции
        /// </summary>
        event Action<string, TimeSpan> SlowOperationDetected;

        /// <summary>
        /// Порог времени для определения медленных операций (по умолчанию 5 секунд)
        /// </summary>
        TimeSpan SlowOperationThreshold { get; set; }
    }

    /// <summary>
    /// Статистика производительности операций
    /// </summary>
    public class PerformanceStats
    {
        /// <summary>
        /// Название операции
        /// </summary>
        public string OperationName { get; set; }

        /// <summary>
        /// Общее количество выполнений
        /// </summary>
        public int TotalExecutions { get; set; }

        /// <summary>
        /// Количество успешных выполнений
        /// </summary>
        public int SuccessfulExecutions { get; set; }

        /// <summary>
        /// Количество неудачных выполнений
        /// </summary>
        public int FailedExecutions { get; set; }

        /// <summary>
        /// Среднее время выполнения
        /// </summary>
        public TimeSpan AverageExecutionTime { get; set; }

        /// <summary>
        /// Минимальное время выполнения
        /// </summary>
        public TimeSpan MinExecutionTime { get; set; }

        /// <summary>
        /// Максимальное время выполнения
        /// </summary>
        public TimeSpan MaxExecutionTime { get; set; }

        /// <summary>
        /// Последнее время выполнения
        /// </summary>
        public TimeSpan LastExecutionTime { get; set; }

        /// <summary>
        /// Время последнего выполнения
        /// </summary>
        public DateTime LastExecutionTimestamp { get; set; }

        /// <summary>
        /// Процент успешности (0-100)
        /// </summary>
        public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions * 100 : 0;

        /// <summary>
        /// Медленные операции (превышающие порог)
        /// </summary>
        public int SlowOperations { get; set; }

        /// <summary>
        /// Процент медленных операций (0-100)
        /// </summary>
        public double SlowOperationRate => TotalExecutions > 0 ? (double)SlowOperations / TotalExecutions * 100 : 0;
    }
}