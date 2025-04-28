using UnityEngine;
using App.Develop.CommonServices.Emotion;

namespace App.Develop.Configs.Common.Emotion
{
    [CreateAssetMenu(fileName = "DisgustConfig", menuName = "MoodColor/Emotions/Disgust")]
    public class DisgustConfig : EmotionConfig
    {
        private void OnEnable()
        {
            _type = EmotionTypes.Disgust;
            _baseColor = BaseEmotionConfigs.Colors.Disgust;
            _maxCapacity = BaseEmotionConfigs.Capacities.LowCapacity;
            _defaultFillRate = BaseEmotionConfigs.Rates.DefaultFillRate;
            _defaultDrainRate = BaseEmotionConfigs.Rates.FastDrainRate;
            _bubbleThreshold = BaseEmotionConfigs.Thresholds.LowBubbleThreshold;
            _intensityInfluence = BaseEmotionConfigs.GetWeakIntensityCurve();
        }
    }
} 