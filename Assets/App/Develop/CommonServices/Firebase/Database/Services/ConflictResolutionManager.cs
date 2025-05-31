using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.DataManagement.DataProviders;
using Newtonsoft.Json;
using UnityEngine;
using Firebase.Database;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Менеджер разрешения конфликтов данных при синхронизации
    /// </summary>
    public class ConflictResolutionManager
    {
        #region События
        
        /// <summary>
        /// Событие, вызываемое при обнаружении конфликта, требующего вмешательства пользователя
        /// </summary>
        public event Action<ConflictData> OnManualResolutionRequired;
        
        /// <summary>
        /// Событие, вызываемое при автоматическом разрешении конфликта
        /// </summary>
        public event Action<ConflictData, ConflictResolutionResult> OnConflictResolved;
        
        /// <summary>
        /// Событие об изменении количества конфликтов, ожидающих разрешения
        /// </summary>
        public event Action<int> OnPendingConflictsCountChanged;
        
        /// <summary>
        /// Событие, вызываемое при обнаружении конфликта, который требует разрешения пользователем
        /// </summary>
        public event Action<EmotionData, EmotionData, Action<EmotionData>> OnManualResolutionRequiredEmotions;
        
        /// <summary>
        /// Событие, вызываемое при автоматическом разрешении конфликта
        /// </summary>
        public event Action<EmotionData, EmotionData, EmotionData, ConflictResolutionStrategy> OnConflictResolvedEmotions;
        
        #endregion
        
        #region Приватные поля
        
        private readonly DatabaseService _databaseService;
        private readonly EmotionSyncSettings _syncSettings;
        private readonly Queue<ConflictData> _pendingConflicts = new Queue<ConflictData>();
        private bool _isProcessingConflicts = false;
        private readonly Dictionary<string, int> _conflictCountByType = new Dictionary<string, int>();
        
        #endregion
        
        #region Конструктор
        
        public ConflictResolutionManager(DatabaseService databaseService, EmotionSyncSettings syncSettings)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _syncSettings = syncSettings ?? throw new ArgumentNullException(nameof(syncSettings));
            
            // Инициализируем счетчики конфликтов по типам
            foreach (var type in Enum.GetValues(typeof(EmotionTypes)))
            {
                _conflictCountByType[type.ToString()] = 0;
            }
        }
        
        #endregion
        
        #region Публичные методы
        
        /// <summary>
        /// Асинхронно разрешает конфликт данных в соответствии с выбранной стратегией
        /// </summary>
        /// <param name="clientData">Локальные данные</param>
        /// <param name="serverData">Данные с сервера</param>
        /// <param name="dataType">Тип данных (например, "emotion", "profile")</param>
        /// <param name="strategy">Стратегия разрешения конфликта (если null, используется из настроек)</param>
        /// <returns>Результат разрешения конфликта</returns>
        public async Task<ConflictResolutionResult> ResolveConflict<T>(T clientData, T serverData, string dataType, ConflictResolutionStrategy? strategy = null)
        {
            // Если стратегия не указана, используем из настроек
            var resolveStrategy = strategy ?? _syncSettings.ConflictStrategy;
            var conflict = new ConflictData(clientData, serverData, dataType, DateTime.Now);
            
            // Логируем информацию о конфликте
            LogConflict(conflict, resolveStrategy);
            
            // Увеличиваем счетчик конфликтов для этого типа
            IncrementConflictCount(dataType);
            
            try
            {
                // Применяем соответствующую стратегию разрешения
                switch (resolveStrategy)
                {
                    case ConflictResolutionStrategy.ServerWins:
                        return await ResolveWithServerData(conflict);
                        
                    case ConflictResolutionStrategy.ClientWins:
                        return await ResolveWithClientData(conflict);
                        
                    case ConflictResolutionStrategy.MostRecent:
                        return await ResolveWithMostRecentData(conflict);
                        
                    case ConflictResolutionStrategy.KeepBoth:
                        return await ResolveByKeepingBoth(conflict);
                        
                    case ConflictResolutionStrategy.AskUser:
                        return await RequireManualResolution(conflict);
                        
                    default:
                        // По умолчанию данные сервера имеют приоритет
                        return await ResolveWithServerData(conflict);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при разрешении конфликта: {ex.Message}", MyLogger.LogCategory.Firebase);
                
                // В случае ошибки возвращаем серверные данные как более надежные
                return new ConflictResolutionResult
                {
                    ResolvedData = serverData,
                    Strategy = ConflictResolutionStrategy.ServerWins,
                    Success = false,
                    Error = ex.Message
                };
            }
            finally
            {
                // Уменьшаем счетчик конфликтов
                DecrementConflictCount(dataType);
            }
        }
        
        /// <summary>
        /// Разрешает конфликт эмоций с учетом их специфики
        /// </summary>
        public async Task<ConflictResolutionResult> ResolveEmotionConflict(EmotionData clientEmotion, EmotionData serverEmotion)
        {
            // Для эмоций используем специализированное решение
            if (clientEmotion.Type != serverEmotion.Type)
            {
                MyLogger.LogWarning($"Несоответствие типов эмоций: клиент {clientEmotion.Type}, сервер {serverEmotion.Type}", MyLogger.LogCategory.Firebase);
                // Если типы не совпадают, что-то не так. Берем серверные данные.
                return new ConflictResolutionResult
                {
                    ResolvedData = serverEmotion,
                    Strategy = ConflictResolutionStrategy.ServerWins,
                    Success = true
                };
            }
            
            string dataType = $"emotion_{clientEmotion.Type}";
            return await ResolveConflict(clientEmotion, serverEmotion, dataType);
        }
        
        /// <summary>
        /// Разрешает конфликт по результатам пользовательского выбора
        /// </summary>
        public async Task<ConflictResolutionResult> ResolveManuallyAsync(ConflictData conflict, bool useClientData)
        {
            if (useClientData)
            {
                return await ResolveWithClientData(conflict);
            }
            else
            {
                return await ResolveWithServerData(conflict);
            }
        }
        
        /// <summary>
        /// Разрешает конфликт путем слияния данных
        /// </summary>
        public async Task<ConflictResolutionResult> ResolveMergeAsync<T>(ConflictData conflict, T mergedData)
        {
            try
            {
                // Здесь мы получаем уже слитые пользователем или автоматикой данные
                var result = new ConflictResolutionResult
                {
                    ResolvedData = mergedData,
                    Strategy = ConflictResolutionStrategy.KeepBoth,
                    Success = true
                };
                
                OnConflictResolved?.Invoke(conflict, result);
                return result;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при слиянии данных: {ex.Message}", MyLogger.LogCategory.Firebase);
                
                return new ConflictResolutionResult
                {
                    ResolvedData = conflict.ServerData,
                    Strategy = ConflictResolutionStrategy.ServerWins,
                    Success = false,
                    Error = ex.Message
                };
            }
        }
        
        /// <summary>
        /// Получает текущее количество конфликтов, ожидающих разрешения
        /// </summary>
        public int GetPendingConflictsCount()
        {
            return _pendingConflicts.Count;
        }
        
        /// <summary>
        /// Автоматически разрешает конфликт в соответствии с выбранной стратегией
        /// </summary>
        /// <param name="_localData">Локальные данные</param>
        /// <param name="_serverData">Данные с сервера</param>
        /// <param name="_strategy">Стратегия разрешения конфликта</param>
        /// <returns>Разрешенные данные</returns>
        public EmotionData ResolveConflict(EmotionData _localData, EmotionData _serverData, ConflictResolutionStrategy _strategy)
        {
            if (_localData == null && _serverData == null)
                return null;

            if (_localData == null)
                return _serverData;

            if (_serverData == null)
                return _localData;

            // Если данные идентичны, конфликта нет
            if (_localData.Equals(_serverData))
                return _localData;

            EmotionData resolvedData = null;

            switch (_strategy)
            {
                case ConflictResolutionStrategy.ServerWins:
                    resolvedData = _serverData.Clone() as EmotionData;
                    break;

                case ConflictResolutionStrategy.ClientWins:
                    resolvedData = _localData.Clone() as EmotionData;
                    break;

                case ConflictResolutionStrategy.MostRecent:
                    resolvedData = _localData.Timestamp >= _serverData.Timestamp 
                        ? _localData.Clone() as EmotionData 
                        : _serverData.Clone() as EmotionData;
                    break;

                case ConflictResolutionStrategy.Merge:
                    resolvedData = MergeEmotionData(_localData, _serverData);
                    break;

                case ConflictResolutionStrategy.Manual:
                    // Вызываем событие для ручного разрешения
                    if (OnManualResolutionRequiredEmotions != null)
                    {
                        bool resolutionComplete = false;
                        EmotionData manuallyResolvedData = null;

                        // Callback для получения результата разрешения
                        void ResolutionCallback(EmotionData result)
                        {
                            manuallyResolvedData = result;
                            resolutionComplete = true;
                        }

                        // Вызываем событие для пользовательского интерфейса
                        OnManualResolutionRequiredEmotions.Invoke(_localData, _serverData, ResolutionCallback);

                        // В асинхронной среде здесь нужно было бы вернуть Task/Promise
                        // Поскольку это синхронный метод, мы просто логируем, что требуется ручное разрешение
                        MyLogger.Log("Требуется ручное разрешение конфликта. Используем временную стратегию MostRecent.", MyLogger.LogCategory.Firebase);
                        
                        // Временно используем стратегию "Самые последние данные"
                        resolvedData = _localData.Timestamp >= _serverData.Timestamp 
                            ? _localData.Clone() as EmotionData 
                            : _serverData.Clone() as EmotionData;
                    }
                    else
                    {
                        // Если нет слушателей для ручного разрешения, используем MostRecent
                        MyLogger.LogWarning("Стратегия Manual выбрана, но нет слушателей для OnManualResolutionRequired. Используем MostRecent.", MyLogger.LogCategory.Firebase);
                        resolvedData = _localData.Timestamp >= _serverData.Timestamp 
                            ? _localData.Clone() as EmotionData 
                            : _serverData.Clone() as EmotionData;
                    }
                    break;

                default:
                    MyLogger.LogError($"Неизвестная стратегия разрешения конфликтов: {_strategy}", MyLogger.LogCategory.Firebase);
                    resolvedData = _localData.Clone() as EmotionData;
                    break;
            }

            // Вызываем событие об успешном разрешении конфликта
            OnConflictResolvedEmotions?.Invoke(_localData, _serverData, resolvedData, _strategy);

            return resolvedData;
        }
        
        /// <summary>
        /// Разрешает конфликты для нескольких элементов данных
        /// </summary>
        /// <param name="_localDataDict">Словарь локальных данных</param>
        /// <param name="_serverDataDict">Словарь данных с сервера</param>
        /// <param name="_strategy">Стратегия разрешения конфликтов</param>
        /// <returns>Разрешенный словарь данных</returns>
        public Dictionary<string, EmotionData> ResolveConflicts(
            Dictionary<string, EmotionData> _localDataDict, 
            Dictionary<string, EmotionData> _serverDataDict, 
            ConflictResolutionStrategy _strategy)
        {
            var resolvedDict = new Dictionary<string, EmotionData>();
            
            // Объединяем ключи из обоих словарей
            HashSet<string> allKeys = new HashSet<string>();
            
            if (_localDataDict != null)
            {
                foreach (var key in _localDataDict.Keys)
                {
                    allKeys.Add(key);
                }
            }
            
            if (_serverDataDict != null)
            {
                foreach (var key in _serverDataDict.Keys)
                {
                    allKeys.Add(key);
                }
            }
            
            // Разрешаем конфликт для каждого ключа
            foreach (var key in allKeys)
            {
                EmotionData localData = _localDataDict != null && _localDataDict.ContainsKey(key) ? _localDataDict[key] : null;
                EmotionData serverData = _serverDataDict != null && _serverDataDict.ContainsKey(key) ? _serverDataDict[key] : null;
                
                EmotionData resolvedData = ResolveConflict(localData, serverData, _strategy);
                
                if (resolvedData != null)
                {
                    resolvedDict[key] = resolvedData;
                }
            }
            
            return resolvedDict;
        }
        
        #endregion
        
        #region Приватные методы
        
        /// <summary>
        /// Разрешает конфликт в пользу серверных данных
        /// </summary>
        private async Task<ConflictResolutionResult> ResolveWithServerData(ConflictData conflict)
        {
            var result = new ConflictResolutionResult
            {
                ResolvedData = conflict.ServerData,
                Strategy = ConflictResolutionStrategy.ServerWins,
                Success = true
            };
            
            OnConflictResolved?.Invoke(conflict, result);
            return result;
        }
        
        /// <summary>
        /// Разрешает конфликт в пользу клиентских данных
        /// </summary>
        private async Task<ConflictResolutionResult> ResolveWithClientData(ConflictData conflict)
        {
            var result = new ConflictResolutionResult
            {
                ResolvedData = conflict.ClientData,
                Strategy = ConflictResolutionStrategy.ClientWins,
                Success = true
            };
            
            OnConflictResolved?.Invoke(conflict, result);
            return result;
        }
        
        /// <summary>
        /// Разрешает конфликт, выбирая наиболее свежие данные
        /// </summary>
        private async Task<ConflictResolutionResult> ResolveWithMostRecentData(ConflictData conflict)
        {
            // Для определения свежести нужны метки времени
            // Это надо реализовывать индивидуально для каждого типа данных
            
            // В качестве примера, для эмоций
            if (conflict.ClientData is EmotionData clientEmotion && conflict.ServerData is EmotionData serverEmotion)
            {
                bool useClientData = clientEmotion.LastUpdate > serverEmotion.LastUpdate;
                
                var result = new ConflictResolutionResult
                {
                    ResolvedData = useClientData ? clientEmotion : serverEmotion,
                    Strategy = ConflictResolutionStrategy.MostRecent,
                    Success = true
                };
                
                OnConflictResolved?.Invoke(conflict, result);
                return result;
            }
            
            // Если тип данных неизвестен или нет метки времени, используем серверные данные
            return await ResolveWithServerData(conflict);
        }
        
        /// <summary>
        /// Разрешает конфликт, сохраняя обе версии данных
        /// </summary>
        private async Task<ConflictResolutionResult> ResolveByKeepingBoth(ConflictData conflict)
        {
            // Этот метод зависит от типа данных и требует специфической реализации
            // В данном случае, просто вызываем метод для ручного разрешения
            return await RequireManualResolution(conflict);
        }
        
        /// <summary>
        /// Запрашивает ручное разрешение конфликта у пользователя
        /// </summary>
        private async Task<ConflictResolutionResult> RequireManualResolution(ConflictData conflict)
        {
            // Добавляем конфликт в очередь для ручного разрешения
            _pendingConflicts.Enqueue(conflict);
            
            // Уведомляем UI о необходимости вмешательства пользователя
            OnManualResolutionRequired?.Invoke(conflict);
            OnPendingConflictsCountChanged?.Invoke(_pendingConflicts.Count);
            
            // В этом случае мы не разрешаем конфликт немедленно
            // UI должен вызвать один из методов разрешения позже
            
            // По умолчанию возвращаем серверные данные
            return new ConflictResolutionResult
            {
                ResolvedData = conflict.ServerData,
                Strategy = ConflictResolutionStrategy.AskUser,
                Success = false, // Отмечаем как неуспешный, так как пользователь еще не принял решение
                Error = "Ожидание решения пользователя"
            };
        }
        
        /// <summary>
        /// Логирует информацию о конфликте
        /// </summary>
        private void LogConflict(ConflictData conflict, ConflictResolutionStrategy strategy)
        {
            try
            {
                string clientJson = JsonConvert.SerializeObject(conflict.ClientData);
                string serverJson = JsonConvert.SerializeObject(conflict.ServerData);
                
                MyLogger.Log($"[Конфликт] Тип: {conflict.DataType}, Стратегия: {strategy}\n" +
                          $"Клиент: {clientJson}\n" +
                          $"Сервер: {serverJson}", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при логировании конфликта: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }
        
        /// <summary>
        /// Увеличивает счетчик конфликтов для данного типа
        /// </summary>
        private void IncrementConflictCount(string dataType)
        {
            if (_conflictCountByType.ContainsKey(dataType))
            {
                _conflictCountByType[dataType]++;
            }
            else
            {
                _conflictCountByType[dataType] = 1;
            }
        }
        
        /// <summary>
        /// Уменьшает счетчик конфликтов для данного типа
        /// </summary>
        private void DecrementConflictCount(string dataType)
        {
            if (_conflictCountByType.ContainsKey(dataType) && _conflictCountByType[dataType] > 0)
            {
                _conflictCountByType[dataType]--;
            }
        }
        
        /// <summary>
        /// Объединяет данные из локального и серверного источников
        /// </summary>
        private EmotionData MergeEmotionData(EmotionData _local, EmotionData _server)
        {
            // Создаем новый объект на основе самых последних данных
            EmotionData baseData = _local.Timestamp >= _server.Timestamp ? _local : _server;
            EmotionData otherData = _local.Timestamp >= _server.Timestamp ? _server : _local;
            
            // Клонируем базовые данные
            EmotionData mergedData = baseData.Clone() as EmotionData;
            
            // Интеллектуальное объединение значений
            // Для числовых значений можем использовать среднее, максимум или другие стратегии
            
            // Для значения эмоции используем среднее
            mergedData.Value = (baseData.Value + otherData.Value) / 2f;
            
            // Для интенсивности берем максимальное значение
            mergedData.Intensity = Mathf.Max(baseData.Intensity, otherData.Intensity);
            
            // Для заметок объединяем, если они различаются
            if (!string.IsNullOrEmpty(otherData.Note) && !string.Equals(baseData.Note, otherData.Note))
            {
                if (string.IsNullOrEmpty(baseData.Note))
                    mergedData.Note = otherData.Note;
                else
                    mergedData.Note = $"{baseData.Note} | {otherData.Note}";
            }
            
            // Обновляем временную метку
            mergedData.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            return mergedData;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Данные о конфликте
    /// </summary>
    public class ConflictData
    {
        public object ClientData { get; }
        public object ServerData { get; }
        public string DataType { get; }
        public DateTime DetectedTime { get; }
        
        public ConflictData(object clientData, object serverData, string dataType, DateTime detectedTime)
        {
            ClientData = clientData;
            ServerData = serverData;
            DataType = dataType;
            DetectedTime = detectedTime;
        }
    }
    
    /// <summary>
    /// Результат разрешения конфликта
    /// </summary>
    public class ConflictResolutionResult
    {
        public object ResolvedData { get; set; }
        public ConflictResolutionStrategy Strategy { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
    }
} 