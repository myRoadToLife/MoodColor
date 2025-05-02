using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Develop.AppServices.Firebase.Database.Models;
using App.Develop.CommonServices.DataManagement;
using App.Develop.CommonServices.Emotion;
using Newtonsoft.Json;
using UnityEngine;
using App.Develop.AppServices.Firebase.Common.SecureStorage;

namespace App.Develop.AppServices.Firebase.Database.Services
{
    public class EmotionHistoryCache
    {
        private const string CACHE_PREFIX = "EmotionHistoryCache_";
        private const string SYNC_SETTINGS_KEY = "EmotionSyncSettings";
        private const string CACHE_INDEX_KEY = "EmotionHistoryCacheIndex";
        private const int DEFAULT_MAX_CACHE_SIZE = 5000;
        
        private readonly List<string> _cacheIndex;
        private EmotionSyncSettings _syncSettings;
        private int _maxCacheSize;
        
        public EmotionHistoryCache()
        {
            try
            {
                _cacheIndex = LoadCacheIndex();
                _syncSettings = LoadSyncSettings();
                _maxCacheSize = _syncSettings?.MaxCacheRecords ?? DEFAULT_MAX_CACHE_SIZE;
                
                Debug.Log($"EmotionHistoryCache инициализирован. В кэше {_cacheIndex.Count} записей.");
            }
            catch (Exception ex)
            {
                // В случае ошибки создаем пустые объекты с значениями по умолчанию
                Debug.LogError($"Ошибка при инициализации EmotionHistoryCache: {ex.Message}");
                _cacheIndex = new List<string>();
                _syncSettings = CreateDefaultSyncSettings();
                _maxCacheSize = DEFAULT_MAX_CACHE_SIZE;
            }
        }
        
        #region Public Methods
        
