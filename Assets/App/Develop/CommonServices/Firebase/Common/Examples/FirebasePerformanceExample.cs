using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using App.Develop.Utils.Logging;
using Firebase.Database;

namespace App.Develop.CommonServices.Firebase.Common.Examples
{
    /// <summary>
    /// Пример использования Firebase компонентов оптимизации производительности
    /// </summary>
    public class FirebasePerformanceExample : MonoBehaviour
    {
        #region Fields

        private IFirebasePerformanceMonitor _performanceMonitor;
        private IFirebaseBatchOperations _batchOperations;

        #endregion

        #region Public Methods

        /// <summary>
        /// Инициализирует пример с необходимыми компонентами
        /// </summary>
        /// <param name="performanceMonitor">Монитор производительности</param>
        /// <param name="batchOperations">Batch операции</param>
        public void Initialize(IFirebasePerformanceMonitor performanceMonitor, IFirebaseBatchOperations batchOperations)
        {
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
            _batchOperations = batchOperations ?? throw new ArgumentNullException(nameof(batchOperations));

            // Подписываемся на события медленных операций
            _performanceMonitor.SlowOperationDetected += OnSlowOperationDetected;

            MyLogger.Log("✅ [FirebasePerformanceExample] Инициализирован", MyLogger.LogCategory.Firebase);
        }

        /// <summary>
        /// Пример мониторинга производительности простой операции
        /// </summary>
        public async Task ExampleSimpleOperationMonitoring()
        {
            var result = await _performanceMonitor.TrackOperationAsync("SimpleUserDataUpdate", async () =>
            {
                // Имитируем обновление данных пользователя
                await Task.Delay(100); // Симуляция Firebase операции

                // Возвращаем результат
                return new { UserId = "user123", Name = "Test User", UpdatedAt = DateTime.UtcNow };
            });

            MyLogger.Log($"🔍 [Example] Простая операция завершена: {result.Name} обновлен в {result.UpdatedAt}", MyLogger.LogCategory.Firebase);

            // Получаем статистику операции
            var stats = _performanceMonitor.GetStats("SimpleUserDataUpdate");
            MyLogger.Log($"📊 [Example] Статистика операции: {stats.TotalExecutions} выполнений, среднее время: {stats.AverageExecutionTime.TotalMilliseconds:F2}ms", MyLogger.LogCategory.Firebase);
        }

        /// <summary>
        /// Пример batch обновления множественных записей
        /// </summary>
        public async Task ExampleBatchUpdate()
        {
            var updates = new Dictionary<string, object>
            {
                { "users/user123/lastLogin", DateTime.UtcNow.ToString() },
                { "users/user123/score", 1500 },
                { "users/user123/level", 5 },
                { "statistics/totalLogins", 12345 },
                { "statistics/activeUsers", 987 }
            };

            var success = await _batchOperations.UpdateMultipleRecordsAsync(updates);

            if (success)
            {
                MyLogger.Log($"✅ [Example] Batch обновление выполнено успешно ({updates.Count} записей)", MyLogger.LogCategory.Firebase);
            }
            else
            {
                MyLogger.LogError("❌ [Example] Ошибка при выполнении batch обновления", MyLogger.LogCategory.Firebase);
            }
        }

        /// <summary>
        /// Пример fan-out операции для нормализованных данных
        /// </summary>
        public async Task ExampleFanOutOperation()
        {
            var emotionData = new
            {
                Id = Guid.NewGuid().ToString(),
                Type = "happy",
                Intensity = 8,
                Timestamp = DateTime.UtcNow,
                UserId = "user123"
            };

            var fanOutOperations = new List<FanOutOperation>
            {
                // Основная запись эмоции
                new FanOutOperation
                {
                    Path = $"emotions/{emotionData.Id}",
                    Value = emotionData,
                    OperationType = FanOutOperationType.Set,
                    Description = "Создание основной записи эмоции"
                },
                
                // Связь пользователь -> эмоции
                new FanOutOperation
                {
                    Path = $"user-emotions/{emotionData.UserId}/{emotionData.Id}",
                    Value = true,
                    OperationType = FanOutOperationType.Set,
                    Description = "Связь пользователь -> эмоция"
                },
                
                // Индекс по типу эмоции
                new FanOutOperation
                {
                    Path = $"emotions-by-type/{emotionData.Type}/{emotionData.Id}",
                    Value = emotionData.Timestamp,
                    OperationType = FanOutOperationType.Set,
                    Description = "Индекс по типу эмоции"
                },
                
                                 // Обновление статистики пользователя
                 new FanOutOperation
                 {
                     Path = $"user-stats/{emotionData.UserId}/totalEmotions",
                     Value = 1, // Простое инкрементирование, или можно использовать специальную логику
                     OperationType = FanOutOperationType.Update,
                     Description = "Увеличение счетчика эмоций пользователя"
                 }
            };

            var success = await _batchOperations.ExecuteFanOutOperationAsync(fanOutOperations);

            if (success)
            {
                MyLogger.Log($"✅ [Example] Fan-out операция выполнена успешно ({fanOutOperations.Count} операций)", MyLogger.LogCategory.Firebase);
            }
            else
            {
                MyLogger.LogError("❌ [Example] Ошибка при выполнении fan-out операции", MyLogger.LogCategory.Firebase);
            }
        }

