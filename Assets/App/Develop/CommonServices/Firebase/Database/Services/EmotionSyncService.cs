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
        private SyncStatusData _syncStatus;
        private bool _isAutomaticSyncEnabled = true; // Флаг для управления автоматической синхронизацией
        #endregion
        
        #region Events
        public event Action<bool, string> OnSyncComplete;
        public event Action<float> OnSyncProgress;
        public event Action<EmotionHistoryRecord> OnRecordSynced;
        public event Action<EmotionHistoryRecord> OnSyncConflict;
        public event Action<bool, string> OnClearComplete; // Событие о завершении очистки
        #endregion
        
        #region Unity Lifecycle
        private void OnEnable()
        {
            if (_isInitialized && _isAutomaticSyncEnabled)
            {
                // При включении компонента проверяем, нужно ли загрузить данные
                CheckAndLoadFromCloud();
            }
        }

        private void OnDisable()
        {
            // Если выключаемся, сохраняем настройки перед выходом
            SaveSyncSettings();
            
            // При выключении синхронизируем данные с облаком, если включена автоматическая синхронизация
            if (_isAutomaticSyncEnabled)
            {
                SyncToCloudBeforeShutdown();
            }
        }
        
        private void OnApplicationQuit()
        {
            // Синхронизируем данные с облаком при закрытии приложения, если включена автоматическая синхронизация
            if (_isAutomaticSyncEnabled)
            {
                SyncToCloudBeforeShutdown();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!_isInitialized || !_isAutomaticSyncEnabled) return;
            
            if (!hasFocus)
            {
                // Приложение теряет фокус - синхронизируем с облаком
                SyncToCloud();
            }
            else
            {
                // Приложение получает фокус - проверяем, нужно ли загрузить данные
                CheckAndLoadFromCloud();
            }
        }

        private void Update()
        {
            if (!_isInitialized || _isSyncing || !_isAutomaticSyncEnabled) return;
            
            // Проверка необходимости автоматической синхронизации
            if (_syncSettings.AutoSync && 
                DateTime.Now - _lastSyncAttempt > _syncSettings.SyncInterval)
            {
                // Проверка подключения к сети
                if (_connectivityManager != null && 
                    (!_syncSettings.SyncOnWifiOnly || _connectivityManager.IsWifiConnected))
                {
                    SyncToCloud();
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
            
            // Инициализируем статус синхронизации
            _syncStatus = new SyncStatusData
            {
                IsLastSyncSuccessful = true, // По умолчанию считаем, что предыдущая синхронизация успешна
                LastSyncTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                SyncErrorMessage = string.Empty
            };
            
            _isInitialized = true;
            
            // MyLogger.Log("EmotionSyncService инициализирован", MyLogger.LogCategory.Firebase);
            
            // При инициализации проверяем, нужно ли загрузить данные, если включена автоматическая синхронизация
            if (_isAutomaticSyncEnabled)
            {
                CheckAndLoadFromCloud();
            }
        }
        
        /// <summary>
        /// Включает или отключает автоматическую синхронизацию при событиях жизненного цикла
        /// </summary>
        public void SetAutomaticSyncEnabled(bool enabled)
        {
            _isAutomaticSyncEnabled = enabled;
            // MyLogger.Log($"Автоматическая синхронизация {(enabled ? "включена" : "отключена")}", MyLogger.LogCategory.Firebase);
        }
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Запускает синхронизацию записей с сервером (публичный метод для внешнего вызова)
        /// </summary>
        public void StartSync()
        {
            // Этот метод используется внешними классами для явного запуска синхронизации
            // Просто делегируем выполнение внутреннему методу SyncToCloud
            SyncToCloud();
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
        
        /// <summary>
        /// Получает статус последней синхронизации
        /// </summary>
        public SyncStatusData GetSyncStatus()
        {
            return _syncStatus;
        }
        
        /// <summary>
        /// Загружает данные из облака
        /// </summary>
        public async Task<bool> LoadDataFromCloud()
        {
            if (!_isInitialized || _isSyncing)
                return false;
            
            if (_databaseService == null || !_databaseService.IsAuthenticated)
            {
                MyLogger.LogWarning("⚠️ Невозможно загрузить данные из облака: Firebase не инициализирован или пользователь не аутентифицирован", 
                    MyLogger.LogCategory.Sync);
                return false;
            }
            
            try
            {
                MyLogger.Log("🔄 Загрузка данных из облака...", MyLogger.LogCategory.Sync);
                
                // Загружаем данные из Firebase
                await SyncFromServer();
                
                MyLogger.Log("✅ Данные успешно загружены из облака", MyLogger.LogCategory.Sync);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при загрузке данных из облака: {ex.Message}", MyLogger.LogCategory.Sync);
                UpdateSyncStatus(false, ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Очищает все данные эмоций в облаке
        /// </summary>
        /// <returns>True, если данные успешно очищены, иначе False</returns>
        public async Task<bool> ClearCloudData()
        {
            try
            {
                MyLogger.Log("🗑️ [ClearCloudData] Метод вызван", MyLogger.LogCategory.ClearHistory);
                
                // Детальная проверка состояния всех зависимостей
                MyLogger.Log($"🔍 [ClearCloudData] Проверка состояния: _isInitialized={_isInitialized}, _databaseService!=null={_databaseService != null}, IsAuthenticated={_databaseService?.IsAuthenticated}, UserId={_databaseService?.UserId}", MyLogger.LogCategory.ClearHistory);
                
                // Проверяем инициализацию сервиса
                if (!_isInitialized)
                {
                    MyLogger.LogError("❌ [ClearCloudData] EmotionSyncService не инициализирован для очистки данных", MyLogger.LogCategory.ClearHistory);
                    return false;
                }
                
                // Проверяем наличие DatabaseService
                if (_databaseService == null)
                {
                    MyLogger.LogError("❌ [ClearCloudData] DatabaseService равен null", MyLogger.LogCategory.ClearHistory);
                    return false;
                }
                
                // Проверяем аутентификацию
                if (!_databaseService.IsAuthenticated)
                {
                    MyLogger.LogWarning("⚠️ [ClearCloudData] Невозможно очистить данные в облаке: пользователь не аутентифицирован", MyLogger.LogCategory.ClearHistory);
                    return false;
                }
                
                // Проверяем UserId
                if (string.IsNullOrEmpty(_databaseService.UserId))
                {
                    MyLogger.LogError("❌ [ClearCloudData] UserId пустой или null", MyLogger.LogCategory.ClearHistory);
                    return false;
                }
                
                // Выполняем очистку
                MyLogger.Log("🗑️ [ClearCloudData] Начинаем очистку данных в облаке...", MyLogger.LogCategory.ClearHistory);
                
                await _databaseService.ClearEmotionHistory();
                
                MyLogger.Log("✅ [ClearCloudData] Данные успешно очищены в облаке", MyLogger.LogCategory.ClearHistory);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [ClearCloudData] Ошибка при очистке данных в облаке: {ex.Message}", MyLogger.LogCategory.ClearHistory);
                MyLogger.LogError($"❌ [ClearCloudData] StackTrace: {ex.StackTrace}", MyLogger.LogCategory.ClearHistory);
                return false;
            }
        }
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Внутренний метод для синхронизации данных с сервером
        /// </summary>
        private async void SyncToCloud()
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
                    
                    // Обновляем статус синхронизации
                    UpdateSyncStatus(true);
                    
                    _isSyncing = false;
                    OnSyncProgress?.Invoke(1f); // Завершающий прогресс 100%
                    OnSyncComplete?.Invoke(success, message);
                    MyLogger.Log(message, MyLogger.LogCategory.Firebase);
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
                
                // Обновляем статус синхронизации
                UpdateSyncStatus(true);
            }
            catch (Exception ex)
            {
                success = false;
                message = $"Ошибка синхронизации: {ex.Message}";
                MyLogger.LogError(message, MyLogger.LogCategory.Firebase);
                
                // Обновляем статус синхронизации с ошибкой
                UpdateSyncStatus(false, ex.Message);
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
        /// Синхронизирует данные с сервера
        /// </summary>
        private async Task SyncFromServer()
        {
            try
            {
                MyLogger.Log("🔄 [SyncFromServer] Получение данных с сервера...", MyLogger.LogCategory.Firebase);
                
                DateTime? lastSyncTime = _syncSettings.LastSyncTime != DateTime.MinValue ? 
                    _syncSettings.LastSyncTime : null;
                
                MyLogger.Log($"🔍 [SyncFromServer] Параметры запроса: lastSyncTime={lastSyncTime?.ToString("O")}, maxRecords={_syncSettings.MaxRecordsPerSync}", MyLogger.LogCategory.Firebase);
                MyLogger.Log($"🔍 [SyncFromServer] DatabaseService состояние: IsAuthenticated={_databaseService.IsAuthenticated}, UserId={_databaseService.UserId}", MyLogger.LogCategory.Firebase);
                
                // Получаем записи с сервера, которые появились после последней синхронизации
                var serverRecords = await _databaseService.GetEmotionHistory(lastSyncTime, null, _syncSettings.MaxRecordsPerSync);
                
                MyLogger.Log($"📊 [SyncFromServer] Получено {serverRecords?.Count ?? 0} записей с сервера", MyLogger.LogCategory.Firebase);
                
                if (serverRecords == null)
                {
                    MyLogger.LogWarning("⚠️ [SyncFromServer] GetEmotionHistory вернул NULL", MyLogger.LogCategory.Firebase);
                    return;
                }
                
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
        
        /// <summary>
        /// Проверяет, нужно ли загрузить данные из облака при запуске/активации
        /// </summary>
        private async void CheckAndLoadFromCloud()
        {
            try
            {
                MyLogger.Log("🔄 Проверка необходимости загрузки данных из облака...", MyLogger.LogCategory.Sync);
                
                // Проверяем статус последней синхронизации
                if (_syncStatus.IsLastSyncSuccessful)
                {
                    MyLogger.Log("🔄 Последняя синхронизация была успешной. Загружаем данные из облака...",
                        MyLogger.LogCategory.Sync);
                    
                    bool loadSuccess = await LoadDataFromCloud();
                    
                    MyLogger.Log($"🔄 Загрузка данных из облака: {(loadSuccess ? "✅ Успешно" : "❌ Неудачно")}",
                        MyLogger.LogCategory.Sync);
                }
                else
                {
                    MyLogger.LogWarning($"⚠️ Последняя синхронизация не была успешной: {_syncStatus.SyncErrorMessage}. " +
                                       "Используем локальные данные.",
                        MyLogger.LogCategory.Sync);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при проверке необходимости загрузки данных: {ex.Message}", 
                    MyLogger.LogCategory.Sync);
            }
        }
        
        /// <summary>
        /// Синхронизирует данные с облаком перед закрытием приложения
        /// </summary>
        private void SyncToCloudBeforeShutdown()
        {
            if (!_isInitialized || _databaseService == null || !_databaseService.IsAuthenticated)
            {
                MyLogger.LogWarning("⚠️ Невозможно синхронизировать данные с облаком: сервис не инициализирован или пользователь не аутентифицирован", 
                    MyLogger.LogCategory.Sync);
                return;
            }
            
            try
            {
                MyLogger.Log("🔄 Синхронизация данных с облаком перед закрытием...", 
                    MyLogger.LogCategory.Sync);
                
                // Получаем несинхронизированные записи
                var unsyncedRecords = _cache.GetUnsyncedRecords();
                
                if (unsyncedRecords == null || unsyncedRecords.Count == 0)
                {
                    MyLogger.Log("✅ Нет несинхронизированных данных", 
                        MyLogger.LogCategory.Sync);
                    return;
                }
                
                // Синхронный вызов, так как приложение закрывается
                Task.Run(async () => 
                {
                    try
                    {
                        // Отправляем все записи пакетно
                        await _databaseService.AddEmotionHistoryBatch(unsyncedRecords);
                        
                        // Обновляем статус синхронизации
                        UpdateSyncStatus(true);
                        
                        MyLogger.Log($"✅ Синхронизировано {unsyncedRecords.Count} записей перед закрытием", 
                            MyLogger.LogCategory.Sync);
                    }
                    catch (Exception ex)
                    {
                        MyLogger.LogError($"❌ Ошибка при синхронизации данных перед закрытием: {ex.Message}", 
                            MyLogger.LogCategory.Sync);
                        UpdateSyncStatus(false, ex.Message);
                    }
                }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Критическая ошибка при синхронизации данных перед закрытием: {ex.Message}", 
                    MyLogger.LogCategory.Sync);
                UpdateSyncStatus(false, ex.Message);
            }
        }
        
        /// <summary>
        /// Обновляет статус синхронизации
        /// </summary>
        private void UpdateSyncStatus(bool isSuccessful, string errorMessage = "")
        {
            _syncStatus.IsLastSyncSuccessful = isSuccessful;
            _syncStatus.LastSyncTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _syncStatus.SyncErrorMessage = errorMessage;
            
            MyLogger.Log($"💾 Статус синхронизации обновлен: {(isSuccessful ? "✅ Успешно" : $"❌ Неудачно: {errorMessage}")}",
                MyLogger.LogCategory.Sync);
        }
        #endregion
    }
} 