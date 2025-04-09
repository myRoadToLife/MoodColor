using System;
using System.Collections.Generic;
using App.Develop.CommonServices.ConfigsManagement;
using App.Develop.CommonServices.Emotion;

namespace App.Develop.CommonServices.DataManagement.DataProviders
{
    public class PlayerDataProvider : DataProvider<PlayerData>
    {
        private ConfigsProviderService _configsProvider;

        public PlayerDataProvider(ISaveLoadService saveLoadService,
            ConfigsProviderService configsProviderService) : base(saveLoadService)
        {
            _configsProvider = configsProviderService;
        }

        protected override PlayerData GetOriginData()
        {
            return new PlayerData()
            {
                EmotionData = InitEmotionData()
            };
        }

        private Dictionary<EmotionTypes, EmotionData> InitEmotionData()
        {
            var emotionData = new Dictionary<EmotionTypes, EmotionData>();

            foreach (EmotionTypes emotionType in Enum.GetValues(typeof(EmotionTypes)))
            {
                var (value, color) = _configsProvider.StartEmotionConfig.GetStartValueFor(emotionType);
                emotionData.Add(emotionType, new EmotionData
                {
                    Value = value,
                    Color = color
                });
            }

            return emotionData;
        }
    }
}
