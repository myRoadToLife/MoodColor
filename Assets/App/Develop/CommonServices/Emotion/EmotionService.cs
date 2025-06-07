using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.App.Develop.Scenes.PersonalAreaScene.UI.Components;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.CommonServices.Firebase.Database.Services;
using App.Develop.CommonServices.DataManagement;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.ConfigsManagement;
using App.Develop.CommonServices.Networking;
using App.Develop.Configs.Common.Emotion;
using UnityEngine;
using App.Develop.CommonServices.Firebase.Common.SecureStorage;
using App.Develop.CommonServices.GameSystem;
using App.Develop.CommonServices.Location;
using App.Develop.DI;
using App.Develop.Utils.Logging;
using Firebase.Auth;
using App.Develop.CommonServices.Firebase.Common.Cache;
using App.Develop.CommonServices.Regional;

// Используем IDatabaseService только из пространства имен Services
using IDatabaseService = App.Develop.CommonServices.Firebase.Database.Services.IDatabaseService;

namespace App.Develop.CommonServices.Emotion
{
    public class EmotionService : IDataReader<PlayerData>, IDataWriter<PlayerData>, IEmotionService, IInitializable, IDisposable
    {
        #region Private Fields

        private readonly Dictionary<EmotionTypes, EmotionData> _emotions;
        private readonly PlayerDataProvider _playerDataProvider;
        private readonly EmotionMixingRules _emotionMixingRules;
        private readonly Dictionary<EmotionTypes, EmotionConfig> _emotionConfigs;
        private readonly EmotionHistory _emotionHistory;
        private readonly EmotionConfigService _emotionConfigService;

        // Firebase компоненты для синхронизации
        private IDatabaseService _databaseService;
        private EmotionHistoryCache _emotionHistoryCache;
        private EmotionSyncService _syncService;
        private ConnectivityManager _connectivityManager;

        // Сервис геолокации
        private ILocationService _locationService;

        // Флаги синхронизации
        private bool _isInitialized;
        private bool _isFirebaseInitialized;

        #region Dependencies

        private readonly IConfigsProvider _configsProvider;
        private readonly IPointsService _pointsService;
        private readonly ILevelSystem _levelSystem;
        private readonly IRegionalStatsService _regionalStatsService;

        #endregion

        #endregion

        #region Events

        // События для оповещения об изменениях
        public event EventHandler<EmotionEvent> OnEmotionEvent;

        // События синхронизации
        public event Action<bool, string> OnSyncComplete;
        public event Action<float> OnSyncProgress;

        #endregion

        #region Constructors