        /// <summary>
        /// Пример атомарной операции
        /// </summary>
        public async Task ExampleAtomicOperation()
        {
            var success = await _batchOperations.ExecuteAtomicOperationAsync(async (updates) =>
            {
                // Имитируем сложную бизнес-логику
                var userId = "user123";
                var transactionAmount = 100;

                // Читаем текущий баланс (в реальности это должно быть через Transaction)
                await Task.Delay(50); // Симуляция чтения
                var currentBalance = 500; // Симуляция значения

                if (currentBalance >= transactionAmount)
                {
                    // Подготавливаем обновления для атомарной операции
                    updates[$"users/{userId}/balance"] = currentBalance - transactionAmount;
                    updates[$"users/{userId}/lastTransaction"] = DateTime.UtcNow.ToString();
                    updates[$"transactions/{Guid.NewGuid()}"] = new
                    {
                        UserId = userId,
                        Amount = -transactionAmount,
                        Type = "purchase",
                        Timestamp = DateTime.UtcNow
                    };
                    updates["statistics/totalTransactions"] = 1; // Простое инкрементирование
                }
                else
                {
                    throw new InvalidOperationException("Недостаточно средств для операции");
                }
            });

            if (success)
            {
                MyLogger.Log("✅ [Example] Атомарная операция выполнена успешно", MyLogger.LogCategory.Firebase);
            }
            else
            {
                MyLogger.LogError("❌ [Example] Ошибка при выполнении атомарной операции", MyLogger.LogCategory.Firebase);
            }
        }

        /// <summary>
        /// Демонстрация статистики производительности
        /// </summary>
        public void ShowPerformanceStats()
        {
            var overallStats = _performanceMonitor.GetStats();

            MyLogger.Log("📊 === СТАТИСТИКА ПРОИЗВОДИТЕЛЬНОСТИ FIREBASE ===", MyLogger.LogCategory.Firebase);
            MyLogger.Log($"📊 Общие операции: {overallStats.TotalExecutions}", MyLogger.LogCategory.Firebase);
            MyLogger.Log($"📊 Успешные: {overallStats.SuccessfulExecutions} ({overallStats.SuccessRate:F1}%)", MyLogger.LogCategory.Firebase);
            MyLogger.Log($"📊 Неудачные: {overallStats.FailedExecutions}", MyLogger.LogCategory.Firebase);
            MyLogger.Log($"📊 Среднее время: {overallStats.AverageExecutionTime.TotalMilliseconds:F2}ms", MyLogger.LogCategory.Firebase);
            MyLogger.Log($"📊 Медленные операции: {overallStats.SlowOperations} ({overallStats.SlowOperationRate:F1}%)", MyLogger.LogCategory.Firebase);
            MyLogger.Log("📊 ===============================================", MyLogger.LogCategory.Firebase);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Обработчик обнаружения медленных операций
        /// </summary>
        private void OnSlowOperationDetected(string operationName, TimeSpan duration)
        {
            MyLogger.LogWarning($"🐌 [Example] Медленная операция обнаружена: {operationName} выполнялась {duration.TotalSeconds:F2} секунд", MyLogger.LogCategory.Firebase);

            // Здесь можно добавить дополнительную логику:
            // - Отправка уведомления разработчикам
            // - Логирование в аналитику
            // - Автоматическая оптимизация конфигурации
        }

        #endregion

        #region Test Methods (для вызова из инспектора)

        /// <summary>
        /// Запускает все примеры подряд (для тестирования)
        /// </summary>
        [ContextMenu("Запустить все примеры")]
        public async void RunAllExamples()
        {
            if (_performanceMonitor == null || _batchOperations == null)
            {
                MyLogger.LogError("❌ [Example] Компоненты не инициализированы. Вызовите Initialize() сначала.", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                MyLogger.Log("🚀 [Example] Запуск всех примеров Firebase оптимизации...", MyLogger.LogCategory.Firebase);

                await ExampleSimpleOperationMonitoring();
                await Task.Delay(1000);

                await ExampleBatchUpdate();
                await Task.Delay(1000);

                await ExampleFanOutOperation();
                await Task.Delay(1000);

                await ExampleAtomicOperation();
                await Task.Delay(1000);

                ShowPerformanceStats();

                MyLogger.Log("✅ [Example] Все примеры выполнены успешно!", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [Example] Ошибка при выполнении примеров: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }

        #endregion
    }
}