using System;
using System.Collections.Generic;
using App.Develop.CommonServices.ConfigsManagement;
using App.Develop.CommonServices.Emotion;
using UnityEngine;

namespace App.Develop.CommonServices.DataManagement.DataProviders
{
    public class PlayerDataProvider : DataProvider<PlayerData>
    {
        private ConfigsProviderService _configsProvider;
        private PlayerData _cachedData;

        public PlayerDataProvider(ISaveLoadService saveLoadService,
            ConfigsProviderService configsProviderService) : base(saveLoadService)
        {
            _configsProvider = configsProviderService;
        }

        protected override PlayerData GetOriginData()
        {
            var newData = new PlayerData
            {
                EmotionData = InitEmotionData()
            };
            return newData;
        }
        
        // Новый метод для получения эмоций
        public List<EmotionData> GetEmotions()
        {
            // Загружаем данные, если еще не загружены
            if (_cachedData == null)
            {
                Load();
                _cachedData = new PlayerData();
                
                // Для каждого читателя
                foreach (var reader in GetDataReaders())
                {
                    if (reader is EmotionService emotionService)
                    {
                        // Загружаем данные из EmotionService в _cachedData
                        emotionService.WriteTo(_cachedData);
                    }
                }
            }
            
            // Преобразуем словарь эмоций в список
            var result = new List<EmotionData>();
            if (_cachedData.EmotionData != null)
            {
                foreach (var emotion in _cachedData.EmotionData.Values)
                {
                    result.Add(emotion);
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Данные эмоций не найдены. Возвращаем пустой список.");
            }
            
            return result;
        }

        private Dictionary<EmotionTypes, EmotionData> InitEmotionData()
        {
            var emotionData = new Dictionary<EmotionTypes, EmotionData>();

            foreach (EmotionTypes emotionType in Enum.GetValues(typeof(EmotionTypes)))
            {
                var (value, color) = _configsProvider.StartEmotionConfig.GetStartValueFor(emotionType);
                emotionData.Add(emotionType, new EmotionData
                {
                    Type = emotionType.ToString(),
                    Value = value,
                    Color = color,
                    Intensity = 0
                });
            }

            return emotionData;
        }
    }
}
