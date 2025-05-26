using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.CommonServices.DataManagement;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion;
using Newtonsoft.Json;
using UnityEngine;
using App.Develop.CommonServices.Firebase.Common.SecureStorage;
using App.Develop.CommonServices.Firebase.Common.Cache;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Firebase.Database.Services
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
        private readonly FirebaseCacheManager _cacheManager;
        
        /// <summary>
        /// Создает новый экземпляр кэша истории эмоций
        /// </summary>
        public EmotionHistoryCache()
        {
            try
            {
                _cacheIndex = LoadCacheIndex();
                _syncSettings = LoadSyncSettings();
                _maxCacheSize = _syncSettings?.MaxCacheRecords ?? DEFAULT_MAX_CACHE_SIZE;
                
                MyLogger.Log($"EmotionHistoryCache инициализирован. В кэше {_cacheIndex.Count} записей.", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                // В случае ошибки создаем пустые объекты с значениями по умолчанию
                MyLogger.LogError($"Ошибка при инициализации EmotionHistoryCache: {ex.Message}", MyLogger.LogCategory.Firebase);
                _cacheIndex = new List<string>();
                _syncSettings = CreateDefaultSyncSettings();
                _maxCacheSize = DEFAULT_MAX_CACHE_SIZE;
            }
        }
        
        /// <summary>
        /// Создает новый экземпляр кэша истории эмоций с менеджером кэша
        /// </summary>
        /// <param name="cacheManager">Менеджер кэша Firebase</param>
        public EmotionHistoryCache(FirebaseCacheManager cacheManager)
        {
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            
            try
            {
                _cacheIndex = LoadCacheIndex();
                _syncSettings = LoadSyncSettings();
                _maxCacheSize = _syncSettings?.MaxCacheRecords ?? DEFAULT_MAX_CACHE_SIZE;
                
                MyLogger.Log($"EmotionHistoryCache инициализирован с менеджером кэша. В кэше {_cacheIndex.Count} записей.", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                // В случае ошибки создаем пустые объекты с значениями по умолчанию
                MyLogger.LogError($"Ошибка при инициализации EmotionHistoryCache: {ex.Message}", MyLogger.LogCategory.Firebase);
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
                MyLogger.LogWarning($"Ошибка при проверке инициализации SecurePlayerPrefs: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }
        
        /// <summary>
        /// Добавляет или обновляет запись в кэше
        /// </summary>
        /// <param name="record">Запись для добавления или обновления</param>
        public void AddOrUpdateRecord(EmotionHistoryRecord record)
        {
            if (record == null) return;
            
            if (string.IsNullOrEmpty(record.Id))
            {
                MyLogger.LogError("Невозможно добавить/обновить запись с пустым ID", MyLogger.LogCategory.Firebase);
                return;
            }
            
            // Проверяем инициализацию SecurePlayerPrefs
            if (!CheckSecurePrefsInitialized())
            {
                MyLogger.LogWarning($"Невозможно добавить/обновить запись {record.Id} в кэш: SecurePlayerPrefs не инициализирован", MyLogger.LogCategory.Firebase);
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
                    
                    MyLogger.Log($"Запись {record.Id} добавлена в кэш. Всего записей: {_cacheIndex.Count}", MyLogger.LogCategory.Firebase);
                }
                // else
                // {
                //     MyLogger.Log($"Запись {record.Id} обновлена в кэше.", MyLogger.LogCategory.Firebase);
                // }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при добавлении/обновлении записи в кэш: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }
        
        /// <summary>
        /// Добавляет запись в кэш
        /// </summary>
        public void AddRecord(EmotionHistoryRecord record)
        {
            AddOrUpdateRecord(record);
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
                MyLogger.LogWarning($"Невозможно получить запись {recordId} из кэша: SecurePlayerPrefs не инициализирован", MyLogger.LogCategory.Firebase);
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
                MyLogger.LogError($"Ошибка при получении записи из кэша: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
            
            return null;
        }
        
        /// <summary>
        /// Обновляет запись в кэше
        /// </summary>
        public void UpdateRecord(EmotionHistoryRecord record)
        {
            AddOrUpdateRecord(record); // Просто перезаписываем
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
                MyLogger.LogWarning($"Невозможно удалить запись {recordId} из кэша: SecurePlayerPrefs не инициализирован", MyLogger.LogCategory.Firebase);
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
                    MyLogger.Log($"Запись {recordId} удалена из кэша. Осталось записей: {_cacheIndex.Count}", MyLogger.LogCategory.Firebase);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при удалении записи из кэша: {ex.Message}", MyLogger.LogCategory.Firebase);
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
                // Пытаемся получить записи из кэша
                foreach (var index in _cacheIndex)
                {
                    var record = GetRecord(index);
                    if (record != null)
                    {
                        records.Add(record);
                    }
                }
                
                MyLogger.Log($"Получено {records.Count} записей из кэша", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при получении всех записей из кэша: {ex.Message}", MyLogger.LogCategory.Firebase);
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
                MyLogger.LogError($"Ошибка при фильтрации записей кэша: {ex.Message}", MyLogger.LogCategory.Firebase);
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
                MyLogger.LogWarning("Невозможно очистить кэш: SecurePlayerPrefs не инициализирован", MyLogger.LogCategory.Firebase);
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
                MyLogger.Log("Кэш истории эмоций очищен", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при очистке кэша: {ex.Message}", MyLogger.LogCategory.Firebase);
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
                MyLogger.LogWarning("Невозможно сохранить настройки синхронизации: SecurePlayerPrefs не инициализирован", MyLogger.LogCategory.Firebase);
                return;
            }
            
            try
            {
                _syncSettings = settings;
                _maxCacheSize = settings.MaxCacheRecords;
                
                string json = JsonConvert.SerializeObject(settings);
                SecurePlayerPrefs.SetString(SYNC_SETTINGS_KEY, json);
                MyLogger.Log("Настройки синхронизации сохранены в кэш", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при сохранении настроек синхронизации: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }
        
        /// <summary>
        /// Получает настройки синхронизации
        /// </summary>
        public EmotionSyncSettings GetSyncSettings()
        {
            return _syncSettings;
        }

        /// <summary>
        /// Принудительно обновляет кэш из Firebase (мягкое обновление - сохраняет локальные записи)
        /// </summary>
        public async Task<bool> RefreshFromFirebase(IDatabaseService databaseService)
        {
            if (databaseService == null || !databaseService.IsAuthenticated)
            {
                MyLogger.LogWarning("Невозможно обновить кэш: DatabaseService не доступен или пользователь не авторизован", MyLogger.LogCategory.Firebase);
                return false;
            }

            try
            {
                MyLogger.Log("Обновление кэша истории эмоций из Firebase (мягкое обновление, MyLogger.LogCategory.Firebase)...");
                
                // Получаем историю из Firebase
                var firebaseRecords = await databaseService.GetEmotionHistory(null, null, 1000);
                
                if (firebaseRecords != null && firebaseRecords.Count > 0)
                {
                    // Важно - НЕ очищаем текущий кэш перед обновлением, чтобы сохранить локальные записи
                    // ClearCache();
                    
                    // Сохраняем текущий список ID, чтобы определить новые записи
                    var previousIds = new HashSet<string>(_cacheIndex);
                    int newRecordsCount = 0;
                    int updatedRecordsCount = 0;
                    
                    // Добавляем записи из Firebase
                    foreach (var record in firebaseRecords)
                    {
                        if (string.IsNullOrEmpty(record.Id))
                        {
                            MyLogger.LogWarning($"Пропускаем запись без ID из Firebase", MyLogger.LogCategory.Firebase);
                            continue;
                        }
                        
                        if (previousIds.Contains(record.Id))
                        {
                            // Существующая запись - обновляем
                            UpdateRecord(record);
                            updatedRecordsCount++;
                        }
                        else
                        {
                            // Новая запись - добавляем
                            AddRecord(record);
                            newRecordsCount++;
                        }
                    }
                    
                    MyLogger.Log($"Кэш обновлен (мягко, MyLogger.LogCategory.Firebase). Загружено {firebaseRecords.Count} записей из Firebase ({newRecordsCount} новых, {updatedRecordsCount} обновлено)");
                    return true;
                }
                else
                {
                    MyLogger.LogWarning("Не найдено записей в Firebase", MyLogger.LogCategory.Firebase);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при обновлении кэша из Firebase: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        /// <summary>
        /// Принудительно заменяет весь кэш данными из Firebase (жесткое обновление)
        /// </summary>
        public async Task<bool> ReplaceFromFirebase(IDatabaseService databaseService)
        {
            if (databaseService == null || !databaseService.IsAuthenticated)
            {
                MyLogger.LogWarning("Невозможно заменить кэш: DatabaseService не доступен или пользователь не авторизован", MyLogger.LogCategory.Firebase);
                return false;
            }

            try
            {
                MyLogger.Log("🔄 [ReplaceFromFirebase] Полная замена кэша истории эмоций данными из Firebase...", MyLogger.LogCategory.Firebase);
                
                // Получаем историю из Firebase
                MyLogger.Log("📡 [ReplaceFromFirebase] Запрашиваем историю из Firebase...", MyLogger.LogCategory.Firebase);
                var firebaseRecords = await databaseService.GetEmotionHistory(null, null, 1000);
                
                if (firebaseRecords != null)
                {
                    MyLogger.Log($"📥 [ReplaceFromFirebase] Получено {firebaseRecords.Count} записей из Firebase", MyLogger.LogCategory.Firebase);
                    
                    // Логируем первые несколько записей для диагностики
                    for (int i = 0; i < Math.Min(3, firebaseRecords.Count); i++)
                    {
                        var record = firebaseRecords[i];
                        MyLogger.Log($"📋 [ReplaceFromFirebase] Запись {i + 1}: Id={record.Id}, Type={record.Type}, Timestamp={record.RecordTime:O}", MyLogger.LogCategory.Firebase);
                    }
                    
                    // ВАЖНО: Полностью очищаем локальный кэш перед загрузкой
                    MyLogger.Log("🗑️ [ReplaceFromFirebase] Очищаем локальный кэш...", MyLogger.LogCategory.Firebase);
                    ClearCache();
                    
                    int addedCount = 0;
                    
                    // Добавляем все записи из Firebase
                    foreach (var record in firebaseRecords)
                    {
                        if (string.IsNullOrEmpty(record.Id))
                        {
                            MyLogger.LogWarning($"⚠️ [ReplaceFromFirebase] Пропускаем запись без ID из Firebase", MyLogger.LogCategory.Firebase);
                            continue;
                        }
                        
                        MyLogger.Log($"➕ [ReplaceFromFirebase] Добавляем запись в кэш: Id={record.Id}, Type={record.Type}", MyLogger.LogCategory.Firebase);
                        AddRecord(record);
                        addedCount++;
                    }
                    
                    MyLogger.Log($"✅ [ReplaceFromFirebase] Кэш полностью заменен. Добавлено {addedCount} записей из Firebase", MyLogger.LogCategory.Firebase);
                    
                    // Проверяем, что записи действительно добавились
                    var cacheRecordsAfter = GetAllRecords();
                    MyLogger.Log($"🔍 [ReplaceFromFirebase] Проверка: в кэше теперь {cacheRecordsAfter.Count} записей", MyLogger.LogCategory.Firebase);
                    
                    return true;
                }
                else
                {
                    MyLogger.LogWarning("⚠️ [ReplaceFromFirebase] Firebase вернул NULL. Кэш очищен, но не заполнен.", MyLogger.LogCategory.Firebase);
                    ClearCache(); // Все равно очищаем кэш, даже если в Firebase нет данных
                    return true; // Возвращаем true, так как операция технически выполнена
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при замене кэша данными из Firebase: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
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
                MyLogger.LogWarning("SecurePlayerPrefs не инициализирован при загрузке индекса кэша. Возвращается пустой список.", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при загрузке индекса кэша: {ex.Message}", MyLogger.LogCategory.Firebase);
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
                MyLogger.LogWarning("Невозможно сохранить индекс кэша: SecurePlayerPrefs не инициализирован", MyLogger.LogCategory.Firebase);
                return;
            }
            
            try
            {
                string json = JsonConvert.SerializeObject(_cacheIndex);
                SecurePlayerPrefs.SetString(CACHE_INDEX_KEY, json);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при сохранении индекса кэша: {ex.Message}", MyLogger.LogCategory.Firebase);
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
                MyLogger.LogWarning("SecurePlayerPrefs не инициализирован. Используются настройки по умолчанию.", MyLogger.LogCategory.Firebase);
                return CreateDefaultSyncSettings();
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при загрузке настроек синхронизации: {ex.Message}", MyLogger.LogCategory.Firebase);
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
                AutoSync = false, // Отключаем автосинхронизацию, теперь синхронизация по требованию
                SyncIntervalMinutes = 15, // Возвращаем разумный интервал
                MaxCacheRecords = DEFAULT_MAX_CACHE_SIZE,
                SyncOnWifiOnly = false
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
                
                MyLogger.Log($"Очищено {recordsToRemove} старых записей из кэша", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при очистке кэша: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }
        
        #endregion
    }
} 