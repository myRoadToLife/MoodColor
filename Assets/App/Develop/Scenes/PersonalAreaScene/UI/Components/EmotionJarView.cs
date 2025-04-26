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
        #endregion

        #region Private Fields
        private Dictionary<EmotionTypes, Image> _emotionJars;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            InitializeJarsDictionary();
        }

        protected override void ValidateReferences()
        {
            if (_joyJarFill == null) LogWarning("Банка Joy не назначена в инспекторе");
            if (_sadnessJarFill == null) LogWarning("Банка Sadness не назначена в инспекторе");
            if (_angerJarFill == null) LogWarning("Банка Anger не назначена в инспекторе");
            if (_fearJarFill == null) LogWarning("Банка Fear не назначена в инспекторе");
            if (_disgustJarFill == null) LogWarning("Банка Disgust не назначена в инспекторе");
        }
        #endregion

        #region Public Methods
        public void Initialize()
        {
            InitializeJarsDictionary();
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
        #endregion
    }
} 