using System;
using System.Collections.Generic;
using UnityEngine;

namespace App.Develop.CommonServices.Emotion
{
    public class EmotionMixResult
    {
        public EmotionTypes ResultType { get; set; }
        public float ResultIntensity { get; set; }
        public Color ResultColor { get; set; }
    }

    public class EmotionMixingRules
    {
        private readonly Dictionary<(EmotionTypes, EmotionTypes), EmotionMixResult> _mixingRules;

        public EmotionMixingRules()
        {
            _mixingRules = new Dictionary<(EmotionTypes, EmotionTypes), EmotionMixResult>();
            InitializeRules();
        }

        private void InitializeRules()
        {
            // Радость + Грусть = Меланхолия (особый тип грусти)
            AddRule(EmotionTypes.Joy, EmotionTypes.Sadness, new EmotionMixResult
            {
                ResultType = EmotionTypes.Sadness,
                ResultIntensity = 0.7f,
                ResultColor = new Color(0.5f, 0.5f, 0.8f)
            });

            // Гнев + Страх = Паника
            AddRule(EmotionTypes.Anger, EmotionTypes.Fear, new EmotionMixResult
            {
                ResultType = EmotionTypes.Fear,
                ResultIntensity = 1.2f,
                ResultColor = new Color(0.8f, 0.2f, 0.2f)
            });

            // Радость + Гнев = Возбуждение (особый тип радости)
            AddRule(EmotionTypes.Joy, EmotionTypes.Anger, new EmotionMixResult
            {
                ResultType = EmotionTypes.Joy,
                ResultIntensity = 1.3f,
                ResultColor = new Color(1f, 0.6f, 0.2f)
            });

            // Страх + Отвращение = Усиленное отвращение
            AddRule(EmotionTypes.Fear, EmotionTypes.Disgust, new EmotionMixResult
            {
                ResultType = EmotionTypes.Disgust,
                ResultIntensity = 1.4f,
                ResultColor = new Color(0.4f, 0.8f, 0.4f)
            });

            // Грусть + Страх = Тревога (особый тип страха)
            AddRule(EmotionTypes.Sadness, EmotionTypes.Fear, new EmotionMixResult
            {
                ResultType = EmotionTypes.Fear,
                ResultIntensity = 0.9f,
                ResultColor = new Color(0.6f, 0.6f, 0.8f)
            });
        }

        private void AddRule(EmotionTypes type1, EmotionTypes type2, EmotionMixResult result)
        {
            _mixingRules[(type1, type2)] = result;
            _mixingRules[(type2, type1)] = result; // Добавляем обратное правило
        }

        public bool TryGetMixResult(EmotionTypes type1, EmotionTypes type2, out EmotionMixResult result)
        {
            return _mixingRules.TryGetValue((type1, type2), out result);
        }

        public bool CanMix(EmotionTypes type1, EmotionTypes type2)
        {
            return _mixingRules.ContainsKey((type1, type2));
        }

        public IEnumerable<(EmotionTypes, EmotionTypes)> GetAllPossibleMixes()
        {
            return _mixingRules.Keys;
        }
    }
} 