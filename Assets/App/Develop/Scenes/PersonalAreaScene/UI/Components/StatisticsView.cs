using App.Develop.Scenes.PersonalAreaScene.UI.Base;
using TMPro;
using UnityEngine;

namespace App.Develop.Scenes.PersonalAreaScene.UI.Components
{
    public class StatisticsView : BaseUIElement, IUIComponent
    {
        #region Constants
        private const string POINTS_FORMAT = "Очки: {0}";
        private const string ENTRIES_FORMAT = "Записей: {0}";
        #endregion

        #region SerializeFields
        [Header("Statistics")]
        [SerializeField] private TMP_Text _pointsText;
        [SerializeField] private TMP_Text _entriesText;
        #endregion

        #region Unity Methods
        protected override void ValidateReferences()
        {
            if (_pointsText == null) LogWarning("Текст очков не назначен в инспекторе");
            if (_entriesText == null) LogWarning("Текст записей не назначен в инспекторе");
        }
        #endregion

        #region Public Methods
        public void Initialize()
        {
            Clear();
        }

        public void Clear()
        {
            SetPoints(0);
            SetEntries(0);
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
        #endregion
    }
} 