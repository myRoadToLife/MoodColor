using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Develop.CommonServices.ConfigsManagement;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.Firebase.Database.Services;
using App.Develop.CommonServices.Firebase.Database.Models;
using UnityEngine;
using App.Develop.Utils.Logging;
using System.Diagnostics;

namespace App.Develop.CommonServices.DataManagement.DataProviders
{
    public class PlayerDataProvider : DataProvider<PlayerData>
    {
        private readonly IConfigsProvider _configsProvider;
        private readonly IDatabaseService _databaseService;
        private PlayerData _cachedData;

        // Добавляем приватные поля для мониторинга производительности
        private readonly Dictionary<string, Stopwatch> _operationTimers = new Dictionary<string, Stopwatch>();

        public PlayerDataProvider(ISaveLoadService saveLoadService,
            IConfigsProvider configsProvider,
            IDatabaseService databaseService) : base(saveLoadService)
        {
            _configsProvider = configsProvider ?? throw new ArgumentNullException(nameof(configsProvider));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        protected override PlayerData GetOriginData()
        {
            try
            {
                var data = new PlayerData
                {
                    EmotionData = InitEmotionData()
                };
                _cachedData = data; // Кэшируем данные
                return data;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при создании начальных данных: {ex.Message}", MyLogger.LogCategory.Default);
                var data = new PlayerData
                {
                    EmotionData = new Dictionary<EmotionTypes, EmotionData>()
                };
                _cachedData = data; // Кэшируем данные
                return data;
            }
        }
        
        public PlayerData GetData()
        {
            if (_cachedData == null)
            {
                MyLogger.LogWarning("[PlayerDataProvider] GetData() вызван, но _cachedData is null. Создаем и возвращаем данные по умолчанию.");
                _cachedData = GetOriginData(); // Инициализируем кэш данными по умолчанию
                // Также обновляем базовое свойство Data
                this.Data = _cachedData;
            }
            return _cachedData;
        }

        public List<EmotionData> GetEmotions()
        {
            if (_cachedData == null)
            {
                MyLogger.LogWarning("[PlayerDataProvider] GetEmotions() вызван, но _cachedData is null. Создаем данные по умолчанию.");
                _cachedData = GetOriginData();
                this.Data = _cachedData;
            }

            var result = new List<EmotionData>();
            if (_cachedData.EmotionData != null)
            {
                foreach (var pair in _cachedData.EmotionData)
                {
                    if (pair.Value != null)
                    {
                        result.Add(pair.Value);
                    }
                }
            }
            else
            {
                MyLogger.LogWarning("⚠️ _cachedData.EmotionData is null в PlayerDataProvider.GetEmotions! Инициализируем заново.");
                _cachedData.EmotionData = InitEmotionData();
                
                // Добавляем новые данные в результат
                foreach (var pair in _cachedData.EmotionData)
                {
                    if (pair.Value != null)
                    {
                        result.Add(pair.Value);
                    }
                }
            }
            return result;
        }

        private Dictionary<EmotionTypes, EmotionData> InitEmotionData()
        {
            var emotionData = new Dictionary<EmotionTypes, EmotionData>();
            try
            {
                if (_configsProvider == null) throw new NullReferenceException("ConfigsProvider is null");
                
                // Проверка на null и логирование предупреждения
                if (_configsProvider.StartEmotionConfig == null)
                {
                    MyLogger.LogWarning("⚠️ StartEmotionConfig is null. Создаем дефолтные эмоции с нулевыми значениями.", MyLogger.LogCategory.Default);
                    
                    // Создаем дефолтные эмоции с нулевыми значениями и белым цветом
                    foreach (EmotionTypes emotionType in Enum.GetValues(typeof(EmotionTypes)))
                    {
                        emotionData.Add(emotionType, new EmotionData { 
                            Type = emotionType.ToString(), 
                            Value = 0, 
                            Intensity = 0, 
                            Color = GetDefaultColorForEmotion(emotionType) 
                        });
                    }
                    
                    return emotionData;
                }

                foreach (EmotionTypes emotionType in Enum.GetValues(typeof(EmotionTypes)))
                {
                    try
                    {
                        var (value, color) = _configsProvider.StartEmotionConfig.GetStartValueFor(emotionType);
                        emotionData.Add(emotionType, new EmotionData { Type = emotionType.ToString(), Value = value, Intensity = 0, Color = color });
                    }
                    catch (Exception ex_inner)
                    {
                        MyLogger.LogError($"❌ Ошибка при добавлении эмоции {emotionType}: {ex_inner.Message}", MyLogger.LogCategory.Default);
                        emotionData.Add(emotionType, new EmotionData { 
                            Type = emotionType.ToString(), 
                            Value = 0, 
                            Intensity = 0, 
                            Color = GetDefaultColorForEmotion(emotionType) 
                        });
                    }
                }
            }
            catch (Exception ex_outer)
            {
                MyLogger.LogError($"❌ Критическая ошибка в InitEmotionData: {ex_outer.Message}. Создаем дефолтные эмоции.", MyLogger.LogCategory.Default);
                emotionData.Clear(); // Очищаем, если что-то успело добавиться до критической ошибки
                foreach (EmotionTypes emotionType in Enum.GetValues(typeof(EmotionTypes)))
                {
                    emotionData.Add(emotionType, new EmotionData { 
                        Type = emotionType.ToString(), 
                        Value = 0, 
                        Intensity = 0, 
                        Color = GetDefaultColorForEmotion(emotionType) 
                    });
                }
            }
            return emotionData;
        }
        
        // Метод для получения дефолтного цвета для эмоции
        private Color GetDefaultColorForEmotion(EmotionTypes type)
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
                EmotionTypes.Neutral => new Color(0.9f, 0.9f, 0.9f), // Светло-серый
                _ => Color.white
            };
        }

