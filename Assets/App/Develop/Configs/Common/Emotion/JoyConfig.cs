using UnityEngine;
using App.Develop.CommonServices.Emotion;

namespace App.Develop.Configs.Common.Emotion
{
    [CreateAssetMenu(fileName = "JoyConfig", menuName = "MoodColor/Emotions/Joy")]
    public class JoyConfig : EmotionConfig
    {
        private void OnEnable()
        {
            _type = EmotionTypes.Joy;
            _baseColor = BaseEmotionConfigs.Colors.Joy;
            _maxCapacity = BaseEmotionConfigs.Capacities.HighCapacity;
            _defaultFillRate = BaseEmotionConfigs.Rates.FastFillRate;
            _defaultDrainRate = BaseEmotionConfigs.Rates.DefaultDrainRate;
            _bubbleThreshold = BaseEmotionConfigs.Thresholds.HighBubbleThreshold;
            _intensityInfluence = BaseEmotionConfigs.GetStrongIntensityCurve();
        }
    }
} 