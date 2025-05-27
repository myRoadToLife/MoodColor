using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.Emotion;
using App.Develop.Configs.Common.Emotion;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace App.Develop.CommonServices.ConfigsManagement
{
    public class ConfigsProviderService : IConfigsProvider
    {
        private readonly IAssetLoader _assetLoader;
        private readonly Dictionary<EmotionTypes, EmotionConfig> _emotionConfigs;
        private bool _isInitialized = false;

        private const string EmotionConfigsAddressableGroup = "Configs/Common/Emotion/";
        // Используем константу из AssetAddresses вместо создания своей
        private const string StartConfigAddressableKey = AssetAddresses.StartEmotionConfig;

        public StartEmotionConfig StartEmotionConfig { get; private set; }

        public ConfigsProviderService(IAssetLoader assetLoader)
        {
            _assetLoader = assetLoader ?? throw new ArgumentNullException(nameof(assetLoader));
            _emotionConfigs = new Dictionary<EmotionTypes, EmotionConfig>();
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await LoadStartEmotionConfigAsync();
            await LoadAllEmotionConfigsAsync();
            _isInitialized = true;
        }

        public EmotionConfig LoadEmotionConfig(EmotionTypes type)
        {
            if (!_isInitialized)
            {
                // LogWarning removed. Caller might get null if not initialized.
            }
            return _emotionConfigs.TryGetValue(type, out EmotionConfig cachedConfig) ? cachedConfig : null;
        }

        public IEnumerable<EmotionTypes> GetAllEmotionTypes()
        {
            if (!_isInitialized)
            {
                // LogWarning removed.
            }
            return _emotionConfigs.Keys;
        }

        public bool HasConfig(EmotionTypes type)
        {
            if (!_isInitialized)
            {
                // LogWarning removed.
            }
            return _emotionConfigs.ContainsKey(type);
        }

        public IReadOnlyDictionary<EmotionTypes, EmotionConfig> GetAllConfigs()
        {
            if (!_isInitialized)
            {
                // LogWarning removed.
            }
            return _emotionConfigs;
        }

        private async Task LoadStartEmotionConfigAsync()
        {
            try
            {
                StartEmotionConfig = await _assetLoader.LoadAssetAsync<StartEmotionConfig>(StartConfigAddressableKey);
                if (StartEmotionConfig == null)
                {
                    throw new InvalidOperationException($"StartEmotionConfig not found at Addressable key: {StartConfigAddressableKey}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading StartEmotionConfig (key: {StartConfigAddressableKey}): {ex.Message}", ex);
            }
        }

        private async Task LoadAllEmotionConfigsAsync()
        {
            List<Task> loadingTasks = new List<Task>();
            foreach (EmotionTypes type in Enum.GetValues(typeof(EmotionTypes)))
            {
                loadingTasks.Add(LoadSingleEmotionConfigAsync(type));
            }
            await Task.WhenAll(loadingTasks);
        }

        private async Task LoadSingleEmotionConfigAsync(EmotionTypes type)
        {
            try
            {
                // Получаем правильный ключ Addressable из констант в AssetAddresses
                string configKey = GetEmotionConfigKey(type);
                EmotionConfig config = await _assetLoader.LoadAssetAsync<EmotionConfig>(configKey);

                if (config != null)
                {
                    _emotionConfigs[type] = config;
                }
                else
                {
                    throw new InvalidOperationException($"Config not found for emotion {type} with key {configKey}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading config for {type}: {ex.Message}", ex);
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
    }
}
