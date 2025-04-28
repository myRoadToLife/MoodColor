using UnityEngine;
using App.Develop.CommonServices.Emotion;

namespace App.Develop.Configs.Common.Emotion
{
    public static class BaseEmotionConfigs
    {
        public static class Colors
        {
            public static readonly Color Joy = new Color(1f, 0.92f, 0.016f);        // Яркий желтый
            public static readonly Color Sadness = new Color(0.118f, 0.565f, 1f);   // Синий
            public static readonly Color Anger = new Color(1f, 0.078f, 0.078f);     // Красный
            public static readonly Color Fear = new Color(0.502f, 0f, 0.502f);      // Фиолетовый
            public static readonly Color Disgust = new Color(0.196f, 0.804f, 0f);   // Зеленый
            public static readonly Color Trust = new Color(0.678f, 0.847f, 0.902f); // Светло-голубой
            public static readonly Color Anticipation = new Color(1f, 0.647f, 0f);  // Оранжевый
            public static readonly Color Surprise = new Color(1f, 0.753f, 0.796f);  // Розовый
            public static readonly Color Love = new Color(1f, 0.412f, 0.706f);      // Малиновый
            public static readonly Color Anxiety = new Color(0.545f, 0f, 0f);       // Темно-красный
            public static readonly Color Neutral = new Color(0.827f, 0.827f, 0.827f); // Серый
        }

        public static class Thresholds
        {
            public const float DefaultBubbleThreshold = 80f;
            public const float HighBubbleThreshold = 90f;
            public const float LowBubbleThreshold = 70f;
        }

        public static class Rates
        {
            public const float DefaultFillRate = 1f;
            public const float FastFillRate = 2f;
            public const float SlowFillRate = 0.5f;
            
            public const float DefaultDrainRate = 0.5f;
            public const float FastDrainRate = 1f;
            public const float SlowDrainRate = 0.25f;
        }

        public static class Capacities
        {
            public const float DefaultCapacity = 100f;
            public const float HighCapacity = 150f;
            public const float LowCapacity = 50f;
        }

        public static AnimationCurve GetDefaultIntensityCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0.2f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f, 1.5f)
            );
        }

        public static AnimationCurve GetStrongIntensityCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0.5f),
                new Keyframe(0.5f, 1.5f),
                new Keyframe(1f, 2f)
            );
        }

        public static AnimationCurve GetWeakIntensityCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0.1f),
                new Keyframe(0.5f, 0.5f),
                new Keyframe(1f, 1f)
            );
        }
    }
} 