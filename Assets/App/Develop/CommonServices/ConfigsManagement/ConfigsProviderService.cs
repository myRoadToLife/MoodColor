using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.Emotion;
using App.Develop.Configs.Common.Emotion;
using UnityEngine;
using System.Collections.Generic;

namespace App.Develop.CommonServices.ConfigsManagement
{
    public class ConfigsProviderService
    {
        private readonly ResourcesAssetLoader _resourcesLoader;
        private readonly Dictionary<EmotionTypes, EmotionConfig> _emotionConfigs;

        public StartEmotionConfig StartEmotionConfig { get; private set; }

        public ConfigsProviderService(ResourcesAssetLoader resourcesLoader)
        {
            _resourcesLoader = resourcesLoader;
            _emotionConfigs = new Dictionary<EmotionTypes, EmotionConfig>();
            
            LoadStartEmotionConfig();
            LoadEmotionConfigs();
        }

        private void LoadStartEmotionConfig()
        {
            StartEmotionConfig = _resourcesLoader.LoadResource<StartEmotionConfig>("Configs/Common/Emotion/StartEmotionConfig");
            
            if (StartEmotionConfig == null)
            {
                Debug.LogError("❌ StartEmotionConfig не найден в ресурсах!");
                return;
            }
            
            Debug.Log("✅ StartEmotionConfig успешно загружен");
        }

        private void LoadEmotionConfigs()
        {
            foreach (EmotionTypes type in System.Enum.GetValues(typeof(EmotionTypes)))
            {
                var config = _resourcesLoader.LoadResource<EmotionConfig>($"Configs/Common/Emotion/{type}Config");
                if (config != null)
                {
                    _emotionConfigs[type] = config;
                    Debug.Log($"✅ Загружен конфиг для эмоции {type}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ Не найден конфиг для эмоции {type}");
                }
            }
        }

        public EmotionConfig LoadEmotionConfig(EmotionTypes type)
        {
            if (_emotionConfigs.TryGetValue(type, out var config))
            {
                return config;
            }
            
            Debug.LogWarning($"⚠️ Конфиг для эмоции {type} не найден");
            return null;
        }
    }
}
