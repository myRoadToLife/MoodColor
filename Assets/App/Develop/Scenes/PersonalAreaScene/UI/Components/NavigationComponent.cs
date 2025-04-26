using System;
using UnityEngine;
using UnityEngine.UI;
using App.Develop.Scenes.PersonalAreaScene.UI.Extensions;

namespace App.Develop.Scenes.PersonalAreaScene.UI.Components
{
    public class NavigationComponent : MonoBehaviour
    {
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
            _logEmotionButton.SetupButton(() => OnLogEmotion?.Invoke());
            _historyButton.SetupButton(() => OnOpenHistory?.Invoke());
            _friendsButton.SetupButton(() => OnOpenFriends?.Invoke());
            _settingsButton.SetupButton(() => OnOpenSettings?.Invoke());
            _workshopButton.SetupButton(() => OnOpenWorkshop?.Invoke());
        }

        public void Clear()
        {
            if (_logEmotionButton != null) _logEmotionButton.onClick.RemoveAllListeners();
            if (_historyButton != null) _historyButton.onClick.RemoveAllListeners();
            if (_friendsButton != null) _friendsButton.onClick.RemoveAllListeners();
            if (_settingsButton != null) _settingsButton.onClick.RemoveAllListeners();
            if (_workshopButton != null) _workshopButton.onClick.RemoveAllListeners();
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
} 