        /// <summary>
        /// Проверяет инициализацию SecurePlayerPrefs
        /// </summary>
        private bool CheckSecurePrefsInitialized()
        {
            try
            {
                // Используем новый публичный метод для проверки
                return SecurePlayerPrefs.IsInitialized();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Ошибка при проверке инициализации SecurePlayerPrefs: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Добавляет запись в кэш
        /// </summary>
        public void AddRecord(EmotionHistoryRecord record)
        {
            if (record == null) return;
            
            // Проверяем инициализацию SecurePlayerPrefs
            if (!CheckSecurePrefsInitialized())
            {
                Debug.LogWarning($"Невозможно добавить запись {record.Id} в кэш: SecurePlayerPrefs не инициализирован");
                return;
            }
            
            try
            {
                string key = GetRecordKey(record.Id);
                string json = JsonConvert.SerializeObject(record);
                
                SecurePlayerPrefs.SetString(key, json);
                
                if (!_cacheIndex.Contains(record.Id))
                {
                    _cacheIndex.Add(record.Id);
                    SaveCacheIndex();
                    
                    // Проверка на превышение размера кэша
                    if (_cacheIndex.Count > _maxCacheSize)
                    {
                        PruneCache();
                    }
                }
                
                Debug.Log($"Запись {record.Id} добавлена в кэш. Всего записей: {_cacheIndex.Count}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при добавлении записи в кэш: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Получает запись из кэша по ID
        /// </summary>
        public EmotionHistoryRecord GetRecord(string recordId)
        {
            if (string.IsNullOrEmpty(recordId)) return null;
            
            // Проверяем инициализацию SecurePlayerPrefs
            if (!CheckSecurePrefsInitialized())
            {
                Debug.LogWarning($"Невозможно получить запись {recordId} из кэша: SecurePlayerPrefs не инициализирован");
                return null;
            }
            
            try
            {
                string key = GetRecordKey(recordId);
                if (SecurePlayerPrefs.HasKey(key))
                {
                    string json = SecurePlayerPrefs.GetString(key);
                    return JsonConvert.DeserializeObject<EmotionHistoryRecord>(json);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при получении записи из кэша: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// Обновляет запись в кэше
        /// </summary>
        public void UpdateRecord(EmotionHistoryRecord record)
        {
            AddRecord(record); // Просто перезаписываем
        }
        
        /// <summary>
        /// Удаляет запись из кэша
        /// </summary>
        public void RemoveRecord(string recordId)
        {
            if (string.IsNullOrEmpty(recordId)) return;
            
            // Проверяем инициализацию SecurePlayerPrefs
            if (!CheckSecurePrefsInitialized())
            {
                Debug.LogWarning($"Невозможно удалить запись {recordId} из кэша: SecurePlayerPrefs не инициализирован");
                return;
            }
            
            try
            {
                string key = GetRecordKey(recordId);
                if (SecurePlayerPrefs.HasKey(key))
                {
                    SecurePlayerPrefs.DeleteKey(key);
                }
                
                if (_cacheIndex.Contains(recordId))
                {
                    _cacheIndex.Remove(recordId);
                    SaveCacheIndex();
                    Debug.Log($"Запись {recordId} удалена из кэша. Осталось записей: {_cacheIndex.Count}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при удалении записи из кэша: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Получает все записи из кэша
        /// </summary>
        public List<EmotionHistoryRecord> GetAllRecords()
        {
            var records = new List<EmotionHistoryRecord>();
            
            try
            {
                foreach (var recordId in _cacheIndex)
                {
                    var record = GetRecord(recordId);
                    if (record != null)
                    {
                        records.Add(record);
                    }
                }
                
                // Сортируем по времени
                records = records.OrderByDescending(r => r.Timestamp).ToList();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при получении всех записей из кэша: {ex.Message}");
            }
            
            return records;
        }
        
        /// <summary>
        /// Получает записи из кэша по заданным параметрам
        /// </summary>
        public List<EmotionHistoryRecord> GetRecords(DateTime? startDate = null, DateTime? endDate = null, 
            string emotionType = null, SyncStatus? syncStatus = null, int limit = 100)
        {
            var allRecords = GetAllRecords();
            var filteredRecords = allRecords.AsEnumerable();
            
            try
            {
                if (startDate.HasValue)
                {
                    var startTimestamp = startDate.Value.ToFileTimeUtc();
                    filteredRecords = filteredRecords.Where(r => r.Timestamp >= startTimestamp);
                }
                
                if (endDate.HasValue)
                {
                    var endTimestamp = endDate.Value.ToFileTimeUtc();
                    filteredRecords = filteredRecords.Where(r => r.Timestamp <= endTimestamp);
                }
                
                if (!string.IsNullOrEmpty(emotionType))
                {
                    filteredRecords = filteredRecords.Where(r => r.Type == emotionType);
                }
                
                if (syncStatus.HasValue)
                {
                    filteredRecords = filteredRecords.Where(r => r.SyncStatus == syncStatus.Value);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при фильтрации записей кэша: {ex.Message}");
                return new List<EmotionHistoryRecord>();
            }
            
            return filteredRecords.Take(limit).ToList();
        }
        
        /// <summary>
        /// Получает записи, которые не были синхронизированы с сервером
        /// </summary>
        public List<EmotionHistoryRecord> GetUnsyncedRecords(int limit = 100)
        {
            return GetRecords(syncStatus: SyncStatus.NotSynced, limit: limit);
        }
        
        /// <summary>
        /// Очищает весь кэш
        /// </summary>
        public void ClearCache()
        {
            // Проверяем инициализацию SecurePlayerPrefs
            if (!CheckSecurePrefsInitialized())
            {
                Debug.LogWarning("Невозможно очистить кэш: SecurePlayerPrefs не инициализирован");
                return;
            }
            
            try
            {
                foreach (var recordId in _cacheIndex)
                {
                    string key = GetRecordKey(recordId);
                    if (SecurePlayerPrefs.HasKey(key))
                    {
                        SecurePlayerPrefs.DeleteKey(key);
                    }
                }
                
                _cacheIndex.Clear();
                SaveCacheIndex();
                Debug.Log("Кэш истории эмоций очищен");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при очистке кэша: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Сохраняет настройки синхронизации
        /// </summary>
        public void SaveSyncSettings(EmotionSyncSettings settings)
        {
            if (settings == null) return;
            
            // Проверяем инициализацию SecurePlayerPrefs
            if (!CheckSecurePrefsInitialized())
            {
                Debug.LogWarning("Невозможно сохранить настройки синхронизации: SecurePlayerPrefs не инициализирован");
                return;
            }
            
            try
            {
                _syncSettings = settings;
                _maxCacheSize = settings.MaxCacheRecords;
                
                string json = JsonConvert.SerializeObject(settings);
                SecurePlayerPrefs.SetString(SYNC_SETTINGS_KEY, json);
                Debug.Log("Настройки синхронизации сохранены в кэш");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при сохранении настроек синхронизации: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Получает настройки синхронизации
        /// </summary>
        public EmotionSyncSettings GetSyncSettings()
        {
            return _syncSettings;
        }

        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Генерирует ключ для записи в кэше
        /// </summary>
        private string GetRecordKey(string recordId)
        {
            return $"{CACHE_PREFIX}{recordId}";
        }
        
        /// <summary>
        /// Загружает индекс кэша
        /// </summary>
        private List<string> LoadCacheIndex()
        {
            try
            {
                if (SecurePlayerPrefs.HasKey(CACHE_INDEX_KEY))
                {
                    string json = SecurePlayerPrefs.GetString(CACHE_INDEX_KEY);
                    return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("SecurePlayerPrefs не инициализирован"))
            {
                Debug.LogWarning("SecurePlayerPrefs не инициализирован при загрузке индекса кэша. Возвращается пустой список.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при загрузке индекса кэша: {ex.Message}");
            }
            
            return new List<string>();
        }
        
        /// <summary>
        /// Сохраняет индекс кэша
        /// </summary>
        private void SaveCacheIndex()
        {
            // Проверяем инициализацию SecurePlayerPrefs
            if (!CheckSecurePrefsInitialized())
            {
                Debug.LogWarning("Невозможно сохранить индекс кэша: SecurePlayerPrefs не инициализирован");
                return;
            }
            
            try
            {
                string json = JsonConvert.SerializeObject(_cacheIndex);
                SecurePlayerPrefs.SetString(CACHE_INDEX_KEY, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при сохранении индекса кэша: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Загружает настройки синхронизации
        /// </summary>
        private EmotionSyncSettings LoadSyncSettings()
        {
            try
            {
                // Пробуем получить настройки, если SecurePlayerPrefs не инициализирован, 
                // будет выброшено исключение, и мы вернем настройки по умолчанию
                if (SecurePlayerPrefs.HasKey(SYNC_SETTINGS_KEY))
                {
                    string json = SecurePlayerPrefs.GetString(SYNC_SETTINGS_KEY);
                    return JsonConvert.DeserializeObject<EmotionSyncSettings>(json) ?? CreateDefaultSyncSettings();
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("SecurePlayerPrefs не инициализирован"))
            {
                Debug.LogWarning("SecurePlayerPrefs не инициализирован. Используются настройки по умолчанию.");
                return CreateDefaultSyncSettings();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при загрузке настроек синхронизации: {ex.Message}");
                return CreateDefaultSyncSettings();
            }
            
            return CreateDefaultSyncSettings();
        }
        
        /// <summary>
        /// Создает настройки синхронизации по умолчанию
        /// </summary>
        private EmotionSyncSettings CreateDefaultSyncSettings()
        {
            return new EmotionSyncSettings 
            {
                AutoSync = true,
                SyncIntervalMinutes = 15,
                MaxCacheRecords = DEFAULT_MAX_CACHE_SIZE,
                SyncOnWifiOnly = true
            };
        }
        
        /// <summary>
        /// Очищает самые старые записи для поддержания размера кэша
        /// </summary>
        private void PruneCache()
        {
            try
            {
                if (_cacheIndex.Count <= _maxCacheSize) return;
                
                // Получаем все записи и сортируем по времени (от старых к новым)
                var records = GetAllRecords().OrderBy(r => r.Timestamp).ToList();
                
                // Удаляем самые старые записи
                int recordsToRemove = _cacheIndex.Count - _maxCacheSize;
                for (int i = 0; i < recordsToRemove && i < records.Count; i++)
                {
                    RemoveRecord(records[i].Id);
                }
                
                Debug.Log($"Очищено {recordsToRemove} старых записей из кэша");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при очистке кэша: {ex.Message}");
            }
        }
        
        #endregion
    }
} 