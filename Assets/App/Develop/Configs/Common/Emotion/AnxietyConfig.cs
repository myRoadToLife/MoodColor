using UnityEngine;
using App.Develop.CommonServices.Emotion;

namespace App.Develop.Configs.Common.Emotion
{
    [CreateAssetMenu(fileName = "AnxietyConfig", menuName = "MoodColor/Emotions/Anxiety")]
    public class AnxietyConfig : EmotionConfig
    {
        private void OnEnable()
        {
            _type = EmotionTypes.Anxiety;
            _baseColor = BaseEmotionConfigs.Colors.Anxiety;
            _maxCapacity = BaseEmotionConfigs.Capacities.DefaultCapacity;
            _defaultFillRate = BaseEmotionConfigs.Rates.FastFillRate;
            _defaultDrainRate = BaseEmotionConfigs.Rates.SlowDrainRate;
            _bubbleThreshold = BaseEmotionConfigs.Thresholds.DefaultBubbleThreshold;
            _intensityInfluence = BaseEmotionConfigs.GetStrongIntensityCurve();
        }
    }
} 