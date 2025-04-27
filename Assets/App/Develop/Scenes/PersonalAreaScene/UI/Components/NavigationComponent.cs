using System;
using UnityEngine;
using UnityEngine.UI;

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
            Debug.Log("🔄 [NavigationComponent] Начало инициализации кнопок...");
            
            LogButtonsState();
            
            if (_logEmotionButton != null) 
            {
                _logEmotionButton.onClick.RemoveAllListeners();
                _logEmotionButton.onClick.AddListener(() => {
                    Debug.Log("🔘 [NavigationComponent] Нажата кнопка LogEmotion");
                    OnLogEmotion?.Invoke();
                });
                Debug.Log("✅ [NavigationComponent] Настроена кнопка LogEmotion");
            }
            else
            {
                Debug.LogError("❌ [NavigationComponent] Кнопка LogEmotion отсутствует или не назначена");
            }
            
            if (_historyButton != null) 
            {
                _historyButton.onClick.RemoveAllListeners();
                _historyButton.onClick.AddListener(() => {
                    Debug.Log("🔘 [NavigationComponent] Нажата кнопка History");
                    OnOpenHistory?.Invoke();
                });
                Debug.Log("✅ [NavigationComponent] Настроена кнопка History");
            }
            else
            {
                Debug.LogError("❌ [NavigationComponent] Кнопка History отсутствует или не назначена");
            }
            
            if (_friendsButton != null) 
            {
                _friendsButton.onClick.RemoveAllListeners();
                _friendsButton.onClick.AddListener(() => {
                    Debug.Log("🔘 [NavigationComponent] Нажата кнопка Friends");
                    OnOpenFriends?.Invoke();
                });
                Debug.Log("✅ [NavigationComponent] Настроена кнопка Friends");
            }
            else
            {
                Debug.LogError("❌ [NavigationComponent] Кнопка Friends отсутствует или не назначена");
            }
            
            if (_settingsButton != null) 
            {
                _settingsButton.onClick.RemoveAllListeners();
                _settingsButton.onClick.AddListener(() => {
                    Debug.Log("🔘 [NavigationComponent] Нажата кнопка Settings");
                    
                    if (OnOpenSettings == null)
                    {
                        Debug.LogError("❌ [NavigationComponent] Нет подписчиков на событие OnOpenSettings!");
                    }
                    else 
                    {
                        Debug.Log("✅ [NavigationComponent] Вызываем OnOpenSettings");
                        OnOpenSettings.Invoke();
                    }
                });
                Debug.Log("✅ [NavigationComponent] Настроена кнопка Settings");
            }
            else
            {
                Debug.LogError("❌ [NavigationComponent] Кнопка Settings отсутствует или не назначена");
            }
            
            if (_workshopButton != null) 
            {
                _workshopButton.onClick.RemoveAllListeners();
                _workshopButton.onClick.AddListener(() => {
                    Debug.Log("🔘 [NavigationComponent] Нажата кнопка Workshop");
                    OnOpenWorkshop?.Invoke();
                });
                Debug.Log("✅ [NavigationComponent] Настроена кнопка Workshop");
            }
            else
            {
                Debug.LogError("❌ [NavigationComponent] Кнопка Workshop отсутствует или не назначена");
            }
            
            Debug.Log("✅ [NavigationComponent] Инициализация кнопок завершена");
        }

        public Button GetSettingsButton()
        {
            Debug.Log($"🔄 [NavigationComponent] Запрошена кнопка настроек: {(_settingsButton != null ? _settingsButton.name : "null")}");
            
            if (_settingsButton != null) 
            {
                Debug.Log($"✅ [NavigationComponent] Информация о кнопке настроек: Interactable = {_settingsButton.interactable}, OnClick listeners count = {_settingsButton.onClick.GetPersistentEventCount()}");
            }
            
            return _settingsButton;
        }

        public void Clear()
        {
            Debug.Log("🔄 [NavigationComponent] Очистка подписок кнопок...");
            
            if (_logEmotionButton != null) _logEmotionButton.onClick.RemoveAllListeners();
            if (_historyButton != null) _historyButton.onClick.RemoveAllListeners();
            if (_friendsButton != null) _friendsButton.onClick.RemoveAllListeners();
            if (_settingsButton != null) _settingsButton.onClick.RemoveAllListeners();
            if (_workshopButton != null) _workshopButton.onClick.RemoveAllListeners();
            
            Debug.Log("✅ [NavigationComponent] Подписки кнопок очищены");
        }

        private void OnDestroy()
        {
            Debug.Log("🔄 [NavigationComponent] OnDestroy вызван, очищаем подписки кнопок");
            Clear();
        }
        
        private void LogButtonsState()
        {
            Debug.Log($"ℹ️ [NavigationComponent] Кнопка LogEmotion: {(_logEmotionButton != null ? "Назначена" : "Не назначена")}");
            Debug.Log($"ℹ️ [NavigationComponent] Кнопка History: {(_historyButton != null ? "Назначена" : "Не назначена")}");
            Debug.Log($"ℹ️ [NavigationComponent] Кнопка Friends: {(_friendsButton != null ? "Назначена" : "Не назначена")}");
            Debug.Log($"ℹ️ [NavigationComponent] Кнопка Settings: {(_settingsButton != null ? "Назначена" : "Не назначена")}");
            Debug.Log($"ℹ️ [NavigationComponent] Кнопка Workshop: {(_workshopButton != null ? "Назначена" : "Не назначена")}");
        }
    }
} 