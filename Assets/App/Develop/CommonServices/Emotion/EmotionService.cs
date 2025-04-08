using System;
using System.Collections.Generic;
using System.Linq;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.Utils.Reactive;

namespace App.Develop.CommonServices.Emotion
{
    public class EmotionService : IDataReader<PlayerData>, IDataWriter<PlayerData>
    {
        private Dictionary<EmotionTypes, ReactiveVariable<int>> _emotions = new();

        public EmotionService(PlayerDataProvider playerDataProvider)
        {
            playerDataProvider.RegisterWriter(this);
            playerDataProvider.RegisterReader(this);
        }
        public List<EmotionTypes> AvailableEmotions => _emotions.Keys.ToList();

        public IReadOnlyVariable<int> GetEmotion(EmotionTypes types)
            => _emotions[types];

        public bool HasEnough(EmotionTypes types, int amount)
            => _emotions[types].Value >= amount;

        public void SpendEmotion(EmotionTypes types, int amount)
        {
            if (HasEnough(types, amount) == false)
                throw new ArgumentException(types.ToString());

            _emotions[types].Value -= amount;
        }

        public void AddEmotion(EmotionTypes types, int amount)
            => _emotions[types].Value += amount;


        public void ReadFrom(PlayerData data)
        {
            foreach (KeyValuePair<EmotionTypes, int> emotion in data.EmotionData)
            {
                if (_emotions.ContainsKey(emotion.Key))
                    _emotions[emotion.Key].Value = emotion.Value;
                else
                    _emotions.Add(emotion.Key, new ReactiveVariable<int>(emotion.Value));
            }
        }

        public void WriteTo(PlayerData data)
        {
            foreach (KeyValuePair<EmotionTypes, ReactiveVariable<int>> emotion in _emotions)
            {
                if (data.EmotionData.ContainsKey(emotion.Key))
                    data.EmotionData[emotion.Key] = emotion.Value.Value;
                else
                    data.EmotionData.Add(emotion.Key, emotion.Value.Value);
            }
        }
    }
}
