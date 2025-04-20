// PersonalAreaService.cs

using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion;
using App.Develop.Utils.Reactive;

namespace App.Develop.Scenes.PersonalAreaScene
{
    public class PersonalAreaService
    {
        private readonly EmotionService _emotionService;

        public PersonalAreaService(EmotionService emotionService)
        {
            _emotionService = emotionService;
        }

        public IReadOnlyVariable<EmotionData> GetEmotionVariable(EmotionTypes type)
            => _emotionService.GetEmotion(type);

        public void AddEmotion(EmotionTypes type, int amount)
            => _emotionService.AddEmotion(type, amount);

        public void SpendEmotion(EmotionTypes type, int amount)
            => _emotionService.SpendEmotion(type, amount);
    }
}