        // Добавление нового метода для слияния GameData
        private void MergeGameData(GameData target, GameData source)
        {
            if (target == null || source == null)
                return;
            
            // Точки - выбираем большее значение
            target.Points = Math.Max(target.Points, source.Points);
            
            // Обновляем время последнего обновления
            target.LastUpdated = DateTime.UtcNow;
            
            // Здесь можно добавить слияние других полей GameData
        }

        // Добавление нового метода для слияния EmotionData
        private void MergeEmotionData(Dictionary<EmotionTypes, EmotionData> target, Dictionary<EmotionTypes, EmotionData> source)
        {
            if (target == null || source == null)
                return;
            
            foreach (var kvp in source)
            {
                if (!target.ContainsKey(kvp.Key))
                {
                    // Если эмоция отсутствует в целевом словаре, добавляем её
                    target[kvp.Key] = kvp.Value.Clone();
                }
                else
                {
                    // Если эмоция уже есть, объединяем данные (берем большее значение)
                    target[kvp.Key].Value = Math.Max(target[kvp.Key].Value, kvp.Value.Value);
                    target[kvp.Key].Intensity = Math.Max(target[kvp.Key].Intensity, kvp.Value.Intensity);
                    
                    // Обновляем время последнего обновления
                    target[kvp.Key].LastUpdated = DateTime.UtcNow;
                }
            }
        }

