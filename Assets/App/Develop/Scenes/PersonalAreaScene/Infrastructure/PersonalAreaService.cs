using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.GameSystem;
using App.Develop.Utils.Reactive;
using App.Develop.Utils.Logging;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace App.Develop.Scenes.PersonalAreaScene.Infrastructure
{
    public interface IPersonalAreaService
    {
        IReadOnlyVariable<EmotionData> GetEmotionVariable(EmotionTypes type);
        void AddEmotion(EmotionTypes type, int amount);
        void SpendEmotion(EmotionTypes type, int amount);
    }

    public class ReactiveEmotionData : IReadOnlyVariable<EmotionData>
    {
        private EmotionData _value;
        public event Action<EmotionData, EmotionData> Changed;

        public EmotionData Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    var oldValue = _value;
                    _value = value;
                    Changed?.Invoke(oldValue, _value);
                }
            }
        }

        public ReactiveEmotionData(EmotionData initialValue)
        {
            _value = initialValue;
        }
    }

    public class PersonalAreaService : IPersonalAreaService
    {
        private readonly EmotionService _emotionService;
        private readonly IPointsService _pointsService;
        private readonly Dictionary<EmotionTypes, ReactiveEmotionData> _emotionVariables;

        public PersonalAreaService(EmotionService emotionService, IPointsService pointsService = null)
        {
            _emotionService = emotionService ?? throw new ArgumentNullException(nameof(emotionService));
            _pointsService = pointsService;
            _emotionVariables = new Dictionary<EmotionTypes, ReactiveEmotionData>();
        }

        public IReadOnlyVariable<EmotionData> GetEmotionVariable(EmotionTypes type)
        {
            if (!Enum.IsDefined(typeof(EmotionTypes), type))
            {
                MyLogger.LogWarning($"Неизвестный тип эмоции: {type}");
                return null;
            }

            if (!_emotionVariables.TryGetValue(type, out var variable))
            {
                var emotion = _emotionService.GetEmotion(type);
                if (emotion == null) return null;
                
                variable = new ReactiveEmotionData(emotion);
                _emotionVariables[type] = variable;
            }

            return variable;
        }

        public void AddEmotion(EmotionTypes type, int amount)
        {
            if (amount <= 0)
            {
                MyLogger.LogWarning($"Попытка добавить неположительное количество эмоций: {amount}");
                return;
            }

            if (!Enum.IsDefined(typeof(EmotionTypes), type))
            {
                MyLogger.LogWarning($"Неизвестный тип эмоции: {type}");
                return;
            }

            _emotionService.AddEmotion(type, amount);
            
            // Начисляем очки за отметку эмоции
            _pointsService?.AddPointsForEmotion();
            
            // Обновляем реактивную переменную
            if (_emotionVariables.TryGetValue(type, out var variable))
            {
                variable.Value = _emotionService.GetEmotion(type);
            }
        }

        public void SpendEmotion(EmotionTypes type, int amount)
        {
            if (amount <= 0)
            {
                MyLogger.LogWarning($"Попытка потратить неположительное количество эмоций: {amount}");
                return;
            }

            if (!Enum.IsDefined(typeof(EmotionTypes), type))
            {
                MyLogger.LogWarning($"Неизвестный тип эмоции: {type}");
                return;
            }

            _emotionService.SpendEmotion(type, amount);
            
            // Обновляем реактивную переменную
            if (_emotionVariables.TryGetValue(type, out var variable))
            {
                variable.Value = _emotionService.GetEmotion(type);
            }
        }
    }
} 