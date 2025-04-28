using UnityEngine;
using App.Develop.CommonServices.Emotion;

namespace App.Develop.Configs.Common.Emotion
{
    [CreateAssetMenu(fileName = "EmotionConfig", menuName = "MoodColor/EmotionConfig")]
    public class EmotionConfig : ScriptableObject
    {
        [SerializeField] protected EmotionTypes _type;
        [SerializeField] protected Color _baseColor;
        [SerializeField, Range(0f, 100f)] protected float _maxCapacity = 100f;
        [SerializeField, Range(0f, 10f)] protected float _defaultFillRate = 1f;
        [SerializeField, Range(0f, 10f)] protected float _defaultDrainRate = 0.5f;
        [SerializeField, Range(0f, 100f)] protected float _bubbleThreshold = 80f;
        [SerializeField] protected AnimationCurve _intensityInfluence;
        
        public virtual EmotionTypes Type => _type;
        public virtual Color BaseColor => _baseColor;
        public virtual float MaxCapacity => _maxCapacity;
        public virtual float DefaultFillRate => _defaultFillRate;
        public virtual float DefaultDrainRate => _defaultDrainRate;
        public virtual float BubbleThreshold => _bubbleThreshold;
        
        public virtual float GetIntensityMultiplier(float intensity)
        {
            return _intensityInfluence != null ? _intensityInfluence.Evaluate(Mathf.Clamp01(intensity)) : 1f;
        }
    }
} 