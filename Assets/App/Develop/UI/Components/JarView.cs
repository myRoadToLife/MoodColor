using App.Develop.AppServices.Firebase.Database.Models;
using App.Develop.CommonServices.Emotion;
using UnityEngine;
using UnityEngine.UI;

namespace App.Develop.UI.Components
{
    /// <summary>
    /// Простой компонент для визуализации банки с эмоцией
    /// </summary>
    public class JarView : MonoBehaviour
    {
        #region UI Elements
        [Header("UI Elements")]
        [SerializeField] private Image _jarImage;         // Изображение банки
        [SerializeField] private Image _liquidImage;      // Изображение жидкости внутри банки
        [SerializeField] private Text _emotionLabel;      // Текст с названием эмоции
        [SerializeField] private Text _amountLabel;       // Текст с количеством
        #endregion

        #region Private Fields
        private JarData _jarData;
        private EmotionTypes _emotionType;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_jarImage == null || _liquidImage == null || _emotionLabel == null || _amountLabel == null)
            {
                Debug.LogError("Не все UI компоненты присвоены");
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Инициализирует банку данными
        /// </summary>
        public void Initialize(JarData jarData, EmotionTypes emotionType)
        {
            _jarData = jarData;
            _emotionType = emotionType;
            
            UpdateVisuals();
        }

        /// <summary>
        /// Обновляет данные банки
        /// </summary>
        public void UpdateJarData(JarData jarData)
        {
            _jarData = jarData;
            UpdateVisuals();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Обновляет визуальное представление банки
        /// </summary>
        private void UpdateVisuals()
        {
            if (_jarData == null) return;
            
            // Обновляем текст с типом эмоции
            if (_emotionLabel != null)
            {
                _emotionLabel.text = _emotionType.ToString();
            }
            
            // Обновляем текст с количеством
            if (_amountLabel != null)
            {
                _amountLabel.text = $"{_jarData.CurrentAmount}/{_jarData.Capacity}";
            }
            
            // Обновляем заполнение жидкости (изменение размера)
            if (_liquidImage != null)
            {
                float fillAmount = (float)_jarData.CurrentAmount / _jarData.Capacity;
                _liquidImage.fillAmount = fillAmount;
            }

            // Обновляем цвет жидкости в зависимости от типа эмоции
            if (_liquidImage != null)
            {
                _liquidImage.color = GetColorForEmotion(_emotionType);
            }
        }

        /// <summary>
        /// Возвращает цвет для указанной эмоции
        /// </summary>
        private Color GetColorForEmotion(EmotionTypes emotionType)
        {
            switch (emotionType)
            {
                case EmotionTypes.Joy: return Color.yellow;
                case EmotionTypes.Sadness: return Color.blue;
                case EmotionTypes.Anger: return Color.red;
                case EmotionTypes.Fear: return new Color(0.5f, 0, 0.5f); // Фиолетовый
                case EmotionTypes.Disgust: return Color.green;
                default: return Color.gray;
            }
        }
        #endregion
    }
} 