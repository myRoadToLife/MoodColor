using System;
using System.Collections.Generic;
using System.Linq;
using App.Develop.AppServices.Firebase.Database.Models;
using App.Develop.AppServices.Firebase.Database.Services;
using App.Develop.CommonServices.DataManagement.DataProviders;
using UnityEngine;

namespace App.Develop.CommonServices.Emotion
{
    [Serializable]
    public class EmotionHistoryEntry
    {
        public EmotionData EmotionData { get; set; }
        public DateTime Timestamp { get; set; }
        public EmotionEventType EventType { get; set; }
        public string SyncId { get; set; }  // ID записи в Firebase (если есть)
        public bool IsSynced { get; set; }  // Флаг синхронизации
        
        public EmotionHistoryEntry()
        {
            SyncId = Guid.NewGuid().ToString();
            IsSynced = false;
        }
    }

    public class EmotionHistory
    {
        private const int MAX_HISTORY_ENTRIES = 1000; // Максимальное количество записей в истории
        private readonly Queue<EmotionHistoryEntry> _historyQueue;
        private readonly Dictionary<EmotionTypes, List<EmotionHistoryEntry>> _historyByType;
        private EmotionHistoryCache _cache;
        private bool _useCache;
        
        // Событие об изменении истории эмоций
        public event Action<EmotionHistoryEntry> OnHistoryEntryAdded;
        
        public EmotionHistory(EmotionHistoryCache cache = null)
        {
            _historyQueue = new Queue<EmotionHistoryEntry>();
            _historyByType = new Dictionary<EmotionTypes, List<EmotionHistoryEntry>>();
            _cache = cache;
            _useCache = _cache != null;
            
            if (_useCache)
            {
                LoadFromCache();
            }
        }
        
        /// <summary>
        /// Устанавливает кэш для синхронизации
        /// </summary>
        public void SetCache(EmotionHistoryCache cache)
        {
            _cache = cache;
            _useCache = _cache != null;
            
            if (_useCache)
            {
                LoadFromCache();
            }
        }

        /// <summary>
        /// Добавляет запись в историю эмоций
        /// </summary>
        public void AddEntry(EmotionData emotion, EmotionEventType eventType, DateTime? timestamp = null)
        {
            if (emotion == null)
            {
                Debug.LogWarning("❌ Попытка добавить null запись в историю эмоций");
                return;
            }

            Debug.Log($"AddEntry: Adding entry for {emotion.Type} with EventType={eventType} at {timestamp ?? emotion.LastUpdate}");

            var entry = new EmotionHistoryEntry
            {
                EmotionData = emotion.Clone(), // Создаем копию для истории
                Timestamp = timestamp ?? emotion.LastUpdate, // Используем LastUpdate, если timestamp не указан
                EventType = eventType
            };

            Debug.Log($"Created entry: Type={entry.EmotionData.Type}, Value={entry.EmotionData.Value}, EventType={entry.EventType}, Timestamp={entry.Timestamp}");

            // Добавляем в очередь
            _historyQueue.Enqueue(entry);
            
            // Добавляем в словарь по типу
            if (Enum.TryParse<EmotionTypes>(emotion.Type, out var emotionType))
            {
                if (!_historyByType.ContainsKey(emotionType))
                {
                    _historyByType[emotionType] = new List<EmotionHistoryEntry>();
                }
                
                _historyByType[emotionType].Add(entry);
            }
            
            // Ограничиваем размер истории
            if (_historyQueue.Count > MAX_HISTORY_ENTRIES)
            {
                var oldestEntry = _historyQueue.Dequeue();
                
                // Также удаляем из словаря по типу
                if (Enum.TryParse<EmotionTypes>(oldestEntry.EmotionData.Type, out var oldEmotionType) && 
                    _historyByType.TryGetValue(oldEmotionType, out var entries))
                {
                    entries.Remove(oldestEntry);
                }
            }
            
            // Если используется кэш, добавляем запись в кэш
            if (_useCache)
            {
                SaveEntryToCache(entry);
            }
            
            // Вызываем событие
            OnHistoryEntryAdded?.Invoke(entry);
        }
        
