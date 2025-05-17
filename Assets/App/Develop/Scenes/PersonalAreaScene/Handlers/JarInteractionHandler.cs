using UnityEngine;
using UnityEngine.EventSystems; 
using System; 
using App.Develop.DI;
using App.Develop.CommonServices.GameSystem; 
using App.Develop.CommonServices.Emotion;   
using App.Develop.Utils.Logging;
using Logger = App.Develop.Utils.Logging.Logger;

namespace App.Develop.Scenes.PersonalAreaScene.Handlers 
{
    public class JarInteractionHandler : MonoBehaviour, IInjectable
    {
        private IPointsService _pointsService;
        private const int POINTS_PER_JAR_CLICK = 10; // Количество очков за клик по банке (можно вынести в конфиг)
        private IEmotionService _emotionService;

        public void Inject(DIContainer container)
        {
            Logger.Log("[JarInteractionHandler] Inject method CALLED.");
            if (container == null)
            {
                Logger.LogError("[JarInteractionHandler] DIContainer is null in Inject method.");
                return;
            }
            
            _pointsService = container.Resolve<IPointsService>();
            if (_pointsService == null)
            {
                Logger.LogError("[JarInteractionHandler] IPointsService не удалось получить из DI контейнера!");
            }
            else
            {
                Logger.Log("[JarInteractionHandler] IPointsService успешно внедрен.");
            }

            _emotionService = container.Resolve<IEmotionService>();
            if (_emotionService == null)
            {
                Logger.LogError("[JarInteractionHandler] EmotionService не удалось получить из DI контейнера!");
            }
            else
            {
                Logger.Log("[JarInteractionHandler] EmotionService успешно внедрен.");
            }
        }

        public void OnJarClicked(string emotionTypeString)
        {
            if (_pointsService == null)
            {
                Logger.LogError("[JarInteractionHandler] _pointsService не инициализирован. Невозможно начислить очки. Убедитесь, что Inject был вызван.");
                return;
            }

            if (string.IsNullOrEmpty(emotionTypeString))
            {
                Logger.LogError("[JarInteractionHandler] Получена пустая строка типа эмоции.");
                return;
            }

            if (Enum.TryParse<EmotionTypes>(emotionTypeString, true, out EmotionTypes emotionType)) // true для case-insensitive
            {
                string detailedDescription = $"JarClick_{emotionType}";
                _pointsService.AddPoints(POINTS_PER_JAR_CLICK, PointsSource.JarInteraction, detailedDescription);
                Logger.Log($"[JarInteractionHandler] Клик по банке {emotionType}. Начислено {POINTS_PER_JAR_CLICK} очков. Источник: {PointsSource.JarInteraction}, Описание: {detailedDescription}. Текущие очки: {_pointsService.CurrentPoints}");

                // Вместо прямого изменения значения эмоции, логируем событие клика
                if (_emotionService != null)
                {
                    _emotionService.LogEmotionEvent(emotionType, EmotionEventType.JarClicked, "Jar Clicked");
                }
                else
                {
                    Logger.LogError("[JarInteractionHandler] EmotionService is not injected!");
                }

                // Опционально: если нужно немедленно увидеть какое-то изменение (например, анимацию банки),
                // но основное значение эмоции будет обновляться через другие механики или не будет меняться вовсе от простого клика.
            }
            else
            {
                Logger.LogError($"[JarInteractionHandler] Не удалось распознать тип эмоции из строки: {emotionTypeString}");
            }
        }
    }
} 