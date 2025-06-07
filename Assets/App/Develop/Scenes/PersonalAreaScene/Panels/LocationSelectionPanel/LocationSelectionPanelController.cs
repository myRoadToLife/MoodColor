using System;
using System.Collections.Generic;
using System.Linq;
using App.Develop.DI;
using App.Develop.Utils.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.Develop.Scenes.PersonalAreaScene.Panels
{
    [Serializable]
    public class LocationRegionOption
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public bool IsAutomatic;

        public LocationRegionOption() { }

        public LocationRegionOption(string id, string displayName, string description = "", bool isAutomatic = false)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            IsAutomatic = isAutomatic;
        }
    }

    public class LocationSelectionPanelController : MonoBehaviour, IInjectable
    {
        #region Serialized Fields
        [Header("Основные элементы")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _currentLocationText;
        [SerializeField] private TMP_Dropdown _regionDropdown;
        [SerializeField] private Toggle _useManualSelectionToggle;
        [SerializeField] private Button _refreshLocationButton;

        [Header("Информация")]
        [SerializeField] private TMP_Text _locationInfoText;
        [SerializeField] private TMP_Text _descriptionText;

        [Header("Кнопки управления")]
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _resetButton;
        [SerializeField] private Button _closeButton;

        [Header("Уведомления")]
        [SerializeField] private GameObject _notificationPanel;
        [SerializeField] private TMP_Text _notificationText;
        #endregion

        #region Private Fields
        private List<LocationRegionOption> _availableRegions;
        private bool _useManualRegionSelection = false;
        private string _manuallySelectedRegion = "";
        private bool _isUpdatingUI = false;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            InitializeUI();
            LoadCurrentSettings();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
        #endregion

        #region IInjectable
        public void Inject(DIContainer container)
        {
            MyLogger.Log("📍 LocationSelectionPanel инициализирован", MyLogger.LogCategory.UI);
        }
        #endregion

        #region Initialization
        private void InitializeUI()
        {
            InitializeAvailableRegions();
            SetupUI();
            SubscribeEvents();
            UpdateLocationInfoText();
        }

        private void InitializeAvailableRegions()
        {
            _availableRegions = new List<LocationRegionOption>
            {
                // Автоматическое определение
                new LocationRegionOption("auto", "🤖 Автоматически", "Автоматическое определение по GPS", true),

                // Минск и районы
                new LocationRegionOption("minsk_center", "🏛️ Минск - Центр"),
                new LocationRegionOption("minsk_north", "🏘️ Минск - Север"),
                new LocationRegionOption("minsk_south", "🏢 Минск - Юг"),
                new LocationRegionOption("minsk_east", "🌅 Минск - Восток"),
                new LocationRegionOption("minsk_west", "🌇 Минск - Запад"),

                // Брестская область
                new LocationRegionOption { DisplayName = "🏰 Брест", Id = "brest" },
                new LocationRegionOption { DisplayName = "🌾 Барановичи", Id = "baranovichi" },
                new LocationRegionOption { DisplayName = "🌲 Пинск", Id = "pinsk" },
                new LocationRegionOption { DisplayName = "🏞️ Брестская область", Id = "brest_region" },

                // Витебская область
                new LocationRegionOption { DisplayName = "🏛️ Витебск", Id = "vitebsk" },
                new LocationRegionOption { DisplayName = "🏔️ Полоцк", Id = "polotsk" },
                new LocationRegionOption { DisplayName = "🌲 Орша", Id = "orsha" },
                new LocationRegionOption { DisplayName = "🍃 Витебская область", Id = "vitebsk_region" },

                // Гомельская область
                new LocationRegionOption { DisplayName = "🏭 Гомель", Id = "gomel" },
                new LocationRegionOption { DisplayName = "⚡ Мозырь", Id = "mozyr" },
                new LocationRegionOption { DisplayName = "🌾 Речица", Id = "rechitsa" },
                new LocationRegionOption { DisplayName = "🌻 Гомельская область", Id = "gomel_region" },

                // Гродненская область
                new LocationRegionOption { DisplayName = "🏰 Гродно", Id = "grodno" },
                new LocationRegionOption { DisplayName = "🌸 Лида", Id = "lida" },
                new LocationRegionOption { DisplayName = "🌳 Слоним", Id = "slonim" },
                new LocationRegionOption { DisplayName = "🏞️ Гродненская область", Id = "grodno_region" },

                // Минская область
                new LocationRegionOption { DisplayName = "🏭 Борисов", Id = "borisov" },
                new LocationRegionOption { DisplayName = "⚙️ Солигорск", Id = "soligorsk" },
                new LocationRegionOption { DisplayName = "🌾 Молодечно", Id = "molodechno" },
                new LocationRegionOption { DisplayName = "🌿 Минская область", Id = "minsk_region" },

                // Могилевская область
                new LocationRegionOption { DisplayName = "🏛️ Могилев", Id = "mogilev" },
                new LocationRegionOption { DisplayName = "🏭 Бобруйск", Id = "bobruisk" },
                new LocationRegionOption { DisplayName = "🌾 Кричев", Id = "krichev" },
                new LocationRegionOption { DisplayName = "🌻 Могилевская область", Id = "mogilev_region" },

                // Международные опции
                new LocationRegionOption { DisplayName = "🇧🇾 Другой регион Беларуси", Id = "other_belarus" },
                new LocationRegionOption { DisplayName = "🇷🇺 Россия", Id = "russia" },
                new LocationRegionOption { DisplayName = "🇺🇦 Украина", Id = "ukraine" },
                new LocationRegionOption { DisplayName = "🇱🇹 Литва", Id = "lithuania" },
                new LocationRegionOption { DisplayName = "🇱🇻 Латвия", Id = "latvia" },
                new LocationRegionOption { DisplayName = "🇪🇪 Эстония", Id = "estonia" },
                new LocationRegionOption { DisplayName = "🇵🇱 Польша", Id = "poland" },
                new LocationRegionOption { DisplayName = "🌍 Другая страна", Id = "international" },
                new LocationRegionOption { DisplayName = "🤐 Предпочитаю не указывать", Id = "prefer_not_say" }
            };
        }

        private void SetupUI()
        {
            if (_titleText != null)
                _titleText.text = "🗺️ Выбор локации";

            if (_regionDropdown != null)
            {
                _regionDropdown.ClearOptions();
                var options = _availableRegions.Select(r => r.DisplayName).ToList();
                _regionDropdown.AddOptions(options);
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = "Выберите ваш регион для получения персонализированных данных об эмоциях в вашем районе. " +
                                      "Можно использовать автоматическое определение по GPS или выбрать регион вручную.";
            }
        }

        private void SubscribeEvents()
        {
            if (_useManualSelectionToggle != null)
                _useManualSelectionToggle.onValueChanged.AddListener(OnUseManualSelectionChanged);

            if (_regionDropdown != null)
                _regionDropdown.onValueChanged.AddListener(OnRegionChanged);

            if (_refreshLocationButton != null)
                _refreshLocationButton.onClick.AddListener(OnRefreshLocationClicked);

            if (_saveButton != null)
                _saveButton.onClick.AddListener(OnSaveClicked);

            if (_resetButton != null)
                _resetButton.onClick.AddListener(OnResetClicked);

            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void UnsubscribeEvents()
        {
            if (_useManualSelectionToggle != null)
                _useManualSelectionToggle.onValueChanged.RemoveListener(OnUseManualSelectionChanged);

            if (_regionDropdown != null)
                _regionDropdown.onValueChanged.RemoveListener(OnRegionChanged);

            if (_refreshLocationButton != null)
                _refreshLocationButton.onClick.RemoveListener(OnRefreshLocationClicked);

            if (_saveButton != null)
                _saveButton.onClick.RemoveListener(OnSaveClicked);

            if (_resetButton != null)
                _resetButton.onClick.RemoveListener(OnResetClicked);

            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(OnCloseClicked);
        }
        #endregion

        #region Event Handlers
        private void OnUseManualSelectionChanged(bool value)
        {
            if (_isUpdatingUI) return;

            _useManualRegionSelection = value;
            UpdateRegionControls();
            SaveToPlayerPrefs();
            MyLogger.Log($"🗺️ Ручной выбор региона: {value}", MyLogger.LogCategory.Regional);
        }

        private void OnRegionChanged(int index)
        {
            if (_isUpdatingUI || index < 0 || index >= _availableRegions.Count) return;

            var selectedRegion = _availableRegions[index];

            if (selectedRegion.Id == "auto")
            {
                _useManualRegionSelection = false;
                _manuallySelectedRegion = "";
                if (_useManualSelectionToggle != null)
                    _useManualSelectionToggle.isOn = false;
            }
            else
            {
                _useManualRegionSelection = true;
                _manuallySelectedRegion = selectedRegion.Id;
                if (_useManualSelectionToggle != null)
                    _useManualSelectionToggle.isOn = true;
            }

            UpdateCurrentLocationDisplay();
            UpdateRegionControls();
            SaveToPlayerPrefs();
            MyLogger.Log($"🗺️ Выбран регион: {selectedRegion.DisplayName} ({selectedRegion.Id})", MyLogger.LogCategory.Regional);
        }

        private void OnRefreshLocationClicked()
        {
            ShowNotification("🗺️ Местоположение обновлено");
            UpdateCurrentLocationDisplay();
        }

        private void OnSaveClicked()
        {
            try
            {
                SaveToPlayerPrefs();
                ShowNotification("💾 Настройки локации сохранены");
                MyLogger.Log("💾 Настройки локации сохранены пользователем", MyLogger.LogCategory.Regional);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка сохранения настроек локации: {ex.Message}", MyLogger.LogCategory.Regional);
                ShowNotification("❌ Ошибка сохранения настроек");
            }
        }

        private void OnResetClicked()
        {
            try
            {
                _useManualRegionSelection = false;
                _manuallySelectedRegion = "";

                SaveToPlayerPrefs();
                LoadCurrentSettings();
                ShowNotification("🔄 Настройки локации сброшены");
                MyLogger.Log("🔄 Настройки локации сброшены", MyLogger.LogCategory.Regional);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка сброса настроек локации: {ex.Message}", MyLogger.LogCategory.Regional);
                ShowNotification("❌ Ошибка сброса настроек");
            }
        }

        private void OnCloseClicked()
        {
            gameObject.SetActive(false);
        }
        #endregion

        #region Settings Management
        private void LoadCurrentSettings()
        {
            try
            {
                _isUpdatingUI = true;

                _useManualRegionSelection = PlayerPrefs.GetInt("Location_UseManualRegion", 0) == 1;
                _manuallySelectedRegion = PlayerPrefs.GetString("Location_ManualRegion", "");

                if (_useManualSelectionToggle != null)
                    _useManualSelectionToggle.isOn = _useManualRegionSelection;

                UpdateRegionDropdown();
                UpdateRegionControls();
                UpdateCurrentLocationDisplay();

                _isUpdatingUI = false;

                MyLogger.Log("📱 Настройки локации загружены в UI", MyLogger.LogCategory.UI);
            }
            catch (Exception ex)
            {
                _isUpdatingUI = false;
                MyLogger.LogError($"❌ Ошибка загрузки настроек локации в UI: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }

        private void SaveToPlayerPrefs()
        {
            try
            {
                PlayerPrefs.SetInt("Location_UseManualRegion", _useManualRegionSelection ? 1 : 0);
                PlayerPrefs.SetString("Location_ManualRegion", _manuallySelectedRegion);
                PlayerPrefs.Save();

                MyLogger.Log("💾 Настройки локации сохранены в PlayerPrefs", MyLogger.LogCategory.Regional);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка сохранения настроек локации в PlayerPrefs: {ex.Message}", MyLogger.LogCategory.Regional);
            }
        }
        #endregion

        #region UI Updates
        private void UpdateRegionDropdown()
        {
            if (_regionDropdown == null) return;

            try
            {
                int selectedIndex = 0;

                if (_useManualRegionSelection && !string.IsNullOrEmpty(_manuallySelectedRegion))
                {
                    selectedIndex = _availableRegions.FindIndex(r => r.Id == _manuallySelectedRegion);
                    if (selectedIndex < 0) selectedIndex = 0;
                }

                _regionDropdown.value = selectedIndex;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка обновления dropdown региона: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }

        private void UpdateRegionControls()
        {
            try
            {
                if (_regionDropdown != null)
                    _regionDropdown.interactable = true; // Всегда доступен для выбора

                if (_refreshLocationButton != null)
                    _refreshLocationButton.interactable = !_useManualRegionSelection; // Доступен только для автоматического определения
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка обновления контролов региона: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }

        private void UpdateCurrentLocationDisplay()
        {
            if (_currentLocationText == null) return;

            try
            {
                if (_useManualRegionSelection && !string.IsNullOrEmpty(_manuallySelectedRegion))
                {
                    var region = _availableRegions.FirstOrDefault(r => r.Id == _manuallySelectedRegion);
                    _currentLocationText.text = region != null ? $"📍 {region.DisplayName}" : "📍 Неизвестный регион";
                }
                else
                {
                    _currentLocationText.text = "📍 Автоматическое определение";
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка обновления отображения текущей локации: {ex.Message}", MyLogger.LogCategory.UI);
                _currentLocationText.text = "📍 Ошибка определения";
            }
        }

        private void UpdateLocationInfoText()
        {
            if (_locationInfoText == null) return;

            try
            {
                _locationInfoText.text = "ℹ️ Информация о локации\n\n" +
                                       "• Автоматическое определение использует GPS\n" +
                                       "• Ручной выбор позволяет указать регион самостоятельно\n" +
                                       "• Данные используются для персонализации контента\n" +
                                       "• Вы можете изменить настройки в любое время";
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка обновления информационного текста: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }

        private void ShowNotification(string message)
        {
            if (_notificationPanel == null || _notificationText == null) return;

            try
            {
                _notificationText.text = message;
                _notificationPanel.SetActive(true);
                CancelInvoke(nameof(HideNotification));
                Invoke(nameof(HideNotification), 3f);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка показа уведомления: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }

        private void HideNotification()
        {
            if (_notificationPanel != null)
            {
                _notificationPanel.SetActive(false);
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Получить текущие настройки локации
        /// </summary>
        public (bool useManual, string regionId) GetCurrentLocationSettings()
        {
            return (_useManualRegionSelection, _manuallySelectedRegion);
        }

        /// <summary>
        /// Получить эффективный ID региона
        /// </summary>
        public string GetEffectiveRegionId(string gpsRegionId)
        {
            return _useManualRegionSelection && !string.IsNullOrEmpty(_manuallySelectedRegion)
                ? _manuallySelectedRegion
                : gpsRegionId ?? "auto";
        }
        #endregion
    }
} 