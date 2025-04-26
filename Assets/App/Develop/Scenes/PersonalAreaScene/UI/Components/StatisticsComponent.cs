using TMPro;
using UnityEngine;

namespace App.Develop.Scenes.PersonalAreaScene.UI.Components
{
    public class StatisticsComponent : MonoBehaviour
    {
        private const string POINTS_FORMAT = "Очки: {0}";
        private const string ENTRIES_FORMAT = "Записей: {0}";

        [SerializeField] private TMP_Text _pointsText;
        [SerializeField] private TMP_Text _entriesText;

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

        public void Clear()
        {
            SetPoints(0);
            SetEntries(0);
        }
    }
} 