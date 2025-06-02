using System;
using System.Collections.Generic;
using App.App.Develop.Scenes.PersonalAreaScene.UI.Base;
using App.Develop.CommonServices.Emotion;
using App.Develop.Scenes.PersonalAreaScene.UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace App.Develop.Scenes.PersonalAreaScene.UI.Components
{
    public class EmotionJarView : BaseUIElement, IUIComponent
    {
        #region Constants
        private const int DEFAULT_CAPACITY = 100;
        #endregion

        #region SerializeFields
        [Header("Emotion Jars")]
        [SerializeField] private Image _joyJarFill;
        [SerializeField] private Image _sadnessJarFill;
        [SerializeField] private Image _angerJarFill;
        [SerializeField] private Image _fearJarFill;
        [SerializeField] private Image _disgustJarFill;
        [SerializeField] private Image _trustJarFill;
        [SerializeField] private Image _anticipationJarFill;
        [SerializeField] private Image _surpriseJarFill;
        [SerializeField] private Image _loveJarFill;
        [SerializeField] private Image _anxietyJarFill;
        [SerializeField] private Image _neutralJarFill;

        [Header("Bubbles")]
        [SerializeField] private Transform _bubblesContainer; // Контейнер для пузырей
        [SerializeField] private GameObject _bubblePrefab; // Префаб пузыря
        [SerializeField] private float _bubbleGenerationRate = 0.5f; // Частота генерации пузырей (в секундах)
        #endregion

        #region Private Fields
        private Dictionary<EmotionTypes, Image> _emotionJars;
        private Dictionary<EmotionTypes, float> _bubbleTimers = new Dictionary<EmotionTypes, float>();
        private Dictionary<EmotionTypes, int> _jarAmounts = new Dictionary<EmotionTypes, int>();
        #endregion

        #region Unity Methods
        private void Awake()
        {
            InitializeJarsDictionary();
        }

        private void Update()
        {
            // Обновляем генерацию пузырей
            UpdateBubbleGeneration();
        }

        protected override void ValidateReferences()
        {
#if UNITY_EDITOR
            // Проверяем обязательные компоненты для пузырей
            if (_bubblesContainer == null) 
            {
                LogWarning("Bubbles container is not assigned in the inspector");
            }
            if (_bubblePrefab == null) 
            {
                LogWarning("Bubble prefab is not assigned in the inspector");
            }

            // Проверяем jar'ы - делаем их опциональными
            ValidateJarReference(_joyJarFill, "Joy jar");
            ValidateJarReference(_sadnessJarFill, "Sadness jar");
            ValidateJarReference(_angerJarFill, "Anger jar");
            ValidateJarReference(_fearJarFill, "Fear jar");
            ValidateJarReference(_disgustJarFill, "Disgust jar");
            ValidateJarReference(_trustJarFill, "Trust jar");
            ValidateJarReference(_anticipationJarFill, "Anticipation jar");
            ValidateJarReference(_surpriseJarFill, "Surprise jar");
            ValidateJarReference(_loveJarFill, "Love jar");
            ValidateJarReference(_anxietyJarFill, "Anxiety jar");
            ValidateJarReference(_neutralJarFill, "Neutral jar");
#endif
        }

#if UNITY_EDITOR
        private void ValidateJarReference(Image jarFill, string jarName)
        {
            if (jarFill == null) 
            {
                LogWarning($"{jarName} is not assigned in the inspector. This jar will be ignored.");
            }
        }
#endif
        #endregion

        #region Public Methods
        public void Initialize()
        {
            try
            {
                InitializeJarsDictionary();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"EmotionJarView: Initialization error - {ex.Message}", ex);
            }
        }

        public void Clear()
        {
            if (_emotionJars == null) return;

            foreach (EmotionTypes type in Enum.GetValues(typeof(EmotionTypes)))
            {
                SetJar((EmotionTypes)type, 0);
            }
        }

        public void SetJar(EmotionTypes type, int amount, int capacity = DEFAULT_CAPACITY)
        {
            if (_emotionJars == null)
            {
                throw new InvalidOperationException("Emotion jars dictionary is not initialized");
            }

            // Сохраняем текущее количество эмоции для генерации пузырей в любом случае
            _jarAmounts[type] = amount;

            // Проверяем наличие банки для данной эмоции
            if (!_emotionJars.TryGetValue(type, out Image jarFill))
            {
                // Добавляем тип в словарь, но с null изображением
                _emotionJars[type] = null;
                return;
            }

            // Если изображение не назначено, просто сохраняем значение
            if (jarFill == null)
            {
                return;
            }

            // Обновляем заполнение банки
            float fillAmount = capacity > 0 ? (float)amount / capacity : 0;
            jarFill.fillAmount = Mathf.Clamp01(fillAmount);
        }
        #endregion

        #region Private Methods
        private void InitializeJarsDictionary()
        {
            _emotionJars = new Dictionary<EmotionTypes, Image>
            {
                { EmotionTypes.Joy, _joyJarFill },
                { EmotionTypes.Sadness, _sadnessJarFill },
                { EmotionTypes.Anger, _angerJarFill },
                { EmotionTypes.Fear, _fearJarFill },
                { EmotionTypes.Disgust, _disgustJarFill },
                { EmotionTypes.Trust, _trustJarFill },
                { EmotionTypes.Anticipation, _anticipationJarFill },
                { EmotionTypes.Surprise, _surpriseJarFill },
                { EmotionTypes.Love, _loveJarFill },
                { EmotionTypes.Anxiety, _anxietyJarFill },
                { EmotionTypes.Neutral, _neutralJarFill }
            };
            
            // Инициализируем словари таймеров и количества эмоций
            _bubbleTimers = new Dictionary<EmotionTypes, float>();
            _jarAmounts = new Dictionary<EmotionTypes, int>();
            
            // Добавляем все типы эмоций в словари
            foreach (EmotionTypes type in Enum.GetValues(typeof(EmotionTypes)))
            {
                _bubbleTimers[type] = 0f;
                _jarAmounts[type] = 0;
            }
        }

        private void UpdateBubbleGeneration()
        {
            if (_bubblePrefab == null || _bubblesContainer == null) return;
            
            foreach (EmotionTypes type in Enum.GetValues(typeof(EmotionTypes)))
            {
                if (!_bubbleTimers.ContainsKey(type) || !_jarAmounts.ContainsKey(type)) continue;

                // Обновляем таймер для текущей эмоции
                _bubbleTimers[type] += Time.deltaTime;

                // Если прошло достаточно времени и в банке есть эмоции
                if (_bubbleTimers[type] >= _bubbleGenerationRate && _jarAmounts[type] > 0)
                {
                    // Сбрасываем таймер
                    _bubbleTimers[type] = 0f;

                    // Создаем пузырь
                    CreateBubble(type);
                }
            }
        }

        private void CreateBubble(EmotionTypes type)
        {
            if (!_emotionJars.TryGetValue(type, out Image jar) || jar == null) return;

            // Создаем пузырь
            GameObject bubble = Instantiate(_bubblePrefab, _bubblesContainer);

            // Устанавливаем начальную позицию пузыря
            RectTransform jarRect = jar.rectTransform;
            RectTransform bubbleRect = bubble.GetComponent<RectTransform>();

            if (bubbleRect != null)
        {
                // Устанавливаем случайную позицию в нижней части банки
                float randomX = UnityEngine.Random.Range(-jarRect.rect.width / 2, jarRect.rect.width / 2);
                bubbleRect.anchoredPosition = new Vector2(randomX, -jarRect.rect.height / 2);
            
                // Добавляем компонент для анимации пузыря
                BubbleAnimation bubbleAnimation = bubble.AddComponent<BubbleAnimation>();
                bubbleAnimation.Initialize(jarRect);
            }
        }
        #endregion
    }

    public class BubbleAnimation : MonoBehaviour
    {
        private RectTransform _jarRect;
        private RectTransform _bubbleRect;
        private float _speed = 50f;
        private float _horizontalAmplitude = 20f;
        private float _frequency = 2f;
        private float _time;
        private float _initialX;

        public void Initialize(RectTransform jarRect)
        {
            _jarRect = jarRect;
            _bubbleRect = GetComponent<RectTransform>();
            _initialX = _bubbleRect.anchoredPosition.x;
            _time = UnityEngine.Random.Range(0f, Mathf.PI * 2);
        }

        private void Update()
        {
            if (_bubbleRect == null || _jarRect == null) return;

            _time += Time.deltaTime;

            // Вычисляем новую позицию
            float x = _initialX + Mathf.Sin(_time * _frequency) * _horizontalAmplitude;
            float y = _bubbleRect.anchoredPosition.y + _speed * Time.deltaTime;

            // Обновляем позицию
            _bubbleRect.anchoredPosition = new Vector2(x, y);

            // Если пузырь вышел за пределы банки, уничтожаем его
            if (y > _jarRect.rect.height / 2)
            {
                Destroy(gameObject);
            }
        }
    }
} 