        /// <summary>
        /// Добавляет запись в историю эмоций на основе записи из Firebase
        /// </summary>
        public void AddEntry(EmotionHistoryRecord record)
        {
            if (record == null) return;
            
            // Конвертируем в EmotionData
            var emotionData = record.ToEmotionData();
            
            // Парсим тип события
            if (Enum.TryParse<EmotionEventType>(record.EventType, out var eventType))
            {
                var entry = new EmotionHistoryEntry
                {
                    EmotionData = emotionData,
                    Timestamp = record.RecordTime,
                    EventType = eventType,
                    SyncId = record.Id,
                    IsSynced = record.SyncStatus == SyncStatus.Synced
                };
                
                // Добавляем в очередь
                _historyQueue.Enqueue(entry);
                
                // Добавляем в словарь по типу
                if (Enum.TryParse<EmotionTypes>(emotionData.Type, out var emotionType))
                {
                    if (!_historyByType.ContainsKey(emotionType))
                    {
                        _historyByType[emotionType] = new List<EmotionHistoryEntry>();
                    }
                    
                    _historyByType[emotionType].Add(entry);
                }
                
                // Ограничиваем размер истории
                if (_historyQueue.Count > MAX_HISTORY_ENTRIES)
                {
                    var oldestEntry = _historyQueue.Dequeue();
                    
                    // Также удаляем из словаря по типу
                    if (Enum.TryParse<EmotionTypes>(oldestEntry.EmotionData.Type, out var oldEmotionType) && 
                        _historyByType.TryGetValue(oldEmotionType, out var entries))
                    {
                        entries.Remove(oldestEntry);
                    }
                }
                
                // Вызываем событие
                OnHistoryEntryAdded?.Invoke(entry);
            }
        }

        /// <summary>
        /// Получает историю эмоций
        /// </summary>
        public IEnumerable<EmotionHistoryEntry> GetHistory(DateTime? from = null, DateTime? to = null)
        {
            var entries = _historyQueue.AsEnumerable();
            Debug.Log($"GetHistory: Total entries in queue: {entries.Count()}");
            
            if (from.HasValue)
            {
                Debug.Log($"Filtering entries from {from.Value}");
                entries = entries.Where(e => e.Timestamp >= from.Value);
                Debug.Log($"After 'from' filter: {entries.Count()} entries");
            }
            
            if (to.HasValue)
            {
                Debug.Log($"Filtering entries to {to.Value}");
                entries = entries.Where(e => e.Timestamp <= to.Value);
                Debug.Log($"After 'to' filter: {entries.Count()} entries");
            }
            
            var result = entries.OrderByDescending(e => e.Timestamp);
            Debug.Log($"GetHistory: Returning {result.Count()} entries");
            foreach (var entry in result)
            {
                Debug.Log($"Entry: Type={entry.EmotionData.Type}, EventType={entry.EventType}, Timestamp={entry.Timestamp}");
            }
            
            return result;
        }

        /// <summary>
        /// Получает историю эмоций по типу
        /// </summary>
        public IEnumerable<EmotionHistoryEntry> GetHistoryByType(EmotionTypes type, DateTime? from = null, DateTime? to = null)
        {
            if (!_historyByType.ContainsKey(type))
                return Enumerable.Empty<EmotionHistoryEntry>();

            var entries = _historyByType[type].AsEnumerable();
            
            if (from.HasValue)
                entries = entries.Where(e => e.Timestamp >= from.Value);
            
            if (to.HasValue)
                entries = entries.Where(e => e.Timestamp <= to.Value);
            
            return entries.OrderByDescending(e => e.Timestamp);
        }

        /// <summary>
        /// Получает средние интенсивности эмоций за период
        /// </summary>
        public Dictionary<EmotionTypes, float> GetAverageIntensityByPeriod(DateTime from, DateTime to)
        {
            var result = new Dictionary<EmotionTypes, float>();
            
            foreach (var type in Enum.GetValues(typeof(EmotionTypes)).Cast<EmotionTypes>())
            {
                var entries = GetHistoryByType(type, from, to)
                    .Where(e => e.EventType == EmotionEventType.IntensityChanged);
                
                if (!entries.Any()) continue;
                
                float avgIntensity = entries.Average(e => e.EmotionData.Intensity);
                result[type] = avgIntensity;
            }
            
            return result;
        }

