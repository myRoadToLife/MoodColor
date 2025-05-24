using System;
using App.App.Develop.Scenes.PersonalAreaScene.UI.Components;
using App.Develop.CommonServices.Emotion;
using UnityEngine;
using App.Develop.Scenes.PersonalAreaScene.UI.Components;
using App.Develop.Utils.Logging;

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

        private void ValidateComponents()
        {
            MyLogger.Log("🔄 [PersonalAreaUIController] Валидация компонентов...", MyLogger.LogCategory.UI);
            
            if (_profileInfo == null) 
            {
                MyLogger.LogError("❌ [PersonalAreaUIController] ProfileInfoComponent не назначен", MyLogger.LogCategory.UI);
            }
            else
            {
                MyLogger.Log("✅ [PersonalAreaUIController] ProfileInfoComponent валиден", MyLogger.LogCategory.UI);
            }
            
            if (_emotionJars == null) 
            {
                MyLogger.LogError("❌ [PersonalAreaUIController] EmotionJarView не назначен", MyLogger.LogCategory.UI);
            }
            else
            {
                MyLogger.Log("✅ [PersonalAreaUIController] EmotionJarView валиден: " + _emotionJars.name, MyLogger.LogCategory.UI);
            }
            
            if (_statistics == null) 
            {
                MyLogger.LogError("❌ [PersonalAreaUIController] StatisticsComponent не назначен", MyLogger.LogCategory.UI);
            }
            else
            {
                MyLogger.Log("✅ [PersonalAreaUIController] StatisticsComponent валиден", MyLogger.LogCategory.UI);
            }
            
            if (_navigation == null) 
            {
                MyLogger.LogError("❌ [PersonalAreaUIController] NavigationComponent не назначен", MyLogger.LogCategory.UI);
            }
            else
            {
                MyLogger.Log("✅ [PersonalAreaUIController] NavigationComponent валиден", MyLogger.LogCategory.UI);
            }
        }

        public void Initialize()
        {
            MyLogger.Log("🔄 [PersonalAreaUIController] Начало инициализации...", MyLogger.LogCategory.UI);
            
            try
            {
                ValidateComponents();
                
                if (_navigation != null)
                {
                    MyLogger.Log("🔄 [PersonalAreaUIController] Инициализация NavigationComponent...", MyLogger.LogCategory.UI);
                    try
                    {
                        _navigation.Initialize();
                        MyLogger.Log("✅ [PersonalAreaUIController] NavigationComponent инициализирован", MyLogger.LogCategory.UI);
                    }
                    catch (Exception ex)
                    {
                        MyLogger.LogError($"❌ [PersonalAreaUIController] Ошибка инициализации NavigationComponent: {ex.Message}", MyLogger.LogCategory.UI);
                    }
                }
                
                if (_emotionJars != null)
                {
                    MyLogger.Log("🔄 [PersonalAreaUIController] Инициализация EmotionJarView...", MyLogger.LogCategory.UI);
                    try
                    {
                        _emotionJars.Initialize();
                        MyLogger.Log("✅ [PersonalAreaUIController] EmotionJarView инициализирован", MyLogger.LogCategory.UI);
                    }
                    catch (Exception ex)
                    {
                        MyLogger.LogError($"❌ [PersonalAreaUIController] Ошибка инициализации EmotionJarView: {ex.Message}\n{ex.StackTrace}", MyLogger.LogCategory.UI);
                    }
                }
                
                MyLogger.Log("✅ [PersonalAreaUIController] Инициализация завершена успешно", MyLogger.LogCategory.UI);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [PersonalAreaUIController] Ошибка инициализации UI контроллера: {ex.Message}\n{ex.StackTrace}", MyLogger.LogCategory.UI);
            }
        }

        public void SetUsername(string username)
        {
            if (_profileInfo == null)
            {
                MyLogger.LogError("❌ [PersonalAreaUIController] Невозможно установить имя пользователя: ProfileInfoComponent отсутствует", MyLogger.LogCategory.UI);
                return;
            }
            
            MyLogger.Log($"🔄 [PersonalAreaUIController] Установка имени пользователя: {username}", MyLogger.LogCategory.UI);
            _profileInfo.SetUsername(username);
        }
        
        public void SetCurrentEmotion(Sprite emotionSprite)
        {
            if (_profileInfo == null)
            {
                MyLogger.LogError("❌ [PersonalAreaUIController] Невозможно установить текущую эмоцию: ProfileInfoComponent отсутствует", MyLogger.LogCategory.UI);
                return;
            }
            
            MyLogger.Log($"🔄 [PersonalAreaUIController] Установка текущей эмоции: {(emotionSprite != null ? emotionSprite.name : "null")}", MyLogger.LogCategory.UI);
            _profileInfo.SetCurrentEmotion(emotionSprite);
        }
        
        public void SetJar(EmotionTypes type, int amount)
        {
            if (_emotionJars == null)
            {
                MyLogger.LogError($"❌ [PersonalAreaUIController] Невозможно установить количество для банки {type}: EmotionJarView отсутствует", MyLogger.LogCategory.UI);
                return;
            }
            
            try
            {
                MyLogger.Log($"🔄 [PersonalAreaUIController] Установка количества {amount} для банки типа {type}", MyLogger.LogCategory.Gameplay);
                _emotionJars.SetJar(type, amount);
                MyLogger.Log($"✅ [PersonalAreaUIController] Количество для банки {type} установлено: {amount}", MyLogger.LogCategory.Gameplay);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [PersonalAreaUIController] Ошибка при установке количества для банки {type}: {ex.Message}", MyLogger.LogCategory.UI);
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
                MyLogger.LogError("❌ [PersonalAreaUIController] Невозможно установить очки: StatisticsComponent отсутствует", MyLogger.LogCategory.UI);
                return;
            }
            
            MyLogger.Log($"🔄 [PersonalAreaUIController] Установка очков: {points}", MyLogger.LogCategory.Gameplay);
            _statistics.SetPoints(points);
        }
        
        public void SetEntries(int entries)
        {
            if (_statistics == null)
            {
                MyLogger.LogError("❌ [PersonalAreaUIController] Невозможно установить записи: StatisticsComponent отсутствует", MyLogger.LogCategory.UI);
                return;
            }
            
            MyLogger.Log($"🔄 [PersonalAreaUIController] Установка записей: {entries}", MyLogger.LogCategory.Gameplay);
            _statistics.SetEntries(entries);
        }

        /// <summary>
        /// Возвращает компонент EmotionJarView
        /// </summary>
        public EmotionJarView GetEmotionJarView()
        {
            MyLogger.Log($"🔄 [PersonalAreaUIController] Запрошен EmotionJarView: {(_emotionJars != null ? _emotionJars.name : "null")}", MyLogger.LogCategory.UI); 
            return _emotionJars;
        }

        public void ClearAll()
        {
            MyLogger.Log("🔄 [PersonalAreaUIController] Очистка всех компонентов...", MyLogger.LogCategory.UI);
            
            try
            {
                if (_profileInfo != null)
                {
                    _profileInfo.Clear();
                    MyLogger.Log("✅ [PersonalAreaUIController] ProfileInfoComponent очищен", MyLogger.LogCategory.UI);
                }
                
                if (_emotionJars != null)
                {
                    _emotionJars.Clear();
                    MyLogger.Log("✅ [PersonalAreaUIController] EmotionJarView очищен", MyLogger.LogCategory.UI);
                }
                
                if (_statistics != null)
                {
                    _statistics.Clear();
                    MyLogger.Log("✅ [PersonalAreaUIController] StatisticsComponent очищен", MyLogger.LogCategory.UI);
                }
                
                if (_navigation != null)
                {
                    _navigation.Clear();
                    MyLogger.Log("✅ [PersonalAreaUIController] NavigationComponent очищен", MyLogger.LogCategory.UI);
                }
                
                MyLogger.Log("✅ [PersonalAreaUIController] Все компоненты очищены", MyLogger.LogCategory.UI);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [PersonalAreaUIController] Ошибка при очистке компонентов: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }

        private void OnDestroy()
        {
            MyLogger.Log("🔄 [PersonalAreaUIController] OnDestroy вызван, очищаем компоненты", MyLogger.LogCategory.UI);
            ClearAll();
        }

        public NavigationComponent GetNavigationComponent()
        {
            MyLogger.Log($"🔄 [PersonalAreaUIController] Запрошен NavigationComponent: {(_navigation != null ? _navigation.name : "null")}", MyLogger.LogCategory.UI); 
            return _navigation;
        }
    }
} 