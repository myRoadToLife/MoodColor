using UnityEngine;
using UnityEngine.UI;
using System;
using App.Develop.Utils.Logging;
using Logger = App.Develop.Utils.Logging.Logger;
using TMPro;

namespace App.Develop.Scenes.PersonalAreaScene.UI.Components
{
    public class NavigationComponent : MonoBehaviour
    {
        public event Action OnLogEmotion;
        public event Action OnOpenHistory;
        public event Action OnOpenFriends;
        public event Action OnOpenSettings;
        public event Action OnOpenWorkshop;
        public event Action OnQuitApplication;

        [SerializeField] private Button _logEmotionButton;
        [SerializeField] private Button _historyButton;
        [SerializeField] private Button _friendsButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _workshopButton;
        [SerializeField] private Button _quitButton;
        
        [SerializeField] private Image _logEmotionIcon;
        [SerializeField] private Image _historyIcon;
        [SerializeField] private Image _friendsIcon;
        [SerializeField] private Image _settingsIcon;
        [SerializeField] private Image _workshopIcon;
        [SerializeField] private Image _quitIcon;
        
        [SerializeField] private TextMeshProUGUI _logEmotionText;
        [SerializeField] private TextMeshProUGUI _historyText;
        [SerializeField] private TextMeshProUGUI _friendsText;
        [SerializeField] private TextMeshProUGUI _settingsText;
        [SerializeField] private TextMeshProUGUI _workshopText;
        [SerializeField] private TextMeshProUGUI _quitText;
        
        [SerializeField] private Color _activeButtonColor = new Color(0.8f, 0.7f, 0.5f, 1f);
        [SerializeField] private Color _inactiveButtonColor = new Color(0.6f, 0.5f, 0.35f, 1f);
        
        private Button _activeButton;
        
        private void Awake()
        {
            if (_logEmotionButton != null) _logEmotionButton.onClick.AddListener(OnLogEmotionButtonClicked);
            if (_historyButton != null) _historyButton.onClick.AddListener(OnHistoryButtonClicked);
            if (_friendsButton != null) _friendsButton.onClick.AddListener(OnFriendsButtonClicked);
            if (_settingsButton != null) _settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            if (_workshopButton != null) _workshopButton.onClick.AddListener(OnWorkshopButtonClicked);
            if (_quitButton != null) _quitButton.onClick.AddListener(OnQuitButtonClicked);
            
            // Установить LogEmotion как активную по умолчанию
            SetActiveButton(_logEmotionButton);
        }
        
        private void OnDestroy()
        {
            if (_logEmotionButton != null) _logEmotionButton.onClick.RemoveListener(OnLogEmotionButtonClicked);
            if (_historyButton != null) _historyButton.onClick.RemoveListener(OnHistoryButtonClicked);
            if (_friendsButton != null) _friendsButton.onClick.RemoveListener(OnFriendsButtonClicked);
            if (_settingsButton != null) _settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
            if (_workshopButton != null) _workshopButton.onClick.RemoveListener(OnWorkshopButtonClicked);
            if (_quitButton != null) _quitButton.onClick.RemoveListener(OnQuitButtonClicked);
        }
        
        private void OnLogEmotionButtonClicked()
        {
            SetActiveButton(_logEmotionButton);
            OnLogEmotion?.Invoke();
        }
        
        private void OnHistoryButtonClicked()
        {
            SetActiveButton(_historyButton);
            OnOpenHistory?.Invoke();
        }
        
        private void OnFriendsButtonClicked()
        {
            SetActiveButton(_friendsButton);
            OnOpenFriends?.Invoke();
        }
        
        private void OnSettingsButtonClicked()
        {
            SetActiveButton(_settingsButton);
            OnOpenSettings?.Invoke();
        }
        
        private void OnWorkshopButtonClicked()
        {
            SetActiveButton(_workshopButton);
            OnOpenWorkshop?.Invoke();
        }
        
        private void OnQuitButtonClicked()
        {
            Logger.Log("🔄 [NavigationComponent] Нажата кнопка выхода из приложения");
            OnQuitApplication?.Invoke();
        }
        
        private void SetActiveButton(Button button)
        {
            if (_activeButton != null)
            {
                // Сбросить предыдущую кнопку
                var panels = _activeButton.GetComponentsInChildren<Image>();
                foreach (var panel in panels)
                {
                    if (panel.gameObject.name == "WoodenPanel")
                    {
                        panel.color = _inactiveButtonColor;
                        break;
                    }
                }
            }
            
            _activeButton = button;
            
            if (_activeButton != null)
            {
                // Подсветить активную кнопку
                var panels = _activeButton.GetComponentsInChildren<Image>();
                foreach (var panel in panels)
                {
                    if (panel.gameObject.name == "WoodenPanel")
                    {
                        panel.color = _activeButtonColor;
                        break;
                    }
                }
                
                // Запустить анимацию
                var animation = _activeButton.GetComponent<ButtonClickAnimation>();
                if (animation != null)
                {
                    animation.PlayAnimation();
                }
            }
        }
        
        // Публичные методы для установки иконок
        public void SetLogEmotionIcon(Sprite icon)
        {
            if (_logEmotionIcon != null) _logEmotionIcon.sprite = icon;
        }
        
        public void SetHistoryIcon(Sprite icon)
        {
            if (_historyIcon != null) _historyIcon.sprite = icon;
        }
        
        public void SetFriendsIcon(Sprite icon)
        {
            if (_friendsIcon != null) _friendsIcon.sprite = icon;
        }
        
        public void SetSettingsIcon(Sprite icon)
        {
            if (_settingsIcon != null) _settingsIcon.sprite = icon;
        }
        
        public void SetWorkshopIcon(Sprite icon)
        {
            if (_workshopIcon != null) _workshopIcon.sprite = icon;
        }
        
        public void SetQuitIcon(Sprite icon)
        {
            if (_quitIcon != null) _quitIcon.sprite = icon;
        }
        
        // Добавляем Initialize для поддержки существующего кода
        public void Initialize()
        {
            Logger.Log("🔄 [NavigationComponent] Инициализация компонента с улучшенным UI");
            // Инициализация уже происходит в Awake
        }
        
        // Метод для поддержки существующего кода
        public Button GetSettingsButton()
        {
            return _settingsButton;
        }
        
        // Метод для поддержки существующего кода
        public void Clear()
        {
            Logger.Log("🔄 [NavigationComponent] Очистка подписок кнопок");
            // Очистка будет выполнена автоматически в OnDestroy
        }
    }
} 