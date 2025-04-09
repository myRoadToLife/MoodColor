using System;
using System.Collections.Generic;
using System.Linq;
using App.Develop.CommonServices.Emotion;
using UnityEngine;

namespace App.Develop.Configs.Common.Emotion
{
    [CreateAssetMenu(menuName = "Configs/Common/Emotion/NewStartEmotionConfig", fileName = "StartEmotionConfig")]
    public class StartEmotionConfig : ScriptableObject
    {
        [SerializeField] private List<EmotionConfig> _values;

        public (int Value, Color Color) GetStartValueFor(EmotionTypes emotionType)
        {
            EmotionConfig config = _values.FirstOrDefault(config => config.Type == emotionType);

            if (config == null)
            {
                Debug.LogError($"No start value configured for emotion type: {emotionType}. Returning default.");
                return (0, Color.white);
            }

            return (config.Value, config.Color);
        }

        [Serializable]
        private class EmotionConfig
        {
            [field: SerializeField] public EmotionTypes Type { get; private set; }
            [field: SerializeField] public int Value { get; private set; }
            [field: SerializeField] public Color Color { get; private set; }
        }
    }
}
