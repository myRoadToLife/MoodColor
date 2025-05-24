using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.Emotion;
using App.Develop.Configs.Common.Emotion;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.ConfigsManagement
{
    public class ConfigsProviderService : IConfigsProvider
    {
        private readonly IAssetLoader _assetLoader;
        private readonly Dictionary<EmotionTypes, EmotionConfig> _emotionConfigs;

        private const string EmotionConfigsAddressableGroup = "Configs/Common/Emotion/";
        // Используем константу из AssetAddresses вместо создания своей
        private const string StartConfigAddressableKey = AssetAddresses.StartEmotionConfig;

        public StartEmotionConfig StartEmotionConfig { get; private set; }
        private bool _isInitialized = false;

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
            MyLogger.Log("✅ ConfigsProviderService инициализирован и все конфиги загружены.", MyLogger.LogCategory.Default);
        }

        private async Task LoadStartEmotionConfigAsync()
        {
            try
            {
                StartEmotionConfig = await _assetLoader.LoadAssetAsync<StartEmotionConfig>(StartConfigAddressableKey);
                
                if (StartEmotionConfig == null)
                {
                    throw new InvalidOperationException($"StartEmotionConfig не найден по ключу Addressable: {StartConfigAddressableKey}");
                }
                MyLogger.Log($"✅ StartEmotionConfig загружен успешно (ключ: {StartConfigAddressableKey}, MyLogger.LogCategory.Default)");
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка загрузки StartEmotionConfig (ключ: {StartConfigAddressableKey}, MyLogger.LogCategory.Default): {ex.Message}");
                throw;
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
            MyLogger.Log("✅ Все EmotionConfig были запрошены для загрузки.", MyLogger.LogCategory.Default);
        }

        private async Task LoadSingleEmotionConfigAsync(EmotionTypes type)
        {
            try
            {
                // Получаем правильный ключ Addressable из констант в AssetAddresses
                string configKey = GetEmotionConfigKey(type);
                var config = await _assetLoader.LoadAssetAsync<EmotionConfig>(configKey);
                
                if (config != null)
                {
                    _emotionConfigs[type] = config;
                    MyLogger.Log($"✅ Загружен конфиг для эмоции {type} по ключу {configKey}", MyLogger.LogCategory.Default);
                }
                else
                {
                    MyLogger.LogWarning($"⚠️ Не найден конфиг для эмоции {type} по ключу {configKey}", MyLogger.LogCategory.Default);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка загрузки конфига для {type}: {ex.Message}", MyLogger.LogCategory.Default);
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

        public EmotionConfig LoadEmotionConfig(EmotionTypes type)
        {
            if (!_isInitialized) MyLogger.LogWarning("ConfigsProviderService не инициализирован! Конфиги могут быть не загружены.", MyLogger.LogCategory.Default);
            return _emotionConfigs.TryGetValue(type, out var cachedConfig) ? cachedConfig : null;
        }

        public IEnumerable<EmotionTypes> GetAllEmotionTypes()
        {
            if (!_isInitialized) MyLogger.LogWarning("ConfigsProviderService не инициализирован!", MyLogger.LogCategory.Default);
            return _emotionConfigs.Keys;
        }

        public bool HasConfig(EmotionTypes type)
        {
            if (!_isInitialized) MyLogger.LogWarning("ConfigsProviderService не инициализирован!", MyLogger.LogCategory.Default);
            return _emotionConfigs.ContainsKey(type);
        }

        public IReadOnlyDictionary<EmotionTypes, EmotionConfig> GetAllConfigs()
        {
            if (!_isInitialized) MyLogger.LogWarning("ConfigsProviderService не инициализирован!", MyLogger.LogCategory.Default);
            return _emotionConfigs;
        }
    }
}
