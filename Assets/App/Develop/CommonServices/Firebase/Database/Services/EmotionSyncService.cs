using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.CommonServices.DataManagement;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.Networking;
using Newtonsoft.Json;
using Firebase.Database;
using UnityEngine;
using App.Develop.CommonServices.Firebase.Common.SecureStorage;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    public class EmotionSyncService : MonoBehaviour
    {
        #region Dependencies
        private IDatabaseService _databaseService;
        private EmotionHistoryCache _cache;
        private ConnectivityManager _connectivityManager;
        #endregion
        
        #region Private fields
        private EmotionSyncSettings _syncSettings;
        private bool _isSyncing;
        private DateTime _lastSyncAttempt;
        private bool _isInitialized;
        #endregion
        
        #region Events
        public event Action<bool, string> OnSyncComplete;
        public event Action<float> OnSyncProgress;
        public event Action<EmotionHistoryRecord> OnRecordSynced;
        public event Action<EmotionHistoryRecord> OnSyncConflict;
        #endregion
        
        #region Unity Lifecycle
        private void OnEnable()
        {
            if (_isInitialized)
            {
                StartSync();
            }
        }

        private void OnDisable()
        {
            // Если выключаемся, сохраняем настройки перед выходом
            SaveSyncSettings();
        }

        private void Update()
        {
            if (!_isInitialized || _isSyncing) return;
            
            // Проверка необходимости автоматической синхронизации
            if (_syncSettings.AutoSync && 
                DateTime.Now - _lastSyncAttempt > _syncSettings.SyncInterval)
            {
                // Проверка подключения к сети
                if (_connectivityManager != null && 
                    (!_syncSettings.SyncOnWifiOnly || _connectivityManager.IsWifiConnected))
                {
                    StartSync();
                }
                else
                {
                    _lastSyncAttempt = DateTime.Now;
                }
            }
        }
        #endregion
        
        #region Initialization
        public void Initialize(
            IDatabaseService databaseService,
            EmotionHistoryCache cache,
            ConnectivityManager connectivityManager)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _connectivityManager = connectivityManager;
            
            _syncSettings = _cache.GetSyncSettings() ?? new EmotionSyncSettings();
            _lastSyncAttempt = DateTime.Now;
            _isInitialized = true;
            
            MyLogger.Log("EmotionSyncService инициализирован", MyLogger.LogCategory.Firebase);
        }
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Запускает синхронизацию записей с сервером
        /// </summary>
        public async void StartSync()
        {
            if (!_isInitialized)
            {
                MyLogger.LogError("EmotionSyncService не инициализирован", MyLogger.LogCategory.Firebase);
                return;
            }
            
            if (_isSyncing)
            {
                MyLogger.LogWarning("Синхронизация уже выполняется", MyLogger.LogCategory.Firebase);
                return;
            }
            
            _isSyncing = true;
            _lastSyncAttempt = DateTime.Now;
            
            bool success = false;
            string message = "";
            
            try
            {
                // Проверка соединения с сервером
                if (_databaseService == null || !_databaseService.IsAuthenticated)
                {
                    throw new InvalidOperationException("Пользователь не авторизован");
                }
                
                MyLogger.Log("Начинаем синхронизацию данных с сервером...", MyLogger.LogCategory.Firebase);
                OnSyncProgress?.Invoke(0f);
                
                // Загружаем настройки синхронизации с сервера
                var serverSettings = await _databaseService.GetSyncSettings();
                if (serverSettings != null)
                {
                    _syncSettings = serverSettings;
                    _cache.SaveSyncSettings(_syncSettings);
                }
                
                // Получаем несинхронизированные записи из кэша
                var unsyncedRecords = _cache.GetUnsyncedRecords(_syncSettings.MaxRecordsPerSync);
                int totalRecords = unsyncedRecords.Count;
                
                MyLogger.Log($"Найдено {totalRecords} несинхронизированных записей", MyLogger.LogCategory.Firebase);
                
                if (totalRecords == 0)
                {
                    // Если нет записей для синхронизации, проверяем новые записи с сервера
                    await SyncFromServer();
                    success = true;
                    message = "Синхронизация выполнена успешно";
                    return;
                }
                
                // Используем батчинг для отправки записей на сервер
                int batchSize = 20; // Оптимальный размер для Firebase
                int processedCount = 0;
                
                for (int i = 0; i < unsyncedRecords.Count; i += batchSize)
                {
                    // Разбиваем записи на группы по batchSize
                    var batch = unsyncedRecords.Skip(i).Take(batchSize).ToList();
                    
                    // Отправляем группу записей
                    try
                    {
                        // Обновляем статус на "Синхронизируется"
                        foreach (var record in batch)
                        {
                            record.SyncStatus = SyncStatus.Syncing;
                            _cache.UpdateRecord(record);
                        }
                        
                        // Отправляем группу записей на сервер с использованием BatchManager
                        await _databaseService.AddEmotionHistoryBatch(batch);
                        
                        // Создаем словарь обновлений статусов
                        var statusUpdates = new Dictionary<string, SyncStatus>();
                        foreach (var record in batch)
                        {
                            statusUpdates[record.Id] = SyncStatus.Synced;
                            processedCount++;
                            OnRecordSynced?.Invoke(record);
                        }
                        
                        // Обновляем статусы в Firebase одним батчем
                        await _databaseService.UpdateEmotionSyncStatusBatch(statusUpdates);
                        
                        // Обновляем статусы в локальном кэше
                        foreach (var record in batch)
                        {
                            record.SyncStatus = SyncStatus.Synced;
                            _cache.UpdateRecord(record);
                        }
                        
                        // Обновляем прогресс
                        float progress = totalRecords > 0 ? (float)processedCount / totalRecords : 1f;
                        OnSyncProgress?.Invoke(progress);
                    }
                    catch (Exception ex)
                    {
                        MyLogger.LogError($"Ошибка пакетной синхронизации: {ex.Message}", MyLogger.LogCategory.Firebase);
                        
                        // Отмечаем все записи в партии как непрошедшие синхронизацию
                        foreach (var record in batch)
                        {
                            record.SyncStatus = SyncStatus.SyncFailed;
                            _cache.UpdateRecord(record);
                        }
                    }
                }
                
                // Получаем данные с сервера
                await SyncFromServer();
                
                // Обновляем время последней синхронизации
                _syncSettings.LastSyncTime = DateTime.Now;
                await _databaseService.UpdateSyncSettings(_syncSettings);
                _cache.SaveSyncSettings(_syncSettings);
                
                success = true;
                message = $"Синхронизировано {processedCount} из {totalRecords} записей";
            }
            catch (Exception ex)
            {
                success = false;
                message = $"Ошибка синхронизации: {ex.Message}";
                MyLogger.LogError(message, MyLogger.LogCategory.Firebase);
            }
            finally
            {
                _isSyncing = false;
                OnSyncProgress?.Invoke(1f); // Завершающий прогресс 100%
                OnSyncComplete?.Invoke(success, message);
                MyLogger.Log(message, MyLogger.LogCategory.Firebase);
            }
        }
        
        /// <summary>
        /// Создает резервную копию данных
        /// </summary>
        public async Task<string> CreateBackup()
        {
            try
            {
                if (!_isInitialized || _databaseService == null || !_databaseService.IsAuthenticated)
                {
                    throw new InvalidOperationException("Сервис не инициализирован или пользователь не авторизован");
                }
                
                string backupId = await _databaseService.CreateBackup();
                _syncSettings.LastBackupTime = DateTime.Now;
                await _databaseService.UpdateSyncSettings(_syncSettings);
                _cache.SaveSyncSettings(_syncSettings);
                
                MyLogger.Log($"Резервная копия создана: {backupId}", MyLogger.LogCategory.Firebase);
                return backupId;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка создания резервной копии: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Восстанавливает данные из резервной копии
        /// </summary>
        public async Task RestoreFromBackup(string backupId)
        {
            try
            {
                if (!_isInitialized || _databaseService == null || !_databaseService.IsAuthenticated)
                {
                    throw new InvalidOperationException("Сервис не инициализирован или пользователь не авторизован");
                }
                
                await _databaseService.RestoreFromBackup(backupId);
                
                // Очищаем локальный кэш и загружаем данные с сервера
                _cache.ClearCache();
                await SyncFromServer();
                
                MyLogger.Log($"Данные восстановлены из резервной копии: {backupId}", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка восстановления из резервной копии: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Обновляет настройки синхронизации
        /// </summary>
        public async Task UpdateSyncSettings(EmotionSyncSettings settings)
        {
            if (settings == null) return;
            
            try
            {
                _syncSettings = settings;
                _cache.SaveSyncSettings(settings);
                
                if (_databaseService != null && _databaseService.IsAuthenticated)
                {
                    await _databaseService.UpdateSyncSettings(settings);
                }
                
                MyLogger.Log("Настройки синхронизации обновлены", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка обновления настроек синхронизации: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Форсирует создание резервной копии, если прошло достаточно времени
        /// </summary>
        public async Task CheckAndCreateBackup()
        {
            if (!_isInitialized || !_syncSettings.BackupEnabled) return;
            
            try
            {
                if (_syncSettings.LastBackupTime == DateTime.MinValue || 
                    DateTime.Now - _syncSettings.LastBackupTime > _syncSettings.BackupInterval)
                {
                    await CreateBackup();
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при проверке резервного копирования: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Синхронизирует данные с сервера
        /// </summary>
        private async Task SyncFromServer()
        {
            try
            {
                MyLogger.Log("Получение данных с сервера...", MyLogger.LogCategory.Firebase);
                
                DateTime? lastSyncTime = _syncSettings.LastSyncTime != DateTime.MinValue ? 
                    _syncSettings.LastSyncTime : null;
                
                // Получаем записи с сервера, которые появились после последней синхронизации
                var serverRecords = await _databaseService.GetEmotionHistory(lastSyncTime, null, _syncSettings.MaxRecordsPerSync);
                
                MyLogger.Log($"Получено {serverRecords.Count} записей с сервера", MyLogger.LogCategory.Firebase);
                
                // Группируем записи по типу обработки
                var newRecords = new List<EmotionHistoryRecord>();
                var updateRecords = new List<EmotionHistoryRecord>();
                var conflictRecords = new List<Tuple<EmotionHistoryRecord, EmotionHistoryRecord>>();
                
                // Классифицируем записи
                foreach (var serverRecord in serverRecords)
                {
                    var localRecord = _cache.GetRecord(serverRecord.Id);
                    
                    if (localRecord == null)
                    {
                        // Новая запись
                        serverRecord.SyncStatus = SyncStatus.Synced;
                        newRecords.Add(serverRecord);
                    }
                    else if (localRecord.SyncStatus == SyncStatus.NotSynced || 
                             localRecord.SyncStatus == SyncStatus.SyncFailed)
                    {
                        // Конфликт
                        conflictRecords.Add(new Tuple<EmotionHistoryRecord, EmotionHistoryRecord>(localRecord, serverRecord));
                    }
                    else
                    {
                        // Обновление существующей записи
                        serverRecord.SyncStatus = SyncStatus.Synced;
                        updateRecords.Add(serverRecord);
                    }
                }
                
                // Обрабатываем новые записи пакетно
                if (newRecords.Count > 0)
                {
                    // Добавляем все новые записи в кэш
                    foreach (var record in newRecords)
                    {
                        _cache.AddRecord(record);
                    }
                    MyLogger.Log($"Добавлено {newRecords.Count} новых записей в кэш", MyLogger.LogCategory.Firebase);
                }
                
                // Обрабатываем обновления пакетно
                if (updateRecords.Count > 0)
                {
                    // Обновляем все записи в кэше
                    foreach (var record in updateRecords)
                    {
                        _cache.UpdateRecord(record);
                    }
                    MyLogger.Log($"Обновлено {updateRecords.Count} записей в кэше", MyLogger.LogCategory.Firebase);
                }
                
                // Обрабатываем конфликты индивидуально, так как требуется особая логика
                if (conflictRecords.Count > 0)
                {
                    foreach (var pair in conflictRecords)
                    {
                        // Разрешаем конфликт согласно стратегии
                        ResolveSyncConflict(pair.Item1, pair.Item2);
                    }
                    MyLogger.Log($"Разрешено {conflictRecords.Count} конфликтов", MyLogger.LogCategory.Firebase);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при синхронизации с сервера: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }
        
        /// <summary>
        /// Разрешает конфликт синхронизации между локальной и серверной записью
        /// </summary>
        private void ResolveSyncConflict(EmotionHistoryRecord localRecord, EmotionHistoryRecord serverRecord)
        {
            // Отмечаем конфликт для уведомления
            localRecord.SyncStatus = SyncStatus.Conflict;
            OnSyncConflict?.Invoke(localRecord);
            
            switch (_syncSettings.ConflictStrategy)
            {
                case ConflictResolutionStrategy.ServerWins:
                    // Используем серверную запись
                    serverRecord.SyncStatus = SyncStatus.Synced;
                    _cache.UpdateRecord(serverRecord);
                    break;
                
                case ConflictResolutionStrategy.ClientWins:
                    // Оставляем локальную запись, она будет синхронизирована при следующей попытке
                    localRecord.SyncStatus = SyncStatus.NotSynced;
                    _cache.UpdateRecord(localRecord);
                    break;
                
                case ConflictResolutionStrategy.MostRecent:
                    // Используем самую свежую запись
                    if (serverRecord.Timestamp > localRecord.Timestamp)
                    {
                        serverRecord.SyncStatus = SyncStatus.Synced;
                        _cache.UpdateRecord(serverRecord);
                    }
                    else
                    {
                        localRecord.SyncStatus = SyncStatus.NotSynced;
                        _cache.UpdateRecord(localRecord);
                    }
                    break;
                
                case ConflictResolutionStrategy.KeepBoth:
                    // Сохраняем обе записи, генерируем новый ID для локальной
                    serverRecord.SyncStatus = SyncStatus.Synced;
                    _cache.UpdateRecord(serverRecord);
                    
                    localRecord.Id = Guid.NewGuid().ToString();
                    localRecord.SyncStatus = SyncStatus.NotSynced;
                    _cache.AddRecord(localRecord);
                    break;
                
                case ConflictResolutionStrategy.AskUser:
                    // Оставляем статус конфликта, решение примет пользователь
                    _cache.UpdateRecord(localRecord);
                    break;
            }
        }
        
        /// <summary>
        /// Сохраняет настройки синхронизации
        /// </summary>
        private void SaveSyncSettings()
        {
            try
            {
                _cache.SaveSyncSettings(_syncSettings);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при сохранении настроек синхронизации: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }
        #endregion
    }
} 