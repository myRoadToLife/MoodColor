using System;
using System.Collections.Generic;
using System.Linq;
using App.Develop.CommonServices.Emotion;
using UnityEngine;
using App.Develop.Utils.Logging;

namespace App.Develop.Configs.Common.Emotion
{
    [CreateAssetMenu(fileName = "StartEmotionConfig", menuName = "MoodColor/StartEmotionConfig")]
    public class StartEmotionConfig : ScriptableObject
    {
        [System.Serializable]
        protected class StartValue
        {
            public EmotionTypes Type;
            [Range(0f, 100f)] public float Value;
            public Color Color;
        }

        [SerializeField] protected List<StartValue> _startValues = new List<StartValue>();

        private void OnEnable()
        {
            if (_startValues == null || _startValues.Count == 0)
            {
                InitializeDefaultValues();
            }
        }

        private void InitializeDefaultValues()
        {
            _startValues = new List<StartValue>
            {
                new StartValue { Type = EmotionTypes.Joy, Value = 50f, Color = BaseEmotionConfigs.Colors.Joy },
                new StartValue { Type = EmotionTypes.Sadness, Value = 20f, Color = BaseEmotionConfigs.Colors.Sadness },
                new StartValue { Type = EmotionTypes.Anger, Value = 10f, Color = BaseEmotionConfigs.Colors.Anger },
                new StartValue { Type = EmotionTypes.Fear, Value = 15f, Color = BaseEmotionConfigs.Colors.Fear },
                new StartValue { Type = EmotionTypes.Disgust, Value = 5f, Color = BaseEmotionConfigs.Colors.Disgust },
                new StartValue { Type = EmotionTypes.Trust, Value = 40f, Color = BaseEmotionConfigs.Colors.Trust },
                new StartValue { Type = EmotionTypes.Anticipation, Value = 30f, Color = BaseEmotionConfigs.Colors.Anticipation },
                new StartValue { Type = EmotionTypes.Surprise, Value = 0f, Color = BaseEmotionConfigs.Colors.Surprise },
                new StartValue { Type = EmotionTypes.Love, Value = 25f, Color = BaseEmotionConfigs.Colors.Love },
                new StartValue { Type = EmotionTypes.Anxiety, Value = 20f, Color = BaseEmotionConfigs.Colors.Anxiety },
                new StartValue { Type = EmotionTypes.Neutral, Value = 60f, Color = BaseEmotionConfigs.Colors.Neutral }
            };
        }

        public virtual (float Value, Color Color) GetStartValueFor(EmotionTypes type)
        {
            var startValue = _startValues?.FirstOrDefault(x => x.Type == type);
            if (startValue != null)
            {
                return (startValue.Value, startValue.Color);
            }

            MyLogger.LogWarning($"Start value not found for emotion type {type}. Using default values.", MyLogger.LogCategory.Emotion);
            return (0f, BaseEmotionConfigs.Colors.Neutral);
        }

        public virtual IEnumerable<EmotionTypes> GetAllEmotionTypes()
        {
            return _startValues?.Select(x => x.Type) ?? Enumerable.Empty<EmotionTypes>();
        }

        public virtual bool HasEmotionType(EmotionTypes type)
        {
            return _startValues?.Any(x => x.Type == type) ?? false;
        }
    }
}
