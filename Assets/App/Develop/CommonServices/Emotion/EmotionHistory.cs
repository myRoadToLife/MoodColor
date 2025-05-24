using System;
using System.Collections.Generic;
using System.Linq;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.CommonServices.Firebase.Database.Services;
using App.Develop.CommonServices.DataManagement.DataProviders;
using UnityEngine;
using App.Develop.Utils.Logging;

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
        public string Note { get; set; }    // Заметка к событию
        
        public EmotionHistoryEntry()
        {
            SyncId = Guid.NewGuid().ToString();
            IsSynced = false;
        }

        // МЕТОД ДЛЯ КЛОНИРОВАНИЯ ЗАПИСИ ИСТОРИИ
        public EmotionHistoryEntry Clone()
        {
            return new EmotionHistoryEntry
            {
                EmotionData = this.EmotionData?.Clone(), // Клонируем EmotionData, если оно не null
                Timestamp = this.Timestamp,
                EventType = this.EventType,
                SyncId = this.SyncId, // SyncId можно копировать, т.к. это идентификатор исходной записи
                IsSynced = this.IsSynced,
                Note = this.Note
            };
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
            
            MyLogger.Log($"[EmotionHistory] Создан новый экземпляр. _useCache={_useCache}, _cache={(_cache == null ? "NULL" : "не NULL")}", MyLogger.LogCategory.Emotion);
            
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
        public void AddEntry(EmotionData emotion, EmotionEventType eventType, DateTime? timestamp = null, string note = null)
        {
            if (emotion == null)
            {
                MyLogger.LogWarning("❌ Попытка добавить null запись в историю эмоций", MyLogger.LogCategory.Emotion);
                return;
            }

            MyLogger.Log($"AddEntry: Adding entry for {emotion.Type} with EventType={eventType} at {timestamp ?? emotion.LastUpdate}", MyLogger.LogCategory.Emotion);

            var entry = new EmotionHistoryEntry
            {
                EmotionData = emotion.Clone(), // Создаем копию для истории
                Timestamp = timestamp ?? emotion.LastUpdate, // Используем LastUpdate, если timestamp не указан
                EventType = eventType,
                Note = note
            };

            // НОВЫЙ ЛОГ ЗДЕСЬ
            MyLogger.Log($"[EmotionHistory.AddEntry] Creating new entry: Timestamp='{entry.Timestamp:O}', Type='{entry.EmotionData?.Type}', Value='{entry.EmotionData?.Value}', EventType='{entry.EventType}', Note='{entry.Note}', SyncId='{entry.SyncId}'", MyLogger.LogCategory.Emotion);

            // Проверяем, не существует ли уже запись с таким SyncId (для избежания дубликатов)
            if (!string.IsNullOrEmpty(entry.SyncId))
            {
                var existingEntry = _historyQueue.FirstOrDefault(e => e.SyncId == entry.SyncId);
                if (existingEntry != null)
                {
                    MyLogger.Log($"[EmotionHistory] Запись с SyncId={entry.SyncId} уже существует, пропускаем добавление");
                    return;
                }
            }

            // MyLogger.Log($"Created entry: Type={entry.EmotionData.Type}, Value={entry.EmotionData.Value}, EventType={entry.EventType}, Timestamp={entry.Timestamp}", MyLogger.LogCategory.Emotion); // Старый лог, можно закомментировать или удалить, если новый его покрывает

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
            if (record == null)
            {
                MyLogger.LogWarning("[EmotionHistory] Попытка добавить NULL EmotionHistoryRecord");
                return;
            }
            
            try
            {
                // Проверяем, не существует ли уже запись с таким SyncId
                if (!string.IsNullOrEmpty(record.Id))
                {
                    var existingEntry = _historyQueue.FirstOrDefault(e => e.SyncId == record.Id);
                    if (existingEntry != null)
                    {
                        MyLogger.Log($"[EmotionHistory] Запись с SyncId={record.Id} уже существует, пропускаем добавление");
                        return;
                    }
                }
                
                // Конвертируем в EmotionData
                var emotionData = record.ToEmotionData();
                
                if (emotionData == null)
                {
                    MyLogger.LogError($"[EmotionHistory] Ошибка конвертации EmotionHistoryRecord в EmotionData: record.Id={record.Id}, record.Type={record.Type}");
                    return;
                }
                
                if (string.IsNullOrEmpty(emotionData.Type))
                {
                    MyLogger.LogError($"[EmotionHistory] EmotionData после конвертации содержит пустой Type: record.Id={record.Id}");
                    return;
                }
                
                // Парсим тип события
                if (!Enum.TryParse<EmotionEventType>(record.EventType, out var eventType))
                {
                    MyLogger.LogWarning($"[EmotionHistory] Не удалось распознать тип события: {record.EventType}, используем ValueChanged");
                    eventType = EmotionEventType.ValueChanged;
                }
                
                var entry = new EmotionHistoryEntry
                {
                    EmotionData = emotionData,
                    Timestamp = record.RecordTime,
                    EventType = eventType,
                    SyncId = record.Id,
                    IsSynced = record.SyncStatus == SyncStatus.Synced,
                    Note = record.Note
                };
                
                MyLogger.Log($"[EmotionHistory] Добавление записи из Firebase: Id={record.Id}, Type={emotionData.Type}, Timestamp={record.RecordTime}");
                
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
                else
                {
                    MyLogger.LogWarning($"[EmotionHistory] Не удалось распознать тип эмоции: {emotionData.Type}");
                }
                
                // Ограничиваем размер истории
                if (_historyQueue.Count > MAX_HISTORY_ENTRIES)
                {
                    var oldestEntry = _historyQueue.Dequeue();
                    
                    // Также удаляем из словаря по типу
                    if (oldestEntry != null && oldestEntry.EmotionData != null && 
                        !string.IsNullOrEmpty(oldestEntry.EmotionData.Type) &&
                        Enum.TryParse<EmotionTypes>(oldestEntry.EmotionData.Type, out var oldEmotionType) && 
                        _historyByType.TryGetValue(oldEmotionType, out var entries))
                    {
                        entries.Remove(oldestEntry);
                    }
                }
                
                // Вызываем событие
                OnHistoryEntryAdded?.Invoke(entry);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"[EmotionHistory] Ошибка при добавлении записи из Firebase: {ex.Message}");
            }
        }

        /// <summary>
        /// Получает историю эмоций
        /// </summary>
        public IEnumerable<EmotionHistoryEntry> GetHistory(DateTime? from = null, DateTime? to = null)
        {
            // Отладочная информация о вызове
            MyLogger.Log($"[EmotionHistory.GetHistory] Вызван с параметрами: from={from?.ToString() ?? "NULL"}, to={to?.ToString() ?? "NULL"}, _historyQueue.Count={_historyQueue.Count}");
            
            // Если очередь пуста, возвращаем пустой список
            if (_historyQueue.Count == 0)
            {
                MyLogger.LogWarning("[EmotionHistory.GetHistory] Очередь истории пуста");
                return new List<EmotionHistoryEntry>();
            }

            // Создаем список из очереди (делаем копию, чтобы не изменять исходную очередь)
            var entriesList = _historyQueue.ToList();
            MyLogger.Log($"[EmotionHistory.GetHistory] Создан список из очереди, размер: {entriesList.Count}");
            
            // Логируем типы событий в истории
            var eventTypeCounts = entriesList.GroupBy(e => e.EventType).ToDictionary(g => g.Key, g => g.Count());
            MyLogger.Log($"[EmotionHistory.GetHistory] Типы событий в истории: {string.Join(", ", eventTypeCounts.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
            
            // Применяем фильтры по датам, если указаны
            if (from.HasValue)
            {
                entriesList = entriesList.Where(e => e.Timestamp >= from.Value).ToList();
                MyLogger.Log($"[EmotionHistory.GetHistory] После фильтра from, размер: {entriesList.Count}");
            }
            
            if (to.HasValue)
            {
                entriesList = entriesList.Where(e => e.Timestamp <= to.Value).ToList();
                MyLogger.Log($"[EmotionHistory.GetHistory] После фильтра to, размер: {entriesList.Count}");
            }
            
            // Сортируем для отображения (последние записи сверху)
            var sortedEntries = entriesList.OrderByDescending(e => e.Timestamp).ToList();
            MyLogger.Log($"[EmotionHistory.GetHistory] Отсортированный список, размер: {sortedEntries.Count}");
            
            // Логирование некоторых записей для отладки
            if (sortedEntries.Count > 0)
            {
                MyLogger.Log("[EmotionHistory.GetHistory] Примеры записей:");
                for (int i = 0; i < Math.Min(5, sortedEntries.Count); i++)
                {
                    var entry = sortedEntries[i];
                    MyLogger.Log($"  {i}: Timestamp={entry.Timestamp:O}, Type={entry.EmotionData?.Type}, Event={entry.EventType}, Note={entry.Note}");
                }
            }
            
            return sortedEntries;
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
            MyLogger.Log($"GetLoggingFrequency: from={from}, to={to}, groupByDay={groupByDay}");
            
            var history = GetHistory(from, to);
            var valueChangedEntries = history.Where(e => e.EventType == EmotionEventType.ValueChanged);
            MyLogger.Log($"Value changed entries: {valueChangedEntries.Count()}");
            
            var groupedEntries = groupByDay
                ? valueChangedEntries.GroupBy(e => e.Timestamp.Date)
                : valueChangedEntries.GroupBy(e => new DateTime(e.Timestamp.Year, e.Timestamp.Month, e.Timestamp.Day, e.Timestamp.Hour, 0, 0));
                
            MyLogger.Log($"Grouped entries count: {groupedEntries.Count()}");
            
            var stats = groupedEntries.Select(group =>
            {
                var emotionTypeCounts = group
                    .GroupBy(e => Enum.Parse<EmotionTypes>(e.EmotionData.Type))
                    .ToDictionary(g => g.Key, g => g.Count());
                    
                MyLogger.Log($"For {group.Key}: Total entries={group.Count()}, Emotion types={string.Join(", ", emotionTypeCounts.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
                
                return new EmotionFrequencyStats
                {
                    Date = group.Key,
                    EntryCount = group.Count(),
                    EmotionTypeCounts = emotionTypeCounts
                };
            }).ToList();
            
            MyLogger.Log($"Returning {stats.Count} frequency stats");
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

        /// <summary>
        /// Удаляет дубликаты записей на основе SyncId
        /// </summary>
        public void RemoveDuplicates()
        {
            MyLogger.Log($"[EmotionHistory] Начало удаления дубликатов. Текущее количество записей: {_historyQueue.Count}");
            
            var uniqueEntries = new List<EmotionHistoryEntry>();
            var seenSyncIds = new HashSet<string>();
            
            // Проходим по всем записям и оставляем только уникальные
            foreach (var entry in _historyQueue)
            {
                if (string.IsNullOrEmpty(entry.SyncId))
                {
                    // Записи без SyncId всегда добавляем (это локальные записи)
                    uniqueEntries.Add(entry);
                }
                else if (!seenSyncIds.Contains(entry.SyncId))
                {
                    // Первая запись с таким SyncId
                    seenSyncIds.Add(entry.SyncId);
                    uniqueEntries.Add(entry);
                }
                else
                {
                    MyLogger.Log($"[EmotionHistory] Найден дубликат с SyncId={entry.SyncId}, удаляем");
                }
            }
            
            // Очищаем и заново заполняем очередь
            _historyQueue.Clear();
            _historyByType.Clear();
            
            foreach (var entry in uniqueEntries)
            {
                _historyQueue.Enqueue(entry);
                
                // Добавляем в словарь по типу
                if (entry.EmotionData != null && !string.IsNullOrEmpty(entry.EmotionData.Type) &&
                    Enum.TryParse<EmotionTypes>(entry.EmotionData.Type, out var emotionType))
                {
                    if (!_historyByType.ContainsKey(emotionType))
                    {
                        _historyByType[emotionType] = new List<EmotionHistoryEntry>();
                    }
                    
                    _historyByType[emotionType].Add(entry);
                }
            }
            
            MyLogger.Log($"[EmotionHistory] Удаление дубликатов завершено. Осталось записей: {_historyQueue.Count}");
        }

        #region Sync Methods
        
        /// <summary>
        /// Загружает историю из кэша
        /// </summary>
        private void LoadFromCache()
        {
            if (_cache == null) 
            {
                MyLogger.LogWarning("[EmotionHistory] Попытка загрузить из кэша, но _cache = null");
                return;
            }
            
            MyLogger.Log("[EmotionHistory] Загрузка записей из кэша...");
            
            try
            {
                var records = _cache.GetAllRecords();
                MyLogger.Log($"[EmotionHistory] Получено {records.Count} записей из кэша");
                
                foreach (var record in records)
                {
                    AddEntry(record);
                }
                
                MyLogger.Log($"[EmotionHistory] Загрузка из кэша завершена. В _historyQueue теперь {_historyQueue.Count} записей");
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"[EmotionHistory] Ошибка при загрузке из кэша: {ex.Message}");
            }

            RemoveDuplicates();
        }
        
        /// <summary>
        /// Сохраняет запись в кэш
        /// </summary>
        private void SaveEntryToCache(EmotionHistoryEntry entry)
        {
            if (_cache == null) return;
            
            try
            {
                // Преобразуем запись в EmotionHistoryRecord, используя обновленный конструктор
                var record = new EmotionHistoryRecord(entry); // Теперь передаем весь entry
                
                // Добавляем в кэш
                _cache.AddRecord(record);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при сохранении записи в кэш: {ex.Message}");
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