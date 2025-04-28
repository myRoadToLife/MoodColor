using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.Emotion;
using App.Develop.Configs.Common.Emotion;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace App.Develop.CommonServices.ConfigsManagement
{
    public class ConfigsProviderService : IConfigsProvider
    {
        private readonly IResourcesLoader _resourcesLoader;
        private readonly Dictionary<EmotionTypes, EmotionConfig> _emotionConfigs;

        private const string EmotionConfigsPath = "Configs/Common/Emotion/";
        private const string StartConfigPath = EmotionConfigsPath + "StartEmotionConfig";

        public StartEmotionConfig StartEmotionConfig { get; private set; }

        public ConfigsProviderService(IResourcesLoader resourcesLoader)
        {
            _resourcesLoader = resourcesLoader ?? throw new ArgumentNullException(nameof(resourcesLoader));
            _emotionConfigs = new Dictionary<EmotionTypes, EmotionConfig>();
            
            LoadStartEmotionConfig();
            LoadEmotionConfigs();
        }

        private void LoadStartEmotionConfig()
        {
            try
            {
                StartEmotionConfig = _resourcesLoader.LoadAsset<StartEmotionConfig>(StartConfigPath);
                
                if (StartEmotionConfig == null)
                {
                    throw new InvalidOperationException("StartEmotionConfig не найден в ресурсах!");
                }
                
                Debug.Log("✅ StartEmotionConfig успешно загружен");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка загрузки StartEmotionConfig: {ex.Message}");
                throw;
            }
        }

        private void LoadEmotionConfigs()
        {
            foreach (EmotionTypes type in Enum.GetValues(typeof(EmotionTypes)))
            {
                try
                {
                    string configPath = $"{EmotionConfigsPath}{type}Config";
                    var config = _resourcesLoader.LoadAsset<EmotionConfig>(configPath);
                    
                    if (config != null)
                    {
                        _emotionConfigs[type] = config;
                        Debug.Log($"✅ Загружен конфиг для эмоции {type}");
                    }
                    else
                    {
                        Debug.LogError($"❌ Не найден конфиг для эмоции {type}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Ошибка загрузки конфига для {type}: {ex.Message}");
                }
            }
        }

        public EmotionConfig LoadEmotionConfig(EmotionTypes type)
        {
            // Проверяем кэш
            if (_emotionConfigs.TryGetValue(type, out var cachedConfig))
            {
                return cachedConfig;
            }

            try
            {
                string configPath = $"{EmotionConfigsPath}{type}Config";
                var config = _resourcesLoader.LoadAsset<EmotionConfig>(configPath);
                
                if (config != null)
                {
                    _emotionConfigs[type] = config;
                    return config;
                }
                
                Debug.LogError($"❌ Не удалось загрузить конфиг для эмоции {type}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка загрузки конфига для {type}: {ex.Message}");
                return null;
            }
        }

        public IEnumerable<EmotionTypes> GetAllEmotionTypes()
        {
            return _emotionConfigs.Keys;
        }

        public bool HasConfig(EmotionTypes type)
        {
            return _emotionConfigs.ContainsKey(type);
        }

        public IReadOnlyDictionary<EmotionTypes, EmotionConfig> GetAllConfigs()
        {
            return _emotionConfigs;
        }
    }
}
