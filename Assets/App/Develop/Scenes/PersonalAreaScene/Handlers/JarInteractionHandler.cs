using UnityEngine;
using UnityEngine.EventSystems; 
using System; 
using App.Develop.DI;
using App.Develop.CommonServices.GameSystem; 
using App.Develop.CommonServices.Emotion;   

namespace App.Develop.Scenes.PersonalAreaScene.Handlers 
{
    public class JarInteractionHandler : MonoBehaviour, IInjectable
    {
        private IPointsService _pointsService;
        private const int POINTS_PER_JAR_CLICK = 10; // Количество очков за клик по банке (можно вынести в конфиг)

        public void Inject(DIContainer container)
        {
            Debug.Log("[JarInteractionHandler] Inject method CALLED.");
            if (container == null)
            {
                Debug.LogError("[JarInteractionHandler] DIContainer is null in Inject method.");
                return;
            }
            
            _pointsService = container.Resolve<IPointsService>();
            if (_pointsService == null)
            {
                Debug.LogError("[JarInteractionHandler] IPointsService не удалось получить из DI контейнера!");
            }
            else
            {
                Debug.Log("[JarInteractionHandler] IPointsService успешно внедрен.");
            }
        }

        public void OnJarClicked(string emotionTypeString)
        {
            if (_pointsService == null)
            {
                Debug.LogError("[JarInteractionHandler] _pointsService не инициализирован. Невозможно начислить очки. Убедитесь, что Inject был вызван.");
                return;
            }

            if (string.IsNullOrEmpty(emotionTypeString))
            {
                Debug.LogError("[JarInteractionHandler] Получена пустая строка типа эмоции.");
                return;
            }

            if (Enum.TryParse<EmotionTypes>(emotionTypeString, true, out EmotionTypes emotionType)) // true для case-insensitive
            {
                string detailedDescription = $"JarClick_{emotionType}";
                _pointsService.AddPoints(POINTS_PER_JAR_CLICK, PointsSource.JarInteraction, detailedDescription);
                Debug.Log($"[JarInteractionHandler] Клик по банке {emotionType}. Начислено {POINTS_PER_JAR_CLICK} очков. Источник: {PointsSource.JarInteraction}, Описание: {detailedDescription}. Текущие очки: {_pointsService.CurrentPoints}");

                // Опционально: можно вызвать событие, если EmotionJarView или другие системы должны отреагировать
                // Например: public static event Action<EmotionTypes> OnVisualJarClicked;
                // OnVisualJarClicked?.Invoke(emotionType);
            }
            else
            {
                Debug.LogError($"[JarInteractionHandler] Не удалось распознать тип эмоции из строки: {emotionTypeString}");
            }
        }
    }
} 