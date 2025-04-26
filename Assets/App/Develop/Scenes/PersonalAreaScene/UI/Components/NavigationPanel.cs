using System;
using App.Develop.Scenes.PersonalAreaScene.UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace App.Develop.Scenes.PersonalAreaScene.UI.Components
{
    public class NavigationPanel : BaseUIElement, IUIComponent
    {
        #region SerializeFields
        [Header("Navigation Buttons")]
        [SerializeField] private Button _logEmotionButton;
        [SerializeField] private Button _historyButton;
        [SerializeField] private Button _friendsButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _workshopButton;
        #endregion

        #region Events
        public event Action OnLogEmotion;
        public event Action OnOpenHistory;
        public event Action OnOpenFriends;
        public event Action OnOpenSettings;
        public event Action OnOpenWorkshop;
        #endregion

        #region Unity Methods
        protected override void ValidateReferences()
        {
            if (_logEmotionButton == null) LogWarning("Кнопка логирования эмоций не назначена в инспекторе");
            if (_historyButton == null) LogWarning("Кнопка истории не назначена в инспекторе");
            if (_friendsButton == null) LogWarning("Кнопка друзей не назначена в инспекторе");
            if (_settingsButton == null) LogWarning("Кнопка настроек не назначена в инспекторе");
            if (_workshopButton == null) LogWarning("Кнопка мастерской не назначена в инспекторе");
        }

        protected override void UnsubscribeFromEvents()
        {
            RemoveButtonListeners();
        }
        #endregion

        #region Public Methods
        public void Initialize()
        {
            SetupButtons();
        }

        public void Clear()
        {
            RemoveButtonListeners();
        }
        #endregion

        #region Private Methods
        private void SetupButtons()
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
            
            button.onClick.AddListener(() => {
                try
                {
                    onClick?.Invoke();
                }
                catch (Exception ex)
                {
                    LogError($"Ошибка при обработке нажатия кнопки: {ex.Message}");
                }
            });
        }

        private void RemoveButtonListeners()
        {
            if (_logEmotionButton != null) _logEmotionButton.onClick.RemoveAllListeners();
            if (_historyButton != null) _historyButton.onClick.RemoveAllListeners();
            if (_friendsButton != null) _friendsButton.onClick.RemoveAllListeners();
            if (_settingsButton != null) _settingsButton.onClick.RemoveAllListeners();
            if (_workshopButton != null) _workshopButton.onClick.RemoveAllListeners();
        }
        #endregion
    }
} 