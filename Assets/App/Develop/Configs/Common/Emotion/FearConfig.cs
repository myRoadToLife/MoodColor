using UnityEngine;
using App.Develop.CommonServices.Emotion;

namespace App.Develop.Configs.Common.Emotion
{
    [CreateAssetMenu(fileName = "FearConfig", menuName = "MoodColor/Emotions/Fear")]
    public class FearConfig : EmotionConfig
    {
        private void OnEnable()
        {
            _type = EmotionTypes.Fear;
            _baseColor = BaseEmotionConfigs.Colors.Fear;
            _maxCapacity = BaseEmotionConfigs.Capacities.DefaultCapacity;
            _defaultFillRate = BaseEmotionConfigs.Rates.FastFillRate;
            _defaultDrainRate = BaseEmotionConfigs.Rates.DefaultDrainRate;
            _bubbleThreshold = BaseEmotionConfigs.Thresholds.DefaultBubbleThreshold;
            _intensityInfluence = BaseEmotionConfigs.GetStrongIntensityCurve();
        }
    }
} 