using System;
using System.Collections.Generic;
using App.Develop.CommonServices.Emotion;

namespace App.Develop.CommonServices.DataManagement.DataProviders
{
    [Serializable]
    public class PlayerData : ISaveData
    {
        public Dictionary<EmotionTypes, EmotionData> EmotionData { get; set; }

        public PlayerData()
        {
            EmotionData = new Dictionary<EmotionTypes, EmotionData>();
        }
    }
}