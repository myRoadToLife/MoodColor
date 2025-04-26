using System;
using App.Develop.CommonServices.Emotion;
using UnityEngine;
using App.Develop.Scenes.PersonalAreaScene.UI.Components;

namespace App.Develop.Scenes.PersonalAreaScene.UI
{
    public class PersonalAreaUIController : MonoBehaviour
    {
        [SerializeField] private ProfileInfoComponent _profileInfo;
        [SerializeField] private EmotionJarsComponent _emotionJars;
        [SerializeField] private StatisticsComponent _statistics;
        [SerializeField] private NavigationComponent _navigation;

        public event Action OnLogEmotion
        {
            add => _navigation.OnLogEmotion += value;
            remove => _navigation.OnLogEmotion -= value;
        }

        public event Action OnOpenHistory
        {
            add => _navigation.OnOpenHistory += value;
            remove => _navigation.OnOpenHistory -= value;
        }

        public event Action OnOpenFriends
        {
            add => _navigation.OnOpenFriends += value;
            remove => _navigation.OnOpenFriends -= value;
        }

        public event Action OnOpenSettings
        {
            add => _navigation.OnOpenSettings += value;
            remove => _navigation.OnOpenSettings -= value;
        }

        public event Action OnOpenWorkshop
        {
            add => _navigation.OnOpenWorkshop += value;
            remove => _navigation.OnOpenWorkshop -= value;
        }

        private void ValidateComponents()
        {
            if (_profileInfo == null) Debug.LogError("ProfileInfoComponent не назначен");
            if (_emotionJars == null) Debug.LogError("EmotionJarsComponent не назначен");
            if (_statistics == null) Debug.LogError("StatisticsComponent не назначен");
            if (_navigation == null) Debug.LogError("NavigationComponent не назначен");
        }

        public void Initialize()
        {
            ValidateComponents();
            _navigation.Initialize();
        }

        public void SetUsername(string username) => _profileInfo.SetUsername(username);
        public void SetCurrentEmotion(Sprite emotionSprite) => _profileInfo.SetCurrentEmotion(emotionSprite);
        public void SetJar(EmotionTypes type, int amount) => _emotionJars.SetJar(type, amount);
        public void SetPoints(int points) => _statistics.SetPoints(points);
        public void SetEntries(int entries) => _statistics.SetEntries(entries);

        public void ClearAll()
        {
            _profileInfo.Clear();
            _emotionJars.Clear();
            _statistics.Clear();
            _navigation.Clear();
        }

        private void OnDestroy()
        {
            ClearAll();
        }
    }
} 