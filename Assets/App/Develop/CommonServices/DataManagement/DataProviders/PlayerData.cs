using System;

namespace App.Develop.CommonServices.DataManagement.DataProviders
{
    [Serializable]
    public class PlayerData : ISaveData
    {
        //Поля должны быть публичные, а класс с аргументом Serializable для корректного сохранения
        public int LastEmotion;
        public int CurrentEmotion;
    }
}
