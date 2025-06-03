using App.App.Develop.Scenes.PersonalAreaScene.UI.Base;
using App.Develop.Scenes.PersonalAreaScene.UI.Base;
using App.Develop.CommonServices.Emotion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace App.App.Develop.Scenes.PersonalAreaScene.UI.Components
{
    public class StatisticsView : BaseUIElement, IUIComponent
    {
        #region Constants
        private const string POINTS_FORMAT = "Очки: {0}";
        private const string ENTRIES_FORMAT = "Записей: {0}";
        private const string REGION_FORMAT = "{0}: {1}";
        private const string NO_REGIONAL_DATA = "Нет данных по районам";
        #endregion

        #region SerializeFields
        [Header("Statistics")]
        [SerializeField] private TMP_Text _pointsText;
        [SerializeField] private TMP_Text _entriesText;

        [Header("Regional Statistics")]
        [SerializeField] private Transform _regionalStatsContainer;
        [SerializeField] private GameObject _regionStatItemPrefab;
        [SerializeField] private TMP_Text _noRegionalDataText;
        [SerializeField] private TMP_Text _regionalStatsTitle;
        #endregion

        #region Private Fields
        private List<GameObject> _regionStatItems = new List<GameObject>();
        #endregion

        #region Unity Methods
        protected override void ValidateReferences()
        {
            if (_pointsText == null) LogWarning("Текст очков не назначен в инспекторе");
            if (_entriesText == null) LogWarning("Текст записей не назначен в инспекторе");
            if (_regionalStatsContainer == null) LogWarning("Контейнер региональной статистики не назначен в инспекторе");
            if (_regionStatItemPrefab == null) LogWarning("Префаб элемента региональной статистики не назначен в инспекторе");
            if (_noRegionalDataText == null) LogWarning("Текст отсутствия региональных данных не назначен в инспекторе");
            if (_regionalStatsTitle == null) LogWarning("Заголовок региональной статистики не назначен в инспекторе");
        }
        #endregion

        #region Public Methods
        public void Initialize()
        {
            Clear();
            InitializeRegionalStats();
        }

        public void Clear()
        {
            SetPoints(0);
            SetEntries(0);
            ClearRegionalStats();
        }

        public void SetPoints(int points)
        {
            if (_pointsText == null) return;
            _pointsText.text = string.Format(POINTS_FORMAT, points);
        }

        public void SetEntries(int entries)
        {
            if (_entriesText == null) return;
            _entriesText.text = string.Format(ENTRIES_FORMAT, entries);
        }

        public void SetRegionalStats(Dictionary<string, RegionalEmotionStats> regionalStats)
        {
            ClearRegionalStats();

            if (regionalStats == null || regionalStats.Count == 0)
            {
                ShowNoRegionalData();
                return;
            }

            HideNoRegionalData();

            foreach (KeyValuePair<string, RegionalEmotionStats> kvp in regionalStats)
            {
                CreateRegionStatItem(kvp.Key, kvp.Value);
            }
        }
        #endregion

        #region Private Methods
        private void InitializeRegionalStats()
        {
            if (_regionalStatsTitle != null)
            {
                _regionalStatsTitle.text = "Эмоции по районам";
            }

            ShowNoRegionalData();
        }

        private void ClearRegionalStats()
        {
            foreach (GameObject item in _regionStatItems)
            {
                if (item != null)
                {
                    DestroyImmediate(item);
                }
            }
            _regionStatItems.Clear();
        }

        private void CreateRegionStatItem(string regionName, RegionalEmotionStats stats)
        {
            if (_regionStatItemPrefab == null || _regionalStatsContainer == null) return;

            GameObject item = Instantiate(_regionStatItemPrefab, _regionalStatsContainer);
            _regionStatItems.Add(item);

            RegionStatItemView regionView = item.GetComponent<RegionStatItemView>();
            if (regionView != null)
            {
                regionView.Setup(regionName, stats);
            }
            else
            {
                // Fallback: если нет компонента RegionStatItemView, используем простой текст
                TMP_Text textComponent = item.GetComponent<TMP_Text>();
                if (textComponent != null)
                {
                    string dominantEmotionName = GetEmotionDisplayName(stats.DominantEmotion);
                    textComponent.text = string.Format(REGION_FORMAT, regionName, dominantEmotionName);
                    textComponent.color = GetEmotionColor(stats.DominantEmotion);
                }
            }
        }

        private void ShowNoRegionalData()
        {
            if (_noRegionalDataText != null)
            {
                _noRegionalDataText.gameObject.SetActive(true);
                _noRegionalDataText.text = NO_REGIONAL_DATA;
            }
        }

        private void HideNoRegionalData()
        {
            if (_noRegionalDataText != null)
            {
                _noRegionalDataText.gameObject.SetActive(false);
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

    #region Data Models
    [System.Serializable]
    public class RegionalEmotionStats
    {
        public EmotionTypes DominantEmotion { get; set; }
        public Dictionary<EmotionTypes, int> EmotionCounts { get; set; }
        public int TotalEmotions { get; set; }
        public float DominantEmotionPercentage { get; set; }

        public RegionalEmotionStats()
        {
            EmotionCounts = new Dictionary<EmotionTypes, int>();
        }
    }
    #endregion
}