        // Модификация метода Load для использования стратегии разрешения конфликтов
        public new async Task Load()
        {
            MyLogger.Log("[PlayerDataProvider] Начало асинхронной загрузки PlayerData.", MyLogger.LogCategory.Default);
            
            // Сначала инициализируем _cachedData, если его ещё нет, чтобы избежать null-ошибок
            if (_cachedData == null)
            {
                _cachedData = GetOriginData();
                MyLogger.Log("[PlayerDataProvider] Инициализирован _cachedData до загрузки локальных данных.", MyLogger.LogCategory.Default);
            }
            
            // Сохраняем ссылку на локальные данные
            PlayerData localPlayerData = null;
            
            try
            {
                // Вызываем базовый метод Load() для загрузки локальных данных
                base.Load();
                
                // Сразу после базовой загрузки сохраняем ссылку на Data в _cachedData
                _cachedData = this.Data;
                MyLogger.Log("[PlayerDataProvider] Обновлен _cachedData из базового Data после base.Load().", MyLogger.LogCategory.Default);
                
                // Теперь GetData должен работать без ошибок
                localPlayerData = GetData();
                
                if (localPlayerData != null)
                {
                    MyLogger.Log("[PlayerDataProvider] PlayerData успешно загружено из локального хранилища.", MyLogger.LogCategory.Default);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"[PlayerDataProvider] Ошибка при загрузке локальных данных: {ex.Message}", MyLogger.LogCategory.Default);
                // Убеждаемся, что _cachedData не null даже при ошибке
                if (_cachedData == null)
                {
                    _cachedData = GetOriginData();
                    localPlayerData = _cachedData;
                }
            }

            PlayerData cloudPlayerData = null;
            bool cloudGameDataLoaded = false;
            bool cloudEmotionDataLoaded = false;

            if (_databaseService != null && _databaseService.IsAuthenticated)
            {
                MyLogger.Log("[PlayerDataProvider] Попытка загрузки данных из Firebase...", MyLogger.LogCategory.Default);
                try
                {
                    GameData gameDataCloud = await RetryOperation(async () => await _databaseService.LoadUserGameData());
                    Dictionary<string, EmotionData> emotionsStringKeyCloud = await RetryOperation(async () => await _databaseService.GetUserEmotions());
                    Dictionary<EmotionTypes, EmotionData> emotionDataCloud = null;

                    if (emotionsStringKeyCloud != null)
                    {
                        emotionDataCloud = emotionsStringKeyCloud
                            .Where(kvp => Enum.TryParse<EmotionTypes>(kvp.Key, out _))
                            .ToDictionary(kvp => Enum.Parse<EmotionTypes>(kvp.Key), kvp => kvp.Value);
                    }

                    if (gameDataCloud != null || emotionDataCloud != null)
                    {
                        cloudPlayerData = new PlayerData();
                        if (gameDataCloud != null)
                        {
                            cloudPlayerData.GameData = gameDataCloud;
                            cloudGameDataLoaded = true;
                            MyLogger.Log("[PlayerDataProvider] GameData успешно загружено из Firebase.", MyLogger.LogCategory.Default);
                        }
                        if (emotionDataCloud != null)
                        {
                            cloudPlayerData.EmotionData = emotionDataCloud;
                            cloudEmotionDataLoaded = true;
                            MyLogger.Log("[PlayerDataProvider] EmotionData успешно загружено из Firebase.", MyLogger.LogCategory.Default);
                        }
                    }
                    else
                    {
                        MyLogger.LogWarning("[PlayerDataProvider] Данные из Firebase не найдены или пусты.", MyLogger.LogCategory.Default);
                    }
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"[PlayerDataProvider] Ошибка при загрузке данных из Firebase: {ex.Message}.", MyLogger.LogCategory.Default);
                }
            }
            else
            {
                MyLogger.Log("[PlayerDataProvider] Firebase недоступен или пользователь не аутентифицирован. Загрузка только локальных данных.", MyLogger.LogCategory.Default);
            }

            // Убеждаемся, что у нас есть какие-то данные перед слиянием
            if (_cachedData == null)
            {
                _cachedData = GetOriginData();
                MyLogger.Log("[PlayerDataProvider] Создан _cachedData по умолчанию перед слиянием данных.", MyLogger.LogCategory.Default);
            }

            // Получаем настройки синхронизации, если возможно
            ConflictResolutionStrategy conflictStrategy = 
                ConflictResolutionStrategy.ServerWins; // По умолчанию

            if (_databaseService != null && _databaseService.IsAuthenticated)
            {
                try
                {
                    var syncSettings = await _databaseService.GetSyncSettings();
                    if (syncSettings != null)
                    {
                        conflictStrategy = syncSettings.ConflictStrategy;
                        MyLogger.Log($"[PlayerDataProvider] Получена стратегия разрешения конфликтов из настроек синхронизации: {conflictStrategy}", MyLogger.LogCategory.Default);
                    }
                }
                catch (Exception ex)
                {
                    MyLogger.LogWarning($"[PlayerDataProvider] Не удалось получить настройки синхронизации: {ex.Message}", MyLogger.LogCategory.Default);
                }
            }

