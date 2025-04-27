using System.Collections.Generic;
using UnityEngine;

namespace App.Develop.CommonServices.Emotion
{
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
            // Базовые правила смешивания
            AddMixRule(EmotionTypes.Joy, EmotionTypes.Sadness, EmotionTypes.Neutral, 0.5f);
            AddMixRule(EmotionTypes.Anger, EmotionTypes.Fear, EmotionTypes.Anxiety, 0.7f);
            AddMixRule(EmotionTypes.Joy, EmotionTypes.Trust, EmotionTypes.Love, 0.8f);
            // Добавьте другие правила по мере необходимости
        }

        private void AddMixRule(EmotionTypes emotion1, EmotionTypes emotion2, EmotionTypes result, float intensity)
        {
            var mixResult = new EmotionMixResult(result, intensity);
            _mixingRules[(emotion1, emotion2)] = mixResult;
            _mixingRules[(emotion2, emotion1)] = mixResult; // Коммутативность
        }
        
        public bool TryGetMixResult(EmotionTypes emotion1, EmotionTypes emotion2, out EmotionMixResult result)
        {
            return _mixingRules.TryGetValue((emotion1, emotion2), out result);
        }
    }

    public class EmotionMixResult
    {
        public EmotionTypes ResultType { get; }
        public float ResultIntensity { get; }

        public EmotionMixResult(EmotionTypes type, float intensity)
        {
            ResultType = type;
            ResultIntensity = Mathf.Clamp01(intensity);
        }
    }
} 