        public EmotionService(
            PlayerDataProvider playerDataProvider,
            IConfigsProvider configsProvider,
            EmotionConfigService emotionConfigService,
            EmotionHistoryCache emotionHistoryCache = null,
            IPointsService pointsService = null,
            ILevelSystem levelSystem = null,
            IRegionalStatsService regionalStatsService = null)
        {
            _playerDataProvider = playerDataProvider ?? throw new ArgumentNullException(nameof(playerDataProvider));
            _configsProvider = configsProvider;
            _emotionConfigService = emotionConfigService;
            _pointsService = pointsService;
            _levelSystem = levelSystem;
            _regionalStatsService = regionalStatsService;
            _emotions = new Dictionary<EmotionTypes, EmotionData>();
            _emotionMixingRules = new EmotionMixingRules();
            _emotionConfigs = new Dictionary<EmotionTypes, EmotionConfig>();

            try
            {
                // Используем переданный кэш или создаем новый
                _emotionHistoryCache = emotionHistoryCache ?? new EmotionHistoryCache();
                _emotionHistory = new EmotionHistory(_emotionHistoryCache);

                MyLogger.Log($"🔧 [EmotionService] Инициализирован с кэшем: {(_emotionHistoryCache != null ? "ДА" : "НЕТ")}", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                // Если произошла ошибка при создании кэша, создаем пустую историю
                MyLogger.LogWarning($"Ошибка при инициализации кэша истории эмоций: {ex.Message}. Будет использована пустая история.");
                _emotionHistory = new EmotionHistory();
            }

            InitializeEmotions();

            if (_emotionConfigService != null)
            {
                LoadEmotionConfigsFromService();
            }
            else
            {
                LoadEmotionConfigs(configsProvider);
            }

            _isInitialized = true;
        }

        // Конструктор для тестов
        internal EmotionService(Dictionary<EmotionTypes, EmotionData> initialEmotions)
        {
            _emotions = initialEmotions ?? throw new ArgumentNullException(nameof(initialEmotions));
            _emotionMixingRules = new EmotionMixingRules();
            _emotionConfigs = new Dictionary<EmotionTypes, EmotionConfig>();
            _emotionHistory = new EmotionHistory();
        }

        #endregion

        #region Firebase Integration

        /// <summary>
        /// Устанавливает сервис геолокации
        /// </summary>
        public void SetLocationService(ILocationService locationService)
        {
            _locationService = locationService;
            MyLogger.Log("🗺️ [EmotionService] LocationService установлен", MyLogger.LogCategory.Location);
        }

        /// <summary>
        /// Инициализирует компоненты для синхронизации с Firebase
        /// </summary>
        public void InitializeFirebaseSync(IDatabaseService databaseService, EmotionSyncService syncService, ConnectivityManager connectivityManager)
        {
            MyLogger.Log("🔗 [InitializeFirebaseSync] Начинаем инициализацию Firebase синхронизации...", MyLogger.LogCategory.ClearHistory);

            _databaseService = databaseService;
            _syncService = syncService;
            _connectivityManager = connectivityManager;

            MyLogger.Log(
                $"🔍 [InitializeFirebaseSync] Проверка зависимостей: _syncService!=null={_syncService != null}, _emotionHistoryCache!=null={_emotionHistoryCache != null}, _connectivityManager!=null={_connectivityManager != null}, _databaseService!=null={_databaseService != null}",
                MyLogger.LogCategory.ClearHistory);

            if (_syncService != null && _emotionHistoryCache != null && _connectivityManager != null && _databaseService != null)
            {
                _syncService.Initialize(_databaseService, _emotionHistoryCache, _connectivityManager);

                // Подписываемся на события синхронизации
                _syncService.OnSyncComplete += HandleSyncComplete;
                _syncService.OnSyncProgress += HandleSyncProgress;
                _syncService.OnRecordSynced += HandleRecordSynced;
                _syncService.OnSyncConflict += HandleSyncConflict;

                _isFirebaseInitialized = true;

                MyLogger.Log("🔗 [InitializeFirebaseSync] ✅ Firebase Sync Services успешно инициализированы. _isFirebaseInitialized = true.",
                    MyLogger.LogCategory.ClearHistory);
            }
            else
            {
                MyLogger.LogWarning("🔗 [InitializeFirebaseSync] ❌ Не удалось инициализировать Firebase Sync. Одна или несколько зависимостей равны null: " +
                                    $"_syncService is null: {_syncService == null}, " +
                                    $"_emotionHistoryCache is null: {_emotionHistoryCache == null}, " +
                                    $"_connectivityManager is null: {_connectivityManager == null}, " +
                                    $"_databaseService is null: {_databaseService == null}",
                    MyLogger.LogCategory.ClearHistory);

                _isFirebaseInitialized = false; // Явно указываем, что инициализация не удалась
            }
        }

        /// <summary>
        /// Запускает синхронизацию с Firebase, если она инициализирована и пользователь аутентифицирован
        /// </summary>
        public void StartSync()
        {
            if (_databaseService == null || !_databaseService.IsAuthenticated)
            {
                MyLogger.LogWarning("🔥 [SYNC] ⚠️ Невозможно запустить синхронизацию: DatabaseService не настроен или пользователь не аутентифицирован.",
                    MyLogger.LogCategory.Firebase);

                return;
            }

            if (!_isFirebaseInitialized)
            {
                MyLogger.LogWarning(
                    "🔥 [SYNC] ⚠️ Невозможно запустить синхронизацию: Firebase не инициализирован должным образом в EmotionService (InitializeFirebaseSync не был успешен или не вызывался).",
                    MyLogger.LogCategory.Firebase);

                return;
            }

            if (_syncService == null)
            {
                MyLogger.LogWarning("🔥 [SYNC] ⚠️ Невозможно запустить синхронизацию: _syncService is null.", MyLogger.LogCategory.Firebase);
                return;
            }

            var unsyncedCount = _emotionHistoryCache?.GetUnsyncedRecords().Count ?? 0;
            MyLogger.Log($"🔥 [SYNC] 📊 Найдено {unsyncedCount} несинхронизированных записей перед запуском синхронизации", MyLogger.LogCategory.Firebase);

            _syncService.StartSync(); // Используем правильный метод StartSync()
            MyLogger.Log("🔥 [SYNC] ✅ Синхронизация запущена через _syncService.StartSync().", MyLogger.LogCategory.Firebase);
        }

        /// <summary>
        /// Принудительно синхронизирует все локальные записи с облаком
        /// </summary>
        public async Task<bool> ForceSyncLocalToCloud()
        {
            MyLogger.Log("🔄 [FORCE-SYNC] 🚀 Начинаем принудительную синхронизацию локальных данных с облаком...", MyLogger.LogCategory.Firebase);

            if (!_isFirebaseInitialized || _databaseService == null || !_databaseService.IsAuthenticated)
            {
                MyLogger.LogWarning(
                    "🔄 [FORCE-SYNC] ⚠️ Невозможно выполнить принудительную синхронизацию: Firebase не инициализирован или пользователь не аутентифицирован",
                    MyLogger.LogCategory.Firebase);

                return false;
            }

            if (_emotionHistoryCache == null)
            {
                MyLogger.LogWarning("🔄 [FORCE-SYNC] ⚠️ Невозможно выполнить принудительную синхронизацию: кэш истории эмоций не инициализирован",
                    MyLogger.LogCategory.Firebase);

                return false;
            }

            try
            {
                // Получаем все локальные записи (не только несинхронизированные)
                var allLocalRecords = _emotionHistoryCache.GetAllRecords();

                if (allLocalRecords == null || !allLocalRecords.Any())
                {
                    MyLogger.LogWarning("🔄 [FORCE-SYNC] ⚠️ Локальных записей не найдено для синхронизации", MyLogger.LogCategory.Firebase);
                    return false;
                }

                MyLogger.Log($"🔄 [FORCE-SYNC] 📊 Всего локальных записей: {allLocalRecords.Count}", MyLogger.LogCategory.Firebase);

                // Устанавливаем всем записям статус "Не синхронизировано" для принудительной отправки
                foreach (var record in allLocalRecords)
                {
                    record.SyncStatus = SyncStatus.NotSynced;
                    _emotionHistoryCache.UpdateRecord(record);
                }

                MyLogger.Log($"🔄 [FORCE-SYNC] 📝 Все {allLocalRecords.Count} записи помечены как несинхронизированные", MyLogger.LogCategory.Firebase);

                // Запускаем синхронизацию
                StartSync();

                // Ждем 2 секунды для начала процесса синхронизации
                await Task.Delay(2000);

                // Проверяем, запустился ли процесс синхронизации
                var stillUnsyncedCount = _emotionHistoryCache.GetUnsyncedRecords().Count;

                if (stillUnsyncedCount > 0)
                {
                    MyLogger.Log($"🔄 [FORCE-SYNC] ⏳ Синхронизация запущена, но еще не завершена. Осталось синхронизировать: {stillUnsyncedCount} записей",
                        MyLogger.LogCategory.Firebase);
                }
                else
                {
                    MyLogger.Log("🔄 [FORCE-SYNC] ✅ Все записи успешно отправлены на синхронизацию!", MyLogger.LogCategory.Firebase);
                }

                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"🔄 [FORCE-SYNC] ❌ Ошибка при принудительной синхронизации: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        /// <summary>
        /// Принудительно обновляет историю из Firebase (мягкое обновление - сохраняет локальные записи)
        /// </summary>
        public async Task<bool> RefreshHistoryFromFirebase()
        {
            MyLogger.Log(
                $"🔄 [RefreshHistoryFromFirebase] Попытка обновления. _isFirebaseInitialized: {_isFirebaseInitialized}, _databaseService null?: {_databaseService == null}, _databaseService.IsAuthenticated: {(_databaseService?.IsAuthenticated ?? false)}",
                MyLogger.LogCategory.Firebase);

            if (!_isFirebaseInitialized || _databaseService == null || !_databaseService.IsAuthenticated)
            {
                MyLogger.LogWarning("🔄 Невозможно обновить историю из Firebase: сервис не инициализирован или пользователь не аутентифицирован",
                    MyLogger.LogCategory.Firebase);

                return false;
            }

            try
            {
                MyLogger.Log("🔄 Начинаем обновление истории из Firebase...", MyLogger.LogCategory.Firebase);

                // Сохраняем локальные несинхронизированные записи
                var unsyncedRecords = _emotionHistoryCache?.GetUnsyncedRecords();
                int unsyncedCount = unsyncedRecords?.Count ?? 0;
                MyLogger.Log($"📝 Найдено {unsyncedCount} несинхронизированных локальных записей", MyLogger.LogCategory.Firebase);

                // Получаем данные из Firebase
                var firebaseRecords = await _databaseService.GetEmotionHistory();

                if (firebaseRecords == null || !firebaseRecords.Any())
                {
                    MyLogger.Log("☁️ В Firebase нет записей истории. Используем локальные данные.", MyLogger.LogCategory.Firebase);

                    if (unsyncedCount > 0)
                    {
                        MyLogger.Log("📤 Отправляем локальные записи в Firebase...", MyLogger.LogCategory.Firebase);
                        StartSync(); // Запускаем синхронизацию локальных данных
                    }

                    return true;
                }

                MyLogger.Log($"📥 Получено {firebaseRecords.Count} записей из Firebase", MyLogger.LogCategory.Firebase);

                // Обновляем кэш, сохраняя несинхронизированные записи
                if (_emotionHistoryCache != null)
                {
                    // Очищаем кэш, но сохраняем несинхронизированные записи
                    var allRecords = _emotionHistoryCache.GetAllRecords();
                    _emotionHistoryCache.ClearCache();

                    // Добавляем записи из Firebase
                    foreach (var record in firebaseRecords)
                    {
                        record.SyncStatus = SyncStatus.Synced;
                        _emotionHistoryCache.AddRecord(record);
                    }

                    // Возвращаем несинхронизированные записи обратно в кэш
                    if (unsyncedRecords != null)
                    {
                        foreach (var record in unsyncedRecords)
                        {
                            _emotionHistoryCache.AddRecord(record);
                        }
                    }

                    // Перезагружаем историю из обновленного кэша
                    _emotionHistory.SetCache(_emotionHistoryCache);

                    MyLogger.Log("✅ История успешно обновлена из Firebase с сохранением локальных изменений", MyLogger.LogCategory.Firebase);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при обновлении истории из Firebase: {ex.Message}", MyLogger.LogCategory.Firebase);
                throw;
            }
        }

        /// <summary>
        /// Принудительно синхронизирует локальную историю с Firebase (заменяет локальные данные данными из Firebase)
        /// Используется после очистки истории для обеспечения полной синхронизации
        /// </summary>
        public async Task<bool> ForceSyncWithFirebase()
        {
            MyLogger.Log(
                $"🔄 [ForceSyncWithFirebase] Принудительная синхронизация с Firebase. _isFirebaseInitialized: {_isFirebaseInitialized}, _databaseService null?: {_databaseService == null}, _databaseService.IsAuthenticated: {(_databaseService?.IsAuthenticated ?? false)}",
                MyLogger.LogCategory.ClearHistory);

            if (!_isFirebaseInitialized || _databaseService == null || !_databaseService.IsAuthenticated)
            {
                MyLogger.LogWarning(
                    "🔄 [ForceSyncWithFirebase] Невозможно синхронизироваться с Firebase: сервис не инициализирован или пользователь не аутентифицирован",
                    MyLogger.LogCategory.ClearHistory);

                return false;
            }

            try
            {
                MyLogger.Log("🔄 [ForceSyncWithFirebase] Начинаем принудительную синхронизацию с Firebase...", MyLogger.LogCategory.ClearHistory);

                // Проверяем подключение
                bool isConnected = await _databaseService.CheckConnection();

                if (!isConnected)
                {
                    MyLogger.LogWarning("🔄 [ForceSyncWithFirebase] Нет соединения с Firebase", MyLogger.LogCategory.ClearHistory);
                    return false;
                }

                MyLogger.Log($"🔍 [ForceSyncWithFirebase] Вызываем _databaseService.GetEmotionHistory() для UserId: {_databaseService.UserId}",
                    MyLogger.LogCategory.ClearHistory);

                // Получаем данные из Firebase
                var firebaseRecords = await _databaseService.GetEmotionHistory();

                MyLogger.Log($"📥 [ForceSyncWithFirebase] Получено {firebaseRecords?.Count ?? 0} записей из Firebase", MyLogger.LogCategory.ClearHistory);

                if (firebaseRecords == null)
                {
                    MyLogger.LogWarning("⚠️ [ForceSyncWithFirebase] GetEmotionHistory вернул NULL", MyLogger.LogCategory.ClearHistory);
                }

                // Полностью заменяем локальный кэш данными из Firebase
                if (_emotionHistoryCache != null)
                {
                    MyLogger.Log("🗑️ [ForceSyncWithFirebase] Полностью очищаем локальный кэш...", MyLogger.LogCategory.ClearHistory);
                    _emotionHistoryCache.ClearCache();

                    // Добавляем записи из Firebase (если они есть)
                    if (firebaseRecords != null && firebaseRecords.Any())
                    {
                        int addedCount = 0;

                        foreach (var record in firebaseRecords)
                        {
                            try
                            {
                                // Гарантируем, что запись имеет правильный статус
                                record.SyncStatus = SyncStatus.Synced;
                                _emotionHistoryCache.AddRecord(record);
                                addedCount++;
                            }
                            catch (Exception recordEx)
                            {
                                MyLogger.LogError($"❌ [ForceSyncWithFirebase] Ошибка при добавлении записи в кэш: {recordEx.Message}",
                                    MyLogger.LogCategory.ClearHistory);
                                // Продолжаем с другими записями
                            }
                        }

                        MyLogger.Log($"➕ [ForceSyncWithFirebase] Успешно добавлено {addedCount} из {firebaseRecords.Count} записей из Firebase в локальный кэш",
                            MyLogger.LogCategory.ClearHistory);
                    }
                    else
                    {
                        MyLogger.Log("📭 [ForceSyncWithFirebase] Firebase пуст - локальный кэш остается пустым", MyLogger.LogCategory.ClearHistory);
                    }

                    // Перезагружаем историю из обновленного кэша
                    _emotionHistory.SetCache(_emotionHistoryCache);

                    // Добавляем дополнительную задержку для гарантии завершения операций
                    await Task.Delay(500);

                    MyLogger.Log("✅ [ForceSyncWithFirebase] Принудительная синхронизация завершена успешно", MyLogger.LogCategory.ClearHistory);
                    return true;
                }

                MyLogger.LogError("❌ [ForceSyncWithFirebase] EmotionHistoryCache не инициализирован", MyLogger.LogCategory.ClearHistory);
                return false;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [ForceSyncWithFirebase] Ошибка при принудительной синхронизации с Firebase: {ex.Message}",
                    MyLogger.LogCategory.ClearHistory);

                return false;
            }
        }

        /// <summary>
        /// Обновляет настройки синхронизации
        /// </summary>
        public async void UpdateSyncSettings(EmotionSyncSettings settings)
        {
            if (!_isFirebaseInitialized || _syncService == null)
            {
                MyLogger.LogWarning("Синхронизация недоступна: Firebase не инициализирован", MyLogger.LogCategory.Firebase);
                return;
            }

            await _syncService.UpdateSyncSettings(settings);
        }

        /// <summary>
        /// Получает текущие настройки синхронизации
        /// </summary>
        public EmotionSyncSettings GetSyncSettings()
        {
            if (_emotionHistoryCache != null)
            {
                return _emotionHistoryCache.GetSyncSettings();
            }

            return null;
        }

        /// <summary>
        /// Создает резервную копию данных
        /// </summary>
        public async void CreateBackup()
        {
            if (!_isFirebaseInitialized || _syncService == null)
            {
                MyLogger.LogWarning("Резервное копирование недоступно: Firebase не инициализирован", MyLogger.LogCategory.Firebase);
                return;
            }

            await _syncService.CreateBackup();
        }

        /// <summary>
        /// Восстанавливает данные из резервной копии
        /// </summary>
        public async void RestoreFromBackup(string backupId)
        {
            if (!_isFirebaseInitialized || _syncService == null)
            {
                MyLogger.LogWarning("Восстановление недоступно: Firebase не инициализирован", MyLogger.LogCategory.Firebase);
                return;
            }

            await _syncService.RestoreFromBackup(backupId);

            // После восстановления из резервной копии обновляем локальные данные
            await RefreshEmotionsFromFirebase();
        }

        /// <summary>
        /// Проверяет необходимость создания резервной копии
        /// </summary>
        public async void CheckAndCreateBackup()
        {
            if (!_isFirebaseInitialized || _syncService == null)
            {
                return;
            }

            await _syncService.CheckAndCreateBackup();
        }

        /// <summary>
        /// Обновляет эмоции из Firebase
        /// </summary>
        private async System.Threading.Tasks.Task RefreshEmotionsFromFirebase()
        {
            if (!_isFirebaseInitialized || _databaseService == null)
            {
                MyLogger.LogWarning("Обновление эмоций недоступно: Firebase не инициализирован", MyLogger.LogCategory.Firebase);
                return;
            }

            try
            {
                // Получаем данные об эмоциях с сервера
                var emotions = await _databaseService.GetUserEmotions();

                if (emotions != null && emotions.Count > 0)
                {
                    // Обновляем локальные эмоции
                    foreach (var emotion in emotions)
                    {
                        if (Enum.TryParse<EmotionTypes>(emotion.Value.Type, out var emotionType))
                        {
                            if (_emotions.ContainsKey(emotionType))
                            {
                                _emotions[emotionType] = emotion.Value;
                            }
                            else
                            {
                                _emotions.Add(emotionType, emotion.Value);
                            }
                        }
                    }

                    MyLogger.Log($"Обновлено {emotions.Count} эмоций из Firebase", MyLogger.LogCategory.Firebase);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при обновлении эмоций из Firebase: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }

        #region Event Handlers

        /// <summary>
        /// Обрабатывает событие завершения синхронизации
        /// </summary>
        private void HandleSyncComplete(bool success, string message)
        {
            MyLogger.Log($"Синхронизация эмоций завершена. Успех: {success}. {message}", MyLogger.LogCategory.Firebase);
            OnSyncComplete?.Invoke(success, message);
        }

        /// <summary>
        /// Обрабатывает событие прогресса синхронизации
        /// </summary>
        private void HandleSyncProgress(float progress)
        {
            OnSyncProgress?.Invoke(progress);
        }

        /// <summary>
        /// Обрабатывает событие синхронизации отдельной записи
        /// </summary>
        private void HandleRecordSynced(EmotionHistoryRecord record)
        {
            // Обновляем статус синхронизации в истории
            _emotionHistory.UpdateSyncStatus(record.Id, true);
        }

        /// <summary>
        /// Обрабатывает событие конфликта синхронизации
        /// </summary>
        private void HandleSyncConflict(EmotionHistoryRecord record)
        {
            MyLogger.LogWarning($"Конфликт синхронизации записи {record.Id} типа {record.Type}", MyLogger.LogCategory.Firebase);
            // Здесь можно добавить логику обработки конфликтов, например, показать UI для выбора пользователем
        }

        #endregion

        #endregion

        #region Emotion Management

        public List<EmotionTypes> AvailableEmotions => _emotions.Keys.ToList();

        public EmotionData GetEmotion(EmotionTypes type)
        {
            if (_emotions.TryGetValue(type, out var emotion))
            {
                return emotion;
            }

            MyLogger.LogWarning($"⚠️ Эмоция {type} не найдена!", MyLogger.LogCategory.Emotion);
            return null;
        }

        public bool HasEnough(EmotionTypes type, float amount)
        {
            var emotion = GetEmotion(type);
            return emotion != null && emotion.Value >= amount;
        }

        public void SpendEmotion(EmotionTypes type, float amount)
        {
            if (!HasEnough(type, amount))
                throw new ArgumentException($"Not enough {type} emotion");

            var emotion = GetEmotion(type);
            UpdateEmotionValue(type, emotion.Value - amount);
        }

        public void AddEmotion(EmotionTypes type, float amount)
        {
            var emotion = GetEmotion(type);

            if (emotion != null)
            {
                UpdateEmotionValue(type, emotion.Value + amount);
            }
        }

        public void ReadFrom(PlayerData data)
        {
            if (data?.EmotionData == null)
            {
                MyLogger.LogWarning("⚠️ EmotionData отсутствует при ReadFrom. Пропускаем.", MyLogger.LogCategory.Emotion);
                return;
            }

            foreach (var emotion in data.EmotionData)
            {
                if (_emotions.ContainsKey(emotion.Key))
                {
                    _emotions[emotion.Key] = emotion.Value;
                }
                else
                {
                    _emotions.Add(emotion.Key, emotion.Value);
                }
            }

            // Добавляем отсутствующие эмоции с дефолтными значениями
            foreach (EmotionTypes type in Enum.GetValues(typeof(EmotionTypes)))
            {
                if (!_emotions.ContainsKey(type))
                {
                    MyLogger.LogWarning($"⚠️ Emotion {type} не был загружен. Создаём по умолчанию.", MyLogger.LogCategory.Emotion);

                    _emotions[type] = new EmotionData
                    {
                        Type = type.ToString(),
                        Value = 0f,
                        LastUpdate = DateTime.UtcNow
                    };
                }
            }
        }

        public void WriteTo(PlayerData data)
        {
            foreach (var emotion in _emotions)
            {
                if (data.EmotionData.ContainsKey(emotion.Key))
                {
                    data.EmotionData[emotion.Key] = emotion.Value;
                }
                else
                {
                    data.EmotionData.Add(emotion.Key, emotion.Value);
                }
            }
        }

        private void InitializeEmotions()
        {
            try
            {
                var emotionsList = _playerDataProvider.GetEmotions();

                if (emotionsList == null || emotionsList.Count == 0)
                {
                    MyLogger.LogWarning("⚠️ PlayerDataProvider вернул пустой список эмоций. Инициализируем дефолтные эмоции.", MyLogger.LogCategory.Emotion);
                    InitializeDefaultEmotions();
                    return;
                }

                foreach (var emotionData in emotionsList)
                {
                    if (emotionData == null)
                    {
                        MyLogger.LogWarning("⚠️ Найдена NULL эмоция в списке. Пропускаем.", MyLogger.LogCategory.Emotion);
                        continue;
                    }

                    if (string.IsNullOrEmpty(emotionData.Type))
                    {
                        MyLogger.LogWarning("⚠️ Найдена эмоция с пустым Type. Пропускаем.", MyLogger.LogCategory.Emotion);
                        continue;
                    }

                    if (Enum.TryParse(emotionData.Type, out EmotionTypes type))
                    {
                        _emotions[type] = emotionData;
                    }
                    else
                    {
                        MyLogger.LogWarning($"⚠️ Не удалось распарсить тип эмоции: {emotionData.Type}", MyLogger.LogCategory.Emotion);
                    }
                }

                // Проверяем, что все типы эмоций присутствуют
                foreach (EmotionTypes type in Enum.GetValues(typeof(EmotionTypes)))
                {
                    if (!_emotions.ContainsKey(type))
                    {
                        MyLogger.LogWarning($"⚠️ Эмоция {type} отсутствует. Создаем по умолчанию.", MyLogger.LogCategory.Emotion);
                        _emotions[type] = CreateDefaultEmotion(type);
                    }
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при инициализации эмоций: {ex.Message}. Инициализируем дефолтные эмоции.", MyLogger.LogCategory.Emotion);
                InitializeDefaultEmotions();
            }
        }

        /// <summary>
        /// Инициализирует дефолтные эмоции при ошибке
        /// </summary>
        private void InitializeDefaultEmotions()
        {
            _emotions.Clear();

            foreach (EmotionTypes type in Enum.GetValues(typeof(EmotionTypes)))
            {
                _emotions[type] = CreateDefaultEmotion(type);
            }
        }

        /// <summary>
        /// Создает эмоцию с дефолтными значениями
        /// </summary>
        private EmotionData CreateDefaultEmotion(EmotionTypes type)
        {
            var config = _emotionConfigs.ContainsKey(type) ? _emotionConfigs[type] : null;

            return new EmotionData
            {
                Type = type.ToString(),
                Value = 0f,
                Intensity = 0f,
                Color = config?.BaseColor ?? GetDefaultColor(type),
                MaxCapacity = config?.MaxCapacity ?? 100f,
                DrainRate = config?.DefaultDrainRate ?? 0.1f,
                BubbleThreshold = config?.BubbleThreshold ?? 80f,
                LastUpdate = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Возвращает дефолтный цвет для эмоции
        /// </summary>
        private Color GetDefaultColor(EmotionTypes type)
        {
            return type switch
            {
                EmotionTypes.Joy => new Color(1f, 0.85f, 0.1f), // Желтый
                EmotionTypes.Sadness => new Color(0.15f, 0.3f, 0.8f), // Синий
                EmotionTypes.Anger => new Color(0.9f, 0.1f, 0.1f), // Красный
                EmotionTypes.Fear => new Color(0.5f, 0.1f, 0.6f), // Фиолетовый
                EmotionTypes.Disgust => new Color(0.1f, 0.6f, 0.2f), // Зеленый
                EmotionTypes.Trust => new Color(0f, 0.6f, 0.9f), // Голубой
                EmotionTypes.Anticipation => new Color(1f, 0.5f, 0f), // Оранжевый
                EmotionTypes.Surprise => new Color(0.8f, 0.4f, 0.9f), // Лавандовый
                EmotionTypes.Love => new Color(0.95f, 0.3f, 0.6f), // Розовый
                EmotionTypes.Anxiety => new Color(0.7f, 0.7f, 0.7f), // Серый
                EmotionTypes.Neutral => Color.white,
                _ => Color.white
            };
        }

        private void LoadEmotionConfigs(IConfigsProvider configsProvider)
        {
            if (configsProvider == null) return;

            foreach (EmotionTypes type in Enum.GetValues(typeof(EmotionTypes)))
            {
                var config = configsProvider.LoadEmotionConfig(type);

                if (config != null)
                {
                    _emotionConfigs[type] = config;
                }
            }
        }

        private void LoadEmotionConfigsFromService()
        {
            if (_emotionConfigService == null) return;

            foreach (EmotionTypes type in Enum.GetValues(typeof(EmotionTypes)))
            {
                var config = _emotionConfigService.GetConfig(type);

                if (config != null)
                {
                    _emotionConfigs[type] = config;
                }
            }
        }

        public void UpdateEmotionValue(EmotionTypes type, float value)
        {
            var emotion = GetEmotion(type);
            if (emotion == null) return;

            float oldValue = emotion.Value;
            ValidateAndUpdateEmotion(emotion, value);

            // Определяем тип события
            EmotionEventType eventType = oldValue < emotion.BubbleThreshold && value >= emotion.BubbleThreshold
                ? EmotionEventType.CapacityExceeded
                : EmotionEventType.ValueChanged;

            MyLogger.Log(
                $"[EmotionService] Before AddEntry in UpdateEmotionValue: EmotionType='{emotion.Type}', LastUpdate='{emotion.LastUpdate}', CurrentTime='{DateTime.Now}'",
                MyLogger.LogCategory.Emotion);

            // Добавляем в историю
            _emotionHistory.AddEntry(emotion, eventType, emotion.LastUpdate);

            // Синхронизируем с Firebase
            if (_isFirebaseInitialized && _databaseService != null)
            {
                SyncEmotionWithFirebase(emotion, eventType);
            }

            // Вызываем событие
            RaiseEmotionEvent(new EmotionEvent(type, eventType, emotion.Value, emotion.Intensity));

            // Начисляем очки, если доступен сервис очков и значение положительное
            if (_pointsService != null && value > 0)
            {
                _pointsService.AddPointsForEmotion();
            }

            // Начисляем опыт, если доступна система уровней и значение положительное
            if (_levelSystem != null && value > 0)
            {
                // Базовое количество опыта за отметку эмоции
                int baseXp = 5;
                _levelSystem.AddXP(baseXp, XPSource.EmotionMarked);
            }
        }

        private void ValidateAndUpdateEmotion(EmotionData emotion, float newValue)
        {
            // Применяем новое значение, учитывая ограничения
            emotion.Value = Mathf.Clamp(newValue, 0f, emotion.MaxCapacity);
            emotion.LastUpdate = DateTime.UtcNow;
        }

        public void UpdateEmotionIntensity(EmotionTypes type, float intensity)
        {
            var emotion = GetEmotion(type);
            if (emotion == null) return;

            emotion.Intensity = Mathf.Clamp01(intensity);
            emotion.LastUpdate = DateTime.Now;

            MyLogger.Log(
                $"[EmotionService] Before AddEntry in UpdateEmotionIntensity: EmotionType='{emotion.Type}', LastUpdate='{emotion.LastUpdate}', CurrentTime='{DateTime.Now}'",
                MyLogger.LogCategory.Emotion);

            _emotionHistory.AddEntry(emotion, EmotionEventType.IntensityChanged, emotion.LastUpdate);

            RaiseEmotionEvent(new EmotionEvent(type, EmotionEventType.IntensityChanged, emotion.Value, emotion.Intensity));
        }

        public bool TryMixEmotions(EmotionTypes source1, EmotionTypes source2)
        {
            if (_emotionMixingRules.TryGetMixResult(source1, source2, out var mixResult))
            {
                var emotion1 = GetEmotion(source1);
                var emotion2 = GetEmotion(source2);

                if (emotion1 != null && emotion2 != null)
                {
                    var newValue = (emotion1.Value + emotion2.Value) * mixResult.ResultIntensity;
                    var resultEmotion = GetEmotion(mixResult.ResultType);

                    if (resultEmotion != null)
                    {
                        resultEmotion.Color = mixResult.ResultColor;
                        resultEmotion.Note = $"{source1} + {source2}";
                        resultEmotion.LastUpdate = DateTime.UtcNow;
                        ValidateAndUpdateEmotion(resultEmotion, newValue);

                        MyLogger.Log(
                            $"[EmotionService] Before AddEntry in TryMixEmotions: EmotionType='{resultEmotion.Type}', LastUpdate='{resultEmotion.LastUpdate}', CurrentTime='{DateTime.Now}'",
                            MyLogger.LogCategory.Emotion);

                        _emotionHistory.AddEntry(resultEmotion, EmotionEventType.EmotionMixed, resultEmotion.LastUpdate);

                        // Синхронизируем с Firebase
                        if (_isFirebaseInitialized && _databaseService != null)
                        {
                            SyncEmotionWithFirebase(resultEmotion, EmotionEventType.EmotionMixed);
                        }

                        RaiseEmotionEvent(new EmotionEvent(mixResult.ResultType,
                            EmotionEventType.EmotionMixed, newValue, mixResult.ResultIntensity));

                        return true;
                    }
                }
            }

            return false;
        }

        public void ProcessTimeBasedEffects()
        {
            foreach (var emotionPair in _emotions)
            {
                var emotion = emotionPair.Value;
                var timeSinceLastUpdate = (DateTime.Now - emotion.LastUpdate).TotalSeconds;

                if (emotion.Value > 0)
                {
                    var drain = emotion.DrainRate * (float)timeSinceLastUpdate;
                    emotion.Value = Mathf.Max(0f, emotion.Value - drain);

                    if (emotion.Value <= 0)
                    {
                        RaiseEmotionEvent(new EmotionEvent(emotionPair.Key,
                            EmotionEventType.EmotionDepleted));
                    }
                }

                if (emotion.Value >= emotion.BubbleThreshold)
                {
                    RaiseEmotionEvent(new EmotionEvent(emotionPair.Key,
                        EmotionEventType.BubbleCreated, emotion.Value, emotion.Intensity));
                }

                emotion.LastUpdate = DateTime.Now;
            }
        }

        public IEnumerable<EmotionHistoryEntry> GetEmotionHistory(DateTime? from = null, DateTime? to = null)
        {
            MyLogger.Log(
                $"🔍 [EmotionService.GetEmotionHistory] Запрос истории эмоций. _emotionHistory!=null={_emotionHistory != null}, _emotionHistoryCache!=null={_emotionHistoryCache != null}",
                MyLogger.LogCategory.Firebase);

            var history = _emotionHistory.GetHistory(from, to);
            var historyList = history?.ToList();

            MyLogger.Log($"📊 [EmotionService.GetEmotionHistory] Получено {historyList?.Count ?? 0} записей из _emotionHistory", MyLogger.LogCategory.Firebase);

            if (_emotionHistoryCache != null)
            {
                var cacheRecords = _emotionHistoryCache.GetAllRecords();
                MyLogger.Log($"📊 [EmotionService.GetEmotionHistory] В кэше находится {cacheRecords?.Count ?? 0} записей", MyLogger.LogCategory.Firebase);
            }

            // Детальная информация о первых записях
            if (historyList != null && historyList.Count > 0)
            {
                MyLogger.Log($"🔍 [EmotionService.GetEmotionHistory] Первые записи:", MyLogger.LogCategory.Firebase);

                for (int i = 0; i < Math.Min(3, historyList.Count); i++)
                {
                    var entry = historyList[i];

                    MyLogger.Log(
                        $"  [{i}] Type={entry.EmotionData?.Type}, Value={entry.EmotionData?.Value}, Timestamp={entry.Timestamp:yyyy-MM-dd HH:mm:ss}, SyncId={entry.SyncId}",
                        MyLogger.LogCategory.Firebase);
                }
            }
            else
            {
                MyLogger.Log($"📭 [EmotionService.GetEmotionHistory] История пуста - возвращаем пустой список", MyLogger.LogCategory.Firebase);
            }

            return historyList ?? new List<EmotionHistoryEntry>();
        }

        public IEnumerable<EmotionHistoryEntry> GetEmotionHistoryByType(EmotionTypes type, DateTime? from = null, DateTime? to = null)
        {
            return _emotionHistory.GetHistoryByType(type, from, to);
        }

        public Dictionary<EmotionTypes, float> GetAverageIntensityByPeriod(DateTime from, DateTime to)
        {
            return _emotionHistory.GetAverageIntensityByPeriod(from, to);
        }

        protected virtual void RaiseEmotionEvent(EmotionEvent e)
        {
            OnEmotionEvent?.Invoke(this, e);
        }

        #region Statistics

        /// <summary>
        /// Получает статистику эмоций по времени суток
        /// </summary>
        /// <param name="from">Начальная дата (опционально)</param>
        /// <param name="to">Конечная дата (опционально)</param>
        /// <returns>Словарь статистики по времени суток</returns>
        public Dictionary<TimeOfDay, EmotionTimeStats> GetEmotionsByTimeOfDay(DateTime? from = null, DateTime? to = null)
        {
            return _emotionHistory.GetEmotionsByTimeOfDay(from, to);
        }

        /// <summary>
        /// Получает статистику частоты записи эмоций
        /// </summary>
        /// <param name="from">Начальная дата</param>
        /// <param name="to">Конечная дата</param>
        /// <param name="groupByDay">Группировать по дням (true) или по часам (false)</param>
        /// <returns>Список статистики частоты записей</returns>
        public List<EmotionFrequencyStats> GetLoggingFrequency(DateTime from, DateTime to, bool groupByDay = true)
        {
            return _emotionHistory.GetLoggingFrequency(from, to, groupByDay).ToList();
        }

        /// <summary>
        /// Получает статистику популярных комбинаций эмоций
        /// </summary>
        /// <param name="from">Начальная дата (опционально)</param>
        /// <param name="to">Конечная дата (опционально)</param>
        /// <param name="topCount">Количество самых популярных комбинаций</param>
        /// <returns>Список статистики комбинаций</returns>
        public List<EmotionCombinationStats> GetPopularEmotionCombinations(DateTime? from = null, DateTime? to = null, int topCount = 5)
        {
            return _emotionHistory.GetPopularEmotionCombinations(from, to, topCount);
        }

        /// <summary>
        /// Получает статистику трендов эмоций
        /// </summary>
        /// <param name="from">Начальная дата</param>
        /// <param name="to">Конечная дата</param>
        /// <param name="groupByDay">Группировать по дням (true) или по часам (false)</param>
        /// <returns>Список статистики трендов</returns>
        public List<EmotionTrendStats> GetEmotionTrends(DateTime from, DateTime to, bool groupByDay = true)
        {
            return _emotionHistory.GetEmotionTrends(from, to, groupByDay);
        }

        #endregion

        #endregion

        #region IInitializable Implementation

        /// <summary>
        /// Инициализирует сервис эмоций
        /// </summary>
        public void Initialize()
        {
            // Загружаем эмоции из провайдера данных
            InitializeEmotions();

            MyLogger.Log("EmotionService инициализирован", MyLogger.LogCategory.Bootstrap);
        }

        #endregion

        #region IEmotionService Implementation

        /// <summary>
        /// Получить текущее значение эмоции
        /// </summary>
        public float GetEmotionValue(EmotionTypes type)
        {
            var emotion = GetEmotion(type);
            return emotion?.Value ?? 0f;
        }

        /// <summary>
        /// Получить текущую интенсивность эмоции
        /// </summary>
        public float GetEmotionIntensity(EmotionTypes type)
        {
            var emotion = GetEmotion(type);
            return emotion?.Intensity ?? 0f;
        }

        /// <summary>
        /// Получить цвет эмоции
        /// </summary>
        public Color GetEmotionColor(EmotionTypes type)
        {
            var emotion = GetEmotion(type);
            return emotion?.Color ?? Color.white;
        }

        /// <summary>
        /// Получить данные эмоции
        /// </summary>
        public EmotionData GetEmotionData(EmotionTypes type)
        {
            return GetEmotion(type);
        }

        /// <summary>
        /// Получить все эмоции
        /// </summary>
        public Dictionary<EmotionTypes, EmotionData> GetAllEmotions()
        {
            return new Dictionary<EmotionTypes, EmotionData>(_emotions);
        }

        /// <summary>
        /// Установить значение эмоции
        /// </summary>
        public void SetEmotionValue(EmotionTypes type, float value, bool needSave = true)
        {
            var emotion = GetEmotion(type);

            if (emotion != null)
            {
                UpdateEmotionValue(type, value);

                if (needSave && _playerDataProvider != null)
                {
                    _playerDataProvider.Save();
                }
            }
        }

        /// <summary>
        /// Установить интенсивность эмоции
        /// </summary>
        public void SetEmotionIntensity(EmotionTypes type, float intensity, bool needSave = true)
        {
            var emotion = GetEmotion(type);

            if (emotion != null)
            {
                UpdateEmotionIntensity(type, intensity);

                if (needSave && _playerDataProvider != null)
                {
                    _playerDataProvider.Save();
                }
            }
        }

        /// <summary>
        /// Сбросить все эмоции к начальным значениям
        /// </summary>
        public void ResetAllEmotions(bool needSave = true)
        {
            foreach (var type in Enum.GetValues(typeof(EmotionTypes)).Cast<EmotionTypes>())
            {
                var emotion = GetEmotion(type);

                if (emotion != null)
                {
                    emotion.Value = 0f;
                    emotion.Intensity = 0f;
                    emotion.LastUpdate = DateTime.UtcNow;
                }
            }

            if (needSave && _playerDataProvider != null)
            {
                _playerDataProvider.Save();
            }

            MyLogger.Log("Все эмоции сброшены к начальным значениям", MyLogger.LogCategory.Emotion);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Синхронизирует эмоцию с Firebase
        /// </summary>
        private async void SyncEmotionWithFirebase(EmotionData emotion, EmotionEventType eventType)
        {
            MyLogger.Log($"📱➡️☁️ [SYNC-EMOTION] Начало синхронизации: Type={emotion.Type}, EventType={eventType}", MyLogger.LogCategory.Firebase);

            if (!_isFirebaseInitialized)
            {
                MyLogger.LogWarning($"❌ [SYNC-EMOTION] Firebase не инициализирован для {emotion.Type}", MyLogger.LogCategory.Firebase);
                return;
            }

            if (_databaseService == null)
            {
                MyLogger.LogWarning($"❌ [SYNC-EMOTION] DatabaseService не доступен для {emotion.Type}", MyLogger.LogCategory.Firebase);
                return;
            }

            if (!_databaseService.IsAuthenticated)
            {
                MyLogger.LogWarning($"❌ [SYNC-EMOTION] Пользователь не аутентифицирован для {emotion.Type}. UserID: {(_databaseService?.UserId ?? "NULL")}",
                    MyLogger.LogCategory.Firebase);

                return;
            }

            try
            {
                MyLogger.Log($"📝 [SYNC-EMOTION] Создаем запись для Firebase: Type={emotion.Type}, Value={emotion.Value}, Timestamp={emotion.LastUpdate:O}",
                    MyLogger.LogCategory.Firebase);

                // Создаем запись для истории эмоций в Firebase
                var record = new EmotionHistoryRecord(emotion, eventType);

                MyLogger.Log($"💾 [SYNC-EMOTION] Отправляем запись в Firebase: Id={record.Id}, Type={record.Type}, UserId={_databaseService.UserId}",
                    MyLogger.LogCategory.Firebase);

                // Отправляем на сервер
                await _databaseService.AddEmotionHistoryRecord(record);

                MyLogger.Log($"✅ [SYNC-EMOTION] Запись успешно добавлена в Firebase: Id={record.Id}", MyLogger.LogCategory.Firebase);

                // Обновляем текущую эмоцию в Firebase
                if (eventType == EmotionEventType.ValueChanged || eventType == EmotionEventType.IntensityChanged)
                {
                    MyLogger.Log($"🔄 [SYNC-EMOTION] Обновляем текущую эмоцию в Firebase: Type={emotion.Type}, Intensity={emotion.Intensity}",
                        MyLogger.LogCategory.Firebase);

                    await _databaseService.UpdateCurrentEmotion(emotion.Type, emotion.Intensity);
                    MyLogger.Log($"✅ [SYNC-EMOTION] Текущая эмоция обновлена в Firebase: Type={emotion.Type}", MyLogger.LogCategory.Firebase);
                }

                MyLogger.Log($"🎉 [SYNC-EMOTION] Эмоция {emotion.Type} полностью синхронизирована с Firebase", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [SYNC-EMOTION] Ошибка синхронизации эмоции {emotion.Type} с Firebase: {ex.Message}", MyLogger.LogCategory.Firebase);
                MyLogger.LogError($"❌ [SYNC-EMOTION] Stack trace: {ex.StackTrace}", MyLogger.LogCategory.Firebase);
            }
        }

        /// <summary>
        /// Синхронизирует эмоцию с Firebase используя предустановленный ID
        /// </summary>
        private async void SyncEmotionWithFirebaseById(EmotionData emotion, EmotionEventType eventType, string recordId)
        {
            MyLogger.Log($"📱➡️☁️ [SYNC-EMOTION-BY-ID] Начало синхронизации с ID: Type={emotion.Type}, EventType={eventType}, RecordId={recordId}",
                MyLogger.LogCategory.Firebase);

            if (!_isFirebaseInitialized)
            {
                MyLogger.LogWarning($"❌ [SYNC-EMOTION-BY-ID] Firebase не инициализирован для {emotion.Type}", MyLogger.LogCategory.Firebase);
                return;
            }

            if (_databaseService == null)
            {
                MyLogger.LogWarning($"❌ [SYNC-EMOTION-BY-ID] DatabaseService не доступен для {emotion.Type}", MyLogger.LogCategory.Firebase);
                return;
            }

            if (!_databaseService.IsAuthenticated)
            {
                MyLogger.LogWarning(
                    $"❌ [SYNC-EMOTION-BY-ID] Пользователь не аутентифицирован для {emotion.Type}. UserID: {(_databaseService?.UserId ?? "NULL")}",
                    MyLogger.LogCategory.Firebase);

                return;
            }

            try
            {
                MyLogger.Log(
                    $"📝 [SYNC-EMOTION-BY-ID] Создаем запись для Firebase: Type={emotion.Type}, Value={emotion.Value}, Timestamp={emotion.LastUpdate:O}, RecordId={recordId}",
                    MyLogger.LogCategory.Firebase);

                // Создаем запись для истории эмоций в Firebase с предустановленным ID
                var record = new EmotionHistoryRecord(emotion, eventType)
                {
                    Id = recordId // Используем переданный ID вместо генерации нового
                };

                MyLogger.Log($"💾 [SYNC-EMOTION-BY-ID] Отправляем запись в Firebase: Id={record.Id}, Type={record.Type}, UserId={_databaseService.UserId}",
                    MyLogger.LogCategory.Firebase);

                // Отправляем на сервер
                await _databaseService.AddEmotionHistoryRecord(record);

                MyLogger.Log($"✅ [SYNC-EMOTION-BY-ID] Запись успешно добавлена в Firebase: Id={record.Id}", MyLogger.LogCategory.Firebase);

                // Обновляем статус синхронизации в локальной истории
                _emotionHistory.UpdateSyncStatus(recordId, true);

                // Обновляем текущую эмоцию в Firebase
                if (eventType == EmotionEventType.ValueChanged || eventType == EmotionEventType.IntensityChanged)
                {
                    MyLogger.Log($"🔄 [SYNC-EMOTION-BY-ID] Обновляем текущую эмоцию в Firebase: Type={emotion.Type}, Intensity={emotion.Intensity}",
                        MyLogger.LogCategory.Firebase);

                    await _databaseService.UpdateCurrentEmotion(emotion.Type, emotion.Intensity);
                    MyLogger.Log($"✅ [SYNC-EMOTION-BY-ID] Текущая эмоция обновлена в Firebase: Type={emotion.Type}", MyLogger.LogCategory.Firebase);
                }

                MyLogger.Log($"🎉 [SYNC-EMOTION-BY-ID] Эмоция {emotion.Type} полностью синхронизирована с Firebase с ID {recordId}",
                    MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [SYNC-EMOTION-BY-ID] Ошибка синхронизации эмоции {emotion.Type} с Firebase: {ex.Message}", MyLogger.LogCategory.Firebase);
                MyLogger.LogError($"❌ [SYNC-EMOTION-BY-ID] Stack trace: {ex.StackTrace}", MyLogger.LogCategory.Firebase);
            }
        }

        #endregion

        // МЕТОД ДЛЯ ЛОГИРОВАНИЯ СОБЫТИЙ (ПЕРЕЗАПИСЬ ДЛЯ ГАРАНТИИ ПОРЯДКА АРГУМЕНТОВ)
        public async void LogEmotionEvent(EmotionTypes type, EmotionEventType eventType, string note = null)
        {
            var emotion = GetEmotion(type);

            if (emotion == null)
            {
                MyLogger.LogWarning($"[EmotionService.LogEmotionEvent] Emotion type '{type}' not found.", MyLogger.LogCategory.Emotion);
                return;
            }

            if (_emotionHistory == null)
            {
                MyLogger.LogError("[EmotionService.LogEmotionEvent] _emotionHistory is null. Cannot log event.", MyLogger.LogCategory.Emotion);
                return;
            }

            // Добавляем запись точно с настоящим временем
            DateTime now = DateTime.UtcNow;
            emotion.LastUpdate = now;

            // Получаем геолокацию, если доступна
            await TrySetLocationData(emotion);

            MyLogger.Log(
                $"[EmotionService.LogEmotionEvent] Добавляем запись: Type='{type}', EventType='{eventType}', Timestamp='{now:O}', RegionId='{emotion.RegionId}'",
                MyLogger.LogCategory.Emotion);

            // ИСПРАВЛЕНИЕ: Создаем одну запись с уникальным ID для локальной истории и Firebase
            string uniqueId = Guid.NewGuid().ToString();

            // Создаем EmotionHistoryEntry с предустановленным SyncId
            var entry = new EmotionHistoryEntry
            {
                EmotionData = emotion.Clone(),
                Timestamp = now,
                EventType = eventType,
                Note = note,
                SyncId = uniqueId, // Используем один ID для локальной записи и Firebase
                IsSynced = false
            };

            // Добавляем в локальную историю (без создания нового SyncId)
            _emotionHistory.AddEntryDirect(entry);

            MyLogger.Log(
                $"[EmotionService.LogEmotionEvent] Logged event: Type='{type}', EventType='{eventType}', Timestamp='{now:O}', SyncId='{uniqueId}'{(string.IsNullOrEmpty(note) ? "" : $", Note='{note}'")}",
                MyLogger.LogCategory.Emotion);

            // Обновляем региональную статистику, если доступна
            await TryUpdateRegionalStats(emotion);

            // Синхронизируем с Firebase, если возможно (используя тот же ID)
            if (_isFirebaseInitialized && _databaseService != null && _databaseService.IsAuthenticated)
            {
                MyLogger.Log($"[EmotionService.LogEmotionEvent] Синхронизируем с Firebase запись: Type='{type}', SyncId='{uniqueId}'",
                    MyLogger.LogCategory.Firebase);

                SyncEmotionWithFirebaseById(emotion, eventType, uniqueId);
            }
        }

        public void ClearHistory()
        {
            // Очищаем локальную историю
            _emotionHistory.Clear();

            // Очищаем кэш истории, чтобы при создании нового аккаунта старые данные не подтягивались
            if (_emotionHistoryCache != null)
            {
                _emotionHistoryCache.ClearCache();
                MyLogger.Log("✅ Кэш истории эмоций успешно очищен", MyLogger.LogCategory.Firebase);
            }
        }

        /// <summary>
        /// Очищает историю эмоций локально и в облаке
        /// </summary>
        /// <returns>True, если очистка выполнена успешно, иначе False</returns>
        public async Task<bool> ClearHistoryWithCloud()
        {
            try
            {
                MyLogger.Log("🗑️ [ClearHistoryWithCloud] Начинаем очистку истории локально и в облаке", MyLogger.LogCategory.ClearHistory);

                // Проверяем состояние Firebase сервисов
                MyLogger.Log(
                    $"🔍 [ClearHistoryWithCloud] Проверка состояния: _isFirebaseInitialized={_isFirebaseInitialized}, _databaseService!=null={_databaseService != null}, IsAuthenticated={_databaseService?.IsAuthenticated}, _syncService!=null={_syncService != null}",
                    MyLogger.LogCategory.ClearHistory);

                // Очищаем локальную историю
                _emotionHistory.Clear();
                MyLogger.Log("✅ [ClearHistoryWithCloud] Локальная история очищена", MyLogger.LogCategory.ClearHistory);

                // Очищаем кэш истории
                if (_emotionHistoryCache != null)
                {
                    _emotionHistoryCache.ClearCache();
                    MyLogger.Log("✅ [ClearHistoryWithCloud] Кэш истории эмоций успешно очищен", MyLogger.LogCategory.ClearHistory);
                }

                // Проверяем возможность очистки в облаке
                if (!_isFirebaseInitialized)
                {
                    MyLogger.LogWarning("⚠️ [ClearHistoryWithCloud] Firebase не инициализирован - невозможно очистить облачные данные",
                        MyLogger.LogCategory.ClearHistory);

                    return false;
                }

                if (_databaseService == null)
                {
                    MyLogger.LogWarning("⚠️ [ClearHistoryWithCloud] DatabaseService не инициализирован - невозможно очистить облачные данные",
                        MyLogger.LogCategory.ClearHistory);

                    return false;
                }

                if (!_databaseService.IsAuthenticated)
                {
                    MyLogger.LogWarning("⚠️ [ClearHistoryWithCloud] Пользователь не аутентифицирован - невозможно очистить облачные данные",
                        MyLogger.LogCategory.ClearHistory);

                    return false;
                }

                if (_syncService == null)
                {
                    MyLogger.LogWarning("⚠️ [ClearHistoryWithCloud] SyncService не инициализирован - невозможно очистить облачные данные",
                        MyLogger.LogCategory.ClearHistory);

                    return false;
                }

                // Очищаем данные в облаке
                MyLogger.Log("🔄 [ClearHistoryWithCloud] Все проверки пройдены, начинаем очистку данных в облаке...", MyLogger.LogCategory.ClearHistory);
                bool cloudClearResult = await _syncService.ClearCloudData();

                if (cloudClearResult)
                {
                    MyLogger.Log("✅ [ClearHistoryWithCloud] Облачные данные успешно очищены", MyLogger.LogCategory.ClearHistory);

                    // Принудительно синхронизируемся с Firebase, чтобы убедиться, что локальные данные соответствуют облачным
                    MyLogger.Log("🔄 [ClearHistoryWithCloud] Принудительная синхронизация с Firebase после очистки...", MyLogger.LogCategory.ClearHistory);
                    bool syncSuccess = await ForceSyncWithFirebase();

                    if (syncSuccess)
                    {
                        MyLogger.Log("✅ [ClearHistoryWithCloud] Принудительная синхронизация завершена успешно", MyLogger.LogCategory.ClearHistory);
                    }
                    else
                    {
                        MyLogger.LogWarning("⚠️ [ClearHistoryWithCloud] Принудительная синхронизация не удалась, но облачные данные очищены",
                            MyLogger.LogCategory.ClearHistory);
                    }

                    return true;
                }
                else
                {
                    MyLogger.LogError("❌ [ClearHistoryWithCloud] Локальная история очищена, но не удалось очистить облачные данные",
                        MyLogger.LogCategory.ClearHistory);

                    return false;
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [ClearHistoryWithCloud] Ошибка при очистке истории: {ex.Message}\nStackTrace: {ex.StackTrace}",
                    MyLogger.LogCategory.ClearHistory);

                return false;
            }
        }

        /// <summary>
        /// Проверяет, инициализирован ли Firebase
        /// </summary>
        public bool IsFirebaseInitialized
        {
            get
            {
                MyLogger.Log($"🔍 [EmotionService.IsFirebaseInitialized] Возвращаем: {_isFirebaseInitialized}", MyLogger.LogCategory.ClearHistory);
                return _isFirebaseInitialized;
            }
        }

        /// <summary>
        /// Проверяет, аутентифицирован ли пользователь в Firebase
        /// </summary>
        public bool IsAuthenticated
        {
            get
            {
                bool firebaseInit = _isFirebaseInitialized;
                bool dbServiceNotNull = _databaseService != null;
                bool dbAuthenticated = _databaseService?.IsAuthenticated ?? false;
                bool result = firebaseInit && dbServiceNotNull && dbAuthenticated;

                MyLogger.Log(
                    $"🔍 [EmotionService.IsAuthenticated] _isFirebaseInitialized={firebaseInit}, _databaseService!=null={dbServiceNotNull}, _databaseService.IsAuthenticated={dbAuthenticated}, result={result}",
                    MyLogger.LogCategory.ClearHistory);

                return result;
            }
        }

        #region Location and Regional Stats Integration

        /// <summary>
        /// Пытается установить данные местоположения для эмоции
        /// </summary>
        private async Task TrySetLocationData(EmotionData emotion)
        {
            try
            {
                if (_locationService == null)
                {
                    MyLogger.Log("🗺️ LocationService не доступен для определения местоположения", MyLogger.LogCategory.Location);
                    return;
                }

                var locationData = await _locationService.GetCurrentLocationAsync();

                if (locationData != null && locationData.IsValid)
                {
                    emotion.RegionId = locationData.RegionId;
                    MyLogger.Log($"🗺️ Установлен RegionId '{locationData.RegionId}' для эмоции '{emotion.Type}'", MyLogger.LogCategory.Location);
                }
                else
                {
                    MyLogger.Log("🗺️ Не удалось получить действительные данные местоположения", MyLogger.LogCategory.Location);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при получении местоположения: {ex.Message}", MyLogger.LogCategory.Location);
            }
        }

        /// <summary>
        /// Пытается обновить региональную статистику
        /// </summary>
        private async Task TryUpdateRegionalStats(EmotionData emotion)
        {
            try
            {
                if (_regionalStatsService == null)
                {
                    MyLogger.Log("📊 RegionalStatsService не доступен для обновления статистики", MyLogger.LogCategory.Regional);
                    return;
                }

                if (string.IsNullOrEmpty(emotion.RegionId))
                {
                    MyLogger.Log("📊 RegionId пуст, пропускаем обновление региональной статистики", MyLogger.LogCategory.Regional);
                    return;
                }

                // Получаем текущую статистику региона
                var currentStats = await _regionalStatsService.GetRegionalStats(emotion.RegionId);

                if (currentStats == null)
                {
                    // Создаем новую статистику для региона
                    currentStats = new RegionalEmotionStats();
                }

                // Парсим тип эмоции
                if (Enum.TryParse<EmotionTypes>(emotion.Type, out EmotionTypes emotionType))
                {
                    // Увеличиваем счетчик эмоции
                    if (currentStats.EmotionCounts.ContainsKey(emotionType))
                    {
                        currentStats.EmotionCounts[emotionType]++;
                    }
                    else
                    {
                        currentStats.EmotionCounts[emotionType] = 1;
                    }

                    // Пересчитываем общую статистику
                    currentStats.TotalEmotions++;

                    // Определяем доминирующую эмоцию
                    var dominantEmotion = currentStats.EmotionCounts
                        .OrderByDescending(kvp => kvp.Value)
                        .First();

                    currentStats.DominantEmotion = dominantEmotion.Key;
                    currentStats.DominantEmotionPercentage = (float)dominantEmotion.Value / currentStats.TotalEmotions * 100f;

                    // Обновляем статистику в сервисе
                    bool success = await _regionalStatsService.UpdateRegionalStats(emotion.RegionId, currentStats);

                    if (success)
                    {
                        MyLogger.Log(
                            $"📊 Региональная статистика обновлена для региона '{emotion.RegionId}': {emotionType} (+1), всего: {currentStats.TotalEmotions}",
                            MyLogger.LogCategory.Regional);
                    }
                    else
                    {
                        MyLogger.LogWarning($"⚠️ Не удалось обновить региональную статистику для региона '{emotion.RegionId}'", MyLogger.LogCategory.Regional);
                    }
                }
                else
                {
                    MyLogger.LogWarning($"⚠️ Не удалось распознать тип эмоции: '{emotion.Type}'", MyLogger.LogCategory.Regional);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при обновлении региональной статистики: {ex.Message}", MyLogger.LogCategory.Regional);
            }
        }

        #endregion

        public void Dispose()
        {
            // Пока что оставим пустым, если нечего освобождать.
            // В будущем здесь можно будет отписаться от событий, например:
            // if (_syncService != null)
            // {
            //     _syncService.OnSyncComplete -= HandleSyncComplete;
            //     _syncService.OnSyncProgress -= HandleSyncProgress;
            //     _syncService.OnRecordSynced -= HandleRecordSynced;
            //     _syncService.OnSyncConflict -= HandleSyncConflict;
            // }
            // MyLogger.Log("[EmotionService] Disposed.", MyLogger.LogCategory.Default);
        }
    }
}
