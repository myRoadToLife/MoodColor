using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Firebase.Common
{
    /// <summary>
    /// Реализация batch операций Firebase
    /// </summary>
    public class FirebaseBatchOperations : IFirebaseBatchOperations
    {
        #region Fields

        private readonly DatabaseReference _databaseRef;
        private readonly IFirebaseErrorHandler _errorHandler;
        private readonly IFirebasePerformanceMonitor _performanceMonitor;

        #endregion

        #region Properties

        /// <summary>
        /// Максимальное количество операций в одном batch (по умолчанию 500)
        /// </summary>
        public int MaxBatchSize { get; set; } = 500;

        #endregion

        #region Constructor

        /// <summary>
        /// Конструктор FirebaseBatchOperations
        /// </summary>
        /// <param name="databaseRef">Ссылка на корень базы данных</param>
        /// <param name="errorHandler">Обработчик ошибок</param>
        /// <param name="performanceMonitor">Монитор производительности</param>
        public FirebaseBatchOperations(
            DatabaseReference databaseRef,
            IFirebaseErrorHandler errorHandler,
            IFirebasePerformanceMonitor performanceMonitor)
        {
            _databaseRef = databaseRef ?? throw new ArgumentNullException(nameof(databaseRef));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));

            MyLogger.Log("✅ [FirebaseBatchOperations] Инициализирован", MyLogger.LogCategory.Firebase);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Выполняет множественное обновление данных в одной транзакции
        /// </summary>
        public async Task<bool> UpdateMultipleRecordsAsync(Dictionary<string, object> updates)
        {
            if (updates == null || !updates.Any())
            {
                MyLogger.LogWarning("[BatchOperations] Попытка обновления с пустым словарем updates", MyLogger.LogCategory.Firebase);
                return true; // Технически успех, но нет операций
            }

            return await _performanceMonitor.TrackOperationAsync($"UpdateMultipleRecords({updates.Count})", async () =>
            {
                var batches = SplitIntoBatches(updates);
                bool allSuccess = true;

                foreach (var batch in batches)
                {
                    var success = await _errorHandler.ExecuteWithRetryAsync(async () =>
                    {
                        await _databaseRef.UpdateChildrenAsync(batch);
                        return true;
                    });

                    if (!success)
                    {
                        allSuccess = false;
                        MyLogger.LogError($"❌ [BatchOperations] Не удалось выполнить batch обновление ({batch.Count} операций)", MyLogger.LogCategory.Firebase);
                    }
                    else
                    {
                        MyLogger.Log($"✅ [BatchOperations] Batch обновление выполнено ({batch.Count} операций)", MyLogger.LogCategory.Firebase);
                    }
                }

                return allSuccess;
            });
        }

        /// <summary>
        /// Создает множественные записи в разных узлах
        /// </summary>
        public async Task<bool> CreateMultipleRecordsAsync(Dictionary<string, object> records)
        {
            if (records == null || !records.Any())
            {
                MyLogger.LogWarning("[BatchOperations] Попытка создания с пустым словарем records", MyLogger.LogCategory.Firebase);
                return true;
            }

            return await _performanceMonitor.TrackOperationAsync($"CreateMultipleRecords({records.Count})", async () =>
            {
                // Для создания записей используем UpdateChildren с новыми ключами
                return await UpdateMultipleRecordsAsync(records);
            });
        }

        /// <summary>
        /// Удаляет множественные записи
        /// </summary>
        public async Task<bool> DeleteMultipleRecordsAsync(List<string> paths)
        {
            if (paths == null || !paths.Any())
            {
                MyLogger.LogWarning("[BatchOperations] Попытка удаления с пустым списком paths", MyLogger.LogCategory.Firebase);
                return true;
            }

            return await _performanceMonitor.TrackOperationAsync($"DeleteMultipleRecords({paths.Count})", async () =>
            {
                // Для удаления устанавливаем значение null
                var deleteUpdates = paths.ToDictionary(path => path, path => (object)null);
                return await UpdateMultipleRecordsAsync(deleteUpdates);
            });
        }

        /// <summary>
        /// Выполняет атомарную операцию (все или ничего)
        /// </summary>
        public async Task<bool> ExecuteAtomicOperationAsync(Func<Dictionary<string, object>, Task> batchOperation)
        {
            return await _performanceMonitor.TrackOperationAsync("AtomicOperation", async () =>
            {
                var updates = new Dictionary<string, object>();

                try
                {
                    // Выполняем пользовательскую логику для подготовки операций
                    await batchOperation(updates);

                    // Выполняем все операции атомарно
                    if (updates.Any())
                    {
                        return await UpdateMultipleRecordsAsync(updates);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"❌ [BatchOperations] Ошибка в атомарной операции: {ex.Message}", MyLogger.LogCategory.Firebase);
                    return false;
                }
            });
        }

        /// <summary>
        /// Создает операцию для управления fan-out структурой данных
        /// </summary>
        public async Task<bool> ExecuteFanOutOperationAsync(IEnumerable<FanOutOperation> fanOutOperations)
        {
            if (fanOutOperations == null || !fanOutOperations.Any())
            {
                MyLogger.LogWarning("[BatchOperations] Попытка fan-out с пустым списком операций", MyLogger.LogCategory.Firebase);
                return true;
            }

            var operations = fanOutOperations.ToList();

            return await _performanceMonitor.TrackOperationAsync($"FanOutOperation({operations.Count})", async () =>
            {
                var updates = new Dictionary<string, object>();

                foreach (var operation in operations)
                {
                    if (string.IsNullOrEmpty(operation.Path))
                    {
                        MyLogger.LogWarning($"[BatchOperations] Пропущена fan-out операция с пустым путем: {operation.Description}", MyLogger.LogCategory.Firebase);
                        continue;
                    }

                    switch (operation.OperationType)
                    {
                        case FanOutOperationType.Set:
                        case FanOutOperationType.Update:
                            updates[operation.Path] = operation.Value;
                            break;

                        case FanOutOperationType.Delete:
                            updates[operation.Path] = null;
                            break;

                        case FanOutOperationType.Push:
                            // Для Push операций нужно сгенерировать уникальный ключ
                            var pushKey = _databaseRef.Child(operation.Path).Push().Key;
                            updates[$"{operation.Path}/{pushKey}"] = operation.Value;
                            break;

                        default:
                            MyLogger.LogWarning($"[BatchOperations] Неизвестный тип fan-out операции: {operation.OperationType}", MyLogger.LogCategory.Firebase);
                            break;
                    }
                }

                MyLogger.Log($"🔄 [BatchOperations] Выполняем fan-out операцию с {updates.Count} обновлениями", MyLogger.LogCategory.Firebase);
                return await UpdateMultipleRecordsAsync(updates);
            });
        }

        /// <summary>
        /// Получает множественные записи одним запросом
        /// </summary>
        public async Task<Dictionary<string, object>> GetMultipleRecordsAsync(List<string> paths)
        {
            if (paths == null || !paths.Any())
            {
                MyLogger.LogWarning("[BatchOperations] Попытка получения с пустым списком paths", MyLogger.LogCategory.Firebase);
                return new Dictionary<string, object>();
            }

            return await _performanceMonitor.TrackOperationAsync($"GetMultipleRecords({paths.Count})", async () =>
            {
                var results = new Dictionary<string, object>();

                // Firebase не поддерживает множественные get в одном запросе,
                // поэтому выполняем параллельные запросы
                var tasks = paths.Select(async path =>
                {
                    try
                    {
                        var snapshot = await _errorHandler.ExecuteWithRetryAsync(() => _databaseRef.Child(path).GetValueAsync());
                        return new { Path = path, Value = snapshot.Value };
                    }
                    catch (Exception ex)
                    {
                        MyLogger.LogError($"❌ [BatchOperations] Ошибка получения данных по пути '{path}': {ex.Message}", MyLogger.LogCategory.Firebase);
                        return new { Path = path, Value = (object)null };
                    }
                });

                var taskResults = await Task.WhenAll(tasks);

                foreach (var result in taskResults)
                {
                    results[result.Path] = result.Value;
                }

                MyLogger.Log($"✅ [BatchOperations] Получено {results.Count} записей", MyLogger.LogCategory.Firebase);
                return results;
            });
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Разбивает словарь обновлений на батчи в соответствии с MaxBatchSize
        /// </summary>
        private List<Dictionary<string, object>> SplitIntoBatches(Dictionary<string, object> updates)
        {
            var batches = new List<Dictionary<string, object>>();
            var currentBatch = new Dictionary<string, object>();

            foreach (var update in updates)
            {
                currentBatch[update.Key] = update.Value;

                if (currentBatch.Count >= MaxBatchSize)
                {
                    batches.Add(currentBatch);
                    currentBatch = new Dictionary<string, object>();
                }
            }

            // Добавляем последний неполный батч
            if (currentBatch.Any())
            {
                batches.Add(currentBatch);
            }

            MyLogger.Log($"🔄 [BatchOperations] Операции разбиты на {batches.Count} батчей (макс. размер: {MaxBatchSize})", MyLogger.LogCategory.Firebase);
            return batches;
        }

        #endregion
    }
}