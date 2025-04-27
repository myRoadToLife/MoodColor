// PersonalAreaService.cs

using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion;
using App.Develop.Utils.Reactive;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace App.Develop.Scenes.PersonalAreaScene
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
        private readonly Dictionary<EmotionTypes, ReactiveEmotionData> _emotionVariables;

        public PersonalAreaService(EmotionService emotionService)
        {
            _emotionService = emotionService ?? throw new ArgumentNullException(nameof(emotionService));
            _emotionVariables = new Dictionary<EmotionTypes, ReactiveEmotionData>();
        }

        public IReadOnlyVariable<EmotionData> GetEmotionVariable(EmotionTypes type)
        {
            if (!Enum.IsDefined(typeof(EmotionTypes), type))
            {
                Debug.LogWarning($"Неизвестный тип эмоции: {type}");
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
                Debug.LogWarning($"Попытка добавить неположительное количество эмоций: {amount}");
                return;
            }

            if (!Enum.IsDefined(typeof(EmotionTypes), type))
            {
                Debug.LogWarning($"Неизвестный тип эмоции: {type}");
                return;
            }

            _emotionService.AddEmotion(type, amount);
            
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
                Debug.LogWarning($"Попытка потратить неположительное количество эмоций: {amount}");
                return;
            }

            if (!Enum.IsDefined(typeof(EmotionTypes), type))
            {
                Debug.LogWarning($"Неизвестный тип эмоции: {type}");
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