            // Слияние данных с учетом стратегии разрешения конфликтов
            if (localPlayerData != null)
            {
                _cachedData = localPlayerData;
            }

            if (cloudPlayerData != null)
            {
                switch (conflictStrategy)
                {
                    case ConflictResolutionStrategy.ServerWins:
                        // Облачные данные имеют приоритет (текущая реализация)
                        if (cloudGameDataLoaded && cloudPlayerData.GameData != null)
                        {
                            _cachedData.GameData = cloudPlayerData.GameData;
                            MyLogger.Log("[PlayerDataProvider] GameData обновлено из облака (стратегия: ServerWins).", MyLogger.LogCategory.Default);
                        }
                        if (cloudEmotionDataLoaded && cloudPlayerData.EmotionData != null)
                        {
                            _cachedData.EmotionData = cloudPlayerData.EmotionData;
                            MyLogger.Log("[PlayerDataProvider] EmotionData обновлено из облака (стратегия: ServerWins).", MyLogger.LogCategory.Default);
                        }
                        break;
                        
                    case ConflictResolutionStrategy.ClientWins:
                        // Локальные данные имеют приоритет
                        // Облачные данные применяются только если локальных нет
                        if (cloudGameDataLoaded && cloudPlayerData.GameData != null && _cachedData.GameData == null)
                        {
                            _cachedData.GameData = cloudPlayerData.GameData;
                            MyLogger.Log("[PlayerDataProvider] GameData обновлено из облака (стратегия: ClientWins, локальные данные отсутствовали).", MyLogger.LogCategory.Default);
                        }
                        if (cloudEmotionDataLoaded && cloudPlayerData.EmotionData != null && _cachedData.EmotionData == null)
                        {
                            _cachedData.EmotionData = cloudPlayerData.EmotionData;
                            MyLogger.Log("[PlayerDataProvider] EmotionData обновлено из облака (стратегия: ClientWins, локальные данные отсутствовали).", MyLogger.LogCategory.Default);
                        }
                        break;
                        
                    case ConflictResolutionStrategy.MostRecent:
                        // Используем самые свежие данные
                        if (cloudGameDataLoaded && cloudPlayerData.GameData != null)
                        {
                            if (_cachedData.GameData == null || 
                                (cloudPlayerData.GameData.LastUpdated > _cachedData.GameData.LastUpdated))
                            {
                                _cachedData.GameData = cloudPlayerData.GameData;
                                MyLogger.Log("[PlayerDataProvider] GameData обновлено из облака (стратегия: MostRecent).", MyLogger.LogCategory.Default);
                            }
                        }
                        
                        if (cloudEmotionDataLoaded && cloudPlayerData.EmotionData != null)
                        {
                            if (_cachedData.EmotionData == null)
                            {
                                _cachedData.EmotionData = cloudPlayerData.EmotionData;
                                MyLogger.Log("[PlayerDataProvider] EmotionData обновлено из облака (стратегия: MostRecent, локальные данные отсутствовали).", MyLogger.LogCategory.Default);
                            }
                            else
                            {
                                // Для каждой эмоции отдельно сравниваем время обновления
                                foreach (var kvp in cloudPlayerData.EmotionData)
                                {
                                    if (!_cachedData.EmotionData.ContainsKey(kvp.Key) ||
                                        (kvp.Value.LastUpdated > _cachedData.EmotionData[kvp.Key].LastUpdated))
                                    {
                                        _cachedData.EmotionData[kvp.Key] = kvp.Value;
                                        MyLogger.Log($"[PlayerDataProvider] Эмоция {kvp.Key} обновлена из облака (стратегия: MostRecent).", MyLogger.LogCategory.Default);
                                    }
                                }
                            }
                        }
                        break;
                        
                    case ConflictResolutionStrategy.Merge:
                        // Слияние данных
                        if (cloudGameDataLoaded && cloudPlayerData.GameData != null)
                        {
                            MergeGameData(_cachedData.GameData, cloudPlayerData.GameData);
                            MyLogger.Log("[PlayerDataProvider] GameData слито с облачными данными (стратегия: Merge).", MyLogger.LogCategory.Default);
                        }
                        
                        if (cloudEmotionDataLoaded && cloudPlayerData.EmotionData != null)
                        {
                            MergeEmotionData(_cachedData.EmotionData, cloudPlayerData.EmotionData);
                            MyLogger.Log("[PlayerDataProvider] EmotionData слито с облачными данными (стратегия: Merge).", MyLogger.LogCategory.Default);
                        }
                        break;
                        
                    default:
                        // По умолчанию - ServerWins
                        if (cloudGameDataLoaded && cloudPlayerData.GameData != null)
                        {
                            _cachedData.GameData = cloudPlayerData.GameData;
                            MyLogger.Log("[PlayerDataProvider] GameData обновлено из облака (стратегия по умолчанию).", MyLogger.LogCategory.Default);
                        }
                        if (cloudEmotionDataLoaded && cloudPlayerData.EmotionData != null)
                        {
                            _cachedData.EmotionData = cloudPlayerData.EmotionData;
                            MyLogger.Log("[PlayerDataProvider] EmotionData обновлено из облака (стратегия по умолчанию).", MyLogger.LogCategory.Default);
                        }
                        break;
                }
            }