        public Dictionary<TimeOfDay, EmotionTimeStats> GetEmotionsByTimeOfDay(DateTime? from = null, DateTime? to = null)
        {
            var result = new Dictionary<TimeOfDay, EmotionTimeStats>();
            var entries = GetHistory(from, to)
                .Where(e => e.EventType == EmotionEventType.ValueChanged);

            // Инициализируем статистику для каждого времени суток
            foreach (TimeOfDay timeOfDay in Enum.GetValues(typeof(TimeOfDay)))
            {
                result[timeOfDay] = new EmotionTimeStats
                {
                    TimeOfDay = timeOfDay,
                    AverageIntensities = new Dictionary<EmotionTypes, float>(),
                    EmotionCounts = new Dictionary<EmotionTypes, int>(),
                    AverageValue = 0,
                    TotalEntries = 0
                };
            }

            foreach (var entry in entries)
            {
                var timeOfDay = TimeHelper.GetTimeOfDay(entry.Timestamp);
                var stats = result[timeOfDay];
                var emotionType = Enum.Parse<EmotionTypes>(entry.EmotionData.Type);

                // Обновляем счетчики
                if (!stats.EmotionCounts.ContainsKey(emotionType))
                {
                    stats.EmotionCounts[emotionType] = 0;
                    stats.AverageIntensities[emotionType] = 0;
                }

                stats.EmotionCounts[emotionType]++;
                stats.TotalEntries++;
                
                // Обновляем средние значения
                float oldAvg = stats.AverageIntensities[emotionType];
                int count = stats.EmotionCounts[emotionType];
                stats.AverageIntensities[emotionType] = oldAvg + (entry.EmotionData.Intensity - oldAvg) / count;
                
                stats.AverageValue = stats.AverageValue + (entry.EmotionData.Value - stats.AverageValue) / stats.TotalEntries;
            }

            return result;
        }

