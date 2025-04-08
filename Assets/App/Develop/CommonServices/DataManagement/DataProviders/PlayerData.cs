using System;
using System.Collections.Generic;
using App.Develop.CommonServices.Emotion;

namespace App.Develop.CommonServices.DataManagement.DataProviders
{
    [Serializable]
    public class PlayerData : ISaveData
    {
        //Поля должны быть публичные, а класс с аргументом Serializable для корректного сохранения
        public Dictionary<EmotionTypes, int> EmotionData;
    }
}