            // Гарантируем, что вложенные объекты не null
            if (_cachedData.GameData == null)
            {
                _cachedData.GameData = new GameData();
                MyLogger.LogWarning("[PlayerDataProvider] GameData было null, инициализировано по умолчанию.", MyLogger.LogCategory.Default);
            }
            if (_cachedData.EmotionData == null)
            {
                _cachedData.EmotionData = InitEmotionData();
                MyLogger.LogWarning("[PlayerDataProvider] EmotionData было null, инициализировано по умолчанию.", MyLogger.LogCategory.Default);
            }

            // Обновляем Data, чтобы оно соответствовало _cachedData
            this.Data = _cachedData;

            // Уведомляем всех читателей об обновленных данных
            foreach (IDataReader<PlayerData> reader in GetDataReaders())
            {
                reader.ReadFrom(_cachedData);
            }
            MyLogger.Log("[PlayerDataProvider] Загрузка данных PlayerData завершена. Данные переданы читателям.", MyLogger.LogCategory.Default);
        }

        // Метод для выполнения операции с повторными попытками при сбоях
        private async Task<T> RetryOperation<T>(Func<Task<T>> operation, int maxRetries = 3, int retryDelayMs = 1000)
        {
            int retryCount = 0;
            Exception lastException = null;
            
            while (retryCount < maxRetries)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    retryCount++;
                    
                    MyLogger.LogWarning($"[PlayerDataProvider] Попытка {retryCount}/{maxRetries} не удалась: {ex.Message}", MyLogger.LogCategory.Default);
                    
                    if (retryCount < maxRetries)
                    {
                        // Экспоненциальное увеличение задержки между попытками
                        int delay = retryDelayMs * (int)Math.Pow(2, retryCount - 1);
                        await Task.Delay(delay);
                    }
                }
            }
            
            throw new AggregateException($"Операция не удалась после {maxRetries} попыток", lastException);
        }

        // Метод для начала замера времени операции
        private void StartTiming(string operationName)
        {
            var sw = new Stopwatch();
            sw.Start();
            _operationTimers[operationName] = sw;
        }

        // Метод для окончания замера времени операции
        private TimeSpan StopTiming(string operationName)
        {
            if (_operationTimers.TryGetValue(operationName, out var sw))
            {
                sw.Stop();
                _operationTimers.Remove(operationName);
                return sw.Elapsed;
            }
            return TimeSpan.Zero;
        }

        // Модификация метода Save для использования мониторинга производительности
        public new async Task Save()
        {
            StartTiming("TotalSave");
            
            MyLogger.Log("[PlayerDataProvider] Save() вызван.", MyLogger.LogCategory.Default); 
            if (_cachedData == null)
            {
                MyLogger.LogWarning("[PlayerDataProvider] _cachedData is null in Save(). Попытка загрузить/создать.", MyLogger.LogCategory.Default);
                await Load(); 
                if (_cachedData == null) 
                {
                     MyLogger.LogError("[PlayerDataProvider] _cachedData все еще null после Load() в Save(). Нечего сохранять.", MyLogger.LogCategory.Default);
                     return;
                }
            }
            if (_cachedData.GameData == null)
            {
                 MyLogger.LogWarning("[PlayerDataProvider] _cachedData.GameData is null in Save(). Инициализируем новым GameData().", MyLogger.LogCategory.Default);
                _cachedData.GameData = new GameData();
            }

            // Локальное сохранение с обработкой ошибок
            StartTiming("LocalSave");
            try
            {
                MyLogger.Log("[PlayerDataProvider] Попытка локального сохранения (base.Save()).", MyLogger.LogCategory.Default); 
                base.Save(); 
                var localSaveTime = StopTiming("LocalSave");
                MyLogger.Log($"[PlayerDataProvider] Локальное сохранение (base.Save()) завершено успешно за {localSaveTime.TotalMilliseconds}мс.", MyLogger.LogCategory.Default); 
            }
            catch (Exception ex)
            {
                StopTiming("LocalSave");
                MyLogger.LogError($"[PlayerDataProvider] Ошибка при локальном сохранении: {ex.Message}", MyLogger.LogCategory.Default);
                // Продолжаем работу, попробуем сохранить хотя бы в облако
            }

            if (_databaseService != null && _databaseService.IsAuthenticated)
            {
                try
                {
                    // Сохранение GameData с повторными попытками
                    StartTiming("CloudSaveGameData");
                    MyLogger.Log($"[PlayerDataProvider] Пытаемся сохранить GameData в облако. Текущие очки для сохранения: {_cachedData.GameData.Points}", MyLogger.LogCategory.Default);
                    await RetryOperation(async () => {
                        await _databaseService.SaveUserGameData(_cachedData.GameData);
                        return true;
                    });
                    var gameDataSaveTime = StopTiming("CloudSaveGameData");
                    MyLogger.Log($"[PlayerDataProvider] GameData успешно сохранено в облако за {gameDataSaveTime.TotalMilliseconds}мс.", MyLogger.LogCategory.Default);
                    
                    // Добавляем сохранение EmotionData с повторными попытками
                    if (_cachedData.EmotionData != null)
                    {
                        StartTiming("CloudSaveEmotionData");
                        MyLogger.Log("[PlayerDataProvider] Пытаемся сохранить EmotionData в облако.", MyLogger.LogCategory.Default);
                        Dictionary<string, EmotionData> emotionDataDict = _cachedData.EmotionData
                            .ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
                        
                        await RetryOperation(async () => {
                            await _databaseService.UpdateUserEmotions(emotionDataDict);
                            return true;
                        });
                        
                        var emotionDataSaveTime = StopTiming("CloudSaveEmotionData");
                        MyLogger.Log($"[PlayerDataProvider] EmotionData успешно сохранено в облако за {emotionDataSaveTime.TotalMilliseconds}мс.", MyLogger.LogCategory.Default);
                    }
                    else
                    {
                        MyLogger.LogWarning("[PlayerDataProvider] EmotionData is null, не сохраняем в облако.", MyLogger.LogCategory.Default);
                    }
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"[PlayerDataProvider] Ошибка при сохранении данных в облако: {ex.Message}", MyLogger.LogCategory.Default);
                }
            }
            else
            {
                MyLogger.LogWarning($"[PlayerDataProvider] IDatabaseService не доступен ({_databaseService == null}) или пользователь не аутентифицирован ({(_databaseService != null ? !_databaseService.IsAuthenticated : "N/A")}). Данные не будут сохранены в облако.", MyLogger.LogCategory.Default); 
            }
            
            var totalSaveTime = StopTiming("TotalSave");
            MyLogger.Log($"[PlayerDataProvider] Все операции сохранения завершены за {totalSaveTime.TotalMilliseconds}мс.", MyLogger.LogCategory.Default);
        }
    }
}

