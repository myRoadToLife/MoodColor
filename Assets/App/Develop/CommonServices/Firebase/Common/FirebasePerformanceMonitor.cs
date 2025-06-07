using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Firebase.Common
{
    /// <summary>
    /// Мониторинг производительности Firebase операций
    /// </summary>
    public class FirebasePerformanceMonitor : IFirebasePerformanceMonitor
    {
        #region Fields

        private readonly ConcurrentDictionary<string, List<OperationMetric>> _operationMetrics;
        private readonly object _lockObject = new object();

        #endregion

        #region Properties

        /// <summary>
        /// Порог времени для определения медленных операций (по умолчанию 5 секунд)
        /// </summary>
        public TimeSpan SlowOperationThreshold { get; set; } = TimeSpan.FromSeconds(5);

        #endregion

        #region Events

        /// <summary>
        /// Событие превышения времени выполнения операции
        /// </summary>
        public event Action<string, TimeSpan> SlowOperationDetected;

        #endregion

        #region Constructor

        /// <summary>
        /// Конструктор FirebasePerformanceMonitor
        /// </summary>
        public FirebasePerformanceMonitor()
        {
            _operationMetrics = new ConcurrentDictionary<string, List<OperationMetric>>();
            MyLogger.Log("✅ [FirebasePerformanceMonitor] Инициализирован", MyLogger.LogCategory.Firebase);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Отслеживает выполнение операции и собирает метрики
        /// </summary>
        public async Task<T> TrackOperationAsync<T>(string operationName, Func<Task<T>> operation)
        {
            var stopwatch = Stopwatch.StartNew();
            var startTime = DateTime.UtcNow;
            bool isSuccess = false;
            Exception operationException = null;

            try
            {
                MyLogger.Log($"🚀 [PerformanceMonitor] Начало операции: {operationName}", MyLogger.LogCategory.Firebase);

                var result = await operation();
                isSuccess = true;

                MyLogger.Log($"✅ [PerformanceMonitor] Операция '{operationName}' завершена успешно за {stopwatch.ElapsedMilliseconds}ms", MyLogger.LogCategory.Firebase);

                return result;
            }
            catch (Exception ex)
            {
                operationException = ex;
                MyLogger.LogError($"❌ [PerformanceMonitor] Операция '{operationName}' завершилась с ошибкой за {stopwatch.ElapsedMilliseconds}ms: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                RecordMetric(operationName, stopwatch.Elapsed, isSuccess, startTime, operationException);
            }
        }

        /// <summary>
        /// Отслеживает выполнение операции без возвращаемого значения
        /// </summary>
        public async Task<bool> TrackOperationAsync(string operationName, Func<Task> operation)
        {
            try
            {
                await TrackOperationAsync(operationName, async () =>
                {
                    await operation();
                    return true;
                });
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [PerformanceMonitor] Операция '{operationName}' не выполнена: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        /// <summary>
        /// Получает статистику производительности операций
        /// </summary>
        public PerformanceStats GetStats(string operationName = null)
        {
            lock (_lockObject)
            {
                if (string.IsNullOrEmpty(operationName))
                {
                    // Возвращаем агрегированную статистику для всех операций
                    return GetAggregatedStats();
                }

                if (!_operationMetrics.TryGetValue(operationName, out var metrics) || !metrics.Any())
                {
                    return new PerformanceStats { OperationName = operationName };
                }

                return CalculateStatsForMetrics(operationName, metrics);
            }
        }

        /// <summary>
        /// Сбрасывает статистику производительности
        /// </summary>
        public void ResetStats()
        {
            lock (_lockObject)
            {
                _operationMetrics.Clear();
                MyLogger.Log("🔄 [PerformanceMonitor] Статистика сброшена", MyLogger.LogCategory.Firebase);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Записывает метрику операции
        /// </summary>
        private void RecordMetric(string operationName, TimeSpan duration, bool isSuccess, DateTime startTime, Exception exception)
        {
            var metric = new OperationMetric
            {
                OperationName = operationName,
                Duration = duration,
                IsSuccess = isSuccess,
                Timestamp = startTime,
                Exception = exception?.GetType().Name
            };

            lock (_lockObject)
            {
                if (!_operationMetrics.ContainsKey(operationName))
                {
                    _operationMetrics[operationName] = new List<OperationMetric>();
                }

                _operationMetrics[operationName].Add(metric);

                // Ограничиваем размер истории (последние 1000 операций)
                if (_operationMetrics[operationName].Count > 1000)
                {
                    _operationMetrics[operationName].RemoveAt(0);
                }
            }

            // Проверяем, не является ли операция медленной
            if (duration > SlowOperationThreshold)
            {
                MyLogger.LogWarning($"🐌 [PerformanceMonitor] Медленная операция обнаружена: {operationName} ({duration.TotalSeconds:F2}s)", MyLogger.LogCategory.Firebase);
                SlowOperationDetected?.Invoke(operationName, duration);
            }
        }

        /// <summary>
        /// Вычисляет статистику для конкретных метрик
        /// </summary>
        private PerformanceStats CalculateStatsForMetrics(string operationName, List<OperationMetric> metrics)
        {
            var successfulMetrics = metrics.Where(m => m.IsSuccess).ToList();
            var failedMetrics = metrics.Where(m => !m.IsSuccess).ToList();
            var slowMetrics = metrics.Where(m => m.Duration > SlowOperationThreshold).ToList();

            var durations = metrics.Select(m => m.Duration).ToList();
            var lastMetric = metrics.LastOrDefault();

            return new PerformanceStats
            {
                OperationName = operationName,
                TotalExecutions = metrics.Count,
                SuccessfulExecutions = successfulMetrics.Count,
                FailedExecutions = failedMetrics.Count,
                AverageExecutionTime = durations.Any() ? TimeSpan.FromTicks((long)durations.Average(d => d.Ticks)) : TimeSpan.Zero,
                MinExecutionTime = durations.Any() ? durations.Min() : TimeSpan.Zero,
                MaxExecutionTime = durations.Any() ? durations.Max() : TimeSpan.Zero,
                LastExecutionTime = lastMetric?.Duration ?? TimeSpan.Zero,
                LastExecutionTimestamp = lastMetric?.Timestamp ?? DateTime.MinValue,
                SlowOperations = slowMetrics.Count
            };
        }

        /// <summary>
        /// Получает агрегированную статистику для всех операций
        /// </summary>
        private PerformanceStats GetAggregatedStats()
        {
            var allMetrics = _operationMetrics.Values.SelectMany(metrics => metrics).ToList();

            if (!allMetrics.Any())
            {
                return new PerformanceStats { OperationName = "Все операции" };
            }

            return CalculateStatsForMetrics("Все операции", allMetrics);
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Метрика отдельной операции
        /// </summary>
        private class OperationMetric
        {
            public string OperationName { get; set; }
            public TimeSpan Duration { get; set; }
            public bool IsSuccess { get; set; }
            public DateTime Timestamp { get; set; }
            public string Exception { get; set; }
        }

        #endregion
    }
}