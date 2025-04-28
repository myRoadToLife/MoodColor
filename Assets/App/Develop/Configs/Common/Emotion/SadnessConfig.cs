using UnityEngine;
using App.Develop.CommonServices.Emotion;

namespace App.Develop.Configs.Common.Emotion
{
    [CreateAssetMenu(fileName = "SadnessConfig", menuName = "MoodColor/Emotions/Sadness")]
    public class SadnessConfig : EmotionConfig
    {
        private void OnEnable()
        {
            _type = EmotionTypes.Sadness;
            _baseColor = BaseEmotionConfigs.Colors.Sadness;
            _maxCapacity = BaseEmotionConfigs.Capacities.DefaultCapacity;
            _defaultFillRate = BaseEmotionConfigs.Rates.SlowFillRate;
            _defaultDrainRate = BaseEmotionConfigs.Rates.DefaultDrainRate;
            _bubbleThreshold = BaseEmotionConfigs.Thresholds.LowBubbleThreshold;
            _intensityInfluence = BaseEmotionConfigs.GetWeakIntensityCurve();
        }
    }
} 