        public IEnumerable<EmotionFrequencyStats> GetLoggingFrequency(DateTime from, DateTime to, bool groupByDay = true)
        {
            Debug.Log($"GetLoggingFrequency: from={from}, to={to}, groupByDay={groupByDay}");
            
            var history = GetHistory(from, to);
            var valueChangedEntries = history.Where(e => e.EventType == EmotionEventType.ValueChanged);
            Debug.Log($"Value changed entries: {valueChangedEntries.Count()}");
            
            var groupedEntries = groupByDay
                ? valueChangedEntries.GroupBy(e => e.Timestamp.Date)
                : valueChangedEntries.GroupBy(e => new DateTime(e.Timestamp.Year, e.Timestamp.Month, e.Timestamp.Day, e.Timestamp.Hour, 0, 0));
                
            Debug.Log($"Grouped entries count: {groupedEntries.Count()}");
            
            var stats = groupedEntries.Select(group =>
            {
                var emotionTypeCounts = group
                    .GroupBy(e => Enum.Parse<EmotionTypes>(e.EmotionData.Type))
                    .ToDictionary(g => g.Key, g => g.Count());
                    
                Debug.Log($"For {group.Key}: Total entries={group.Count()}, Emotion types={string.Join(", ", emotionTypeCounts.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
                
                return new EmotionFrequencyStats
                {
                    Date = group.Key,
                    EntryCount = group.Count(),
                    EmotionTypeCounts = emotionTypeCounts
                };
            }).ToList();
            
            Debug.Log($"Returning {stats.Count} frequency stats");
            return stats;
        }

        public List<EmotionCombinationStats> GetPopularEmotionCombinations(DateTime? from = null, DateTime? to = null, int topCount = 5)
        {
            var entries = GetHistory(from, to)
                .Where(e => e.EventType == EmotionEventType.EmotionMixed)
                .ToList();

            var combinations = new Dictionary<(EmotionTypes, EmotionTypes), EmotionCombinationStats>();

            foreach (var entry in entries)
            {
                // Предполагаем, что у нас есть информация о комбинации в данных
                if (entry.EmotionData.Note?.Contains("+") == true)
                {
                    var parts = entry.EmotionData.Note.Split('+');
                    if (parts.Length == 2 &&
                        Enum.TryParse<EmotionTypes>(parts[0].Trim(), out var first) &&
                        Enum.TryParse<EmotionTypes>(parts[1].Trim(), out var second))
                    {
                        var key = (first, second);
                        if (!combinations.ContainsKey(key))
                        {
                            combinations[key] = new EmotionCombinationStats
                            {
                                FirstEmotion = first,
                                SecondEmotion = second,
                                CombinationCount = 0,
                                AverageResultIntensity = 0,
                                MostCommonResult = Enum.Parse<EmotionTypes>(entry.EmotionData.Type)
                            };
                        }

                        var stats = combinations[key];
                        stats.CombinationCount++;
                        stats.AverageResultIntensity = ((stats.AverageResultIntensity * (stats.CombinationCount - 1)) + 
                                                       entry.EmotionData.Intensity) / stats.CombinationCount;
                    }
                }
            }

            return combinations.Values
                .OrderByDescending(c => c.CombinationCount)
                .Take(topCount)
                .ToList();
        }

        public List<EmotionTrendStats> GetEmotionTrends(DateTime from, DateTime to, bool groupByDay = true)
        {
            var result = new List<EmotionTrendStats>();
            var entries = GetHistory(from, to)
                .Where(e => e.EventType == EmotionEventType.ValueChanged)
                .GroupBy(e => groupByDay ? e.Timestamp.Date : 
                    new DateTime(e.Timestamp.Year, e.Timestamp.Month, e.Timestamp.Day, e.Timestamp.Hour, 0, 0))
                .OrderBy(g => g.Key);

            EmotionTrendStats previousStats = null;

            foreach (var group in entries)
            {
                var stats = new EmotionTrendStats
                {
                    Date = group.Key,
                    AverageIntensities = new Dictionary<EmotionTypes, float>()
                };

                // Вычисляем средние значения для каждого типа эмоций
                foreach (var type in Enum.GetValues(typeof(EmotionTypes)).Cast<EmotionTypes>())
                {
                    var typeEntries = group.Where(e => e.EmotionData.Type == type.ToString());
                    if (typeEntries.Any())
                    {
                        float avgValue = typeEntries.Average(e => e.EmotionData.Value);
                        stats.AverageIntensities[type] = avgValue;
                    }
                }

                // Определяем доминирующую эмоцию
                if (stats.AverageIntensities.Any())
                {
                    stats.DominantEmotion = stats.AverageIntensities
                        .OrderByDescending(kvp => kvp.Value)
                        .First().Key;

                    // Вычисляем тренд
                    if (previousStats != null && previousStats.AverageIntensities.Any())
                    {
                        var commonEmotions = stats.AverageIntensities.Keys
                            .Intersect(previousStats.AverageIntensities.Keys);

                        if (commonEmotions.Any())
                        {
                            float totalDiff = 0;
                            foreach (var emotion in commonEmotions)
                            {
                                totalDiff += stats.AverageIntensities[emotion] - previousStats.AverageIntensities[emotion];
                            }
                            stats.TrendValue = totalDiff / commonEmotions.Count();
                        }
                    }
                    else
                    {
                        // Для первой записи используем среднее значение как тренд
                        stats.TrendValue = stats.AverageIntensities.Values.Average();
                    }
                }

                result.Add(stats);
                previousStats = stats;
            }

            return result;
        }

        public void Clear()
        {
            _historyQueue.Clear();
            _historyByType.Clear();
        }

        #region Sync Methods
        
        /// <summary>
        /// Загружает историю из кэша
        /// </summary>
        private void LoadFromCache()
        {
            if (_cache == null) return;
            
            try
            {
                Debug.Log("Загрузка истории эмоций из кэша...");
                
                // Очищаем текущую историю
                _historyQueue.Clear();
                _historyByType.Clear();
                
                // Получаем все записи из кэша
                var records = _cache.GetAllRecords();
                Debug.Log($"Найдено {records.Count} записей в кэше");
                
                // Добавляем записи в историю
                foreach (var record in records)
                {
                    AddEntry(record);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при загрузке истории из кэша: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Сохраняет запись в кэш
        /// </summary>
        private void SaveEntryToCache(EmotionHistoryEntry entry)
        {
            if (_cache == null) return;
            
            try
            {
                // Преобразуем запись в EmotionHistoryRecord
                var record = new EmotionHistoryRecord(entry.EmotionData, entry.EventType)
                {
                    Id = entry.SyncId,
                    SyncStatus = entry.IsSynced ? SyncStatus.Synced : SyncStatus.NotSynced
                };
                
                // Добавляем в кэш
                _cache.AddRecord(record);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при сохранении записи в кэш: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Обновляет статус синхронизации записи
        /// </summary>
        public void UpdateSyncStatus(string syncId, bool isSynced)
        {
            if (string.IsNullOrEmpty(syncId)) return;
            
            // Находим запись в истории и обновляем статус
            var entry = _historyQueue.FirstOrDefault(e => e.SyncId == syncId);
            
            if (entry != null)
            {
                entry.IsSynced = isSynced;
                
                // Также обновляем в кэше
                if (_useCache)
                {
                    var record = _cache.GetRecord(syncId);
                    if (record != null)
                    {
                        record.SyncStatus = isSynced ? SyncStatus.Synced : SyncStatus.NotSynced;
                        _cache.UpdateRecord(record);
                    }
                }
            }
        }
        
        /// <summary>
        /// Получает несинхронизированные записи
        /// </summary>
        public List<EmotionHistoryEntry> GetUnsyncedEntries(int limit = 100)
        {
            return _historyQueue
                .Where(e => !e.IsSynced)
                .Take(limit)
                .ToList();
        }
        
        /// <summary>
        /// Получает количество несинхронизированных записей
        /// </summary>
        public int GetUnsyncedCount()
        {
            return _historyQueue.Count(e => !e.IsSynced);
        }
        
        #endregion
    }
} 