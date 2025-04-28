using App.Develop.CommonServices.Emotion;
using App.Develop.Configs.Common.Emotion;
using System.Collections.Generic;

namespace App.Develop.CommonServices.ConfigsManagement
{
    public interface IConfigsProvider
    {
        StartEmotionConfig StartEmotionConfig { get; }
        EmotionConfig LoadEmotionConfig(EmotionTypes type);
        IEnumerable<EmotionTypes> GetAllEmotionTypes();
        bool HasConfig(EmotionTypes type);
        IReadOnlyDictionary<EmotionTypes, EmotionConfig> GetAllConfigs();
    }
} 