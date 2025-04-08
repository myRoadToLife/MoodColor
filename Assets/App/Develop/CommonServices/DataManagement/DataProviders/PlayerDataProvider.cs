using System;
using System.Collections.Generic;
using App.Develop.CommonServices.Emotion;

namespace App.Develop.CommonServices.DataManagement.DataProviders
{
    public class PlayerDataProvider : DataProvider<PlayerData>
    {
        //Тут будем передавать сервис конфигов
        public PlayerDataProvider(ISaveLoadService saveLoadService) : base(saveLoadService)
        {
        }

        protected override PlayerData GetOrigenData()
        {
            return new PlayerData()
            {
                EmotionData = InitEmotionData()
            };
        }

        private Dictionary<EmotionTypes, int> InitEmotionData()
        {
            Dictionary<EmotionTypes, int> emotionData = new();

            foreach (EmotionTypes emotionType in Enum.GetValues(typeof(EmotionTypes)))
                emotionData.Add(emotionType, 0);

            return emotionData;
        }
    }
}
