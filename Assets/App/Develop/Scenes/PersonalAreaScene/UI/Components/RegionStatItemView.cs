using App.App.Develop.Scenes.PersonalAreaScene.UI.Components;
using App.Develop.CommonServices.Emotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace App.App.Develop.Scenes.PersonalAreaScene.UI.Components
{
    /// <summary>
    /// Компонент для отображения статистики по отдельному району
    /// </summary>
    public class RegionStatItemView : MonoBehaviour
    {
        #region Constants
        private const string REGION_NAME_FORMAT = "{0}";
        private const string DOMINANT_EMOTION_FORMAT = "Преобладает: {0}";
        private const string PERCENTAGE_FORMAT = "{0:F1}%";
        private const string TOTAL_COUNT_FORMAT = "Всего: {0}";
        #endregion

        #region SerializeFields
        [Header("UI Elements")]
        [SerializeField] private TMP_Text _regionNameText;
        [SerializeField] private TMP_Text _dominantEmotionText;
        [SerializeField] private TMP_Text _percentageText;
        [SerializeField] private TMP_Text _totalEmotionsText;
        [SerializeField] private Image _emotionColorIndicator;
        [SerializeField] private Transform _emotionListContainer;
        [SerializeField] private GameObject _emotionCountItemPrefab;
        #endregion

        #region Private Fields
        private RegionalEmotionStats _currentStats;
        #endregion

        #region Public Methods
        /// <summary>
        /// Настраивает отображение статистики для региона
        /// </summary>
        /// <param name="regionName">Название региона</param>
        /// <param name="stats">Статистика по эмоциям</param>
        public void Setup(string regionName, RegionalEmotionStats stats)
        {
            _currentStats = stats;

            SetupRegionInfo(regionName);
            SetupDominantEmotion();
            SetupEmotionCounts();
        }
        #endregion

        #region Private Methods
        private void SetupRegionInfo(string regionName)
        {
            if (_regionNameText != null)
            {
                _regionNameText.text = regionName;
            }

            if (_totalEmotionsText != null)
            {
                _totalEmotionsText.text = $"Всего: {_currentStats.TotalEmotions}";
            }
        }

        private void SetupDominantEmotion()
        {
            if (_dominantEmotionText != null)
            {
                string emotionName = GetEmotionDisplayName(_currentStats.DominantEmotion);
                _dominantEmotionText.text = $"Преобладает: {emotionName}";
            }

            if (_percentageText != null)
            {
                _percentageText.text = $"{_currentStats.DominantEmotionPercentage:F1}%";
            }

            if (_emotionColorIndicator != null)
            {
                _emotionColorIndicator.color = GetEmotionColor(_currentStats.DominantEmotion);
            }
        }

        private void SetupEmotionCounts()
        {
            if (_emotionListContainer == null || _emotionCountItemPrefab == null) return;

            // Очищаем предыдущие элементы
            foreach (Transform child in _emotionListContainer)
            {
                if (child != null)
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            // Создаем элементы для каждой эмоции (показываем топ-3)
            var sortedEmotions = new List<KeyValuePair<EmotionTypes, int>>();
            foreach (var kvp in _currentStats.EmotionCounts)
            {
                sortedEmotions.Add(kvp);
            }

            sortedEmotions.Sort((a, b) => b.Value.CompareTo(a.Value));

            int maxItems = Mathf.Min(3, sortedEmotions.Count);
            for (int i = 0; i < maxItems; i++)
            {
                var emotionKvp = sortedEmotions[i];
                CreateEmotionCountItem(emotionKvp.Key, emotionKvp.Value);
            }
        }

        private void CreateEmotionCountItem(EmotionTypes emotionType, int count)
        {
            GameObject item = Instantiate(_emotionCountItemPrefab, _emotionListContainer);

            TMP_Text textComponent = item.GetComponent<TMP_Text>();
            if (textComponent != null)
            {
                string emotionName = GetEmotionDisplayName(emotionType);
                float percentage = _currentStats.TotalEmotions > 0 ?
                    (count * 100f) / _currentStats.TotalEmotions : 0f;

                textComponent.text = $"{emotionName}: {count} ({percentage:F1}%)";
                textComponent.color = GetEmotionColor(emotionType);
            }

            Image imageComponent = item.GetComponent<Image>();
            if (imageComponent != null)
            {
                imageComponent.color = GetEmotionColor(emotionType);
            }
        }

        private string GetEmotionDisplayName(EmotionTypes emotionType)
        {
            return emotionType switch
            {
                EmotionTypes.Joy => "Радость",
                EmotionTypes.Sadness => "Грусть",
                EmotionTypes.Anger => "Гнев",
                EmotionTypes.Fear => "Страх",
                EmotionTypes.Disgust => "Отвращение",
                EmotionTypes.Trust => "Доверие",
                EmotionTypes.Anticipation => "Предвкушение",
                EmotionTypes.Surprise => "Удивление",
                EmotionTypes.Love => "Любовь",
                EmotionTypes.Anxiety => "Тревога",
                EmotionTypes.Neutral => "Нейтральное",
                _ => emotionType.ToString()
            };
        }

        private Color GetEmotionColor(EmotionTypes emotionType)
        {
            return emotionType switch
            {
                EmotionTypes.Joy => new Color(1f, 0.85f, 0.1f),      // Жёлтый
                EmotionTypes.Sadness => new Color(0.15f, 0.3f, 0.8f), // Синий
                EmotionTypes.Anger => new Color(0.9f, 0.1f, 0.1f),    // Красный
                EmotionTypes.Fear => new Color(0.5f, 0.1f, 0.6f),     // Фиолетовый
                EmotionTypes.Disgust => new Color(0.1f, 0.6f, 0.2f),  // Зелёный
                EmotionTypes.Trust => new Color(0f, 0.6f, 0.9f),      // Голубой
                EmotionTypes.Anticipation => new Color(1f, 0.5f, 0f), // Оранжевый
                EmotionTypes.Surprise => new Color(0.8f, 0.4f, 0.9f), // Розовый
                EmotionTypes.Love => new Color(0.95f, 0.3f, 0.6f),    // Малиновый
                EmotionTypes.Anxiety => new Color(0.7f, 0.7f, 0.7f),  // Серый
                EmotionTypes.Neutral => Color.white,                   // Белый
                _ => Color.white
            };
        }
        #endregion
    }
}