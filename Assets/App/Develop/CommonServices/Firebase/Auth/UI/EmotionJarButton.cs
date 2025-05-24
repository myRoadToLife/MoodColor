// Assets/App/Develop/MoodColor/UI/EmotionJarButton.cs

using System;
using App.Develop.CommonServices.Firebase.Database.Services;
using App.Develop.CommonServices.Emotion;
using UnityEngine;
using UnityEngine.EventSystems;
using App.Develop.Utils.Logging; // Для IPointerClickHandler

namespace App.Develop.MoodColor.UI
{
    public class EmotionJarButton : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private EmotionTypes _emotionType; // Укажи тип эмоции в инспекторе

        private EmotionSelectionManager _selectionManager;

        private void Start()
        {
            // Находим менеджер на сцене (или получаем через DI)
            _selectionManager = FindFirstObjectByType<EmotionSelectionManager>();
            if (_selectionManager == null)
            {
                MyLogger.LogError("EmotionSelectionManager не найден!", MyLogger.LogCategory.Firebase);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _selectionManager?.SelectEmotion(_emotionType);
        }
    }
}
