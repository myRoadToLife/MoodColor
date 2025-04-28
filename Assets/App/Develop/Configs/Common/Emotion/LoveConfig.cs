using UnityEngine;
using App.Develop.CommonServices.Emotion;

namespace App.Develop.Configs.Common.Emotion
{
    [CreateAssetMenu(fileName = "LoveConfig", menuName = "MoodColor/Emotions/Love")]
    public class LoveConfig : EmotionConfig
    {
        private void OnEnable()
        {
            _type = EmotionTypes.Love;
            _baseColor = BaseEmotionConfigs.Colors.Love;
            _maxCapacity = BaseEmotionConfigs.Capacities.HighCapacity;
            _defaultFillRate = BaseEmotionConfigs.Rates.DefaultFillRate;
            _defaultDrainRate = BaseEmotionConfigs.Rates.SlowDrainRate;
            _bubbleThreshold = BaseEmotionConfigs.Thresholds.HighBubbleThreshold;
            _intensityInfluence = BaseEmotionConfigs.GetStrongIntensityCurve();
        }
    }
} 