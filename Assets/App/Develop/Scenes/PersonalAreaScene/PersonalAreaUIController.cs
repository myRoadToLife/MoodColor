using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using App.Develop.CommonServices.Emotion;

namespace App.Develop.Scenes.PersonalAreaScene
{
    public class PersonalAreaUIController : MonoBehaviour
    {
        private const string POINTS_FORMAT = "Очки: {0}";
        private const string ENTRIES_FORMAT = "Записей: {0}";

        [Header("Profile Info")]
        [SerializeField] private TMP_Text _usernameText;
        [SerializeField] private Image _currentEmotionImage;

        [Header("Emotion Jars (Filled Images)")]
        [SerializeField] private Image _joyJarFill;
        [SerializeField] private Image _sadnessJarFill;
        [SerializeField] private Image _angerJarFill;
        [SerializeField] private Image _fearJarFill;

        [Header("Statistics")]
        [SerializeField] private TMP_Text _pointsText;
        [SerializeField] private TMP_Text _entriesText;

        [Header("Buttons")]
        [SerializeField] private Button _logEmotionButton;
        [SerializeField] private Button _historyButton;
        [SerializeField] private Button _friendsButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _workshopButton;

        private Dictionary<EmotionTypes, Image> _emotionJars;

        public event Action OnLogEmotion;
        public event Action OnOpenHistory;
        public event Action OnOpenFriends;
        public event Action OnOpenSettings;
        public event Action OnOpenWorkshop;

        private void Awake()
        {
            InitializeEmotionJars();
        }

        private void InitializeEmotionJars()
        {
            _emotionJars = new Dictionary<EmotionTypes, Image>
            {
                { EmotionTypes.Joy, _joyJarFill },
                { EmotionTypes.Sadness, _sadnessJarFill },
                { EmotionTypes.Anger, _angerJarFill },
                { EmotionTypes.Fear, _fearJarFill }
            };
        }

        public void Initialize()
        {
            InitializeButtons();
        }

        private void InitializeButtons()
        {
            SetupButton(_logEmotionButton, () => OnLogEmotion?.Invoke());
            SetupButton(_historyButton, () => OnOpenHistory?.Invoke());
            SetupButton(_friendsButton, () => OnOpenFriends?.Invoke());
            SetupButton(_settingsButton, () => OnOpenSettings?.Invoke());
            SetupButton(_workshopButton, () => OnOpenWorkshop?.Invoke());
        }

        private void SetupButton(Button button, Action onClick)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }

        public void SetUsername(string username)
        {
            if (_usernameText == null) return;
            _usernameText.text = username;
        }

        public void SetCurrentEmotion(Sprite emotionSprite)
        {
            if (_currentEmotionImage == null) return;
            _currentEmotionImage.sprite = emotionSprite;
            _currentEmotionImage.enabled = emotionSprite != null;
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

        public void SetJar(EmotionTypes type, int value, int maxValue = 100)
        {
            if (!_emotionJars.TryGetValue(type, out var jar))
            {
                Debug.LogWarning($"❓ Неизвестный тип эмоции: {type}");
                return;
            }

            float fillAmount = Mathf.Clamp01((float)value / maxValue);
            jar.fillAmount = fillAmount;
        }

        public void ClearAll()
        {
            SetUsername(string.Empty);
            SetCurrentEmotion(null);
            SetPoints(0);
            SetEntries(0);

            foreach (var type in Enum.GetValues(typeof(EmotionTypes)))
            {
                SetJar((EmotionTypes)type, 0);
            }
        }
    }
}
