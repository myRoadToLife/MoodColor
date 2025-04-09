using App.Develop.CommonServices.AssetManagement;
using App.Develop.Configs.Common.Emotion;

namespace App.Develop.CommonServices.ConfigsManagement
{
    public class ConfigsProviderService
    {
        private ResourcesAssetLoader _resourcesAssetLoader;

        public ConfigsProviderService(ResourcesAssetLoader resourcesAssetLoader)
        {
            _resourcesAssetLoader = resourcesAssetLoader;
        }

        public StartEmotionConfig StartEmotionConfig { get; private set; }

        public void LoadAll()
        {
            //Подгружать конфиги из ресурсов
            LoadStartEmotionConfig();
        }

        private void LoadStartEmotionConfig()
            => StartEmotionConfig = _resourcesAssetLoader.LoadResource<StartEmotionConfig>("Configs/Common/Emotion/StartEmotionConfig");
    }
}
