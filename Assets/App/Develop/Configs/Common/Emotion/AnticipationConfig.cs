using UnityEngine;
using App.Develop.CommonServices.Emotion;

namespace App.Develop.Configs.Common.Emotion
{
    [CreateAssetMenu(fileName = "AnticipationConfig", menuName = "MoodColor/Emotions/Anticipation")]
    public class AnticipationConfig : EmotionConfig
    {
        private void OnEnable()
        {
            _type = EmotionTypes.Anticipation;
            _baseColor = BaseEmotionConfigs.Colors.Anticipation;
            _maxCapacity = BaseEmotionConfigs.Capacities.DefaultCapacity;
            _defaultFillRate = BaseEmotionConfigs.Rates.DefaultFillRate;
            _defaultDrainRate = BaseEmotionConfigs.Rates.DefaultDrainRate;
            _bubbleThreshold = BaseEmotionConfigs.Thresholds.DefaultBubbleThreshold;
            _intensityInfluence = BaseEmotionConfigs.GetStrongIntensityCurve();
        }
    }
} 