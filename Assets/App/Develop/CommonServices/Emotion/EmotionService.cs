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
        private readonly Dictionary<EmotionTypes, ReactiveVariable<EmotionData>> _emotions = new Dictionary<EmotionTypes, ReactiveVariable<EmotionData>>();
        private readonly PlayerDataProvider _playerDataProvider;

        public EmotionService(PlayerDataProvider playerDataProvider)
        {
            _playerDataProvider = playerDataProvider;
            
            // Инициализируем словарь со всеми типами эмоций
            foreach (EmotionTypes type in Enum.GetValues(typeof(EmotionTypes)))
            {
                _emotions[type] = new ReactiveVariable<EmotionData>(new EmotionData
                {
                    Type = type.ToString(),
                    Value = 0,
                });
            }
            
            // Загружаем данные из сохранения, если есть
            LoadEmotions();
        }

        public List<EmotionTypes> AvailableEmotions => _emotions.Keys.ToList();

        public ReactiveVariable<EmotionData> GetEmotion(EmotionTypes type)
        {
            // Проверяем наличие ключа в словаре
            if (!_emotions.ContainsKey(type))
            {
                Debug.LogWarning($"⚠️ Эмоция {type} не найдена в словаре. Создаем новую.");
                
                // Если ключа нет, создаем новую запись
                _emotions[type] = new ReactiveVariable<EmotionData>(new EmotionData
                {
                    Type = type.ToString(),
                    Value = 0,
                });
            }
            
            return _emotions[type];
        }

        public bool HasEnough(EmotionTypes type, int amount)
            => GetEmotion(type).Value.Value >= amount;

        public void SpendEmotion(EmotionTypes type, int amount)
        {
            if (!HasEnough(type, amount))
                throw new ArgumentException($"Not enough {type} emotion");

            var current = GetEmotion(type).Value;
            current.Value = current.Value - amount;
        }

        public void AddEmotion(EmotionTypes type, int amount)
        {
            var current = GetEmotion(type).Value;
            current.Value = current.Value + amount;
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
                    GetEmotion(emotion.Key).Value = emotion.Value;
                else
                    _emotions.Add(emotion.Key, new ReactiveVariable<EmotionData>(emotion.Value));
            }

            // 🧩 Добавляем отсутствующие эмоции с дефолтными значениями
            foreach (EmotionTypes type in Enum.GetValues(typeof(EmotionTypes)))
            {
                if (!_emotions.ContainsKey(type))
                {
                    Debug.LogWarning($"⚠️ Emotion {type} не был загружен. Создаём по умолчанию.");
                    _emotions[type] = new ReactiveVariable<EmotionData>(new EmotionData
                    {
                        Type = type.ToString(),
                        Value = 0,
                    });
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
            return GetEmotion(type).Value.Color;
        }

        private void LoadEmotions()
        {
            // Загрузка эмоций из сохранения или API
            var savedEmotions = _playerDataProvider.GetEmotions();
            
            if (savedEmotions != null && savedEmotions.Count > 0)
            {
                foreach (var emotion in savedEmotions)
                {
                    if (Enum.TryParse<EmotionTypes>(emotion.Type, out var type))
                    {
                        if (!_emotions.ContainsKey(type))
                        {
                            _emotions[type] = new ReactiveVariable<EmotionData>(new EmotionData());
                        }
                        _emotions[type].Value = emotion;
                    }
                }
            }
            
            // Проверяем, что все типы эмоций инициализированы
            foreach (EmotionTypes type in Enum.GetValues(typeof(EmotionTypes)))
            {
                if (!_emotions.ContainsKey(type))
                {
                    Debug.LogWarning($"⚠️ Эмоция {type} не найдена. Создаём с дефолтными значениями.");
                    _emotions[type] = new ReactiveVariable<EmotionData>(new EmotionData
                    {
                        Type = type.ToString(),
                        Value = 0,
                        Intensity = 0
                    });
                }
            }
        }
    }
}