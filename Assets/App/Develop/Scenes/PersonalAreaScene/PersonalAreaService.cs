// PersonalAreaService.cs

using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion;
using App.Develop.Utils.Reactive;
using UnityEngine;

namespace App.Develop.Scenes.PersonalAreaScene
{
    public interface IPersonalAreaService
    {
        IReadOnlyVariable<EmotionData> GetEmotionVariable(EmotionTypes type);
        void AddEmotion(EmotionTypes type, int amount);
        void SpendEmotion(EmotionTypes type, int amount);
    }

    public class PersonalAreaService : IPersonalAreaService
    {
        private readonly EmotionService _emotionService;

        public PersonalAreaService(EmotionService emotionService)
        {
            _emotionService = emotionService ?? throw new System.ArgumentNullException(nameof(emotionService));
        }

        public IReadOnlyVariable<EmotionData> GetEmotionVariable(EmotionTypes type)
        {
            if (!System.Enum.IsDefined(typeof(EmotionTypes), type))
            {
                Debug.LogWarning($"Неизвестный тип эмоции: {type}");
                return null;
            }

            return _emotionService.GetEmotion(type);
        }

        public void AddEmotion(EmotionTypes type, int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"Попытка добавить неположительное количество эмоций: {amount}");
                return;
            }

            if (!System.Enum.IsDefined(typeof(EmotionTypes), type))
            {
                Debug.LogWarning($"Неизвестный тип эмоции: {type}");
                return;
            }

            _emotionService.AddEmotion(type, amount);
        }

        public void SpendEmotion(EmotionTypes type, int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"Попытка потратить неположительное количество эмоций: {amount}");
                return;
            }

            if (!System.Enum.IsDefined(typeof(EmotionTypes), type))
            {
                Debug.LogWarning($"Неизвестный тип эмоции: {type}");
                return;
            }

            _emotionService.SpendEmotion(type, amount);
        }
    }
}
