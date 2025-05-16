using System;
using System.Collections.Generic;
using System.Threading.Tasks; // Добавлено для Task
using App.Develop.CommonServices.ConfigsManagement;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.Firebase.Database.Services; // Добавлено для IDatabaseService
using UnityEngine;

namespace App.Develop.CommonServices.DataManagement.DataProviders
{
    public class PlayerDataProvider : DataProvider<PlayerData>
    {
        private readonly IConfigsProvider _configsProvider;
        private readonly IDatabaseService _databaseService;
        private PlayerData _cachedData;

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
                Debug.LogError($"❌ Ошибка при создании начальных данных: {ex.Message}");
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
                Debug.LogError("[PlayerDataProvider] GetData() вызван, но _cachedData is null. Убедитесь, что Load() был вызван и завершен ранее. Возвращаются данные по умолчанию.");
                return GetOriginData(); // Возвращаем данные по умолчанию, чтобы избежать null
            }
            return _cachedData;
        }

        public List<EmotionData> GetEmotions()
        {
            if (_cachedData == null)
            {
                Debug.LogError("[PlayerDataProvider] GetEmotions() вызван, но _cachedData is null. Убедитесь, что Load() был вызван и завершен ранее. Возвращается пустой список.");
                return new List<EmotionData>();
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
                Debug.LogWarning("⚠️ _cachedData.EmotionData is null в PlayerDataProvider.GetEmotions!");
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
                    Debug.LogWarning("⚠️ StartEmotionConfig is null. Создаем дефолтные эмоции с нулевыми значениями.");
                    
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
                        Debug.LogError($"❌ Ошибка при добавлении эмоции {emotionType}: {ex_inner.Message}");
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
                Debug.LogError($"❌ Критическая ошибка в InitEmotionData: {ex_outer.Message}. Создаем дефолтные эмоции.");
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

        public new async Task Load()
        {
            bool cloudLoadAttempted = false;
            if (_databaseService != null && _databaseService.IsAuthenticated)
            {
                try
                {
                    Debug.Log("[PlayerDataProvider] Пытаемся загрузить GameData из облака...");
                    GameData cloudGameData = await _databaseService.LoadUserGameData();
                    if (cloudGameData != null)
                    {
                        if (_cachedData == null) 
                        {
                            base.Load(); 
                            var baseDataProperty = typeof(DataProvider<PlayerData>).GetProperty("Data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (baseDataProperty != null) _cachedData = (PlayerData)baseDataProperty.GetValue(this);
                            if (_cachedData == null) _cachedData = GetOriginData();
                        }
                        _cachedData.GameData = cloudGameData; 
                        Debug.Log("[PlayerDataProvider] GameData успешно загружены из облака.");
                    }
                    else
                    {
                        Debug.LogWarning("[PlayerDataProvider] GameData из облака не найдено или пусто. Используем локальные/дефолтные.");
                        base.Load(); 
                        var baseDataProperty = typeof(DataProvider<PlayerData>).GetProperty("Data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (baseDataProperty != null) _cachedData = (PlayerData)baseDataProperty.GetValue(this);
                        if (_cachedData == null) _cachedData = GetOriginData();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PlayerDataProvider] Ошибка при загрузке GameData из облака: {ex.Message}. Используем локальные/дефолтные.");
                    base.Load(); 
                    var baseDataProperty = typeof(DataProvider<PlayerData>).GetProperty("Data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (baseDataProperty != null) _cachedData = (PlayerData)baseDataProperty.GetValue(this);
                    if (_cachedData == null) _cachedData = GetOriginData();
                }
                cloudLoadAttempted = true;
            }
            
            if (!cloudLoadAttempted) 
            {
                 base.Load();
                var baseDataProperty = typeof(DataProvider<PlayerData>).GetProperty("Data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (baseDataProperty != null) _cachedData = (PlayerData)baseDataProperty.GetValue(this);
                if (_cachedData == null) _cachedData = GetOriginData();
            }

            if (_cachedData == null)
            {
                try
                {
                    var type = typeof(DataProvider<PlayerData>);
                    var propInfo = type.GetProperty("Data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (propInfo != null) _cachedData = (PlayerData)propInfo.GetValue(this);
                    else Debug.LogWarning("[PlayerDataProvider] Не удалось получить доступ к Data через рефлексию в Load");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PlayerDataProvider] Ошибка при попытке доступа к Data через рефлексию в Load: {ex.Message}");
                }
            }
            
            if (_cachedData == null)
            {
                Debug.LogWarning("[PlayerDataProvider] _cachedData остался null после всех попыток загрузки. Создаем GetOriginData().");
                _cachedData = GetOriginData();
            }
        }

        public new async Task Save()
        {
            Debug.Log("[PlayerDataProvider] Save() вызван."); 
            if (_cachedData == null)
            {
                Debug.LogWarning("[PlayerDataProvider] _cachedData is null in Save(). Попытка загрузить/создать.");
                await Load(); 
                if (_cachedData == null) 
                {
                     Debug.LogError("[PlayerDataProvider] _cachedData все еще null после Load() в Save(). Нечего сохранять.");
                     return;
                }
            }
            if (_cachedData.GameData == null)
            {
                 Debug.LogWarning("[PlayerDataProvider] _cachedData.GameData is null in Save(). Инициализируем новым GameData().");
                _cachedData.GameData = new GameData();
            }

            Debug.Log("[PlayerDataProvider] Попытка локального сохранения (base.Save())."); 
            base.Save(); 
            Debug.Log("[PlayerDataProvider] Локальное сохранение (base.Save()) завершено."); 

            if (_databaseService != null && _databaseService.IsAuthenticated)
            {
                try
                {
                    Debug.Log($"[PlayerDataProvider] Пытаемся сохранить GameData в облако. Текущие очки для сохранения: {_cachedData.GameData.Points}"); 
                    await _databaseService.SaveUserGameData(_cachedData.GameData);
                    Debug.Log("[PlayerDataProvider] GameData успешно сохранено в облако.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PlayerDataProvider] Ошибка при сохранении GameData в облако: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[PlayerDataProvider] IDatabaseService не доступен ({_databaseService == null}) или пользователь не аутентифицирован ({(_databaseService != null ? !_databaseService.IsAuthenticated : "N/A")}). GameData не будет сохранено в облако."); 
            }
        }
    }
}
