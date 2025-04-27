using System;
using System.Collections.Generic;
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
            if (_joyJarFill == null) LogWarning("Банка Joy не назначена в инспекторе");
            if (_sadnessJarFill == null) LogWarning("Банка Sadness не назначена в инспекторе");
            if (_angerJarFill == null) LogWarning("Банка Anger не назначена в инспекторе");
            if (_fearJarFill == null) LogWarning("Банка Fear не назначена в инспекторе");
            if (_disgustJarFill == null) LogWarning("Банка Disgust не назначена в инспекторе");
            
            // Проверяем компоненты для пузырей
            if (_bubblesContainer == null) LogWarning("Контейнер для пузырей не назначен");
            if (_bubblePrefab == null) LogWarning("Префаб пузыря не назначен");
        }
        #endregion

        #region Public Methods
        public void Initialize()
        {
            Debug.Log("EmotionJarView: Начало инициализации");
            try
            {
                InitializeJarsDictionary();
                Debug.Log("EmotionJarView: Инициализация завершена");
            }
            catch (Exception ex)
            {
                Debug.LogError($"EmotionJarView: Ошибка инициализации - {ex.Message}");
            }
        }

        public void Clear()
        {
            if (_emotionJars == null) return;

            foreach (var type in Enum.GetValues(typeof(EmotionTypes)))
            {
                SetJar((EmotionTypes)type, 0);
            }
        }

        public void SetJar(EmotionTypes type, int amount, int capacity = DEFAULT_CAPACITY)
        {
            if (_emotionJars == null)
            {
                LogWarning("Словарь банок эмоций не инициализирован");
                return;
            }

            if (!_emotionJars.TryGetValue(type, out Image jarFill))
            {
                LogWarning($"Неизвестный тип эмоции: {type}");
                return;
            }

            if (jarFill == null)
            {
                LogWarning($"Изображение заполнения для типа {type} не назначено");
                return;
            }

            float fillAmount = capacity > 0 ? (float)amount / capacity : 0;
            jarFill.fillAmount = Mathf.Clamp01(fillAmount);
            
            // Сохраняем текущее количество эмоции для генерации пузырей
            _jarAmounts[type] = amount;
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
                { EmotionTypes.Disgust, _disgustJarFill }
            };
        }

        #region Bubble Generation
        private void UpdateBubbleGeneration()
        {
            if (_bubblesContainer == null || _bubblePrefab == null) return;
            
            foreach (EmotionTypes type in Enum.GetValues(typeof(EmotionTypes)))
            {
                // Проверяем, есть ли таймер для этой эмоции
                if (!_bubbleTimers.ContainsKey(type))
                {
                    _bubbleTimers[type] = 0f;
                }
                
                // Проверяем, есть ли количество для этой эмоции
                if (!_jarAmounts.ContainsKey(type))
                {
                    _jarAmounts[type] = 0;
                }
                
                // Получаем текущее количество эмоции
                int amount = _jarAmounts[type];
                
                // Если в банке есть эмоция, генерируем пузыри
                if (amount > 0)
                {
                    // Увеличиваем таймер
                    _bubbleTimers[type] += Time.deltaTime;
                    
                    // Вычисляем интервал генерации (чем больше эмоции, тем чаще генерируются пузыри)
                    float interval = _bubbleGenerationRate * (1f - (float)amount / DEFAULT_CAPACITY);
                    interval = Mathf.Clamp(interval, 0.1f, _bubbleGenerationRate);
                    
                    // Если прошло достаточно времени, генерируем пузырь
                    if (_bubbleTimers[type] >= interval)
                    {
                        _bubbleTimers[type] = 0f;
                        GenerateBubble(type);
                    }
                }
            }
        }

        private void GenerateBubble(EmotionTypes type)
        {
            // Получаем банку для эмоции
            if (!_emotionJars.TryGetValue(type, out Image jarFill) || jarFill == null) return;
            
            // Создаем позицию для пузыря (в верхней части банки)
            Vector3 position = jarFill.transform.position;
            position.y += jarFill.rectTransform.rect.height * jarFill.fillAmount * 0.5f;
            
            // Создаем новый пузырь
            GameObject bubble = Instantiate(_bubblePrefab, position, Quaternion.identity, _bubblesContainer);
            
            // Настраиваем компоненты пузыря (размер, цвет, скорость движения)
            var bubbleComponent = bubble.GetComponent<BubbleComponent>();
            if (bubbleComponent != null)
            {
                bubbleComponent.Initialize(type, GetEmotionColor(type));
            }
            else
            {
                // Если нет компонента BubbleComponent, настраиваем базовые параметры
                var image = bubble.GetComponent<Image>();
                if (image != null)
                {
                    image.color = GetEmotionColor(type);
                }
                
                // Случайный размер пузыря
                float scale = UnityEngine.Random.Range(0.5f, 1.5f);
                bubble.transform.localScale = new Vector3(scale, scale, 1f);
                
                // Удаляем пузырь через несколько секунд
                Destroy(bubble, 5f);
            }
        }

        private Color GetEmotionColor(EmotionTypes type)
        {
            switch (type)
            {
                case EmotionTypes.Joy: return Color.yellow;
                case EmotionTypes.Sadness: return Color.blue;
                case EmotionTypes.Anger: return Color.red;
                case EmotionTypes.Fear: return new Color(0.5f, 0, 0.5f); // Фиолетовый
                case EmotionTypes.Disgust: return Color.green;
                default: return Color.white;
            }
        }
        #endregion
        #endregion
    }
} 