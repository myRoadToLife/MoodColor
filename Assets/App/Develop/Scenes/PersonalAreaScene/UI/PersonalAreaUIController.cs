using System;
using App.App.Develop.Scenes.PersonalAreaScene.UI.Components;
using App.Develop.CommonServices.Emotion;
using UnityEngine;
using App.Develop.Scenes.PersonalAreaScene.UI.Components;

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

        private void ValidateComponents()
        {
            Debug.Log("🔄 [PersonalAreaUIController] Валидация компонентов...");
            
            if (_profileInfo == null) 
            {
                Debug.LogError("❌ [PersonalAreaUIController] ProfileInfoComponent не назначен");
            }
            else
            {
                Debug.Log("✅ [PersonalAreaUIController] ProfileInfoComponent валиден");
            }
            
            if (_emotionJars == null) 
            {
                Debug.LogError("❌ [PersonalAreaUIController] EmotionJarView не назначен");
            }
            else
            {
                Debug.Log("✅ [PersonalAreaUIController] EmotionJarView валиден: " + _emotionJars.name);
            }
            
            if (_statistics == null) 
            {
                Debug.LogError("❌ [PersonalAreaUIController] StatisticsComponent не назначен");
            }
            else
            {
                Debug.Log("✅ [PersonalAreaUIController] StatisticsComponent валиден");
            }
            
            if (_navigation == null) 
            {
                Debug.LogError("❌ [PersonalAreaUIController] NavigationComponent не назначен");
            }
            else
            {
                Debug.Log("✅ [PersonalAreaUIController] NavigationComponent валиден");
            }
        }

        public void Initialize()
        {
            Debug.Log("🔄 [PersonalAreaUIController] Начало инициализации...");
            
            try
            {
                ValidateComponents();
                
                if (_navigation != null)
                {
                    Debug.Log("🔄 [PersonalAreaUIController] Инициализация NavigationComponent...");
                    try
                    {
                        _navigation.Initialize();
                        Debug.Log("✅ [PersonalAreaUIController] NavigationComponent инициализирован");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"❌ [PersonalAreaUIController] Ошибка инициализации NavigationComponent: {ex.Message}");
                    }
                }
                
                if (_emotionJars != null)
                {
                    Debug.Log("🔄 [PersonalAreaUIController] Инициализация EmotionJarView...");
                    try
                    {
                        _emotionJars.Initialize();
                        Debug.Log("✅ [PersonalAreaUIController] EmotionJarView инициализирован");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"❌ [PersonalAreaUIController] Ошибка инициализации EmotionJarView: {ex.Message}\n{ex.StackTrace}");
                    }
                }
                
                Debug.Log("✅ [PersonalAreaUIController] Инициализация завершена успешно");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ [PersonalAreaUIController] Ошибка инициализации UI контроллера: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public void SetUsername(string username)
        {
            if (_profileInfo == null)
            {
                Debug.LogError("❌ [PersonalAreaUIController] Невозможно установить имя пользователя: ProfileInfoComponent отсутствует");
                return;
            }
            
            Debug.Log($"🔄 [PersonalAreaUIController] Установка имени пользователя: {username}");
            _profileInfo.SetUsername(username);
        }
        
        public void SetCurrentEmotion(Sprite emotionSprite)
        {
            if (_profileInfo == null)
            {
                Debug.LogError("❌ [PersonalAreaUIController] Невозможно установить текущую эмоцию: ProfileInfoComponent отсутствует");
                return;
            }
            
            Debug.Log($"🔄 [PersonalAreaUIController] Установка текущей эмоции: {(emotionSprite != null ? emotionSprite.name : "null")}");
            _profileInfo.SetCurrentEmotion(emotionSprite);
        }
        
        public void SetJar(EmotionTypes type, int amount)
        {
            if (_emotionJars == null)
            {
                Debug.LogError($"❌ [PersonalAreaUIController] Невозможно установить количество для банки {type}: EmotionJarView отсутствует");
                return;
            }
            
            try
            {
                Debug.Log($"🔄 [PersonalAreaUIController] Установка количества {amount} для банки типа {type}");
                _emotionJars.SetJar(type, amount);
                Debug.Log($"✅ [PersonalAreaUIController] Количество для банки {type} установлено: {amount}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ [PersonalAreaUIController] Ошибка при установке количества для банки {type}: {ex.Message}");
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
                Debug.LogError("❌ [PersonalAreaUIController] Невозможно установить очки: StatisticsComponent отсутствует");
                return;
            }
            
            Debug.Log($"🔄 [PersonalAreaUIController] Установка очков: {points}");
            _statistics.SetPoints(points);
        }
        
        public void SetEntries(int entries)
        {
            if (_statistics == null)
            {
                Debug.LogError("❌ [PersonalAreaUIController] Невозможно установить записи: StatisticsComponent отсутствует");
                return;
            }
            
            Debug.Log($"🔄 [PersonalAreaUIController] Установка записей: {entries}");
            _statistics.SetEntries(entries);
        }

        /// <summary>
        /// Возвращает компонент EmotionJarView
        /// </summary>
        public EmotionJarView GetEmotionJarView()
        {
            Debug.Log($"🔄 [PersonalAreaUIController] Запрошен EmotionJarView: {(_emotionJars != null ? _emotionJars.name : "null")}"); 
            return _emotionJars;
        }

        public void ClearAll()
        {
            Debug.Log("🔄 [PersonalAreaUIController] Очистка всех компонентов...");
            
            try
            {
                if (_profileInfo != null)
                {
                    _profileInfo.Clear();
                    Debug.Log("✅ [PersonalAreaUIController] ProfileInfoComponent очищен");
                }
                
                if (_emotionJars != null)
                {
                    _emotionJars.Clear();
                    Debug.Log("✅ [PersonalAreaUIController] EmotionJarView очищен");
                }
                
                if (_statistics != null)
                {
                    _statistics.Clear();
                    Debug.Log("✅ [PersonalAreaUIController] StatisticsComponent очищен");
                }
                
                if (_navigation != null)
                {
                    _navigation.Clear();
                    Debug.Log("✅ [PersonalAreaUIController] NavigationComponent очищен");
                }
                
                Debug.Log("✅ [PersonalAreaUIController] Все компоненты очищены");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ [PersonalAreaUIController] Ошибка при очистке компонентов: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            Debug.Log("🔄 [PersonalAreaUIController] OnDestroy вызван, очищаем компоненты");
            ClearAll();
        }

        public NavigationComponent GetNavigationComponent()
        {
            Debug.Log($"🔄 [PersonalAreaUIController] Запрошен NavigationComponent: {(_navigation != null ? _navigation.name : "null")}"); 
            return _navigation;
        }
    }
} 