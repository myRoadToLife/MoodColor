using System;
using System.Collections.Generic;
using System.Linq;
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

    [Serializable]
    public class RegionOption
    {
        public string DisplayName;
        public string Id;
        public string Description;
        public bool IsAutomatic;

        public RegionOption() { }

        public RegionOption(string id, string displayName, string description = "", bool isAutomatic = false)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            IsAutomatic = isAutomatic;
        }
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
        [SerializeField] private TMP_Dropdown _regionDropdown;

        [Header("Отображение текущего региона")]
        [SerializeField] private TMP_Text _currentRegionText;

        [Header("Темы")]
        [SerializeField]
        private List<ThemeOption> _themeOptions = new List<ThemeOption>
        {
            new ThemeOption { DisplayName = "По умолчанию", Value = ThemeType.Default },
            new ThemeOption { DisplayName = "Светлая", Value = ThemeType.Light },
            new ThemeOption { DisplayName = "Тёмная", Value = ThemeType.Dark }
        };

        [Header("Языки")]
        [SerializeField]
        private List<LanguageOption> _languageOptions = new List<LanguageOption>
        {
            new LanguageOption { DisplayName = "Русский", Value = "ru" },
            new LanguageOption { DisplayName = "English", Value = "en" }
        };

        [Header("Регионы")]
        [SerializeField]
        private List<RegionOption> _regionOptions = new List<RegionOption>();

        [Header("Кнопки")]
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _resetButton;
        [SerializeField] private Button _deleteAccountButton;
        [SerializeField] private Button _privacySettingsButton;
        [SerializeField] private Button _locationSettingsButton;
        [SerializeField] private Button _closeButton;

        [Header("Сообщения")]
        [SerializeField] private GameObject _popupPanel;
        [SerializeField] private TMP_Text _popupText;
        
        [Header("Панели")]
        [SerializeField] private GameObject _privacyPanel;
        [SerializeField] private GameObject _locationPanel;
        #endregion

        #region Private Fields
        private ISettingsManager _settingsManager;
        private PanelManager _panelManager;
        private IAuthStateService _authStateService;
        private SceneSwitcher _sceneSwitcher;
        private bool _isInitialized = false;
        private bool _isUpdatingUI = false;
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

            InitializeRegionOptions();
            InitializeControls();
            SubscribeEvents();
            LoadCurrentSettings();

            _isInitialized = true;
        }

        private void InitializeRegionOptions()
        {
            _regionOptions = new List<RegionOption>
            {
                // Автоматическое определение
                new RegionOption("auto", "🤖 Автоматически", "Автоматическое определение по GPS", true),

                // Минск и районы
                new RegionOption("minsk_center", "🏛️ Минск - Центр"),
                new RegionOption("minsk_north", "🏘️ Минск - Север"),
                new RegionOption("minsk_south", "🏢 Минск - Юг"),
                new RegionOption("minsk_east", "🌅 Минск - Восток"),
                new RegionOption("minsk_west", "🌇 Минск - Запад"),

                // Брестская область
                new RegionOption { DisplayName = "🏰 Брест", Id = "brest" },
                new RegionOption { DisplayName = "🌾 Барановичи", Id = "baranovichi" },
                new RegionOption { DisplayName = "🌲 Пинск", Id = "pinsk" },
                new RegionOption { DisplayName = "🏞️ Брестская область", Id = "brest_region" },

                // Витебская область
                new RegionOption { DisplayName = "🏛️ Витебск", Id = "vitebsk" },
                new RegionOption { DisplayName = "🏔️ Полоцк", Id = "polotsk" },
                new RegionOption { DisplayName = "🌲 Орша", Id = "orsha" },
                new RegionOption { DisplayName = "🍃 Витебская область", Id = "vitebsk_region" },

                // Гомельская область
                new RegionOption { DisplayName = "🏭 Гомель", Id = "gomel" },
                new RegionOption { DisplayName = "⚡ Мозырь", Id = "mozyr" },
                new RegionOption { DisplayName = "🌾 Речица", Id = "rechitsa" },
                new RegionOption { DisplayName = "🌻 Гомельская область", Id = "gomel_region" },

                // Гродненская область
                new RegionOption { DisplayName = "🏰 Гродно", Id = "grodno" },
                new RegionOption { DisplayName = "🌸 Лида", Id = "lida" },
                new RegionOption { DisplayName = "🌳 Слоним", Id = "slonim" },
                new RegionOption { DisplayName = "🏞️ Гродненская область", Id = "grodno_region" },

                // Минская область
                new RegionOption { DisplayName = "🏭 Борисов", Id = "borisov" },
                new RegionOption { DisplayName = "⚙️ Солигорск", Id = "soligorsk" },
                new RegionOption { DisplayName = "🌾 Молодечно", Id = "molodechno" },
                new RegionOption { DisplayName = "🌿 Минская область", Id = "minsk_region" },

                // Могилевская область
                new RegionOption { DisplayName = "🏛️ Могилев", Id = "mogilev" },
                new RegionOption { DisplayName = "🏭 Бобруйск", Id = "bobruisk" },
                new RegionOption { DisplayName = "🌾 Кричев", Id = "krichev" },
                new RegionOption { DisplayName = "🌻 Могилевская область", Id = "mogilev_region" },

                // Международные опции
                new RegionOption { DisplayName = "🇧🇾 Другой регион Беларуси", Id = "other_belarus" },
                new RegionOption { DisplayName = "🇷🇺 Россия", Id = "russia" },
                new RegionOption { DisplayName = "🇺🇦 Украина", Id = "ukraine" },
                new RegionOption { DisplayName = "🇱🇹 Литва", Id = "lithuania" },
                new RegionOption { DisplayName = "🇱🇻 Латвия", Id = "latvia" },
                new RegionOption { DisplayName = "🇪🇪 Эстония", Id = "estonia" },
                new RegionOption { DisplayName = "🇵🇱 Польша", Id = "poland" },
                new RegionOption { DisplayName = "🌍 Другая страна", Id = "international" },
                new RegionOption { DisplayName = "🤐 Предпочитаю не указывать", Id = "prefer_not_say" }
            };
        }

        private void InitializeControls()
        {
            // Настройка тем
            if (_themeDropdown != null)
            {
                _themeDropdown.ClearOptions();
                _themeDropdown.AddOptions(_themeOptions.ConvertAll(t => t.DisplayName));
            }

            // Настройка языков
            if (_languageDropdown != null)
            {
                _languageDropdown.ClearOptions();
                _languageDropdown.AddOptions(_languageOptions.ConvertAll(l => l.DisplayName));
            }

            // Настройка регионов
            if (_regionDropdown != null)
            {
                _regionDropdown.ClearOptions();
                _regionDropdown.AddOptions(_regionOptions.ConvertAll(r => r.DisplayName));
            }

            // Инициализация текста текущего региона
            UpdateCurrentRegionDisplay();
        }

        private void SubscribeEvents()
        {
            if (_saveButton != null)
                _saveButton.onClick.AddListener(SaveSettings);

            if (_resetButton != null)
                _resetButton.onClick.AddListener(ResetSettings);

            if (_deleteAccountButton != null)
                _deleteAccountButton.onClick.AddListener(ShowDeleteAccountPanel);

            if (_privacySettingsButton != null)
                _privacySettingsButton.onClick.AddListener(ShowPrivacyPanel);

            if (_locationSettingsButton != null)
                _locationSettingsButton.onClick.AddListener(ShowLocationPanel);

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

            if (_regionDropdown != null)
                _regionDropdown.onValueChanged.AddListener(OnRegionChanged);
        }

        private void UnsubscribeEvents()
        {
            if (_saveButton != null)
                _saveButton.onClick.RemoveListener(SaveSettings);

            if (_resetButton != null)
                _resetButton.onClick.RemoveListener(ResetSettings);

            if (_deleteAccountButton != null)
                _deleteAccountButton.onClick.RemoveListener(ShowDeleteAccountPanel);

            if (_privacySettingsButton != null)
                _privacySettingsButton.onClick.RemoveListener(ShowPrivacyPanel);

            if (_locationSettingsButton != null)
                _locationSettingsButton.onClick.RemoveListener(ShowLocationPanel);

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

            if (_regionDropdown != null)
                _regionDropdown.onValueChanged.RemoveListener(OnRegionChanged);
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

        private void OnRegionChanged(int index)
        {
            if (_isUpdatingUI || index < 0 || index >= _regionOptions.Count) return;
            
            var selectedRegion = _regionOptions[index];
            _settingsManager.SetSelectedRegion(selectedRegion.Id);
            UpdateCurrentRegionDisplay();
            
            MyLogger.Log($"🗺️ Выбран регион: {selectedRegion.DisplayName} ({selectedRegion.Id})", MyLogger.LogCategory.Regional);
        }
        #endregion

        #region Settings Management
        private void LoadCurrentSettings()
        {
            try
            {
                _isUpdatingUI = true;
                
                var settings = _settingsManager.GetCurrentSettings();

                if (_notificationsToggle != null)
                    _notificationsToggle.isOn = settings.notifications;

                if (_soundToggle != null)
                    _soundToggle.isOn = settings.sound;

                if (_themeDropdown != null)
                {
                    int themeIndex = _themeOptions.FindIndex(t => t.Value == settings.theme);
                    _themeDropdown.value = themeIndex >= 0 ? themeIndex : 0;
                }

                if (_languageDropdown != null)
                {
                    int languageIndex = _languageOptions.FindIndex(l => l.Value == settings.language);
                    _languageDropdown.value = languageIndex >= 0 ? languageIndex : 0;
                }

                if (_regionDropdown != null)
                {
                    int regionIndex = _regionOptions.FindIndex(r => r.Id == settings.selectedRegion);
                    _regionDropdown.value = regionIndex >= 0 ? regionIndex : 0;
                }
                
                UpdateCurrentRegionDisplay();
                
                _isUpdatingUI = false;
                
                MyLogger.Log("📱 Настройки загружены в UI панели настроек", MyLogger.LogCategory.UI);
            }
            catch (Exception ex)
            {
                _isUpdatingUI = false;
                MyLogger.LogError($"❌ Ошибка загрузки настроек в UI: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }

        private void SaveSettings()
        {
            _settingsManager.SaveSettings();
            UpdateCurrentRegionDisplay();
            ShowPopup("Настройки сохранены");
            MyLogger.Log("💾 Настройки панели сохранены", MyLogger.LogCategory.UI);
        }

        private void ResetSettings()
        {
            _settingsManager.ResetSettings();
            LoadCurrentSettings();
            ShowPopup("Настройки сброшены");
            MyLogger.Log("🔄 Настройки панели сброшены", MyLogger.LogCategory.UI);
        }
        #endregion

        #region Privacy Settings
        private void ShowPrivacyPanel()
        {
            try
            {
                if (_privacyPanel != null)
                {
                    // Показываем панель конфиденциальности поверх основной панели
                    _privacyPanel.SetActive(true);
                    
                    // Убеждаемся что панель конфиденциальности отображается поверх
                    _privacyPanel.transform.SetAsLastSibling();
                    
                    MyLogger.Log("🔒 Открыта панель настроек конфиденциальности", MyLogger.LogCategory.UI);
                }
                else
                {
                    MyLogger.LogWarning("⚠️ Панель конфиденциальности не назначена", MyLogger.LogCategory.UI);
                    ShowPopup("Панель конфиденциальности недоступна");
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при открытии панели конфиденциальности: {ex.Message}", MyLogger.LogCategory.UI);
                ShowPopup("Ошибка открытия панели конфиденциальности");
            }
        }
        #endregion

        #region Location Settings
        private void ShowLocationPanel()
        {
            try
            {
                if (_locationPanel != null)
                {
                    // Показываем панель выбора локации поверх основной панели
                    _locationPanel.SetActive(true);
                    
                    // Убеждаемся что панель локации отображается поверх
                    _locationPanel.transform.SetAsLastSibling();
                    
                    MyLogger.Log("📍 Открыта панель выбора локации", MyLogger.LogCategory.UI);
                }
                else
                {
                    MyLogger.LogWarning("⚠️ Панель выбора локации не назначена", MyLogger.LogCategory.UI);
                    ShowPopup("Панель выбора локации недоступна");
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при открытии панели выбора локации: {ex.Message}", MyLogger.LogCategory.UI);
                ShowPopup("Ошибка открытия панели выбора локации");
            }
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

        private void UpdateCurrentRegionDisplay()
        {
            if (_currentRegionText != null && _settingsManager != null)
            {
                try
                {
                    var settings = _settingsManager.GetCurrentSettings();
                    var region = _regionOptions.FirstOrDefault(r => r.Id == settings.selectedRegion);
                    
                    if (region != null)
                    {
                        _currentRegionText.text = $"📍 Текущий регион: {region.DisplayName}";
                    }
                    else
                    {
                        _currentRegionText.text = "📍 Текущий регион: Не выбран";
                    }
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"❌ Ошибка обновления отображения региона: {ex.Message}", MyLogger.LogCategory.UI);
                    _currentRegionText.text = "📍 Текущий регион: Ошибка загрузки";
                }
            }
        }
        #endregion
    }
}