using App.App.Develop.Scenes.PersonalAreaScene.UI.Base;
using App.Develop.CommonServices.Emotion;
using App.Develop.Scenes.PersonalAreaScene.UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace App.Develop.Scenes.PersonalAreaScene.UI.Components
{
    /// <summary>
    /// Компонент для управления пузырями, вылетающими из банок
    /// </summary>
    public class BubbleComponent : BaseUIElement
    {
        #region SerializeFields
        [Header("Bubble Settings")]
        [SerializeField] private Image _bubbleImage;          // Изображение пузыря
        [SerializeField] private float _floatSpeed = 50f;     // Скорость подъема
        [SerializeField] private float _wobbleAmount = 20f;   // Амплитуда колебаний
        [SerializeField] private float _wobbleSpeed = 2f;     // Частота колебаний
        [SerializeField] private float _lifetime = 5f;        // Время жизни пузыря
        [SerializeField] private float _fadeSpeed = 0.5f;     // Скорость исчезновения
        #endregion

        #region Private Fields
        private EmotionTypes _emotionType;
        private float _initialX;
        private float _elapsedTime = 0f;
        private bool _isFading = false;
        #endregion

        #region Unity Methods
        protected override void ValidateReferences()
        {
            if (_bubbleImage == null) 
            {
                _bubbleImage = GetComponent<Image>();
                if (_bubbleImage == null)
                {
                    LogError("Изображение пузыря не найдено");
                }
            }
        }

        private void Start()
        {
            _initialX = transform.position.x;
            Destroy(gameObject, _lifetime);
        }

        private void Update()
        {
            _elapsedTime += Time.deltaTime;
            
            // Движение вверх
            transform.position += Vector3.up * _floatSpeed * Time.deltaTime;
            
            // Колебания в стороны
            float xOffset = Mathf.Sin(_elapsedTime * _wobbleSpeed) * _wobbleAmount * Time.deltaTime;
            transform.position = new Vector3(_initialX + xOffset, transform.position.y, transform.position.z);
            
            // Уменьшение прозрачности в конце жизни
            if (_elapsedTime > _lifetime * 0.7f && !_isFading)
            {
                _isFading = true;
            }
            
            if (_isFading && _bubbleImage != null)
            {
                Color color = _bubbleImage.color;
                color.a = Mathf.Max(0, color.a - _fadeSpeed * Time.deltaTime);
                _bubbleImage.color = color;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Инициализирует пузырь
        /// </summary>
        public void Initialize(EmotionTypes type, Color bubbleColor)
        {
            ValidateReferences();
            
            _emotionType = type;
            
            if (_bubbleImage != null)
            {
                _bubbleImage.color = bubbleColor;
            }
            
            // Случайный размер пузыря
            float randomScale = Random.Range(0.5f, 1.5f);
            transform.localScale = new Vector3(randomScale, randomScale, 1f);
            
            // Немного случайности в параметрах движения
            _floatSpeed *= Random.Range(0.8f, 1.2f);
            _wobbleAmount *= Random.Range(0.8f, 1.2f);
            _wobbleSpeed *= Random.Range(0.8f, 1.2f);
        }
        #endregion
    }
} 