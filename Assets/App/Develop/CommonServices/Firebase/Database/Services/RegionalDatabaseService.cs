using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Develop.CommonServices.Firebase.Common.Cache;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.Utils.Logging;
using Firebase.Database;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    public class RegionalDatabaseService : FirebaseDatabaseServiceBase, IRegionalDatabaseService
    {
        #region Constants
        private const string REGIONS_PATH = "regions";
        private const string EMOTIONS_PATH = "emotions";
        #endregion

        #region Constructor
        public RegionalDatabaseService(
            DatabaseReference database,
            FirebaseCacheManager cacheManager,
            DataValidationService validationService)
            : base(database, cacheManager, validationService)
        {
            MyLogger.Log("RegionalDatabaseService инициализирован", MyLogger.LogCategory.Regional);
        }
        #endregion

        #region IRegionalDatabaseService Implementation
        public async Task<Dictionary<string, RegionData>> GetAllRegionData()
        {
            try
            {
                DatabaseReference regionsRef = _database.Child(REGIONS_PATH);
                DataSnapshot snapshot = await regionsRef.GetValueAsync();

                if (snapshot == null || !snapshot.Exists)
                {
                    MyLogger.Log("Нет данных о регионах в Firebase", MyLogger.LogCategory.Regional);
                    return new Dictionary<string, RegionData>();
                }

                Dictionary<string, RegionData> regions = new Dictionary<string, RegionData>();
                
                foreach (DataSnapshot regionSnapshot in snapshot.Children)
                {
                    string regionName = regionSnapshot.Key;
                    RegionData regionData = ParseRegionData(regionSnapshot);
                    
                    if (regionData != null)
                    {
                        regions[regionName] = regionData;
                    }
                }

                MyLogger.Log($"Загружено {regions.Count} регионов из Firebase", MyLogger.LogCategory.Regional);
                return regions;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при получении данных всех регионов: {ex.Message}", MyLogger.LogCategory.Regional);
                return new Dictionary<string, RegionData>();
            }
        }

        public async Task<RegionData> GetRegionData(string regionName)
        {
            if (string.IsNullOrEmpty(regionName))
            {
                MyLogger.LogWarning("Передано пустое название региона", MyLogger.LogCategory.Regional);
                return null;
            }

            try
            {
                DatabaseReference regionRef = _database.Child(REGIONS_PATH).Child(regionName);
                DataSnapshot snapshot = await regionRef.GetValueAsync();

                if (snapshot == null || !snapshot.Exists)
                {
                    MyLogger.Log($"Регион '{regionName}' не найден в Firebase", MyLogger.LogCategory.Regional);
                    return null;
                }

                RegionData regionData = ParseRegionData(snapshot);
                MyLogger.Log($"Загружены данные региона '{regionName}'", MyLogger.LogCategory.Regional);
                return regionData;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при получении данных региона '{regionName}': {ex.Message}", MyLogger.LogCategory.Regional);
                return null;
            }
        }

        public async Task<bool> SaveRegionData(string regionName, RegionData regionData)
        {
            if (string.IsNullOrEmpty(regionName) || regionData == null)
            {
                MyLogger.LogWarning("Некорректные параметры для сохранения данных региона", MyLogger.LogCategory.Regional);
                return false;
            }

            try
            {
                DatabaseReference regionRef = _database.Child(REGIONS_PATH).Child(regionName);
                
                Dictionary<string, object> regionDict = new Dictionary<string, object>
                {
                    ["name"] = regionData.Name ?? regionName,
                    ["emotions"] = regionData.Emotions ?? new Dictionary<string, int>(),
                    ["lastUpdated"] = ServerValue.Timestamp
                };

                if (regionData.Location != null)
                {
                    regionDict["location"] = new Dictionary<string, object>
                    {
                        ["latitude"] = regionData.Location.Latitude,
                        ["longitude"] = regionData.Location.Longitude
                    };
                }

                await regionRef.SetValueAsync(regionDict);
                MyLogger.Log($"Данные региона '{regionName}' сохранены в Firebase", MyLogger.LogCategory.Regional);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при сохранении данных региона '{regionName}': {ex.Message}", MyLogger.LogCategory.Regional);
                return false;
            }
        }

        public async Task<bool> UpdateRegionEmotionStats(string regionName, string emotionType, int count)
        {
            if (string.IsNullOrEmpty(regionName) || string.IsNullOrEmpty(emotionType))
            {
                MyLogger.LogWarning("Некорректные параметры для обновления статистики эмоций региона", MyLogger.LogCategory.Regional);
                return false;
            }

            try
            {
                DatabaseReference emotionRef = _database.Child(REGIONS_PATH)
                    .Child(regionName)
                    .Child(EMOTIONS_PATH)
                    .Child(emotionType);

                await emotionRef.SetValueAsync(count);
                MyLogger.Log($"Статистика эмоции '{emotionType}' в регионе '{regionName}' обновлена: {count}", MyLogger.LogCategory.Regional);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при обновлении статистики эмоции '{emotionType}' в регионе '{regionName}': {ex.Message}", MyLogger.LogCategory.Regional);
                return false;
            }
        }

        public async Task<bool> IncrementRegionEmotionCount(string regionName, string emotionType, int increment = 1)
        {
            if (string.IsNullOrEmpty(regionName) || string.IsNullOrEmpty(emotionType))
            {
                MyLogger.LogWarning("Некорректные параметры для увеличения счетчика эмоций региона", MyLogger.LogCategory.Regional);
                return false;
            }

            try
            {
                DatabaseReference emotionRef = _database.Child(REGIONS_PATH)
                    .Child(regionName)
                    .Child(EMOTIONS_PATH)
                    .Child(emotionType);

                // Получаем текущее значение
                DataSnapshot snapshot = await emotionRef.GetValueAsync();
                int currentCount = 0;
                
                if (snapshot.Exists && snapshot.Value != null)
                {
                    if (int.TryParse(snapshot.Value.ToString(), out int parsedValue))
                    {
                        currentCount = parsedValue;
                    }
                }

                int newCount = currentCount + increment;
                await emotionRef.SetValueAsync(newCount);
                
                MyLogger.Log($"Счетчик эмоции '{emotionType}' в регионе '{regionName}' увеличен на {increment}: {currentCount} -> {newCount}", MyLogger.LogCategory.Regional);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при увеличении счетчика эмоции '{emotionType}' в регионе '{regionName}': {ex.Message}", MyLogger.LogCategory.Regional);
                return false;
            }
        }

        public async Task<bool> DeleteRegionData(string regionName)
        {
            if (string.IsNullOrEmpty(regionName))
            {
                MyLogger.LogWarning("Передано пустое название региона для удаления", MyLogger.LogCategory.Regional);
                return false;
            }

            try
            {
                DatabaseReference regionRef = _database.Child(REGIONS_PATH).Child(regionName);
                await regionRef.RemoveValueAsync();
                
                MyLogger.Log($"Данные региона '{regionName}' удалены из Firebase", MyLogger.LogCategory.Regional);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при удалении данных региона '{regionName}': {ex.Message}", MyLogger.LogCategory.Regional);
                return false;
            }
        }

        public async Task<List<string>> GetAvailableRegions()
        {
            try
            {
                DatabaseReference regionsRef = _database.Child(REGIONS_PATH);
                DataSnapshot snapshot = await regionsRef.GetValueAsync();

                if (snapshot == null || !snapshot.Exists)
                {
                    MyLogger.Log("Нет доступных регионов в Firebase", MyLogger.LogCategory.Regional);
                    return new List<string>();
                }

                List<string> regionNames = snapshot.Children.Select(child => child.Key).ToList();
                MyLogger.Log($"Найдено {regionNames.Count} доступных регионов", MyLogger.LogCategory.Regional);
                return regionNames;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при получении списка доступных регионов: {ex.Message}", MyLogger.LogCategory.Regional);
                return new List<string>();
            }
        }
        #endregion

        #region Private Methods
        private RegionData ParseRegionData(DataSnapshot snapshot)
        {
            try
            {
                if (snapshot == null || !snapshot.Exists)
                {
                    return null;
                }

                RegionData regionData = new RegionData
                {
                    Name = snapshot.Key,
                    Emotions = new Dictionary<string, int>()
                };

                // Парсим название
                if (snapshot.Child("name").Exists)
                {
                    regionData.Name = snapshot.Child("name").Value?.ToString() ?? snapshot.Key;
                }

                // Парсим эмоции
                DataSnapshot emotionsSnapshot = snapshot.Child(EMOTIONS_PATH);
                if (emotionsSnapshot.Exists)
                {
                    foreach (DataSnapshot emotionSnapshot in emotionsSnapshot.Children)
                    {
                        string emotionType = emotionSnapshot.Key;
                        if (int.TryParse(emotionSnapshot.Value?.ToString(), out int count))
                        {
                            regionData.Emotions[emotionType] = count;
                        }
                    }
                }

                // Парсим геолокацию
                DataSnapshot locationSnapshot = snapshot.Child("location");
                if (locationSnapshot.Exists)
                {
                    DataSnapshot latSnapshot = locationSnapshot.Child("latitude");
                    DataSnapshot lonSnapshot = locationSnapshot.Child("longitude");
                    
                    if (latSnapshot.Exists && lonSnapshot.Exists)
                    {
                        if (double.TryParse(latSnapshot.Value?.ToString(), out double lat) &&
                            double.TryParse(lonSnapshot.Value?.ToString(), out double lon))
                        {
                            regionData.Location = new GeoLocation
                            {
                                Latitude = lat,
                                Longitude = lon
                            };
                        }
                    }
                }

                return regionData;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при парсинге данных региона: {ex.Message}", MyLogger.LogCategory.Regional);
                return null;
            }
        }
        #endregion
    }
} 