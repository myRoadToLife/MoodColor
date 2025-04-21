using System;
using System.Collections.Generic;
using System.Linq;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.Utils.Reactive;
using UnityEngine;

namespace App.Develop.CommonServices.Emotion
{
    public class EmotionService : IDataReader<PlayerData>, IDataWriter<PlayerData>
    {
        private Dictionary<EmotionTypes, ReactiveVariable<EmotionData>> _emotions = new();

        public EmotionService(PlayerDataProvider playerDataProvider)
        {
            // Инициализация всех типов эмоций
            foreach (EmotionTypes type in Enum.GetValues(typeof(EmotionTypes)))
            {
                _emotions[type] = new ReactiveVariable<EmotionData>(new EmotionData
                {
                    Value = 0,
                    Color = GetDefaultColorForEmotion(type)
                });
            }
            playerDataProvider.RegisterWriter(this);
            playerDataProvider.RegisterReader(this);
        }
        
        private Color GetDefaultColorForEmotion(EmotionTypes type)
        {
            return type switch
            {
                EmotionTypes.Joy => Color.yellow,
                EmotionTypes.Sadness => Color.blue,
                EmotionTypes.Anger => Color.red,
                EmotionTypes.Fear => Color.gray,
                EmotionTypes.Disgust => Color.magenta,
                _ => Color.white
            };
        }

        public List<EmotionTypes> AvailableEmotions => _emotions.Keys.ToList();

        public IReadOnlyVariable<EmotionData> GetEmotion(EmotionTypes type)
        {
            if (!_emotions.ContainsKey(type))
            {
                Debug.LogError($"Emotion type {type} not found in dictionary");
                return null;
            }
            return _emotions[type];
        }
        public bool HasEnough(EmotionTypes type, int amount)
            => _emotions[type].Value.Value >= amount;

        public void SpendEmotion(EmotionTypes type, int amount)
        {
            if (!HasEnough(type, amount))
                throw new ArgumentException($"Not enough {type} emotion");

            var current = _emotions[type].Value;
            _emotions[type].Value = new EmotionData
            {
                Value = current.Value - amount,
                Color = current.Color
            };
        }

        public void AddEmotion(EmotionTypes type, int amount)
        {
            var current = _emotions[type].Value;
            _emotions[type].Value = new EmotionData
            {
                Value = current.Value + amount,
                Color = current.Color
            };
        }

        public void ReadFrom(PlayerData data)
        {
            foreach (var emotion in data.EmotionData)
            {
                if (_emotions.ContainsKey(emotion.Key))
                {
                    _emotions[emotion.Key].Value = emotion.Value;
                }
                else
                {
                    _emotions.Add(emotion.Key, new ReactiveVariable<EmotionData>(emotion.Value));
                }
            }
        }

        public void WriteTo(PlayerData data)
        {
            foreach (var emotion in _emotions)
            {
                if (data.EmotionData.ContainsKey(emotion.Key))
                {
                    data.EmotionData[emotion.Key] = emotion.Value.Value;
                }
                else
                {
                    data.EmotionData.Add(emotion.Key, emotion.Value.Value);
                }
            }
        }

        // Новый метод для получения цвета эмоции
        public Color GetEmotionColor(EmotionTypes type)
        {
            return _emotions[type].Value.Color;
        }
    }
}