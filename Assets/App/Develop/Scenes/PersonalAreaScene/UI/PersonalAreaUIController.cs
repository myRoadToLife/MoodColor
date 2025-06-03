using System;
using App.App.Develop.Scenes.PersonalAreaScene.UI.Components;
using App.Develop.CommonServices.Emotion;
using UnityEngine;
using App.Develop.Scenes.PersonalAreaScene.UI.Components;
using System.Collections.Generic;

namespace App.Develop.Scenes.PersonalAreaScene.UI
{
    public class PersonalAreaUIController : MonoBehaviour
    {
        [SerializeField] private ProfileInfoComponent _profileInfo;
        [SerializeField] private EmotionJarView _emotionJars;
        [SerializeField] private StatisticsView _statistics;
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

        public event Action OnQuitApplication
        {
            add => _navigation.OnQuitApplication += value;
            remove => _navigation.OnQuitApplication -= value;
        }

        private void OnDestroy()
        {
            ClearAll();
        }

        private void ValidateComponents()
        {
            if (_profileInfo == null)
            {
                throw new InvalidOperationException("[PersonalAreaUIController] ProfileInfoComponent is not assigned");
            }
            if (_emotionJars == null)
            {
                throw new InvalidOperationException("[PersonalAreaUIController] EmotionJarView is not assigned");
            }
            if (_statistics == null)
            {
                throw new InvalidOperationException("[PersonalAreaUIController] StatisticsComponent is not assigned");
            }
            if (_navigation == null)
            {
                throw new InvalidOperationException("[PersonalAreaUIController] NavigationComponent is not assigned");
            }
        }

        public void Initialize()
        {
            try
            {
                ValidateComponents();

                if (_navigation != null)
                {
                    try
                    {
                        _navigation.Initialize();
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"[PersonalAreaUIController] Error initializing NavigationComponent: {ex.Message}", ex);
                    }
                }

                if (_emotionJars != null)
                {
                    try
                    {
                        _emotionJars.Initialize();
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"[PersonalAreaUIController] Error initializing EmotionJarView: {ex.Message}\n{ex.StackTrace}", ex);
                    }
                }

                if (_statistics != null)
                {
                    try
                    {
                        _statistics.Initialize();
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"[PersonalAreaUIController] Error initializing StatisticsView: {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"[PersonalAreaUIController] Error initializing UI controller: {ex.Message}\n{ex.StackTrace}", ex);
            }
        }

        public void SetUsername(string username)
        {
            if (_profileInfo == null)
            {
                throw new InvalidOperationException("[PersonalAreaUIController] Cannot set username: ProfileInfoComponent is missing");
            }
            _profileInfo.SetUsername(username);
        }

        public void SetCurrentEmotion(Sprite emotionSprite)
        {
            if (_profileInfo == null)
            {
                throw new InvalidOperationException("[PersonalAreaUIController] Cannot set current emotion: ProfileInfoComponent is missing");
            }
            _profileInfo.SetCurrentEmotion(emotionSprite);
        }

        public void SetJar(EmotionTypes type, int amount)
        {
            if (_emotionJars == null)
            {
                throw new InvalidOperationException($"[PersonalAreaUIController] Cannot set amount for jar {type}: EmotionJarView is missing");
            }
            try
            {
                _emotionJars.SetJar(type, amount);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"[PersonalAreaUIController] Error setting amount for jar {type}: {ex.Message}", ex);
            }
        }

        public void SetJarFloat(EmotionTypes type, float amount)
        {
            SetJar(type, Mathf.RoundToInt(amount));
        }

        public void SetPoints(int points)
        {
            if (_statistics == null)
            {
                throw new InvalidOperationException("[PersonalAreaUIController] Cannot set points: StatisticsComponent is missing");
            }
            _statistics.SetPoints(points);
        }

        public void SetEntries(int entries)
        {
            if (_statistics == null)
            {
                throw new InvalidOperationException("[PersonalAreaUIController] Cannot set entries: StatisticsComponent is missing");
            }
            _statistics.SetEntries(entries);
        }

        /// <summary>
        /// Устанавливает региональную статистику эмоций города
        /// </summary>
        /// <param name="regionalStats">Словарь с региональной статистикой</param>
        public void SetRegionalStats(Dictionary<string, RegionalEmotionStats> regionalStats)
        {
            if (_statistics == null)
            {
                throw new InvalidOperationException("[PersonalAreaUIController] Cannot set regional stats: StatisticsComponent is missing");
            }

            try
            {
                _statistics.SetRegionalStats(regionalStats);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"[PersonalAreaUIController] Error setting regional stats: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Возвращает компонент EmotionJarView
        /// </summary>
        public EmotionJarView GetEmotionJarView()
        {
            return _emotionJars;
        }

        public NavigationComponent GetNavigationComponent()
        {
            return _navigation;
        }

        /// <summary>
        /// Возвращает компонент StatisticsView
        /// </summary>
        public StatisticsView GetStatisticsView()
        {
            return _statistics;
        }

        public void ClearAll()
        {
            try
            {
                if (_profileInfo != null)
                {
                    _profileInfo.Clear();
                }
                if (_emotionJars != null)
                {
                    _emotionJars.Clear();
                }
                if (_statistics != null)
                {
                    _statistics.Clear();
                }
                if (_navigation != null)
                {
                    _navigation.Clear();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"[PersonalAreaUIController] Error clearing components: {ex.Message}", ex);
            }
        }
    }
}