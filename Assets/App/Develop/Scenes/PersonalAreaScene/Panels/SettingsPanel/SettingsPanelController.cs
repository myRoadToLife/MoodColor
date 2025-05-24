using System;
using System.Collections.Generic;
using App.Develop.CommonServices.Firebase.Auth.Services;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.CommonServices.UI;
using App.Develop.DI;
using App.Develop.Utils.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.Develop.Scenes.PersonalAreaScene.Settings
{
    #region Models and Enums
    [Serializable]
    public class ThemeOption
    {
        public string DisplayName;
        public ThemeType Value;
    }

    [Serializable]
    public class LanguageOption
    {
        public string DisplayName;
        public string Value;
    }
    #endregion

    public class SettingsPanelController : MonoBehaviour, IInjectable
    {
        #region Serialized Fields
        [Header("Настройки")]
        [SerializeField] private Toggle _notificationsToggle;
        [SerializeField] private Toggle _soundToggle;
        [SerializeField] private TMP_Dropdown _themeDropdown;
        [SerializeField] private TMP_Dropdown _languageDropdown;
        
        [Header("Темы")]
        [SerializeField] private List<ThemeOption> _themeOptions = new List<ThemeOption>
        {
            new ThemeOption { DisplayName = "По умолчанию", Value = ThemeType.Default },
            new ThemeOption { DisplayName = "Светлая", Value = ThemeType.Light },
            new ThemeOption { DisplayName = "Тёмная", Value = ThemeType.Dark }
        };
        
        [Header("Языки")]
        [SerializeField] private List<LanguageOption> _languageOptions = new List<LanguageOption>
        {
            new LanguageOption { DisplayName = "Русский", Value = "ru" },
            new LanguageOption { DisplayName = "English", Value = "en" }
        };

        [Header("Кнопки")]
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _resetButton;
        [SerializeField] private Button _deleteAccountButton;
        [SerializeField] private Button _closeButton;
        
        [Header("Сообщения")]
        [SerializeField] private GameObject _popupPanel;
        [SerializeField] private TMP_Text _popupText;
        #endregion

        #region Private Fields
        private ISettingsManager _settingsManager;
        private PanelManager _panelManager;
        private IAuthStateService _authStateService;
        private SceneSwitcher _sceneSwitcher;
        private bool _isInitialized = false;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            if (_isInitialized)
            {
                LoadCurrentSettings();
            }
        }
        
        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
        #endregion

        #region Initialization
        public void Inject(DIContainer container)
        {
            _settingsManager = container.Resolve<ISettingsManager>();
            _panelManager = container.Resolve<PanelManager>();
            _authStateService = container.Resolve<IAuthStateService>();
            _sceneSwitcher = container.Resolve<SceneSwitcher>();

            InitializeControls();
            SubscribeEvents();
            LoadCurrentSettings();
            
            _isInitialized = true;
        }

        private void InitializeControls()
        {
            // Инициализация выпадающего списка тем
            _themeDropdown.ClearOptions();
            List<TMP_Dropdown.OptionData> themeOptionData = new List<TMP_Dropdown.OptionData>();
            foreach (var theme in _themeOptions)
            {
                themeOptionData.Add(new TMP_Dropdown.OptionData(theme.DisplayName));
            }
            _themeDropdown.AddOptions(themeOptionData);

            // Инициализация выпадающего списка языков
            _languageDropdown.ClearOptions();
            List<TMP_Dropdown.OptionData> languageOptionData = new List<TMP_Dropdown.OptionData>();
            foreach (var language in _languageOptions)
            {
                languageOptionData.Add(new TMP_Dropdown.OptionData(language.DisplayName));
            }
            _languageDropdown.AddOptions(languageOptionData);
        }

        private void SubscribeEvents()
        {
            if (_saveButton != null)
                _saveButton.onClick.AddListener(SaveSettings);
            
            if (_resetButton != null)
                _resetButton.onClick.AddListener(ResetSettings);
            
            if (_deleteAccountButton != null)
                _deleteAccountButton.onClick.AddListener(ShowDeleteAccountPanel);
                
            if (_closeButton != null)
                _closeButton.onClick.AddListener(ClosePanel);
                
            if (_notificationsToggle != null)
                _notificationsToggle.onValueChanged.AddListener(OnNotificationsChanged);
                
            if (_soundToggle != null)
                _soundToggle.onValueChanged.AddListener(OnSoundChanged);
                
            if (_themeDropdown != null)
                _themeDropdown.onValueChanged.AddListener(OnThemeChanged);
                
            if (_languageDropdown != null)
                _languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }

        private void UnsubscribeEvents()
        {
            if (_saveButton != null)
                _saveButton.onClick.RemoveListener(SaveSettings);
            
            if (_resetButton != null)
                _resetButton.onClick.RemoveListener(ResetSettings);
            
            if (_deleteAccountButton != null)
                _deleteAccountButton.onClick.RemoveListener(ShowDeleteAccountPanel);
                
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(ClosePanel);
                
            if (_notificationsToggle != null)
                _notificationsToggle.onValueChanged.RemoveListener(OnNotificationsChanged);
                
            if (_soundToggle != null)
                _soundToggle.onValueChanged.RemoveListener(OnSoundChanged);
                
            if (_themeDropdown != null)
                _themeDropdown.onValueChanged.RemoveListener(OnThemeChanged);
                
            if (_languageDropdown != null)
                _languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
        }
        #endregion

        #region UI Event Handlers
        private void OnNotificationsChanged(bool value)
        {
            _settingsManager.SetNotifications(value);
        }

        private void OnSoundChanged(bool value)
        {
            _settingsManager.SetSound(value);
        }

        private void OnThemeChanged(int index)
        {
            if (index >= 0 && index < _themeOptions.Count)
            {
                _settingsManager.SetTheme(_themeOptions[index].Value);
            }
        }

        private void OnLanguageChanged(int index)
        {
            if (index >= 0 && index < _languageOptions.Count)
            {
                _settingsManager.SetLanguage(_languageOptions[index].Value);
            }
        }
        #endregion

        #region Settings Management
        private void LoadCurrentSettings()
        {
            if (_settingsManager == null)
            {
                MyLogger.LogError("SettingsManager не инициализирован!");
                return;
            }

            SettingsData settings = _settingsManager.GetCurrentSettings();
            
            // Проверяем все UI элементы перед использованием
            if (_notificationsToggle == null)
            {
                MyLogger.LogError("_notificationsToggle не назначен в инспекторе!");
                return;
            }
            
            if (_soundToggle == null)
            {
                MyLogger.LogError("_soundToggle не назначен в инспекторе!");
                return;
            }
            
            if (_themeDropdown == null)
            {
                MyLogger.LogError("_themeDropdown не назначен в инспекторе!");
                return;
            }
            
            if (_languageDropdown == null)
            {
                MyLogger.LogError("_languageDropdown не назначен в инспекторе!");
                return;
            }
            
            if (_themeOptions == null || _themeOptions.Count == 0)
            {
                MyLogger.LogError("_themeOptions не настроен!");
                return;
            }
            
            if (_languageOptions == null || _languageOptions.Count == 0)
            {
                MyLogger.LogError("_languageOptions не настроен!");
                return;
            }

            _notificationsToggle.isOn = settings.notifications;
            _soundToggle.isOn = settings.sound;

            int themeIndex = _themeOptions.FindIndex(t => t.Value == settings.theme);
            if (themeIndex != -1)
            {
                _themeDropdown.value = themeIndex;
            }

            int languageIndex = _languageOptions.FindIndex(l => l.Value == settings.language);
            if (languageIndex != -1)
            {
                _languageDropdown.value = languageIndex;
            }
        }

        private void SaveSettings()
        {
            _settingsManager.SaveSettings();
            ShowPopup("Настройки сохранены");
        }

        private void ResetSettings()
        {
            _settingsManager.ResetSettings();
            LoadCurrentSettings();
            ShowPopup("Настройки сброшены");
        }
        #endregion

        #region Account Deletion
        private void ShowDeleteAccountPanel()
        {
            MyLogger.Log("🔘 Запрос на показ панели удаления аккаунта");

            // Проверяем состояние аутентификации перед показом панели
            if (_authStateService == null || !_authStateService.IsAuthenticated)
            {
                MyLogger.LogError("❌ Пользователь не авторизован при попытке показать панель удаления");
                ShowPopup("Для удаления аккаунта необходимо войти в систему.");
                
                // Перенаправляем на экран входа с небольшой задержкой
                Invoke(nameof(RedirectToAuth), 2f);
                return;
            }
            
            MyLogger.Log($"✅ Показ панели удаления для пользователя: {_authStateService.CurrentUser.Email}");
            _ = _panelManager.TogglePanelAsync<AccountDeletionManager>(AssetAddresses.DeletionAccountPanel);
        }

        private void RedirectToAuth()
        {
            _sceneSwitcher.ProcessSwitchSceneFor(new OutputPersonalAreaScreenArgs(new AuthSceneInputArgs()));
        }
        
        private void ClosePanel()
        {
            if (_panelManager != null)
            {
                _ = _panelManager.TogglePanelAsync<SettingsPanelController>(AssetAddresses.SettingsPanel);
            }
        }
        #endregion

        #region Utilities
        private void ShowPopup(string message)
        {
            if (_popupPanel == null || _popupText == null) return;
            
            _popupText.text = message;
            _popupPanel.SetActive(true);
            CancelInvoke(nameof(HidePopup));
            Invoke(nameof(HidePopup), 3f);
        }

        private void HidePopup()
        {
            if (_popupPanel != null)
                _popupPanel.SetActive(false);
        }
        #endregion
    }
} 