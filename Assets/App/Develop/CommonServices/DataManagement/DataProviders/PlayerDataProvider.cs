using System;
using System.Collections.Generic;
using App.Develop.CommonServices.ConfigsManagement;
using App.Develop.CommonServices.Emotion;
using UnityEngine;

namespace App.Develop.CommonServices.DataManagement.DataProviders
{
    public class PlayerDataProvider : DataProvider<PlayerData>
    {
        private readonly ConfigsProviderService _configsProvider;
        private PlayerData _cachedData; // Добавим кэшированные данные

        public PlayerDataProvider(ISaveLoadService saveLoadService,
            ConfigsProviderService configsProviderService) : base(saveLoadService)
        {
            _configsProvider = configsProviderService ?? throw new ArgumentNullException(nameof(configsProviderService));
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
                // Возвращаем данные с пустым словарем эмоций, чтобы избежать NullReferenceException
                var data = new PlayerData
                {
                    EmotionData = new Dictionary<EmotionTypes, EmotionData>()
                };
                _cachedData = data; // Кэшируем данные
                return data;
            }
        }

        public List<EmotionData> GetEmotions()
        {
            try
            {
                // Загрузка данных, если не загружены
                if (_cachedData == null)
                {
                    Load();
                    // Если _cachedData все еще null после загрузки,
                    // создаем новые данные через GetOriginData
                    if (_cachedData == null)
                    {
                        _cachedData = GetOriginData();
                    }
                }
                
                // Создаем список для результата
                var result = new List<EmotionData>();
                
                if (_cachedData?.EmotionData != null)
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
                    Debug.LogWarning("⚠️ EmotionData отсутствуют или null!");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка получения эмоций: {ex.Message}");
                return new List<EmotionData>(); // Возвращаем пустой список
            }
        }

        private Dictionary<EmotionTypes, EmotionData> InitEmotionData()
        {
            var emotionData = new Dictionary<EmotionTypes, EmotionData>();

            try
            {
                if (_configsProvider == null)
                {
                    Debug.LogError("❌ ConfigsProvider is null!");
                    throw new NullReferenceException("ConfigsProvider is null");
                }

                if (_configsProvider.StartEmotionConfig == null)
                {
                    Debug.LogError("❌ StartEmotionConfig is null!");
                    throw new NullReferenceException("StartEmotionConfig is null");
                }

                foreach (EmotionTypes emotionType in Enum.GetValues(typeof(EmotionTypes)))
                {
                    try
                    {
                        var (value, color) = _configsProvider.StartEmotionConfig.GetStartValueFor(emotionType);
                        
                        var newEmotionData = new EmotionData
                        {
                            Type = emotionType.ToString(),
                            Value = value,
                            Intensity = 0,
                            Color = color // Прямое присвоение цвета
                        };
                        
                        emotionData.Add(emotionType, newEmotionData);
                        
                        Debug.Log($"✅ Эмоция {emotionType} успешно добавлена в InitEmotionData");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"❌ Ошибка при добавлении эмоции {emotionType}: {ex.Message}");
                        
                        // Добавляем эмоцию с дефолтными значениями
                        var defaultEmotionData = new EmotionData
                        {
                            Type = emotionType.ToString(),
                            Value = 0,
                            Intensity = 0,
                            Color = Color.white
                        };
                        
                        emotionData.Add(emotionType, defaultEmotionData);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Критическая ошибка в InitEmotionData: {ex.Message}");
                
                // Создаем базовые эмоции с дефолтными значениями
                foreach (EmotionTypes emotionType in Enum.GetValues(typeof(EmotionTypes)))
                {
                    var defaultEmotionData = new EmotionData
                    {
                        Type = emotionType.ToString(),
                        Value = 0,
                        Intensity = 0,
                        Color = Color.white
                    };
                    
                    emotionData.Add(emotionType, defaultEmotionData);
                }
            }

            return emotionData;
        }

        // Переопределяем метод Load для обновления кэшированных данных
        public new void Load()
        {
            base.Load();
            // Здесь можно добавить код для обновления _cachedData, если есть доступ к загруженным данным
        }

        // Переопределяем метод Save для обновления кэшированных данных
        public new void Save()
        {
            base.Save();
            // Здесь можно добавить код для обновления _cachedData после сохранения
        }
    }
}
