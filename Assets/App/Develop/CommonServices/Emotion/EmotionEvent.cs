using System;

namespace App.Develop.CommonServices.Emotion
{
    public enum EmotionEventType
    {
        ValueChanged,
        IntensityChanged,
        CapacityExceeded,
        BubbleCreated,
        EmotionMixed,
        EmotionDepleted
    }

    public class EmotionEvent : EventArgs
    {
        public EmotionTypes Type { get; }
        public EmotionEventType EventType { get; }
        public float Value { get; }
        public float Intensity { get; }
        public DateTime Timestamp { get; }
        
        public EmotionEvent(EmotionTypes type, EmotionEventType eventType, float value = 0, float intensity = 0)
        {
            Type = type;
            EventType = eventType;
            Value = value;
            Intensity = intensity;
            Timestamp = DateTime.Now;
        }
    }
} 