using System;
using System.Collections.Generic;
using System.Linq;
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
using App.Develop.DI;
using App.Develop.Utils.Logging;

// Используем IDatabaseService только из пространства имен Services
using IDatabaseService = App.Develop.CommonServices.Firebase.Database.Services.IDatabaseService;

namespace App.Develop.CommonServices.Emotion
{
    public class EmotionService : IDataReader<PlayerData>, IDataWriter<PlayerData>, IEmotionService, IInitializable
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
        
        #region Dependencies
        
        private readonly IConfigsProvider _configsProvider;
        private readonly IPointsService _pointsService;
        private readonly ILevelSystem _levelSystem;
        
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
            IPointsService pointsService = null,
            ILevelSystem levelSystem = null)
        {
            _playerDataProvider = playerDataProvider ?? throw new ArgumentNullException(nameof(playerDataProvider));
            _configsProvider = configsProvider;
            _emotionConfigService = emotionConfigService;
            _pointsService = pointsService;
            _levelSystem = levelSystem;
            _emotions = new Dictionary<EmotionTypes, EmotionData>();
            _emotionMixingRules = new EmotionMixingRules();
            _emotionConfigs = new Dictionary<EmotionTypes, EmotionConfig>();
            
            try
            {
                // Создаем кэш для истории эмоций с обработкой возможных ошибок
                _emotionHistoryCache = new EmotionHistoryCache();
                _emotionHistory = new EmotionHistory(_emotionHistoryCache);
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
        /// Инициализирует компоненты для синхронизации с Firebase
        /// </summary>
        public void InitializeFirebaseSync(
            IDatabaseService databaseService, 
            EmotionSyncService syncService)
        {
            if (!_isInitialized)
            {
                MyLogger.LogError("EmotionService не инициализирован", MyLogger.LogCategory.Bootstrap);
                return;
            }
            
            if (databaseService == null || syncService == null)
            {
                MyLogger.LogWarning("Невозможно инициализировать синхронизацию: отсутствуют необходимые зависимости", MyLogger.LogCategory.Firebase);
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
                MyLogger.Log("Синхронизация с Firebase инициализирована", MyLogger.LogCategory.Firebase);
            }
            else
            {
                MyLogger.LogWarning("Невозможно инициализировать сервис синхронизации: отсутствуют необходимые компоненты", MyLogger.LogCategory.Firebase);
            }
        }
        
        /// <summary>
        /// Запускает синхронизацию эмоций с Firebase
        /// </summary>
        public void StartSync()
        {
            if (!_isFirebaseInitialized || _syncService == null)
            {
                MyLogger.LogWarning("Синхронизация недоступна: Firebase не инициализирован", MyLogger.LogCategory.Firebase);
                return;
            }
            
            _syncService.StartSync();
        }
        
        /// <summary>
        /// Принудительно обновляет историю из Firebase (мягкое обновление - сохраняет локальные записи)
        /// </summary>
        public async System.Threading.Tasks.Task RefreshHistoryFromFirebase()
        {
            if (!_isFirebaseInitialized)
            {
                MyLogger.LogWarning("Обновление истории недоступно: Firebase не инициализирован", MyLogger.LogCategory.Firebase);
                return;
            }
            
            if (_databaseService == null)
            {
                MyLogger.LogWarning("Обновление истории недоступно: DatabaseService не инициализирован", MyLogger.LogCategory.Firebase);
                return;
            }
            
            if (_emotionHistoryCache == null)
            {
                MyLogger.LogWarning("Обновление истории недоступно: EmotionHistoryCache не инициализирован", MyLogger.LogCategory.Firebase);
                return;
            }
            
            if (!_databaseService.IsAuthenticated)
            {
                MyLogger.LogWarning("Обновление истории недоступно: пользователь не аутентифицирован", MyLogger.LogCategory.Firebase);
                return;
            }
            
            try
            {
                MyLogger.Log("Принудительное обновление истории эмоций из Firebase (мягкое)...", MyLogger.LogCategory.Firebase);
                
                // Проверяем сколько записей в кэше до обновления
                int beforeCount = _emotionHistoryCache.GetAllRecords().Count;
                MyLogger.Log($"Перед обновлением в кэше {beforeCount} записей", MyLogger.LogCategory.Firebase);
                
                // Обновляем кэш из Firebase (мягкое обновление)
                bool success = await _emotionHistoryCache.RefreshFromFirebase(_databaseService);
                
                if (success)
                {
                    // Проверяем сколько записей в кэше после обновления
                    int afterCount = _emotionHistoryCache.GetAllRecords().Count;
                    MyLogger.Log($"После обновления в кэше {afterCount} записей (изменение: {afterCount - beforeCount})", MyLogger.LogCategory.Firebase);
                    
                    // Сохраняем количество записей в истории до обновления
                    int historyCountBefore = 0;
                    if (_emotionHistory != null)
                    {
                        historyCountBefore = _emotionHistory.GetHistory().Count();
                        MyLogger.Log($"Перед обновлением в истории {historyCountBefore} записей", MyLogger.LogCategory.Firebase);
                    }
                    
                    // Перезагружаем историю из обновленного кэша
                    _emotionHistory.SetCache(_emotionHistoryCache);
                    
                    // Проверяем количество записей в истории после обновления
                    if (_emotionHistory != null)
                    {
                        int historyCountAfter = _emotionHistory.GetHistory().Count();
                        MyLogger.Log($"После обновления в истории {historyCountAfter} записей (изменение: {historyCountAfter - historyCountBefore})", MyLogger.LogCategory.Firebase);
                    }
                    
                    MyLogger.Log("История эмоций успешно обновлена из Firebase", MyLogger.LogCategory.Firebase);
                }
                else
                {
                    MyLogger.LogWarning("Не удалось обновить историю из Firebase: RefreshFromFirebase вернул false", MyLogger.LogCategory.Firebase);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при обновлении истории из Firebase: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }

        /// <summary>
        /// Полностью заменяет локальную историю данными из Firebase (жесткое обновление)
        /// </summary>
        public async System.Threading.Tasks.Task ReplaceHistoryFromFirebase()
        {
            if (!_isFirebaseInitialized)
            {
                MyLogger.LogWarning("Замена истории недоступна: Firebase не инициализирован", MyLogger.LogCategory.Firebase);
                return;
            }
            
            if (_databaseService == null)
            {
                MyLogger.LogWarning("Замена истории недоступна: DatabaseService не инициализирован", MyLogger.LogCategory.Firebase);
                return;
            }
            
            if (_emotionHistoryCache == null)
            {
                MyLogger.LogWarning("Замена истории недоступна: EmotionHistoryCache не инициализирован", MyLogger.LogCategory.Firebase);
                return;
            }
            
            if (!_databaseService.IsAuthenticated)
            {
                MyLogger.LogWarning("Замена истории недоступна: пользователь не аутентифицирован", MyLogger.LogCategory.Firebase);
                return;
            }
            
            try
            {
                MyLogger.Log("�� Полная замена локальной истории данными из Firebase...", MyLogger.LogCategory.Firebase);
                
                // Проверяем сколько записей в кэше до замены
                int beforeCount = _emotionHistoryCache.GetAllRecords().Count;
                MyLogger.Log($"Перед заменой в кэше {beforeCount} записей", MyLogger.LogCategory.Firebase);
                
                // Полностью заменяем кэш данными из Firebase
                bool success = await _emotionHistoryCache.ReplaceFromFirebase(_databaseService);
                
                if (success)
                {
                    // Проверяем сколько записей в кэше после замены
                    int afterCount = _emotionHistoryCache.GetAllRecords().Count;
                    MyLogger.Log($"После замены в кэше {afterCount} записей", MyLogger.LogCategory.Firebase);
                    
                    // Сохраняем количество записей в истории до обновления
                    int historyCountBefore = 0;
                    if (_emotionHistory != null)
                    {
                        historyCountBefore = _emotionHistory.GetHistory().Count();
                        MyLogger.Log($"Перед обновлением в истории {historyCountBefore} записей", MyLogger.LogCategory.Firebase);
                    }
                    
                    // Перезагружаем историю из обновленного кэша
                    _emotionHistory.SetCache(_emotionHistoryCache);
                    
                    // Проверяем количество записей в истории после обновления
                    if (_emotionHistory != null)
                    {
                        int historyCountAfter = _emotionHistory.GetHistory().Count();
                        MyLogger.Log($"После обновления в истории {historyCountAfter} записей", MyLogger.LogCategory.Firebase);
                    }
                    
                    MyLogger.Log("✅ Локальная история полностью заменена данными из Firebase", MyLogger.LogCategory.Firebase);
                }
                else
                {
                    MyLogger.LogWarning("❌ Не удалось заменить историю данными из Firebase: ReplaceFromFirebase вернул false", MyLogger.LogCategory.Firebase);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при замене истории данными из Firebase: {ex.Message}", MyLogger.LogCategory.Firebase);
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
            EmotionEventType eventType = oldValue < emotion.BubbleThreshold && value >= emotion.BubbleThreshold ? 
                EmotionEventType.CapacityExceeded : EmotionEventType.ValueChanged;
            
            MyLogger.Log($"[EmotionService] Before AddEntry in UpdateEmotionValue: EmotionType='{emotion.Type}', LastUpdate='{emotion.LastUpdate}', CurrentTime='{DateTime.Now}'", MyLogger.LogCategory.Emotion);
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

            MyLogger.Log($"[EmotionService] Before AddEntry in UpdateEmotionIntensity: EmotionType='{emotion.Type}', LastUpdate='{emotion.LastUpdate}', CurrentTime='{DateTime.Now}'", MyLogger.LogCategory.Emotion);
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
                        MyLogger.Log($"[EmotionService] Before AddEntry in TryMixEmotions: EmotionType='{resultEmotion.Type}', LastUpdate='{resultEmotion.LastUpdate}', CurrentTime='{DateTime.Now}'", MyLogger.LogCategory.Emotion);
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
            MyLogger.Log($"🔄 [SyncEmotionWithFirebase] Начало синхронизации: Type={emotion.Type}, EventType={eventType}", MyLogger.LogCategory.Firebase);
            
            if (!_isFirebaseInitialized)
            {
                MyLogger.LogWarning($"❌ [SyncEmotionWithFirebase] Firebase не инициализирован для {emotion.Type}", MyLogger.LogCategory.Firebase);
                return;
            }
            
            if (_databaseService == null)
            {
                MyLogger.LogWarning($"❌ [SyncEmotionWithFirebase] DatabaseService не доступен для {emotion.Type}", MyLogger.LogCategory.Firebase);
                return;
            }
            
            if (!_databaseService.IsAuthenticated)
            {
                MyLogger.LogWarning($"❌ [SyncEmotionWithFirebase] Пользователь не аутентифицирован для {emotion.Type}", MyLogger.LogCategory.Firebase);
                return;
            }
            
            try
            {
                MyLogger.Log($"📝 [SyncEmotionWithFirebase] Создаем запись для Firebase: Type={emotion.Type}, Value={emotion.Value}, Timestamp={emotion.LastUpdate:O}", MyLogger.LogCategory.Firebase);
                
                // Создаем запись для истории эмоций в Firebase
                var record = new EmotionHistoryRecord(emotion, eventType);
                
                MyLogger.Log($"💾 [SyncEmotionWithFirebase] Отправляем запись в Firebase: Id={record.Id}, Type={record.Type}", MyLogger.LogCategory.Firebase);
                
                // Отправляем на сервер
                await _databaseService.AddEmotionHistoryRecord(record);
                
                MyLogger.Log($"✅ [SyncEmotionWithFirebase] Запись успешно добавлена в Firebase: Id={record.Id}", MyLogger.LogCategory.Firebase);
                
                // Обновляем текущую эмоцию в Firebase
                if (eventType == EmotionEventType.ValueChanged || eventType == EmotionEventType.IntensityChanged)
                {
                    MyLogger.Log($"🔄 [SyncEmotionWithFirebase] Обновляем текущую эмоцию в Firebase: Type={emotion.Type}, Intensity={emotion.Intensity}", MyLogger.LogCategory.Firebase);
                    await _databaseService.UpdateCurrentEmotion(emotion.Type, emotion.Intensity);
                    MyLogger.Log($"✅ [SyncEmotionWithFirebase] Текущая эмоция обновлена в Firebase: Type={emotion.Type}", MyLogger.LogCategory.Firebase);
                }
                
                MyLogger.Log($"🎉 [SyncEmotionWithFirebase] Эмоция {emotion.Type} полностью синхронизирована с Firebase", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [SyncEmotionWithFirebase] Ошибка синхронизации эмоции {emotion.Type} с Firebase: {ex.Message}", MyLogger.LogCategory.Firebase);
                MyLogger.LogError($"❌ [SyncEmotionWithFirebase] Stack trace: {ex.StackTrace}", MyLogger.LogCategory.Firebase);
            }
        }
        #endregion

        // МЕТОД ДЛЯ ЛОГИРОВАНИЯ СОБЫТИЙ (ПЕРЕЗАПИСЬ ДЛЯ ГАРАНТИИ ПОРЯДКА АРГУМЕНТОВ)
        public void LogEmotionEvent(EmotionTypes type, EmotionEventType eventType, string note = null)
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
            
            MyLogger.Log($"[EmotionService.LogEmotionEvent] Добавляем запись: Type='{type}', EventType='{eventType}', Timestamp='{now:O}'", MyLogger.LogCategory.Emotion);
            
            // Убедимся в правильном порядке: emotion, eventType, DateTime.Now, note
            _emotionHistory.AddEntry(emotion, eventType, now, note); 
            
            MyLogger.Log($"[EmotionService.LogEmotionEvent] Logged event: Type='{type}', EventType='{eventType}', Timestamp='{now:O}'{(string.IsNullOrEmpty(note) ? "" : $", Note='{note}'")}", MyLogger.LogCategory.Emotion);
            
            // Синхронизируем с Firebase, если возможно
            if (_isFirebaseInitialized && _databaseService != null && _databaseService.IsAuthenticated)
            {
                MyLogger.Log($"[EmotionService.LogEmotionEvent] Синхронизируем с Firebase запись: Type='{type}'", MyLogger.LogCategory.Firebase);
                SyncEmotionWithFirebase(emotion, eventType);
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
        /// Проверяет, инициализирован ли Firebase
        /// </summary>
        public bool IsFirebaseInitialized => _isFirebaseInitialized;
        
        /// <summary>
        /// Проверяет, аутентифицирован ли пользователь в Firebase
        /// </summary>
        public bool IsAuthenticated => _isFirebaseInitialized && _databaseService != null && _databaseService.IsAuthenticated;
    }
}