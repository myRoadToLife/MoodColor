using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Database;
using UnityEngine;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Менеджер для выполнения пакетных операций в Firebase
    /// </summary>
    public class FirebaseBatchManager
    {
        private DatabaseReference _rootReference;
        private List<BatchOperation> _pendingOperations;
        private bool _isBatchInProgress;
        private int _maxBatchSize = 25; // Ограничение на количество операций в одном батче
        
        public event Action<bool, string> OnBatchCompleted;
        
        public FirebaseBatchManager(DatabaseReference rootReference)
        {
            _rootReference = rootReference ?? throw new ArgumentNullException(nameof(rootReference));
            _pendingOperations = new List<BatchOperation>();
            _isBatchInProgress = false;
        }
        
        /// <summary>
        /// Добавить операцию добавления/обновления данных в текущий батч
        /// </summary>
        public void AddUpdateOperation<T>(string path, T data)
        {
            _pendingOperations.Add(new BatchOperation
            {
                Path = path,
                Data = data,
                OperationType = BatchOperationType.Update
            });
            
            CheckAndExecuteBatchIfNeeded();
        }
        
        /// <summary>
        /// Добавить операцию удаления данных в текущий батч
        /// </summary>
        public void AddDeleteOperation(string path)
        {
            _pendingOperations.Add(new BatchOperation
            {
                Path = path,
                OperationType = BatchOperationType.Delete
            });
            
            CheckAndExecuteBatchIfNeeded();
        }
        
        /// <summary>
        /// Добавить операцию изменения приоритета в текущий батч
        /// </summary>
        public void AddPriorityOperation(string path, object priority)
        {
            _pendingOperations.Add(new BatchOperation
            {
                Path = path,
                Data = priority,
                OperationType = BatchOperationType.Priority
            });
            
            CheckAndExecuteBatchIfNeeded();
        }
        
        /// <summary>
        /// Принудительно выполнить все накопленные операции
        /// </summary>
        public async Task<bool> ExecuteBatchAsync()
        {
            if (_pendingOperations.Count == 0 || _isBatchInProgress)
                return true;
            
            _isBatchInProgress = true;
            bool success = false;
            string message = "";
            
            try
            {
                // Создаем словарь для пакетного обновления
                var updates = new Dictionary<string, object>();
                
                foreach (var operation in _pendingOperations)
                {
                    string fullPath = operation.Path;
                    
                    switch (operation.OperationType)
                    {
                        case BatchOperationType.Update:
                            // Добавляем операцию обновления в словарь
                            updates[fullPath] = operation.Data;
                            break;
                        case BatchOperationType.Delete:
                            // Для удаления устанавливаем null
                            updates[fullPath] = null;
                            break;
                        case BatchOperationType.Priority:
                            // Приоритеты требуют отдельной операции, выполним их последовательно
                            await _rootReference.Child(operation.Path).SetPriorityAsync(operation.Data);
                            break;
                    }
                }
                
                // Если есть операции для пакетного обновления, выполняем их одним запросом
                if (updates.Count > 0)
                {
                    await _rootReference.UpdateChildrenAsync(updates);
                }
                
                MyLogger.Log($"✅ Успешно выполнен батч из {_pendingOperations.Count} операций", MyLogger.LogCategory.Firebase);
                _pendingOperations.Clear();
                success = true;
                message = $"Батч выполнен успешно ({_pendingOperations.Count} операций)";
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка выполнения батча: {ex.Message}", MyLogger.LogCategory.Firebase);
                message = $"Ошибка выполнения батча: {ex.Message}";
            }
            finally
            {
                _isBatchInProgress = false;
                OnBatchCompleted?.Invoke(success, message);
            }
            
            return success;
        }
        
        /// <summary>
        /// Проверить необходимость выполнения батча и выполнить его, если достигнут лимит
        /// </summary>
        private void CheckAndExecuteBatchIfNeeded()
        {
            if (_pendingOperations.Count >= _maxBatchSize && !_isBatchInProgress)
            {
                _ = ExecuteBatchAsync();
            }
        }
        
        /// <summary>
        /// Отмена всех ожидающих операций без их выполнения
        /// </summary>
        public void CancelPendingOperations()
        {
            if (!_isBatchInProgress)
            {
                _pendingOperations.Clear();
                MyLogger.Log("❌ Все ожидающие операции отменены", MyLogger.LogCategory.Firebase);
            }
        }
        
        /// <summary>
        /// Получить количество ожидающих операций
        /// </summary>
        public int GetPendingOperationsCount()
        {
            return _pendingOperations.Count;
        }
        
        /// <summary>
        /// Установить максимальный размер батча
        /// </summary>
        public void SetMaxBatchSize(int size)
        {
            if (size > 0)
            {
                _maxBatchSize = size;
            }
        }
        
        /// <summary>
        /// Проверить, выполняется ли в данный момент батч операций
        /// </summary>
        public bool IsBatchInProgress()
        {
            return _isBatchInProgress;
        }
    }
    
    /// <summary>
    /// Перечисление типов операций для батча
    /// </summary>
    public enum BatchOperationType
    {
        Update,
        Delete,
        Priority
    }
    
    /// <summary>
    /// Класс для хранения информации об операции батча
    /// </summary>
    public class BatchOperation
    {
        public string Path { get; set; }
        public object Data { get; set; }
        public BatchOperationType OperationType { get; set; }
    }
} 