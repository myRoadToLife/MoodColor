using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Firebase.Common
{
    /// <summary>
    /// Менеджер для управления операциями Firebase в offline режиме
    /// </summary>
    public class OfflineManager : IOfflineManager
    {
        #region Fields

        private readonly Queue<IDatabaseOperation> _operationQueue;
        private readonly HashSet<string> _queuedOperationIds;
        private readonly object _queueLock = new object();
        private bool _isOnline = true;
        private bool _isProcessingQueue = false;

        #endregion

        #region Events

        /// <summary>
        /// Событие изменения состояния подключения
        /// </summary>
        public event Action<bool> ConnectionStateChanged;

        /// <summary>
        /// Событие обработки очереди операций
        /// </summary>
        public event Action<int> QueueProcessed;

        #endregion

        #region Properties

        /// <summary>
        /// Проверяет состояние подключения
        /// </summary>
        public bool IsOnline => _isOnline;

        /// <summary>
        /// Количество операций в очереди
        /// </summary>
        public int QueuedOperationsCount
        {
            get
            {
                lock (_queueLock)
                {
                    return _operationQueue.Count;
                }
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Конструктор OfflineManager
        /// </summary>
        public OfflineManager()
        {
            _operationQueue = new Queue<IDatabaseOperation>();
            _queuedOperationIds = new HashSet<string>();

            MyLogger.Log("✅ [OfflineManager] Инициализирован", MyLogger.LogCategory.Firebase);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Выполняет операцию с учетом состояния подключения
        /// </summary>
        /// <param name="operation">Операция для выполнения</param>
        /// <returns>True, если операция выполнена успешно</returns>
        public async Task<bool> ExecuteOperationAsync(IDatabaseOperation operation)
        {
            if (operation == null)
            {
                MyLogger.LogError("[OfflineManager] Операция не может быть null", MyLogger.LogCategory.Firebase);
                return false;
            }

            if (_isOnline)
            {
                try
                {
                    MyLogger.Log($"🌐 [OfflineManager] Выполняем операцию online: {operation.Description}", MyLogger.LogCategory.Firebase);
                    return await operation.ExecuteAsync();
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"❌ [OfflineManager] Ошибка выполнения операции online: {ex.Message}", MyLogger.LogCategory.Firebase);

                    // Если операция failed online, ставим в очередь
                    QueueOperation(operation);
                    return false;
                }
            }
            else
            {
                MyLogger.Log($"📴 [OfflineManager] Подключение отсутствует, ставим в очередь: {operation.Description}", MyLogger.LogCategory.Firebase);
                QueueOperation(operation);
                return false;
            }
        }

        /// <summary>
        /// Добавляет операцию в очередь для выполнения при восстановлении подключения
        /// </summary>
        /// <param name="operation">Операция для постановки в очередь</param>
        public void QueueOperation(IDatabaseOperation operation)
        {
            if (operation == null)
            {
                MyLogger.LogError("[OfflineManager] Операция для очереди не может быть null", MyLogger.LogCategory.Firebase);
                return;
            }

            lock (_queueLock)
            {
                // Предотвращаем дублирование операций
                if (_queuedOperationIds.Contains(operation.OperationId))
                {
                    MyLogger.Log($"⚠️ [OfflineManager] Операция уже в очереди: {operation.OperationId}", MyLogger.LogCategory.Firebase);
                    return;
                }

                _operationQueue.Enqueue(operation);
                _queuedOperationIds.Add(operation.OperationId);

                MyLogger.Log($"📋 [OfflineManager] Операция добавлена в очередь: {operation.Description} (всего в очереди: {_operationQueue.Count})", MyLogger.LogCategory.Firebase);
            }
        }

        /// <summary>
        /// Очищает очередь операций
        /// </summary>
        public void ClearQueue()
        {
            lock (_queueLock)
            {
                int removedCount = _operationQueue.Count;
                _operationQueue.Clear();
                _queuedOperationIds.Clear();

                MyLogger.Log($"🗑️ [OfflineManager] Очередь очищена, удалено операций: {removedCount}", MyLogger.LogCategory.Firebase);
            }
        }

        /// <summary>
        /// Обновляет состояние подключения
        /// </summary>
        /// <param name="isConnected">Состояние подключения</param>
        public void UpdateConnectionState(bool isConnected)
        {
            if (_isOnline != isConnected)
            {
                _isOnline = isConnected;
                MyLogger.Log($"🔄 [OfflineManager] Состояние подключения изменено: {(isConnected ? "ONLINE" : "OFFLINE")}", MyLogger.LogCategory.Firebase);

                ConnectionStateChanged?.Invoke(isConnected);

                if (isConnected)
                {
                    // Начинаем обработку очереди при восстановлении подключения
                    _ = ProcessQueueAsync();
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Обрабатывает очередь операций при восстановлении подключения
        /// </summary>
        private async Task ProcessQueueAsync()
        {
            if (_isProcessingQueue)
            {
                MyLogger.Log("⚠️ [OfflineManager] Очередь уже обрабатывается", MyLogger.LogCategory.Firebase);
                return;
            }

            _isProcessingQueue = true;

            try
            {
                int processedCount = 0;
                int totalOperations;

                lock (_queueLock)
                {
                    totalOperations = _operationQueue.Count;
                }

                if (totalOperations > 0)
                {
                    MyLogger.Log($"🔄 [OfflineManager] Начинаем обработку очереди операций, всего: {totalOperations}", MyLogger.LogCategory.Firebase);
                }

                // Обрабатываем операции по приоритету
                var operationsToProcess = new List<IDatabaseOperation>();

                lock (_queueLock)
                {
                    while (_operationQueue.Count > 0)
                    {
                        operationsToProcess.Add(_operationQueue.Dequeue());
                    }
                    _queuedOperationIds.Clear();
                }

                // Сортируем по приоритету (высший приоритет первым)
                operationsToProcess = operationsToProcess.OrderByDescending(op => op.Priority).ToList();

                foreach (var operation in operationsToProcess)
                {
                    if (!_isOnline)
                    {
                        MyLogger.Log("📴 [OfflineManager] Подключение потеряно во время обработки очереди, останавливаем", MyLogger.LogCategory.Firebase);

                        // Возвращаем необработанные операции в очередь
                        for (int i = processedCount; i < operationsToProcess.Count; i++)
                        {
                            QueueOperation(operationsToProcess[i]);
                        }
                        break;
                    }

                    try
                    {
                        MyLogger.Log($"🔄 [OfflineManager] Выполняем из очереди: {operation.Description}", MyLogger.LogCategory.Firebase);
                        bool success = await operation.ExecuteAsync();

                        if (success)
                        {
                            processedCount++;
                            MyLogger.Log($"✅ [OfflineManager] Операция выполнена успешно: {operation.Description}", MyLogger.LogCategory.Firebase);
                        }
                        else
                        {
                            MyLogger.LogError($"❌ [OfflineManager] Операция failed, возвращаем в очередь: {operation.Description}", MyLogger.LogCategory.Firebase);
                            QueueOperation(operation);
                        }
                    }
                    catch (Exception ex)
                    {
                        MyLogger.LogError($"❌ [OfflineManager] Исключение при выполнении операции: {operation.Description}, ошибка: {ex.Message}", MyLogger.LogCategory.Firebase);
                        QueueOperation(operation); // Возвращаем в очередь при ошибке
                    }

                    // Небольшая задержка между операциями
                    await Task.Delay(100);
                }

                if (processedCount > 0)
                {
                    MyLogger.Log($"✅ [OfflineManager] Обработка очереди завершена, успешно выполнено операций: {processedCount}/{totalOperations}", MyLogger.LogCategory.Firebase);
                    QueueProcessed?.Invoke(processedCount);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [OfflineManager] Критическая ошибка обработки очереди: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
            finally
            {
                _isProcessingQueue = false;
            }
        }

        #endregion
    }
}