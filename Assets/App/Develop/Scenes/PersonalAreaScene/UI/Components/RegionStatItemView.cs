using App.Develop.CommonServices.Emotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.App.Develop.Scenes.PersonalAreaScene.UI.Components
{
    public class RegionStatItemView : MonoBehaviour
    {
        #region Constants
        private const string REGION_NAME_FORMAT = "{0}";
        private const string DOMINANT_EMOTION_FORMAT = "Преобладает: {0}";
        private const string PERCENTAGE_FORMAT = "{0:F1}%";
        private const string TOTAL_COUNT_FORMAT = "Всего: {0}";
        #endregion

        #region SerializeFields
        [Header("UI References")]
        [SerializeField] private TMP_Text _regionNameText;
        [SerializeField] private TMP_Text _dominantEmotionText;
        [SerializeField] private TMP_Text _percentageText;
        [SerializeField] private TMP_Text _totalCountText;
        [SerializeField] private Image _emotionColorIndicator;
        [SerializeField] private Image _backgroundImage;
        #endregion

        #region Private Fields
        private RegionalEmotionStats _currentStats;
        private string _regionName;
        #endregion

        #region Public Methods
        public void Setup(string regionName, RegionalEmotionStats stats)
        {
            _regionName = regionName;
            _currentStats = stats;
            
            UpdateDisplay();
        }
        #endregion

        #region Private Methods
        private void UpdateDisplay()
        {
            if (_currentStats == null) return;
            
            SetRegionName(_regionName);
            SetDominantEmotion(_currentStats.DominantEmotion);
            SetPercentage(_currentStats.DominantEmotionPercentage);
            SetTotalCount(_currentStats.TotalEmotions);
            SetEmotionColor(_currentStats.DominantEmotion);
        }

        private void SetRegionName(string regionName)
        {
            if (_regionNameText != null)
            {
                _regionNameText.text = string.Format(REGION_NAME_FORMAT, regionName);
            }
        }

        private void SetDominantEmotion(EmotionTypes emotionType)
        {
            if (_dominantEmotionText != null)
            {
                string emotionName = GetEmotionDisplayName(emotionType);
                _dominantEmotionText.text = string.Format(DOMINANT_EMOTION_FORMAT, emotionName);
            }
        }

        private void SetPercentage(float percentage)
        {
            if (_percentageText != null)
            {
                _percentageText.text = string.Format(PERCENTAGE_FORMAT, percentage);
            }
        }

        private void SetTotalCount(int totalCount)
        {
            if (_totalCountText != null)
            {
                _totalCountText.text = string.Format(TOTAL_COUNT_FORMAT, totalCount);
            }
        }

        private void SetEmotionColor(EmotionTypes emotionType)
        {
            Color emotionColor = GetEmotionColor(emotionType);
            
            if (_emotionColorIndicator != null)
            {
                _emotionColorIndicator.color = emotionColor;
            }
            
            if (_backgroundImage != null)
            {
                Color backgroundColor = emotionColor;
                backgroundColor.a = 0.1f; // Делаем фон полупрозрачным
                _backgroundImage.color = backgroundColor;
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
                EmotionTypes.Joy => new Color(1f, 0.85f, 0.1f),
                EmotionTypes.Sadness => new Color(0.15f, 0.3f, 0.8f),
                EmotionTypes.Anger => new Color(0.9f, 0.1f, 0.1f),
                EmotionTypes.Fear => new Color(0.5f, 0.1f, 0.6f),
                EmotionTypes.Disgust => new Color(0.1f, 0.6f, 0.2f),
                EmotionTypes.Trust => new Color(0f, 0.6f, 0.9f),
                EmotionTypes.Anticipation => new Color(1f, 0.5f, 0f),
                EmotionTypes.Surprise => new Color(0.8f, 0.4f, 0.9f),
                EmotionTypes.Love => new Color(0.95f, 0.3f, 0.6f),
                EmotionTypes.Anxiety => new Color(0.7f, 0.7f, 0.7f),
                EmotionTypes.Neutral => Color.white,
                _ => Color.white
            };
        }
        #endregion
    }
} 