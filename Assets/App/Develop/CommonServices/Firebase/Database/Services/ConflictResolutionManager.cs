using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.DataManagement.DataProviders;
using Newtonsoft.Json;
using UnityEngine;
using Firebase.Database;

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
        private readonly Dictionary<string, int> _conflictCountByType = new Dictionary<string, int>();
        
        #endregion
        
        #region Конструктор
        
        public ConflictResolutionManager(DatabaseService databaseService, EmotionSyncSettings syncSettings)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _syncSettings = syncSettings ?? throw new ArgumentNullException(nameof(syncSettings));
            
            // Инициализируем счетчики конфликтов по типам
            foreach (EmotionTypes type in Enum.GetValues(typeof(EmotionTypes)))
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
            ConflictResolutionStrategy resolveStrategy = strategy ?? _syncSettings.ConflictStrategy;
            ConflictData conflict = new ConflictData(clientData, serverData, dataType, DateTime.Now);
            
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
                        return await ResolveByMergingOrManual(conflict);
                        
                    case ConflictResolutionStrategy.AskUser:
                        return await RequireManualResolution(conflict);
                        
                    default:
                        // По умолчанию данные сервера имеют приоритет
                        return await ResolveWithServerData(conflict);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при разрешении конфликта для типа {dataType}: {ex.Message}", ex);
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
        /// Разрешает конфликт путем слияния данных (данные уже слиты)
        /// </summary>
        public Task<ConflictResolutionResult> ResolveMergeAsync<T>(ConflictData conflict, T mergedData)
        {
            try
            {
                // Здесь мы получаем уже слитые пользователем или автоматикой данные
                ConflictResolutionResult result = new ConflictResolutionResult
                {
                    ResolvedData = mergedData,
                    Strategy = ConflictResolutionStrategy.KeepBoth,
                    Success = true
                };
                
                OnConflictResolved?.Invoke(conflict, result);
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при слиянии данных для типа {conflict.DataType}: {ex.Message}", ex);
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
        /// <param name="localData">Локальные данные</param>
        /// <param name="serverData">Данные с сервера</param>
        /// <param name="strategy">Стратегия разрешения конфликта</param>
        /// <returns>Разрешенные данные</returns>
        public EmotionData ResolveConflict(EmotionData localData, EmotionData serverData, ConflictResolutionStrategy strategy)
        {
            if (localData == null && serverData == null)
                return null;

            if (localData == null)
                return serverData;

            if (serverData == null)
                return localData;

            // Если данные идентичны, конфликта нет
            if (localData.Equals(serverData))
                return localData;

            EmotionData resolvedData = null;

            switch (strategy)
            {
                case ConflictResolutionStrategy.ServerWins:
                    resolvedData = serverData.Clone() as EmotionData;
                    break;

                case ConflictResolutionStrategy.ClientWins:
                    resolvedData = localData.Clone() as EmotionData;
                    break;

                case ConflictResolutionStrategy.MostRecent:
                    resolvedData = localData.Timestamp >= serverData.Timestamp 
                        ? localData.Clone() as EmotionData 
                        : serverData.Clone() as EmotionData;
                    break;

                case ConflictResolutionStrategy.Merge:
                    resolvedData = MergeEmotionData(localData, serverData);
                    break;

                case ConflictResolutionStrategy.Manual:
                    if (OnManualResolutionRequiredEmotions != null)
                    {
                        OnManualResolutionRequiredEmotions.Invoke(localData, serverData, (EmotionData chosenData) => {
                            // This callback would be invoked by UI, but this method is synchronous.
                            // For now, we can't use the result of this callback directly here.
                            // The UI would need to call another method to finalize.
                            // This part of the logic might need redesign for sync vs async.
                        });
                        resolvedData = localData.Timestamp >= serverData.Timestamp 
                            ? localData.Clone() as EmotionData 
                            : serverData.Clone() as EmotionData;
                    }
                    else
                    {
                        resolvedData = localData.Timestamp >= serverData.Timestamp 
                            ? localData.Clone() as EmotionData 
                            : serverData.Clone() as EmotionData;
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(strategy), $"Неизвестная стратегия разрешения конфликтов: {strategy}");
            }

            // Вызываем событие об успешном разрешении конфликта
            OnConflictResolvedEmotions?.Invoke(localData, serverData, resolvedData, strategy);

            return resolvedData;
        }
        
        /// <summary>
        /// Разрешает конфликты для нескольких элементов данных
        /// </summary>
        /// <param name="localDataDict">Словарь локальных данных</param>
        /// <param name="serverDataDict">Словарь данных с сервера</param>
        /// <param name="strategy">Стратегия разрешения конфликтов</param>
        /// <returns>Разрешенный словарь данных</returns>
        public Dictionary<string, EmotionData> ResolveConflicts(
            Dictionary<string, EmotionData> localDataDict, 
            Dictionary<string, EmotionData> serverDataDict, 
            ConflictResolutionStrategy strategy)
        {
            var resolvedDict = new Dictionary<string, EmotionData>();
            
            // Объединяем ключи из обоих словарей
            HashSet<string> allKeys = new HashSet<string>();
            
            if (localDataDict != null)
            {
                foreach (var key in localDataDict.Keys)
                {
                    allKeys.Add(key);
                }
            }
            
            if (serverDataDict != null)
            {
                foreach (var key in serverDataDict.Keys)
                {
                    allKeys.Add(key);
                }
            }
            
            // Разрешаем конфликт для каждого ключа
            foreach (var key in allKeys)
            {
                EmotionData localData = (localDataDict != null && localDataDict.TryGetValue(key, out EmotionData ld)) ? ld : null;
                EmotionData serverData = (serverDataDict != null && serverDataDict.TryGetValue(key, out EmotionData sd)) ? sd : null;
                
                EmotionData resolvedData = ResolveConflict(localData, serverData, strategy);
                
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
        private Task<ConflictResolutionResult> ResolveWithServerData(ConflictData conflict)
        {
            ConflictResolutionResult result = new ConflictResolutionResult
            {
                ResolvedData = conflict.ServerData,
                Strategy = ConflictResolutionStrategy.ServerWins,
                Success = true
            };
            
            OnConflictResolved?.Invoke(conflict, result);
            return Task.FromResult(result);
        }
        
        /// <summary>
        /// Разрешает конфликт в пользу клиентских данных
        /// </summary>
        private Task<ConflictResolutionResult> ResolveWithClientData(ConflictData conflict)
        {
            ConflictResolutionResult result = new ConflictResolutionResult
            {
                ResolvedData = conflict.ClientData,
                Strategy = ConflictResolutionStrategy.ClientWins,
                Success = true
            };
            
            OnConflictResolved?.Invoke(conflict, result);
            return Task.FromResult(result);
        }
        
        /// <summary>
        /// Разрешает конфликт, выбирая наиболее свежие данные
        /// </summary>
        private Task<ConflictResolutionResult> ResolveWithMostRecentData(ConflictData conflict)
        {
            if (conflict.ClientData is ITimeStamped clientStamped && conflict.ServerData is ITimeStamped serverStamped)
            {
                bool useClientData = clientStamped.Timestamp >= serverStamped.Timestamp;
                
                ConflictResolutionResult result = new ConflictResolutionResult
                {
                    ResolvedData = useClientData ? conflict.ClientData : conflict.ServerData,
                    Strategy = ConflictResolutionStrategy.MostRecent,
                    Success = true
                };
                
                OnConflictResolved?.Invoke(conflict, result);
                return Task.FromResult(result);
            }
            // Если тип данных неизвестен или нет метки времени, используем серверные данные
            return ResolveWithServerData(conflict);
        }
        
        /// <summary>
        /// Разрешает конфликт, сохраняя обе версии данных (требует ручного вмешательства или слияния)
        /// </summary>
        private Task<ConflictResolutionResult> ResolveByMergingOrManual(ConflictData conflict)
        {
            // Этот метод зависит от типа данных и требует специфической реализации
            // В данном случае, просто вызываем метод для ручного разрешения
            return RequireManualResolution(conflict);
        }
        
        /// <summary>
        /// Запрашивает ручное разрешение конфликта у пользователя
        /// </summary>
        private Task<ConflictResolutionResult> RequireManualResolution(ConflictData conflict)
        {
            _pendingConflicts.Enqueue(conflict);
            OnManualResolutionRequired?.Invoke(conflict);
            OnPendingConflictsCountChanged?.Invoke(_pendingConflicts.Count);

            // По умолчанию возвращаем серверные данные, т.к. решение отложено
            ConflictResolutionResult result = new ConflictResolutionResult
            {
                ResolvedData = conflict.ServerData,
                Strategy = ConflictResolutionStrategy.AskUser,
                Success = false,
                Error = "Ожидание решения пользователя"
            };
            return Task.FromResult(result);
        }
        
        /// <summary>
        /// Логирует информацию о конфликте (детали)
        /// </summary>
        private void LogConflictDetails(ConflictData conflict, ConflictResolutionStrategy strategy)
        {
            // Метод оставлен пустым, так как логирование удалено
            // Можно полностью удалить, если нет внешних вызовов
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
        private EmotionData MergeEmotionData(EmotionData local, EmotionData server)
        {
            EmotionData baseData = local.Timestamp >= server.Timestamp ? local : server;
            EmotionData otherData = local.Timestamp >= server.Timestamp ? server : local;
            
            EmotionData mergedData = baseData.Clone() as EmotionData;
            
            if (mergedData == null) throw new InvalidOperationException("Clone returned null for EmotionData.");

            mergedData.Value = (baseData.Value + otherData.Value) / 2f;
            mergedData.Intensity = Mathf.Max(baseData.Intensity, otherData.Intensity);

            if (!string.IsNullOrEmpty(otherData.Note) && !string.Equals(baseData.Note, otherData.Note))
            {
                if (string.IsNullOrEmpty(baseData.Note))
                    mergedData.Note = otherData.Note;
                else
                    mergedData.Note = $"{baseData.Note} | {otherData.Note}";
            }
            
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

    public interface ITimeStamped
    {
        long Timestamp { get; }
    }
} 