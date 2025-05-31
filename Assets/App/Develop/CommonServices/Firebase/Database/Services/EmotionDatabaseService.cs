using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Develop.CommonServices.Firebase.Common.Cache;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.Utils.Logging;
using Firebase.Database;
using Newtonsoft.Json;
using UnityEngine;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Сервис для работы с эмоциями в Firebase Database
    /// </summary>
    public class EmotionDatabaseService : FirebaseDatabaseServiceBase, IEmotionDatabaseService
    {
        #region Private Fields
        private readonly EmotionHistoryCache _emotionHistoryCache;
        #endregion
        
        #region Constructor
        /// <summary>
        /// Создает новый экземпляр сервиса эмоций
        /// </summary>
        /// <param name="database">Ссылка на базу данных</param>
        /// <param name="cacheManager">Менеджер кэша Firebase</param>
        /// <param name="validationService">Сервис валидации данных</param>
        public EmotionDatabaseService(
            DatabaseReference database,
            FirebaseCacheManager cacheManager,
            DataValidationService validationService = null) 
            : base(database, cacheManager, validationService)
        {
            _emotionHistoryCache = new EmotionHistoryCache(cacheManager);
            MyLogger.Log("✅ EmotionDatabaseService инициализирован", MyLogger.LogCategory.Firebase);
        }
        #endregion

        #region IEmotionDatabaseService Implementation
        
        /// <summary>
        /// Получает все эмоции пользователя
        /// </summary>
        public async Task<Dictionary<string, EmotionData>> GetUserEmotions()
        {
            if (!CheckAuthentication())
            {
                return new Dictionary<string, EmotionData>();
            }

            try
            {
                var emotionsRef = _database.Child("users").Child(_userId).Child("emotions");
                var snapshot = await emotionsRef.GetValueAsync();

                if (!snapshot.Exists)
                {
                    MyLogger.Log($"Эмоции не найдены для пользователя {_userId}", MyLogger.LogCategory.Firebase);
                    return new Dictionary<string, EmotionData>();
                }

                var emotions = new Dictionary<string, EmotionData>();

                foreach (var child in snapshot.Children)
                {
                    try
                    {
                        string type = child.Key;
                        var data = JsonConvert.DeserializeObject<EmotionData>(child.GetRawJsonValue());
                        emotions[type] = data;
                    }
                    catch (Exception ex)
                    {
                        MyLogger.LogError($"Ошибка при чтении эмоции {child.Key}: {ex.Message}", MyLogger.LogCategory.Firebase);
                    }
                }

                MyLogger.Log($"Получено {emotions.Count} эмоций для пользователя {_userId}", MyLogger.LogCategory.Firebase);
                return emotions;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка получения эмоций: {ex.Message}", MyLogger.LogCategory.Firebase);
                return new Dictionary<string, EmotionData>();
            }
        }
        
        /// <summary>
        /// Обновляет эмоции пользователя
        /// </summary>
        public async Task UpdateUserEmotions(Dictionary<string, EmotionData> emotions)
        {
            if (!CheckAuthentication())
            {
                return;
            }

            try
            {
                if (emotions == null || emotions.Count == 0)
                {
                    MyLogger.LogWarning("Пустой словарь эмоций", MyLogger.LogCategory.Firebase);
                    return;
                }
                
                // Используем механизм батчинга для пакетной обработки
                foreach (var kvp in emotions)
                {
                    string path = $"users/{_userId}/emotions/{kvp.Key}";
                    string json = JsonConvert.SerializeObject(kvp.Value);
                    var emotionDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    _batchManager.AddUpdateOperation(path, emotionDict);
                }
                
                // Принудительно выполняем батч
                await _batchManager.ExecuteBatchAsync();
                
                MyLogger.Log($"Обновлено {emotions.Count} эмоций через механизм батчинга", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка обновления эмоций: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Обновляет конкретную эмоцию пользователя
        /// </summary>
        public async Task UpdateUserEmotion(EmotionData emotion)
        {
            if (!CheckAuthentication())
            {
                return;
            }

            try
            {
                if (emotion == null)
                {
                    throw new ArgumentNullException(nameof(emotion));
                }
                
                if (string.IsNullOrEmpty(emotion.Id))
                {
                    emotion.Id = Guid.NewGuid().ToString();
                }
                
                string json = JsonConvert.SerializeObject(emotion);
                var emotionDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                
                // Используем механизм батчинга
                string path = $"users/{_userId}/emotions/{emotion.Id}";
                _batchManager.AddUpdateOperation(path, emotionDict);
                
                // Выполняем батч немедленно, так как это одиночная операция
                await _batchManager.ExecuteBatchAsync();
                
                MyLogger.Log($"Эмоция {emotion.Type} обновлена через механизм батчинга", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка обновления эмоции: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Добавляет запись в историю эмоций
        /// </summary>
        public async Task AddEmotionHistoryRecord(EmotionHistoryRecord record)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("📝 [HISTORY-RECORD] ⚠️ Пользователь не авторизован для добавления записи в историю эмоций. UserId: NULL или пустой", MyLogger.LogCategory.Firebase);
                return;
            }

            if (record == null)
            {
                MyLogger.LogError("📝 [HISTORY-RECORD] ❌ Попытка добавить null запись в историю эмоций", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                string path = $"users/{_userId}/emotionHistory/{record.Id}";
                MyLogger.Log($"📝 [HISTORY-RECORD] ➕ Попытка сохранения записи: Path='{path}', RecordId='{record.Id}', Type='{record.Type}', UserId='{_userId}'", MyLogger.LogCategory.Firebase);
                var userHistoryRef = _database.Child("users").Child(_userId).Child("emotionHistory").Child(record.Id);
                await userHistoryRef.SetValueAsync(record.ToDictionary());
                
                // Кэширование записи
                _emotionHistoryCache.AddOrUpdateRecord(record);
                
                MyLogger.Log($"📝 [HISTORY-RECORD] ✅ Запись успешно сохранена: Path='{path}'", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"📝 [HISTORY-RECORD] ❌ Ошибка сохранения записи истории эмоций: Path='users/{_userId}/emotionHistory/{record.Id}', Error='{ex.Message}'", MyLogger.LogCategory.Firebase);
                MyLogger.LogError($"📝 [HISTORY-RECORD] ❌ Stack trace: {ex.StackTrace}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Добавляет запись в историю эмоций на основе эмоции и события
        /// </summary>
        public async Task AddEmotionHistoryRecord(EmotionData emotion, EmotionEventType eventType)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для добавления записи в историю эмоций", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                if (emotion == null)
                {
                    throw new ArgumentNullException(nameof(emotion), "Эмоция не может быть null");
                }
                
                // Создаем запись
                var record = new EmotionHistoryRecord(emotion, eventType);
                
                // Добавляем запись
                await AddEmotionHistoryRecord(record);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка добавления записи в историю эмоций: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Пакетное добавление записей в историю
        /// </summary>
        public async Task AddEmotionHistoryBatch(List<EmotionHistoryRecord> records)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для добавления записей в историю эмоций", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                if (records == null || records.Count == 0)
                {
                    return;
                }
                
                // Используем механизм батчинга для пакетной обработки
                foreach (var record in records)
                {
                    // Генерируем ID, если его нет
                    if (string.IsNullOrEmpty(record.Id))
                    {
                        record.Id = Guid.NewGuid().ToString();
                    }
                    
                    // Добавляем операцию в батч
                    string path = $"users/{_userId}/emotionHistory/{record.Id}";
                    _batchManager.AddUpdateOperation(path, record.ToDictionary());
                    
                    // Кэширование записи
                    _emotionHistoryCache.AddOrUpdateRecord(record);
                }
                
                // Принудительно выполняем батч
                await _batchManager.ExecuteBatchAsync();
                
                MyLogger.Log($"Добавлено {records.Count} записей в историю эмоций через механизм батчинга", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка пакетного добавления записей в историю эмоций: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Обновляет статусы синхронизации нескольких записей одним батчем
        /// </summary>
        public async Task UpdateEmotionSyncStatusBatch(Dictionary<string, SyncStatus> recordStatusPairs)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для обновления статусов синхронизации", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                if (recordStatusPairs == null || recordStatusPairs.Count == 0)
                {
                    MyLogger.LogWarning("Пустой словарь записей для обновления статусов", MyLogger.LogCategory.Firebase);
                    return;
                }
                
                // Используем механизм батчинга для всех записей
                foreach (var kvp in recordStatusPairs)
                {
                    if (string.IsNullOrEmpty(kvp.Key))
                    {
                        MyLogger.LogWarning("Пропуск записи с пустым ID", MyLogger.LogCategory.Firebase);
                        continue;
                    }
                    
                    string path = $"users/{_userId}/emotionHistory/{kvp.Key}/syncStatus";
                    _batchManager.AddUpdateOperation(path, kvp.Value.ToString());
                }
                
                // Выполняем батч для всех операций
                await _batchManager.ExecuteBatchAsync();
                
                MyLogger.Log($"Обновлены статусы синхронизации для {recordStatusPairs.Count} записей через механизм батчинга", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка пакетного обновления статусов синхронизации: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Удаляет несколько записей из истории одним батчем
        /// </summary>
        public async Task DeleteEmotionHistoryRecordBatch(List<string> recordIds)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для удаления записей", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                if (recordIds == null || recordIds.Count == 0)
                {
                    MyLogger.LogWarning("Пустой список записей для удаления", MyLogger.LogCategory.Firebase);
                    return;
                }
                
                // Используем механизм батчинга для всех записей
                foreach (var recordId in recordIds)
                {
                    if (string.IsNullOrEmpty(recordId))
                    {
                        MyLogger.LogWarning("Пропуск записи с пустым ID", MyLogger.LogCategory.Firebase);
                        continue;
                    }
                    
                    string path = $"users/{_userId}/emotionHistory/{recordId}";
                    _batchManager.AddDeleteOperation(path);
                }
                
                // Выполняем батч для всех операций
                await _batchManager.ExecuteBatchAsync();
                
                MyLogger.Log($"Удалено {recordIds.Count} записей из истории эмоций через механизм батчинга", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка пакетного удаления записей из истории эмоций: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Получает историю эмоций пользователя
        /// </summary>
        public async Task<List<EmotionHistoryRecord>> GetEmotionHistory(DateTime? startDate = null, DateTime? endDate = null, int limit = 100)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("❌ [GetEmotionHistory] Пользователь не авторизован для получения истории эмоций", MyLogger.LogCategory.Firebase);
                return new List<EmotionHistoryRecord>();
            }

            try
            {
                string path = $"users/{_userId}/emotionHistory";
                MyLogger.Log($"🔍 [GetEmotionHistory] Запрашиваем данные по пути: {path}", MyLogger.LogCategory.Firebase);
                
                // Получаем ВСЕ данные без сортировки, фильтрацию делаем локально
                var query = _database.Child("users").Child(_userId).Child("emotionHistory");
                
                MyLogger.Log($"⏳ [GetEmotionHistory] Выполняем запрос к Firebase...", MyLogger.LogCategory.Firebase);
                var snapshot = await query.GetValueAsync();
                
                MyLogger.Log($"📊 [GetEmotionHistory] Ответ от Firebase: Exists={snapshot.Exists}, ChildrenCount={snapshot.ChildrenCount}", MyLogger.LogCategory.Firebase);
                
                var allRecords = new List<EmotionHistoryRecord>();
                
                if (snapshot.Exists && snapshot.ChildrenCount > 0)
                {
                    MyLogger.Log($"📋 [GetEmotionHistory] Обрабатываем {snapshot.ChildrenCount} записей...", MyLogger.LogCategory.Firebase);
                    
                    int processedCount = 0;
                    foreach (var child in snapshot.Children)
                    {
                        try
                        {
                            var record = JsonConvert.DeserializeObject<EmotionHistoryRecord>(child.GetRawJsonValue());
                            if (record != null)
                            {
                                allRecords.Add(record);
                                
                                // Кэшируем запись
                                _emotionHistoryCache.AddOrUpdateRecord(record);
                            }
                            processedCount++;
                        }
                        catch (Exception ex)
                        {
                            MyLogger.LogError($"❌ [GetEmotionHistory] Ошибка при десериализации записи {processedCount + 1}: {ex.Message}", MyLogger.LogCategory.Firebase);
                        }
                    }
                }
                else
                {
                    MyLogger.LogWarning($"⚠️ [GetEmotionHistory] Firebase вернул пустой результат или snapshot не существует", MyLogger.LogCategory.Firebase);
                }
                
                // Локальная фильтрация по датам
                var filteredRecords = allRecords;
                
                if (startDate.HasValue || endDate.HasValue)
                {
                    MyLogger.Log($"🔍 [GetEmotionHistory] Применяем локальную фильтрацию по датам: startDate={startDate?.ToString("O")}, endDate={endDate?.ToString("O")}", MyLogger.LogCategory.Firebase);
                    
                    filteredRecords = allRecords.Where(record =>
                    {
                        if (startDate.HasValue && record.RecordTime < startDate.Value)
                            return false;
                        if (endDate.HasValue && record.RecordTime > endDate.Value)
                            return false;
                        return true;
                    }).ToList();
                    
                    MyLogger.Log($"📊 [GetEmotionHistory] После фильтрации по датам: {filteredRecords.Count} из {allRecords.Count} записей", MyLogger.LogCategory.Firebase);
                }
                
                // Применяем лимит после фильтрации
                var result = filteredRecords.OrderByDescending(r => r.RecordTime).Take(limit).ToList();
                
                MyLogger.Log($"🎯 [GetEmotionHistory] Итого получено {result.Count} записей истории эмоций (после всех фильтров)", MyLogger.LogCategory.Firebase);
                return result;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [GetEmotionHistory] Ошибка получения истории эмоций: {ex.Message}", MyLogger.LogCategory.Firebase);
                MyLogger.LogError($"❌ [GetEmotionHistory] Stack trace: {ex.StackTrace}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Получает несинхронизированные записи истории эмоций
        /// </summary>
        public async Task<List<EmotionHistoryRecord>> GetUnsyncedEmotionHistory(int limit = 50)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для получения несинхронизированных записей", MyLogger.LogCategory.Firebase);
                return new List<EmotionHistoryRecord>();
            }

            try
            {
                var query = _database.Child("users").Child(_userId).Child("emotionHistory")
                    .OrderByChild("syncStatus")
                    .EqualTo(SyncStatus.NotSynced.ToString())
                    .LimitToFirst(limit);
                
                var snapshot = await query.GetValueAsync();
                var result = new List<EmotionHistoryRecord>();
                
                if (snapshot.Exists && snapshot.ChildrenCount > 0)
                {
                    foreach (var child in snapshot.Children)
                    {
                        try
                        {
                            var record = JsonConvert.DeserializeObject<EmotionHistoryRecord>(child.GetRawJsonValue());
                            if (record != null)
                            {
                                result.Add(record);
                                
                                // Кэшируем запись
                                _emotionHistoryCache.AddOrUpdateRecord(record);
                            }
                        }
                        catch (Exception ex)
                        {
                            MyLogger.LogError($"Ошибка при десериализации несинхронизированной записи: {ex.Message}", MyLogger.LogCategory.Firebase);
                        }
                    }
                }
                
                MyLogger.Log($"Получено {result.Count} несинхронизированных записей", MyLogger.LogCategory.Firebase);
                return result;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка получения несинхронизированных записей: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Обновляет статус синхронизации записи истории эмоций
        /// </summary>
        public async Task UpdateEmotionHistoryRecordStatus(string recordId, SyncStatus status)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для обновления статуса синхронизации", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(recordId))
                {
                    throw new ArgumentException("ID записи не может быть пустым", nameof(recordId));
                }
                
                // Используем механизм батчинга
                string path = $"users/{_userId}/emotionHistory/{recordId}/syncStatus";
                _batchManager.AddUpdateOperation(path, status.ToString());
                
                // Выполняем батч немедленно, так как это одиночная операция
                await _batchManager.ExecuteBatchAsync();
                
                MyLogger.Log($"Статус синхронизации записи {recordId} обновлен на {status} через механизм батчинга", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка обновления статуса синхронизации: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Удаляет запись из истории эмоций
        /// </summary>
        public async Task DeleteEmotionHistoryRecord(string recordId)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для удаления записи", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(recordId))
                {
                    throw new ArgumentException("ID записи не может быть пустым", nameof(recordId));
                }
                
                // Используем механизм батчинга
                string path = $"users/{_userId}/emotionHistory/{recordId}";
                _batchManager.AddDeleteOperation(path);
                
                // Выполняем батч немедленно
                await _batchManager.ExecuteBatchAsync();
                
                MyLogger.Log($"Запись {recordId} удалена из истории эмоций через механизм батчинга", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка удаления записи из истории эмоций: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Получает статистику по эмоциям за период
        /// </summary>
        public async Task<Dictionary<string, int>> GetEmotionStatistics(DateTime startDate, DateTime endDate)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для получения статистики эмоций", MyLogger.LogCategory.Firebase);
                return new Dictionary<string, int>();
            }

            try
            {
                var records = await GetEmotionHistory(startDate, endDate, 1000);
                var stats = new Dictionary<string, int>();
                
                foreach (var record in records)
                {
                    if (!string.IsNullOrEmpty(record.Type))
                    {
                        if (stats.ContainsKey(record.Type))
                        {
                            stats[record.Type]++;
                        }
                        else
                        {
                            stats[record.Type] = 1;
                        }
                    }
                }
                
                MyLogger.Log($"Получена статистика эмоций с {startDate} по {endDate}: {stats.Count} типов", MyLogger.LogCategory.Firebase);
                return stats;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка получения статистики эмоций: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Получает настройки синхронизации пользователя
        /// </summary>
        public async Task<EmotionSyncSettings> GetSyncSettings()
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для получения настроек синхронизации", MyLogger.LogCategory.Firebase);
                return null;
            }

            try
            {
                var snapshot = await _database.Child("users").Child(_userId).Child("syncSettings").GetValueAsync();
                
                if (snapshot.Exists)
                {
                    var settings = JsonConvert.DeserializeObject<EmotionSyncSettings>(snapshot.GetRawJsonValue());
                    MyLogger.Log("Настройки синхронизации получены с сервера", MyLogger.LogCategory.Firebase);
                    return settings;
                }
                
                // Если настроек нет, создаем и сохраняем дефолтные
                var defaultSettings = new EmotionSyncSettings();
                await UpdateSyncSettings(defaultSettings);
                
                MyLogger.Log("Созданы и сохранены дефолтные настройки синхронизации", MyLogger.LogCategory.Firebase);
                return defaultSettings;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка получения настроек синхронизации: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Обновляет настройки синхронизации пользователя
        /// </summary>
        public async Task UpdateSyncSettings(EmotionSyncSettings settings)
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для обновления настроек синхронизации", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                if (settings == null)
                {
                    throw new ArgumentNullException(nameof(settings), "Настройки не могут быть null");
                }
                
                // Сериализуем настройки в словарь
                var json = JsonConvert.SerializeObject(settings);
                var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                
                // Сохраняем настройки в Firebase
                await _database.Child("users").Child(_userId).Child("syncSettings").UpdateChildrenAsync(dictionary);
                
                MyLogger.Log("Настройки синхронизации обновлены", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка обновления настроек синхронизации: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Очищает историю эмоций пользователя
        /// </summary>
        public async Task ClearEmotionHistory()
        {
            if (!CheckAuthentication())
            {
                MyLogger.LogWarning("Пользователь не авторизован для очистки истории эмоций", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                // Используем механизм батчинга
                string path = $"users/{_userId}/emotionHistory";
                _batchManager.AddDeleteOperation(path);
                
                // Выполняем батч немедленно
                await _batchManager.ExecuteBatchAsync();
                
                MyLogger.Log("История эмоций очищена через механизм батчинга", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка очистки истории эмоций: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        #endregion
    }
} 