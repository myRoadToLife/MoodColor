using App.Develop.CommonServices.AssetManagement;
using App.Develop.Configs.Common.Emotion;
using UnityEngine;
using System;

namespace App.Develop.CommonServices.ConfigsManagement
{
    public class ConfigsProviderService
    {
        private ResourcesAssetLoader _resourcesLoader;

        public ConfigsProviderService(ResourcesAssetLoader resourcesLoader)
        {
            try
            {
                _resourcesLoader = resourcesLoader ?? throw new ArgumentNullException(nameof(resourcesLoader));
                
                // Загружаем конфигурацию эмоций
                StartEmotionConfig = _resourcesLoader.LoadResource<StartEmotionConfig>("Configs/Common/Emotion/StartEmotionConfig");
                
                if (StartEmotionConfig == null)
                {
                    Debug.LogError("❌ StartEmotionConfig не найден в ресурсах!");
                }
                else
                {
                    Debug.Log("✅ StartEmotionConfig успешно загружен");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка инициализации ConfigsProviderService: {ex.Message}");
                throw;
            }
        }

        public StartEmotionConfig StartEmotionConfig { get; private set; }

        public void LoadAll()
        {
            //Подгружать конфиги из ресурсов
            LoadStartEmotionConfig();
        }

        private void LoadStartEmotionConfig()
            => StartEmotionConfig = _resourcesLoader.LoadResource<StartEmotionConfig>("Configs/Common/Emotion/StartEmotionConfig");
    }
}
