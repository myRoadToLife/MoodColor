using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.App.Develop.Scenes.PersonalAreaScene.UI.Components;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.CommonServices.Firebase.Database.Services;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Regional
{
    public class RegionalStatsService : IRegionalStatsService
    {
        #region Private Fields
        private readonly IRegionalDatabaseService _databaseService;
        private Dictionary<string, RegionalEmotionStats> _cachedStats;
        private bool _isInitialized;
        private DateTime _lastCacheUpdate;
        private const int CACHE_EXPIRY_MINUTES = 30;
        #endregion

        #region Constructor
        public RegionalStatsService(IRegionalDatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _cachedStats = new Dictionary<string, RegionalEmotionStats>();
            _lastCacheUpdate = DateTime.MinValue;
        }
        #endregion

        #region IRegionalStatsService Implementation
        public void Initialize()
        {
            if (_isInitialized) return;
            
            MyLogger.Log("Инициализация RegionalStatsService", MyLogger.LogCategory.Regional);
            _isInitialized = true;
        }

        public async Task<Dictionary<string, RegionalEmotionStats>> GetAllRegionalStats()
        {
            if (!_isInitialized)
            {
                MyLogger.LogWarning("RegionalStatsService не инициализирован", MyLogger.LogCategory.Regional);
                return new Dictionary<string, RegionalEmotionStats>();
            }

            if (IsCacheValid())
            {
                return new Dictionary<string, RegionalEmotionStats>(_cachedStats);
            }

            try
            {
                await RefreshCache();
                return new Dictionary<string, RegionalEmotionStats>(_cachedStats);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при получении региональной статистики: {ex.Message}", MyLogger.LogCategory.Regional);
                return new Dictionary<string, RegionalEmotionStats>();
            }
        }

        public async Task<RegionalEmotionStats> GetRegionalStats(string regionName)
        {
            if (string.IsNullOrEmpty(regionName))
            {
                MyLogger.LogWarning("Передано пустое название региона", MyLogger.LogCategory.Regional);
                return null;
            }

            Dictionary<string, RegionalEmotionStats> allStats = await GetAllRegionalStats();
            return allStats.TryGetValue(regionName, out RegionalEmotionStats stats) ? stats : null;
        }

        public async Task<bool> UpdateRegionalStats(string regionName, RegionalEmotionStats stats)
        {
            if (string.IsNullOrEmpty(regionName) || stats == null)
            {
                MyLogger.LogWarning("Некорректные параметры для обновления региональной статистики", MyLogger.LogCategory.Regional);
                return false;
            }

            try
            {
                // Обновляем кэш
                _cachedStats[regionName] = stats;
                
                // Сохраняем в Firebase (если доступен)
                if (_databaseService != null)
                {
                    RegionData regionData = ConvertToRegionData(regionName, stats);
                    await _databaseService.SaveRegionData(regionName, regionData);
                }

                MyLogger.Log($"Статистика региона '{regionName}' обновлена", MyLogger.LogCategory.Regional);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при обновлении статистики региона '{regionName}': {ex.Message}", MyLogger.LogCategory.Regional);
                return false;
            }
        }

        public async Task<List<string>> GetAvailableRegions()
        {
            Dictionary<string, RegionalEmotionStats> allStats = await GetAllRegionalStats();
            return allStats.Keys.ToList();
        }
        #endregion

        #region Private Methods
        private bool IsCacheValid()
        {
            return _cachedStats.Count > 0 && 
                   (DateTime.Now - _lastCacheUpdate).TotalMinutes < CACHE_EXPIRY_MINUTES;
        }

        private async Task RefreshCache()
        {
            _cachedStats.Clear();

            if (_databaseService == null)
            {
                // Если Firebase недоступен, создаем тестовые данные
                CreateMockData();
                _lastCacheUpdate = DateTime.Now;
                return;
            }

            try
            {
                Dictionary<string, RegionData> regionDataDict = await _databaseService.GetAllRegionData();
                
                if (regionDataDict == null || regionDataDict.Count == 0)
                {
                    MyLogger.Log("Нет данных о регионах в Firebase, создаем тестовые данные", MyLogger.LogCategory.Regional);
                    CreateMockData();
                }
                else
                {
                    foreach (KeyValuePair<string, RegionData> kvp in regionDataDict)
                    {
                        RegionalEmotionStats stats = ConvertFromRegionData(kvp.Value);
                        _cachedStats[kvp.Key] = stats;
                    }
                }

                _lastCacheUpdate = DateTime.Now;
                MyLogger.Log($"Кэш региональной статистики обновлен. Загружено {_cachedStats.Count} регионов", MyLogger.LogCategory.Regional);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при обновлении кэша: {ex.Message}. Используем тестовые данные", MyLogger.LogCategory.Regional);
                CreateMockData();
                _lastCacheUpdate = DateTime.Now;
            }
        }

        private void CreateMockData()
        {
            // Создаем тестовые данные для демонстрации
            _cachedStats["Центральный"] = new RegionalEmotionStats
            {
                DominantEmotion = EmotionTypes.Joy,
                DominantEmotionPercentage = 45.2f,
                TotalEmotions = 1250,
                EmotionCounts = new Dictionary<EmotionTypes, int>
                {
                    { EmotionTypes.Joy, 565 },
                    { EmotionTypes.Trust, 312 },
                    { EmotionTypes.Love, 198 },
                    { EmotionTypes.Neutral, 175 }
                }
            };

            _cachedStats["Северный"] = new RegionalEmotionStats
            {
                DominantEmotion = EmotionTypes.Sadness,
                DominantEmotionPercentage = 38.7f,
                TotalEmotions = 890,
                EmotionCounts = new Dictionary<EmotionTypes, int>
                {
                    { EmotionTypes.Sadness, 344 },
                    { EmotionTypes.Anxiety, 267 },
                    { EmotionTypes.Fear, 156 },
                    { EmotionTypes.Neutral, 123 }
                }
            };

            _cachedStats["Южный"] = new RegionalEmotionStats
            {
                DominantEmotion = EmotionTypes.Love,
                DominantEmotionPercentage = 42.1f,
                TotalEmotions = 1100,
                EmotionCounts = new Dictionary<EmotionTypes, int>
                {
                    { EmotionTypes.Love, 463 },
                    { EmotionTypes.Joy, 341 },
                    { EmotionTypes.Trust, 198 },
                    { EmotionTypes.Surprise, 98 }
                }
            };

            _cachedStats["Восточный"] = new RegionalEmotionStats
            {
                DominantEmotion = EmotionTypes.Anger,
                DominantEmotionPercentage = 35.8f,
                TotalEmotions = 750,
                EmotionCounts = new Dictionary<EmotionTypes, int>
                {
                    { EmotionTypes.Anger, 268 },
                    { EmotionTypes.Disgust, 187 },
                    { EmotionTypes.Fear, 142 },
                    { EmotionTypes.Anxiety, 153 }
                }
            };
        }

        private RegionalEmotionStats ConvertFromRegionData(RegionData regionData)
        {
            if (regionData?.Emotions == null)
            {
                return new RegionalEmotionStats();
            }

            RegionalEmotionStats stats = new RegionalEmotionStats();
            int totalCount = 0;
            EmotionTypes dominantEmotion = EmotionTypes.Neutral;
            int maxCount = 0;

            foreach (KeyValuePair<string, int> kvp in regionData.Emotions)
            {
                if (Enum.TryParse(kvp.Key, out EmotionTypes emotionType))
                {
                    stats.EmotionCounts[emotionType] = kvp.Value;
                    totalCount += kvp.Value;

                    if (kvp.Value > maxCount)
                    {
                        maxCount = kvp.Value;
                        dominantEmotion = emotionType;
                    }
                }
            }

            stats.DominantEmotion = dominantEmotion;
            stats.TotalEmotions = totalCount;
            stats.DominantEmotionPercentage = totalCount > 0 ? (maxCount * 100f) / totalCount : 0f;

            return stats;
        }

        private RegionData ConvertToRegionData(string regionName, RegionalEmotionStats stats)
        {
            RegionData regionData = new RegionData
            {
                Name = regionName,
                Emotions = new Dictionary<string, int>()
            };

            if (stats?.EmotionCounts != null)
            {
                foreach (KeyValuePair<EmotionTypes, int> kvp in stats.EmotionCounts)
                {
                    regionData.Emotions[kvp.Key.ToString()] = kvp.Value;
                }
            }

            return regionData;
        }
        #endregion
    }
} 