using System;
using System.Collections.Generic;
using System.Linq;
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
    }

    public class EmotionHistory
    {
        private const int MAX_HISTORY_ENTRIES = 1000; // Максимальное количество записей в истории
        private readonly Queue<EmotionHistoryEntry> _historyQueue;
        private readonly Dictionary<EmotionTypes, List<EmotionHistoryEntry>> _historyByType;

        public EmotionHistory()
        {
            _historyQueue = new Queue<EmotionHistoryEntry>();
            _historyByType = new Dictionary<EmotionTypes, List<EmotionHistoryEntry>>();
        }

        public void AddEntry(EmotionData emotion, EmotionEventType eventType)
        {
            if (emotion == null)
            {
                Debug.LogWarning("❌ Попытка добавить null запись в историю эмоций");
                return;
            }

            var entry = new EmotionHistoryEntry
            {
                EmotionData = emotion.Clone(), // Создаем копию для истории
                Timestamp = DateTime.UtcNow,
                EventType = eventType
            };

            // Добавляем в общую очередь
            _historyQueue.Enqueue(entry);
            if (_historyQueue.Count > MAX_HISTORY_ENTRIES)
            {
                var oldEntry = _historyQueue.Dequeue();
                if (Enum.TryParse(oldEntry.EmotionData.Type, out EmotionTypes type))
                {
                    _historyByType[type]?.Remove(oldEntry);
                }
            }

            // Добавляем в историю по типу
            if (Enum.TryParse(emotion.Type, out EmotionTypes emotionType))
            {
                if (!_historyByType.ContainsKey(emotionType))
                {
                    _historyByType[emotionType] = new List<EmotionHistoryEntry>();
                }
                _historyByType[emotionType].Add(entry);
            }
        }

        public IEnumerable<EmotionHistoryEntry> GetHistory(DateTime? from = null, DateTime? to = null)
        {
            var entries = _historyQueue.AsEnumerable();
            
            if (from.HasValue)
                entries = entries.Where(e => e.Timestamp >= from.Value);
            
            if (to.HasValue)
                entries = entries.Where(e => e.Timestamp <= to.Value);
            
            return entries.OrderByDescending(e => e.Timestamp);
        }

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

        public Dictionary<EmotionTypes, float> GetAverageIntensityByPeriod(DateTime from, DateTime to)
        {
            var result = new Dictionary<EmotionTypes, float>();
            
            foreach (var type in Enum.GetValues(typeof(EmotionTypes)).Cast<EmotionTypes>())
            {
                var entries = GetHistoryByType(type, from, to);
                if (!entries.Any()) continue;
                
                float avgIntensity = entries.Average(e => e.EmotionData.Intensity);
                result[type] = avgIntensity;
            }
            
            return result;
        }

        public Dictionary<TimeOfDay, EmotionTimeStats> GetEmotionsByTimeOfDay(DateTime? from = null, DateTime? to = null)
        {
            var result = new Dictionary<TimeOfDay, EmotionTimeStats>();
            var entries = GetHistory(from, to);

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
                stats.TotalEntries++;
                
                if (!stats.EmotionCounts.ContainsKey(emotionType))
                {
                    stats.EmotionCounts[emotionType] = 0;
                    stats.AverageIntensities[emotionType] = 0;
                }

                stats.EmotionCounts[emotionType]++;
                
                // Обновляем средние значения
                float oldAvg = stats.AverageIntensities[emotionType];
                int count = stats.EmotionCounts[emotionType];
                stats.AverageIntensities[emotionType] = oldAvg + (entry.EmotionData.Intensity - oldAvg) / count;
                
                stats.AverageValue = stats.AverageValue + (entry.EmotionData.Value - stats.AverageValue) / stats.TotalEntries;
            }

            return result;
        }

        public List<EmotionFrequencyStats> GetLoggingFrequency(DateTime from, DateTime to, bool groupByDay = true)
        {
            var result = new List<EmotionFrequencyStats>();
            var entries = GetHistory(from, to).ToList();
            
            if (!entries.Any()) return result;

            // Группируем записи по дням
            var groupedEntries = groupByDay 
                ? entries.GroupBy(e => e.Timestamp.Date)
                : entries.GroupBy(e => new DateTime(e.Timestamp.Year, e.Timestamp.Month, e.Timestamp.Day, e.Timestamp.Hour, 0, 0));

            foreach (var group in groupedEntries)
            {
                var stats = new EmotionFrequencyStats
                {
                    Date = group.Key,
                    EntryCount = group.Count(),
                    EmotionTypeCounts = new Dictionary<EmotionTypes, int>()
                };

                // Подсчитываем количество каждого типа эмоций
                foreach (var entry in group)
                {
                    if (Enum.TryParse<EmotionTypes>(entry.EmotionData.Type, out var emotionType))
                    {
                        if (!stats.EmotionTypeCounts.ContainsKey(emotionType))
                            stats.EmotionTypeCounts[emotionType] = 0;
                        stats.EmotionTypeCounts[emotionType]++;
                    }
                }

                // Вычисляем среднее время между записями
                if (group.Count() > 1)
                {
                    var orderedEntries = group.OrderBy(e => e.Timestamp).ToList();
                    var totalTime = TimeSpan.Zero;
                    
                    for (int i = 1; i < orderedEntries.Count; i++)
                    {
                        totalTime += orderedEntries[i].Timestamp - orderedEntries[i - 1].Timestamp;
                    }
                    
                    stats.AverageTimeBetweenEntries = TimeSpan.FromTicks(totalTime.Ticks / (orderedEntries.Count - 1));
                }

                result.Add(stats);
            }

            return result.OrderBy(s => s.Date).ToList();
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
            var groupedEntries = GetHistory(from, to)
                .GroupBy(e => groupByDay ? e.Timestamp.Date : 
                    new DateTime(e.Timestamp.Year, e.Timestamp.Month, e.Timestamp.Day, e.Timestamp.Hour, 0, 0))
                .OrderBy(g => g.Key);

            EmotionTrendStats previousStats = null;

            foreach (var group in groupedEntries)
            {
                var stats = new EmotionTrendStats
                {
                    Date = group.Key,
                    AverageIntensities = new Dictionary<EmotionTypes, float>()
                };

                // Вычисляем средние интенсивности для каждого типа эмоций
                foreach (var type in Enum.GetValues(typeof(EmotionTypes)).Cast<EmotionTypes>())
                {
                    var typeEntries = group.Where(e => e.EmotionData.Type == type.ToString());
                    if (typeEntries.Any())
                    {
                        stats.AverageIntensities[type] = typeEntries.Average(e => e.EmotionData.Intensity);
                    }
                }

                // Определяем доминирующую эмоцию
                if (stats.AverageIntensities.Any())
                {
                    stats.DominantEmotion = stats.AverageIntensities
                        .OrderByDescending(kvp => kvp.Value)
                        .First().Key;
                }

                // Вычисляем тренд
                if (previousStats != null && previousStats.AverageIntensities.Any())
                {
                    float previousAvg = previousStats.AverageIntensities.Values.Average();
                    float currentAvg = stats.AverageIntensities.Values.Average();
                    stats.TrendValue = currentAvg - previousAvg;
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
    }
} 