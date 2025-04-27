using System;
using System.Collections.Generic;
using System.Linq;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.ConfigsManagement;
using App.Develop.Configs.Common.Emotion;
using UnityEngine;

namespace App.Develop.CommonServices.Emotion
{
    public class EmotionService : IDataReader<PlayerData>, IDataWriter<PlayerData>
    {
        private readonly Dictionary<EmotionTypes, EmotionData> _emotions;
        private readonly PlayerDataProvider _playerDataProvider;
        private readonly EmotionMixingRules _emotionMixingRules;
        private readonly Dictionary<EmotionTypes, EmotionConfig> _emotionConfigs;

        // События для оповещения об изменениях
        public event EventHandler<EmotionEvent> OnEmotionEvent;

        public EmotionService(PlayerDataProvider playerDataProvider, 
            ConfigsProviderService configsProvider)
        {
            _playerDataProvider = playerDataProvider ?? throw new ArgumentNullException(nameof(playerDataProvider));
            _emotions = new Dictionary<EmotionTypes, EmotionData>();
            _emotionMixingRules = new EmotionMixingRules();
            _emotionConfigs = new Dictionary<EmotionTypes, EmotionConfig>();

            InitializeEmotions();
            LoadEmotionConfigs(configsProvider);
        }

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

        private void LoadEmotionConfigs(ConfigsProviderService configsProvider)
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

        public void UpdateEmotionValue(EmotionTypes type, float newValue)
        {
            var emotion = GetEmotion(type);
            if (emotion == null) return;

            ValidateAndUpdateEmotion(type, newValue);
        }

        public void UpdateEmotionIntensity(EmotionTypes type, float intensity)
        {
            var emotion = GetEmotion(type);
            if (emotion == null) return;

            emotion.Intensity = Mathf.Clamp01(intensity);
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
                    ValidateAndUpdateEmotion(mixResult.ResultType, newValue);
                    
                    RaiseEmotionEvent(new EmotionEvent(mixResult.ResultType, 
                        EmotionEventType.EmotionMixed, newValue, mixResult.ResultIntensity));
                    return true;
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

        private void ValidateAndUpdateEmotion(EmotionTypes type, float newValue)
        {
            var emotion = GetEmotion(type);
            if (emotion == null) return;

            if (_emotionConfigs.TryGetValue(type, out var config))
            {
                if (newValue > config.MaxCapacity)
                {
                    RaiseEmotionEvent(new EmotionEvent(type, EmotionEventType.CapacityExceeded));
                    newValue = config.MaxCapacity;
                }
            }

            emotion.Value = Mathf.Max(0f, newValue);
            emotion.LastUpdate = DateTime.Now;
            
            RaiseEmotionEvent(new EmotionEvent(type, EmotionEventType.ValueChanged, 
                emotion.Value, emotion.Intensity));
        }

        protected virtual void RaiseEmotionEvent(EmotionEvent e)
        {
            OnEmotionEvent?.Invoke(this, e);
        }
    }
}