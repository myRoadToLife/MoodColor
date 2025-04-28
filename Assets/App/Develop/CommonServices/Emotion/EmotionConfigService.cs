using System;
using System.Collections.Generic;
using System.Linq;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.Configs.Common.Emotion;
using UnityEngine;

namespace App.Develop.CommonServices.Emotion
{
    public class EmotionConfigService
    {
        private readonly ResourcesAssetLoader _assetLoader;
        private readonly Dictionary<EmotionTypes, EmotionConfig> _emotionConfigs;
        private StartEmotionConfig _startConfig;

        private const string EmotionConfigsPath = "Configs/Common/Emotion/";
        private const string StartConfigPath = EmotionConfigsPath + "StartEmotionConfig";

        public EmotionConfigService(ResourcesAssetLoader assetLoader)
        {
            _assetLoader = assetLoader ?? throw new ArgumentNullException(nameof(assetLoader));
            _emotionConfigs = new Dictionary<EmotionTypes, EmotionConfig>();
            LoadConfigs();
        }

        private void LoadConfigs()
        {
            // Загружаем стартовый конфиг
            _startConfig = _assetLoader.LoadAsset<StartEmotionConfig>(StartConfigPath);
            if (_startConfig == null)
            {
                Debug.LogError("Failed to load StartEmotionConfig!");
                return;
            }

            // Загружаем конфиги для каждой эмоции
            foreach (EmotionTypes type in _startConfig.GetAllEmotionTypes())
            {
                string configPath = $"{EmotionConfigsPath}{type}Config";
                var config = _assetLoader.LoadAsset<EmotionConfig>(configPath);
                
                if (config != null)
                {
                    _emotionConfigs[type] = config;
                }
                else
                {
                    Debug.LogError($"Failed to load config for emotion type: {type}");
                }
            }
        }

        public EmotionConfig GetConfig(EmotionTypes type)
        {
            if (_emotionConfigs.TryGetValue(type, out var config))
            {
                return config;
            }

            Debug.LogWarning($"Config not found for emotion type: {type}");
            return null;
        }

        public (float Value, Color Color) GetStartValue(EmotionTypes type)
        {
            return _startConfig != null 
                ? _startConfig.GetStartValueFor(type) 
                : (0f, Color.white);
        }

        public IEnumerable<EmotionTypes> GetAllEmotionTypes()
        {
            return _startConfig?.GetAllEmotionTypes() ?? Enumerable.Empty<EmotionTypes>();
        }

        public bool HasConfig(EmotionTypes type)
        {
            return _emotionConfigs.ContainsKey(type);
        }
    }
} 