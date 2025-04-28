using UnityEngine;
using App.Develop.CommonServices.Emotion;

namespace App.Develop.Configs.Common.Emotion
{
    [CreateAssetMenu(fileName = "NeutralConfig", menuName = "MoodColor/Emotions/Neutral")]
    public class NeutralConfig : EmotionConfig
    {
        private void OnEnable()
        {
            _type = EmotionTypes.Neutral;
            _baseColor = BaseEmotionConfigs.Colors.Neutral;
            _maxCapacity = BaseEmotionConfigs.Capacities.DefaultCapacity;
            _defaultFillRate = BaseEmotionConfigs.Rates.SlowFillRate;
            _defaultDrainRate = BaseEmotionConfigs.Rates.SlowDrainRate;
            _bubbleThreshold = BaseEmotionConfigs.Thresholds.LowBubbleThreshold;
            _intensityInfluence = BaseEmotionConfigs.GetWeakIntensityCurve();
        }
    }
} 