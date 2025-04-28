using UnityEngine;
using App.Develop.CommonServices.Emotion;

namespace App.Develop.Configs.Common.Emotion
{
    [CreateAssetMenu(fileName = "SurpriseConfig", menuName = "MoodColor/Emotions/Surprise")]
    public class SurpriseConfig : EmotionConfig
    {
        private void OnEnable()
        {
            _type = EmotionTypes.Surprise;
            _baseColor = BaseEmotionConfigs.Colors.Surprise;
            _maxCapacity = BaseEmotionConfigs.Capacities.LowCapacity;
            _defaultFillRate = BaseEmotionConfigs.Rates.FastFillRate;
            _defaultDrainRate = BaseEmotionConfigs.Rates.FastDrainRate;
            _bubbleThreshold = BaseEmotionConfigs.Thresholds.HighBubbleThreshold;
            _intensityInfluence = BaseEmotionConfigs.GetStrongIntensityCurve();
        }
    }
} 