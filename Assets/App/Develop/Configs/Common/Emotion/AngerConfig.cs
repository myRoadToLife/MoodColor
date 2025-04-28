using UnityEngine;
using App.Develop.CommonServices.Emotion;

namespace App.Develop.Configs.Common.Emotion
{
    [CreateAssetMenu(fileName = "AngerConfig", menuName = "MoodColor/Emotions/Anger")]
    public class AngerConfig : EmotionConfig
    {
        private void OnEnable()
        {
            _type = EmotionTypes.Anger;
            _baseColor = BaseEmotionConfigs.Colors.Anger;
            _maxCapacity = BaseEmotionConfigs.Capacities.LowCapacity;
            _defaultFillRate = BaseEmotionConfigs.Rates.FastFillRate;
            _defaultDrainRate = BaseEmotionConfigs.Rates.FastDrainRate;
            _bubbleThreshold = BaseEmotionConfigs.Thresholds.LowBubbleThreshold;
            _intensityInfluence = BaseEmotionConfigs.GetStrongIntensityCurve();
        }
    }
} 