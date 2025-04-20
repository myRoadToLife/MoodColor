using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using App.Develop.CommonServices.Emotion;

namespace App.Develop.Scenes.PersonalAreaScene
{
    public class PersonalAreaUIController : MonoBehaviour
    {
        [Header("Profile Info")]
        [SerializeField] private TMP_Text _usernameText;
        [SerializeField] private Image _currentEmotionImage; // 🔄 теперь это Image, а не TMP_Text

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

        public event Action OnLogEmotion;
        public event Action OnOpenHistory;
        public event Action OnOpenFriends;
        public event Action OnOpenSettings;
        public event Action OnOpenWorkshop;

        public void Initialize()
        {
            _logEmotionButton.onClick.AddListener(() => OnLogEmotion?.Invoke());
            _historyButton.onClick.AddListener(() => OnOpenHistory?.Invoke());
            _friendsButton.onClick.AddListener(() => OnOpenFriends?.Invoke());
            _settingsButton.onClick.AddListener(() => OnOpenSettings?.Invoke());
            _workshopButton.onClick.AddListener(() => OnOpenWorkshop?.Invoke());
        }

        public void SetUsername(string username)
        {
            _usernameText.text = username;
        }

        public void SetCurrentEmotion(Sprite emotionSprite)
        {
            _currentEmotionImage.sprite = emotionSprite;
            _currentEmotionImage.enabled = emotionSprite != null;
        }

        public void SetPoints(int points)
        {
            _pointsText.text = $"Очки: {points}";
        }

        public void SetEntries(int entries)
        {
            _entriesText.text = $"Записей: {entries}";
        }

        public void SetJar(EmotionTypes type, int value, int maxValue = 100)
        {
            float fillAmount = Mathf.Clamp01((float)value / maxValue);

            switch (type)
            {
                case EmotionTypes.Joy:
                    _joyJarFill.fillAmount = fillAmount;
                    break;
                case EmotionTypes.Sadness:
                    _sadnessJarFill.fillAmount = fillAmount;
                    break;
                case EmotionTypes.Anger:
                    _angerJarFill.fillAmount = fillAmount;
                    break;
                case EmotionTypes.Fear:
                    _fearJarFill.fillAmount = fillAmount;
                    break;
                default:
                    Debug.LogWarning($"❓ Неизвестный тип эмоции: {type}");
                    break;
            }
        }

        public void ClearAll()
        {
            SetUsername(string.Empty);
            SetCurrentEmotion(null);
            SetPoints(0);
            SetEntries(0);
            SetJar(EmotionTypes.Joy, 0);
            SetJar(EmotionTypes.Sadness, 0);
            SetJar(EmotionTypes.Anger, 0);
            SetJar(EmotionTypes.Fear, 0);
        }
    }
}
