using System;
using System.Collections.Generic;
using System.Linq;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.Configs.Common.Emotion;
using UnityEngine;
using System.Threading.Tasks;

namespace App.Develop.CommonServices.Emotion
{
    public class EmotionConfigService
    {
        private readonly IAssetLoader _assetLoader;
        private readonly Dictionary<EmotionTypes, EmotionConfig> _emotionConfigs;
        private StartEmotionConfig _startConfig;
        private bool _isInitialized = false;

        private const string EmotionConfigsAddressableGroup = "Configs/Common/Emotion/";
        private const string StartConfigAddressableKey = AssetAddresses.StartEmotionConfig;

        public EmotionConfigService(IAssetLoader assetLoader)
        {
            _assetLoader = assetLoader ?? throw new ArgumentNullException(nameof(assetLoader));
            _emotionConfigs = new Dictionary<EmotionTypes, EmotionConfig>();
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            _startConfig = await _assetLoader.LoadAssetAsync<StartEmotionConfig>(StartConfigAddressableKey);
            if (_startConfig == null)
            {
                Debug.LogError($"Failed to load StartEmotionConfig from Addressable: {StartConfigAddressableKey}!");
                _isInitialized = true;
                return;
            }
            Debug.Log($"✅ StartEmotionConfig ({StartConfigAddressableKey}) успешно загружен в EmotionConfigService");

            List<Task> loadingTasks = new List<Task>();
            foreach (EmotionTypes type in _startConfig.GetAllEmotionTypes())
            {
                loadingTasks.Add(LoadSingleEmotionConfigAsync(type));
            }
            await Task.WhenAll(loadingTasks);
            Debug.Log("✅ Все EmotionConfig для EmotionConfigService были запрошены для загрузки.");
            _isInitialized = true;
        }

        private async Task LoadSingleEmotionConfigAsync(EmotionTypes type)
        {
            string configKey = GetEmotionConfigKey(type);
            var config = await _assetLoader.LoadAssetAsync<EmotionConfig>(configKey);
            
            if (config != null)
            {
                _emotionConfigs[type] = config;
                Debug.Log($"✅ EmotionConfigService: Загружен конфиг для {type} по ключу {configKey}");
            }
            else
            {
                Debug.LogWarning($"⚠️ EmotionConfigService: Не найден конфиг для {type} по ключу {configKey}");
            }
        }

        // Получение правильного ключа для каждого типа эмоции из AssetAddresses
        private string GetEmotionConfigKey(EmotionTypes type)
        {
            return type switch
            {
                EmotionTypes.Joy => AssetAddresses.JoyConfig,
                EmotionTypes.Sadness => AssetAddresses.SadnessConfig,
                EmotionTypes.Anger => AssetAddresses.AngerConfig,
                EmotionTypes.Fear => AssetAddresses.FearConfig,
                EmotionTypes.Disgust => AssetAddresses.DisgustConfig,
                EmotionTypes.Trust => AssetAddresses.TrustConfig,
                EmotionTypes.Anticipation => AssetAddresses.AnticipationConfig,
                EmotionTypes.Surprise => AssetAddresses.SurpriseConfig, 
                EmotionTypes.Love => AssetAddresses.LoveConfig,
                EmotionTypes.Anxiety => AssetAddresses.AnxietyConfig,
                EmotionTypes.Neutral => AssetAddresses.NeutralConfig,
                _ => $"{EmotionConfigsAddressableGroup}{type}Config" // Fallback для новых типов
            };
        }

        public EmotionConfig GetConfig(EmotionTypes type)
        {
            if (!_isInitialized) Debug.LogWarning("EmotionConfigService не инициализирован! Конфиги могут быть не загружены.");
            return _emotionConfigs.TryGetValue(type, out var config) ? config : null;
        }

        public (float Value, Color Color) GetStartValue(EmotionTypes type)
        {
            if (!_isInitialized) Debug.LogWarning("EmotionConfigService не инициализирован! StartConfig может быть не загружен.");
            return _startConfig != null 
                ? _startConfig.GetStartValueFor(type) 
                : (0f, Color.white);
        }

        public IEnumerable<EmotionTypes> GetAllEmotionTypes()
        {
            if (!_isInitialized) Debug.LogWarning("EmotionConfigService не инициализирован! StartConfig может быть не загружен.");
            return _startConfig?.GetAllEmotionTypes() ?? Enumerable.Empty<EmotionTypes>();
        }

        public bool HasConfig(EmotionTypes type)
        {
            if (!_isInitialized) Debug.LogWarning("EmotionConfigService не инициализирован!");
            return _emotionConfigs.ContainsKey(type);
        }
    }
} 