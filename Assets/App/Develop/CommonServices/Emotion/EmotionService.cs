using System;
using System.Collections.Generic;
using System.Linq;
using App.Develop.AppServices.Firebase.Database.Interfaces;
using App.Develop.AppServices.Firebase.Database.Models;
using App.Develop.AppServices.Firebase.Database.Services;
using App.Develop.CommonServices.DataManagement;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.ConfigsManagement;
using App.Develop.CommonServices.Networking;
using App.Develop.Configs.Common.Emotion;
using UnityEngine;
using App.Develop.AppServices.Firebase.Common.SecureStorage;

// Используем IDatabaseService только из пространства имен Services
using IDatabaseService = App.Develop.AppServices.Firebase.Database.Services.IDatabaseService;

namespace App.Develop.CommonServices.Emotion
{
    public class EmotionService : IDataReader<PlayerData>, IDataWriter<PlayerData>
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
        
        // Флаги синхронизации
        private bool _isInitialized;
        private bool _isFirebaseInitialized;
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
            EmotionConfigService emotionConfigService = null,
            ConnectivityManager connectivityManager = null)
        {
            _playerDataProvider = playerDataProvider ?? throw new ArgumentNullException(nameof(playerDataProvider));
            _emotions = new Dictionary<EmotionTypes, EmotionData>();
            _emotionMixingRules = new EmotionMixingRules();
            _emotionConfigs = new Dictionary<EmotionTypes, EmotionConfig>();
            _emotionConfigService = emotionConfigService;
            _connectivityManager = connectivityManager;
            
            // Создаем кэш для истории эмоций
            _emotionHistoryCache = new EmotionHistoryCache();
            _emotionHistory = new EmotionHistory(_emotionHistoryCache);

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
        /// Инициализирует компоненты для синхронизации с Firebase
        /// </summary>
        public void InitializeFirebaseSync(
            IDatabaseService databaseService, 
            EmotionSyncService syncService)
        {
            if (!_isInitialized)
            {
                Debug.LogError("EmotionService не инициализирован");
                return;
            }
            
            if (databaseService == null || syncService == null)
            {
                Debug.LogError("Невозможно инициализировать синхронизацию: отсутствуют необходимые зависимости");
                return;
            }
            
            _databaseService = databaseService;
            _syncService = syncService;
            
            // Инициализируем сервис синхронизации, если есть все необходимые компоненты
            if (_syncService != null && _emotionHistoryCache != null)
            {
                _syncService.Initialize(_databaseService, _emotionHistoryCache, _connectivityManager);
                
                // Подписываемся на события синхронизации
                _syncService.OnSyncComplete += HandleSyncComplete;
                _syncService.OnSyncProgress += HandleSyncProgress;
                _syncService.OnRecordSynced += HandleRecordSynced;
                _syncService.OnSyncConflict += HandleSyncConflict;
                
                _isFirebaseInitialized = true;
                Debug.Log("Синхронизация с Firebase инициализирована");
            }
            else
            {
                Debug.LogWarning("Невозможно инициализировать сервис синхронизации: отсутствуют необходимые компоненты");
            }
        }
        
        /// <summary>
        /// Запускает синхронизацию эмоций с Firebase
        /// </summary>
        public void StartSync()
        {
            if (!_isFirebaseInitialized || _syncService == null)
            {
                Debug.LogWarning("Синхронизация недоступна: Firebase не инициализирован");
                return;
            }
            
            _syncService.StartSync();
        }
        
        /// <summary>
        /// Обновляет настройки синхронизации
        /// </summary>
        public async void UpdateSyncSettings(EmotionSyncSettings settings)
        {
            if (!_isFirebaseInitialized || _syncService == null)
            {
                Debug.LogWarning("Синхронизация недоступна: Firebase не инициализирован");
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
                Debug.LogWarning("Резервное копирование недоступно: Firebase не инициализирован");
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
                Debug.LogWarning("Восстановление недоступно: Firebase не инициализирован");
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
                Debug.LogWarning("Обновление эмоций недоступно: Firebase не инициализирован");
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
                    
                    Debug.Log($"Обновлено {emotions.Count} эмоций из Firebase");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при обновлении эмоций из Firebase: {ex.Message}");
            }
        }
        
        #region Event Handlers
        
        /// <summary>
        /// Обрабатывает событие завершения синхронизации
        /// </summary>
        private void HandleSyncComplete(bool success, string message)
        {
            Debug.Log($"Синхронизация эмоций завершена. Успех: {success}. {message}");
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
            Debug.LogWarning($"Конфликт синхронизации записи {record.Id} типа {record.Type}");
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

            Debug.LogWarning($"⚠️ Эмоция {type} не найдена!");
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
                Debug.LogWarning("⚠️ EmotionData отсутствует при ReadFrom. Пропускаем.");
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
                    Debug.LogWarning($"⚠️ Emotion {type} не был загружен. Создаём по умолчанию.");
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
            var emotionsList = _playerDataProvider.GetEmotions();
            foreach (var emotionData in emotionsList)
            {
                if (Enum.TryParse(emotionData.Type, out EmotionTypes type))
                {
                    _emotions[type] = emotionData;
                }
            }
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
                var config = _emotionConfigService.LoadEmotionConfig(type);
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
            EmotionEventType eventType = oldValue < emotion.BubbleThreshold && value >= emotion.BubbleThreshold ? 
                EmotionEventType.CapacityExceeded : EmotionEventType.ValueChanged;
            
            // Добавляем в историю
            _emotionHistory.AddEntry(emotion, eventType, emotion.LastUpdate);
            
            // Синхронизируем с Firebase
            if (_isFirebaseInitialized && _databaseService != null)
            {
                SyncEmotionWithFirebase(emotion, eventType);
            }
            
            // Вызываем событие
            RaiseEmotionEvent(new EmotionEvent(type, eventType, emotion.Value, emotion.Intensity));
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
            emotion.LastUpdate = DateTime.UtcNow;
            _emotionHistory.AddEntry(emotion, EmotionEventType.IntensityChanged, emotion.LastUpdate);
            
            // Синхронизируем с Firebase
            if (_isFirebaseInitialized && _databaseService != null)
            {
                SyncEmotionWithFirebase(emotion, EmotionEventType.IntensityChanged);
            }
            
            RaiseEmotionEvent(new EmotionEvent(type, EmotionEventType.IntensityChanged, 
                emotion.Value, emotion.Intensity));
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
            return _emotionHistory.GetHistory(from, to);
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

        #region Private Methods
        /// <summary>
        /// Синхронизирует эмоцию с Firebase
        /// </summary>
        private async void SyncEmotionWithFirebase(EmotionData emotion, EmotionEventType eventType)
        {
            if (!_isFirebaseInitialized || _databaseService == null || !_databaseService.IsAuthenticated)
            {
                return;
            }
            
            try
            {
                // Создаем запись для истории эмоций в Firebase
                var record = new EmotionHistoryRecord(emotion, eventType);
                
                // Отправляем на сервер
                await _databaseService.AddEmotionHistoryRecord(record);
                
                // Обновляем текущую эмоцию в Firebase
                if (eventType == EmotionEventType.ValueChanged || eventType == EmotionEventType.IntensityChanged)
                {
                    await _databaseService.UpdateCurrentEmotion(emotion.Type, emotion.Intensity);
                }
                
                Debug.Log($"Эмоция {emotion.Type} синхронизирована с Firebase");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка синхронизации эмоции с Firebase: {ex.Message}");
            }
        }
        #endregion
    }
}