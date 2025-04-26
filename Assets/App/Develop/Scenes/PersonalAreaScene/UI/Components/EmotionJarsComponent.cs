using System;
using System.Collections.Generic;
using App.Develop.CommonServices.Emotion;
using UnityEngine;
using UnityEngine.UI;

namespace App.Develop.Scenes.PersonalAreaScene.UI.Components
{
    public class EmotionJarsComponent : MonoBehaviour
    {
        private const int DEFAULT_CAPACITY = 100;

        [Header("Emotion Jars (Filled Images)")]
        [SerializeField] private Image _joyJarFill;
        [SerializeField] private Image _sadnessJarFill;
        [SerializeField] private Image _angerJarFill;
        [SerializeField] private Image _fearJarFill;
        [SerializeField] private Image _disgustJarFill;

        private Dictionary<EmotionTypes, Image> _emotionJars;

        private void Awake()
        {
            InitializeEmotionJars();
        }

        private void InitializeEmotionJars()
        {
            _emotionJars = new Dictionary<EmotionTypes, Image>();
            
            if (_joyJarFill != null) _emotionJars.Add(EmotionTypes.Joy, _joyJarFill);
            if (_sadnessJarFill != null) _emotionJars.Add(EmotionTypes.Sadness, _sadnessJarFill);
            if (_angerJarFill != null) _emotionJars.Add(EmotionTypes.Anger, _angerJarFill);
            if (_fearJarFill != null) _emotionJars.Add(EmotionTypes.Fear, _fearJarFill);
            if (_disgustJarFill != null) _emotionJars.Add(EmotionTypes.Disgust, _disgustJarFill);
            
            ValidateJars();
        }

        private void ValidateJars()
        {
            if (_joyJarFill == null) Debug.LogWarning("Банка Joy не назначена в инспекторе");
            if (_sadnessJarFill == null) Debug.LogWarning("Банка Sadness не назначена в инспекторе");
            if (_angerJarFill == null) Debug.LogWarning("Банка Anger не назначена в инспекторе");
            if (_fearJarFill == null) Debug.LogWarning("Банка Fear не назначена в инспекторе");
            if (_disgustJarFill == null) Debug.LogWarning("Банка Disgust не назначена в инспекторе");
        }

        public void SetJar(EmotionTypes type, int amount, int capacity = DEFAULT_CAPACITY)
        {
            if (_emotionJars == null)
            {
                Debug.LogWarning("Словарь банок эмоций не инициализирован");
                return;
            }

            if (!_emotionJars.TryGetValue(type, out Image jarFill))
            {
                Debug.LogWarning($"Неизвестный тип эмоции: {type}");
                return;
            }

            if (jarFill == null)
            {
                Debug.LogWarning($"Изображение заполнения для типа {type} не назначено");
                return;
            }

            float fillAmount = capacity > 0 ? (float)amount / capacity : 0;
            jarFill.fillAmount = Mathf.Clamp01(fillAmount);
        }

        public void Clear()
        {
            if (_emotionJars == null) return;
            
            foreach (var type in Enum.GetValues(typeof(EmotionTypes)))
            {
                try
                {
                    SetJar((EmotionTypes)type, 0);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Ошибка при очистке банки {type}: {ex.Message}");
                }
            }
        }
    }
} 