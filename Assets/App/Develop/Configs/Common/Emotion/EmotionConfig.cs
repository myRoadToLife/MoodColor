using UnityEngine;
using App.Develop.CommonServices.Emotion;

namespace App.Develop.Configs.Common.Emotion
{
    [CreateAssetMenu(fileName = "EmotionConfig", menuName = "MoodColor/EmotionConfig")]
    public class EmotionConfig : ScriptableObject
    {
        [SerializeField] private EmotionTypes _type;
        [SerializeField] private Color _baseColor;
        [SerializeField, Range(0f, 100f)] private float _maxCapacity = 100f;
        [SerializeField, Range(0f, 10f)] private float _defaultFillRate = 1f;
        [SerializeField, Range(0f, 10f)] private float _defaultDrainRate = 0.5f;
        [SerializeField, Range(0f, 100f)] private float _bubbleThreshold = 80f;
        [SerializeField] private AnimationCurve _intensityInfluence;
        
        public EmotionTypes Type => _type;
        public Color BaseColor => _baseColor;
        public float MaxCapacity => _maxCapacity;
        public float DefaultFillRate => _defaultFillRate;
        public float DefaultDrainRate => _defaultDrainRate;
        public float BubbleThreshold => _bubbleThreshold;
        
        public float GetIntensityMultiplier(float intensity)
        {
            return _intensityInfluence.Evaluate(Mathf.Clamp01(intensity));
        }
    }
} 