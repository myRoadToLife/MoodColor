using UnityEngine;
using App.Develop.CommonServices.Emotion;

namespace App.Develop.Configs.Common.Emotion
{
    [CreateAssetMenu(fileName = "TrustConfig", menuName = "MoodColor/Emotions/Trust")]
    public class TrustConfig : EmotionConfig
    {
        private void OnEnable()
        {
            _type = EmotionTypes.Trust;
            _baseColor = BaseEmotionConfigs.Colors.Trust;
            _maxCapacity = BaseEmotionConfigs.Capacities.HighCapacity;
            _defaultFillRate = BaseEmotionConfigs.Rates.SlowFillRate;
            _defaultDrainRate = BaseEmotionConfigs.Rates.SlowDrainRate;
            _bubbleThreshold = BaseEmotionConfigs.Thresholds.DefaultBubbleThreshold;
            _intensityInfluence = BaseEmotionConfigs.GetWeakIntensityCurve();
        }
    }
} 