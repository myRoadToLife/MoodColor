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
        private const int DEFAULT_CAPACITY = 100; // Константа для емкости по умолчанию

        [Header("Profile Info")]
        [SerializeField] private TMP_Text _usernameText;
        [SerializeField] private Image _currentEmotionImage;

        [Header("Emotion Jars (Filled Images)")]
        [SerializeField] private Image _joyJarFill;
        [SerializeField] private Image _sadnessJarFill;
        [SerializeField] private Image _angerJarFill;
        [SerializeField] private Image _fearJarFill;
        [SerializeField] private Image _disgustJarFill;

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
            _emotionJars = new Dictionary<EmotionTypes, Image>();
            
            // Проверяем каждую банку перед добавлением в словарь
            if (_joyJarFill != null) _emotionJars.Add(EmotionTypes.Joy, _joyJarFill);
            if (_sadnessJarFill != null) _emotionJars.Add(EmotionTypes.Sadness, _sadnessJarFill);
            if (_angerJarFill != null) _emotionJars.Add(EmotionTypes.Anger, _angerJarFill);
            if (_fearJarFill != null) _emotionJars.Add(EmotionTypes.Fear, _fearJarFill);
            if (_disgustJarFill != null) _emotionJars.Add(EmotionTypes.Disgust, _disgustJarFill);
            
            // Предупреждаем о недостающих банках
            if (_joyJarFill == null) Debug.LogWarning("Банка Joy не назначена в инспекторе");
            if (_sadnessJarFill == null) Debug.LogWarning("Банка Sadness не назначена в инспекторе");
            if (_angerJarFill == null) Debug.LogWarning("Банка Anger не назначена в инспекторе");
            if (_fearJarFill == null) Debug.LogWarning("Банка Fear не назначена в инспекторе");
            if (_disgustJarFill == null) Debug.LogWarning("Банка Disgust не назначена в инспекторе");
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
            
            // Добавляем try-catch для обработки ошибок в обработчиках событий
            button.onClick.AddListener(() => {
                try
                {
                    onClick?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Ошибка при обработке нажатия кнопки: {ex.Message}");
                }
            });
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

        public void SetJar(EmotionTypes type, int amount, int capacity)
        {
            // Проверяем, что словарь _emotionJars существует
            if (_emotionJars == null)
            {
                Debug.LogWarning("Словарь банок эмоций не инициализирован");
                return;
            }

            // Проверяем, есть ли банка для данного типа в словаре
            if (!_emotionJars.TryGetValue(type, out Image jarFill))
            {
                Debug.LogWarning($"Неизвестный тип эмоции: {type}");
                return;
            }

            // Проверяем, что ссылка на изображение заполнения не null
            if (jarFill == null)
            {
                Debug.LogWarning($"Изображение заполнения для типа {type} не назначено");
                return;
            }

            // Устанавливаем заполнение банки
            float fillAmount = capacity > 0 ? (float)amount / capacity : 0;
            jarFill.fillAmount = Mathf.Clamp01(fillAmount);
        }

        // Перегрузка метода с двумя параметрами
        public void SetJar(EmotionTypes type, int amount)
        {
            SetJar(type, amount, DEFAULT_CAPACITY);
        }

        public void ClearAll()
        {
            SetUsername(string.Empty);
            SetCurrentEmotion(null);
            SetPoints(0);
            SetEntries(0);

            // Проверяем, что словарь инициализирован
            if (_emotionJars != null)
            {
                foreach (var type in Enum.GetValues(typeof(EmotionTypes)))
                {
                    try
                    {
                        // Используем перегрузку с двумя параметрами
                        SetJar((EmotionTypes)type, 0);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Ошибка при очистке банки {type}: {ex.Message}");
                    }
                }
            }
        }
    }
}