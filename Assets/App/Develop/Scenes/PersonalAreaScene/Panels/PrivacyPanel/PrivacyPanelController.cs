using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using App.Develop.DI;
using App.Develop.Utils.Logging;
using App.Develop.UI;
using App.Develop.CommonServices.Privacy;

namespace App.Develop.Scenes.PersonalAreaScene.Privacy
{
    /// <summary>
    /// Контроллер панели настроек конфиденциальности
    /// </summary>
    public class PrivacyPanelController : MonoBehaviour, IInjectable
    {
        #region Serialized Fields
        
        [Header("Основные настройки")]
        [SerializeField] private Toggle _allowGlobalDataSharingToggle;
        [SerializeField] private Toggle _allowLocationTrackingToggle;
        [SerializeField] private Toggle _anonymizeDataToggle;
        
        [Header("Выбор региона")]
        [SerializeField] private Toggle _useManualRegionToggle;
        [SerializeField] private TMP_Dropdown _regionDropdown;
        [SerializeField] private TMP_Text _currentLocationText;
        [SerializeField] private Button _refreshLocationButton;
        
        [Header("Информация")]
        [SerializeField] private TMP_Text _privacyInfoText;
        [SerializeField] private TMP_Text _dataUsageText;
        
        [Header("Кнопки управления")]
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _resetButton;
        [SerializeField] private Button _revokeAllButton;
        [SerializeField] private Button _closeButton;
        
        [Header("Уведомления")]
        [SerializeField] private GameObject _notificationPanel;
        [SerializeField] private TMP_Text _notificationText;
        [SerializeField] private Button _notificationOkButton;
        
        #endregion
        
        #region Private Fields
        
        // Временно используем простые настройки без PrivacyService
        private bool _allowGlobalDataSharing = true;
        private bool _allowLocationTracking = true;
        private bool _anonymizeData = false;
        private bool _useManualRegionSelection = false;
        private string _manuallySelectedRegion = "";
        
        private List<RegionOption> _availableRegions;
        private bool _isInitialized = false;
        private bool _isUpdatingUI = false;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            InitializeUI();
            SubscribeEvents();
            LoadCurrentSettings();
            _isInitialized = true;
        }
        
        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
        
        #endregion
        
        #region IInjectable
        
        public void Inject(DIContainer container)
        {
            // Пока что не используем DI, инициализируем в Start
            MyLogger.Log("✅ PrivacyPanelController инициализирован", MyLogger.LogCategory.UI);
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeUI()
        {
            try
            {
                // Инициализируем доступные регионы
                InitializeAvailableRegions();
                
                // Настраиваем dropdown регионов
                if (_regionDropdown != null)
                {
                    _regionDropdown.ClearOptions();
                    var options = _availableRegions.Select(r => r.DisplayName).ToList();
                    _regionDropdown.AddOptions(options);
                }
                
                // Устанавливаем информационные тексты
                UpdateInfoTexts();
                
                MyLogger.Log("🎨 UI панели конфиденциальности инициализирован", MyLogger.LogCategory.UI);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка инициализации UI: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }
        
        private void InitializeAvailableRegions()
        {
            _availableRegions = new List<RegionOption>
            {
                // Автоматический режим
                new RegionOption("auto", "🤖 Автоматически", "Автоматическое определение по GPS", true),
                
                // Минск (столица)
                new RegionOption("minsk_center", "🏛️ Минск - Центр"),
                new RegionOption("minsk_north", "🏘️ Минск - Север"),
                new RegionOption("minsk_south", "🏢 Минск - Юг"),
                new RegionOption("minsk_east", "🌅 Минск - Восток"),
                new RegionOption("minsk_west", "🌇 Минск - Запад"),
                
                // Брестская область
                new RegionOption("brest", "🏰 Брест"),
                new RegionOption("baranovichi", "🌾 Барановичи"),
                new RegionOption("pinsk", "🌲 Пинск"),
                new RegionOption("brest_region", "🏞️ Брестская область"),
                
                // Витебская область  
                new RegionOption("vitebsk", "🏛️ Витебск"),
                new RegionOption("polotsk", "🏔️ Полоцк"),
                new RegionOption("orsha", "🌲 Орша"),
                new RegionOption("vitebsk_region", "🍃 Витебская область"),
                
                // Гомельская область
                new RegionOption("gomel", "🏭 Гомель"),
                new RegionOption("mozyr", "⚡ Мозырь"),
                new RegionOption("rechitsa", "🌾 Речица"),
                new RegionOption("gomel_region", "🌻 Гомельская область"),
                
                // Гродненская область
                new RegionOption("grodno", "🏰 Гродно"),
                new RegionOption("lida", "🌸 Лида"),
                new RegionOption("slonim", "🌳 Слоним"),
                new RegionOption("grodno_region", "🏞️ Гродненская область"),
                
                // Минская область
                new RegionOption("borisov", "🏭 Борисов"),
                new RegionOption("soligorsk", "⚙️ Солигорск"),
                new RegionOption("molodechno", "🌾 Молодечно"),
                new RegionOption("minsk_region", "🌿 Минская область"),
                
                // Могилевская область
                new RegionOption("mogilev", "🏛️ Могилев"),
                new RegionOption("bobruisk", "🏭 Бобруйск"),
                new RegionOption("krichev", "🌾 Кричев"),
                new RegionOption("mogilev_region", "🌻 Могилевская область"),
                
                // Общие варианты
                new RegionOption("other_belarus", "🇧🇾 Другой регион Беларуси"),
                new RegionOption("russia", "🇷🇺 Россия"),
                new RegionOption("ukraine", "🇺🇦 Украина"),
                new RegionOption("lithuania", "🇱🇹 Литва"),
                new RegionOption("latvia", "🇱🇻 Латвия"),
                new RegionOption("estonia", "🇪🇪 Эстония"),
                new RegionOption("poland", "🇵🇱 Польша"),
                new RegionOption("international", "🌍 Другая страна"),
                new RegionOption("prefer_not_say", "🤐 Предпочитаю не указывать")
            };
        }
        
        private void SubscribeEvents()
        {
            try
            {
                // Основные настройки
                if (_allowGlobalDataSharingToggle != null)
                    _allowGlobalDataSharingToggle.onValueChanged.AddListener(OnGlobalDataSharingChanged);
                
                if (_allowLocationTrackingToggle != null)
                    _allowLocationTrackingToggle.onValueChanged.AddListener(OnLocationTrackingChanged);
                
                if (_anonymizeDataToggle != null)
                    _anonymizeDataToggle.onValueChanged.AddListener(OnAnonymizeDataChanged);
                
                // Настройки региона
                if (_useManualRegionToggle != null)
                    _useManualRegionToggle.onValueChanged.AddListener(OnUseManualRegionChanged);
                
                if (_regionDropdown != null)
                    _regionDropdown.onValueChanged.AddListener(OnRegionChanged);
                
                if (_refreshLocationButton != null)
                    _refreshLocationButton.onClick.AddListener(OnRefreshLocationClicked);
                
                // Кнопки управления
                if (_saveButton != null)
                    _saveButton.onClick.AddListener(OnSaveClicked);
                
                if (_resetButton != null)
                    _resetButton.onClick.AddListener(OnResetClicked);
                
                if (_revokeAllButton != null)
                    _revokeAllButton.onClick.AddListener(OnRevokeAllClicked);
                
                if (_closeButton != null)
                    _closeButton.onClick.AddListener(OnCloseClicked);
                
                if (_notificationOkButton != null)
                    _notificationOkButton.onClick.AddListener(HideNotification);
                
                MyLogger.Log("🔗 События панели конфиденциальности подписаны", MyLogger.LogCategory.UI);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка подписки на события: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }
        
        private void UnsubscribeEvents()
        {
            try
            {
                // Основные настройки
                if (_allowGlobalDataSharingToggle != null)
                    _allowGlobalDataSharingToggle.onValueChanged.RemoveListener(OnGlobalDataSharingChanged);
                
                if (_allowLocationTrackingToggle != null)
                    _allowLocationTrackingToggle.onValueChanged.RemoveListener(OnLocationTrackingChanged);
                
                if (_anonymizeDataToggle != null)
                    _anonymizeDataToggle.onValueChanged.RemoveListener(OnAnonymizeDataChanged);
                
                // Настройки региона
                if (_useManualRegionToggle != null)
                    _useManualRegionToggle.onValueChanged.RemoveListener(OnUseManualRegionChanged);
                
                if (_regionDropdown != null)
                    _regionDropdown.onValueChanged.RemoveListener(OnRegionChanged);
                
                if (_refreshLocationButton != null)
                    _refreshLocationButton.onClick.RemoveListener(OnRefreshLocationClicked);
                
                // Кнопки управления
                if (_saveButton != null)
                    _saveButton.onClick.RemoveListener(OnSaveClicked);
                
                if (_resetButton != null)
                    _resetButton.onClick.RemoveListener(OnResetClicked);
                
                if (_revokeAllButton != null)
                    _revokeAllButton.onClick.RemoveListener(OnRevokeAllClicked);
                
                if (_closeButton != null)
                    _closeButton.onClick.RemoveListener(OnCloseClicked);
                
                if (_notificationOkButton != null)
                    _notificationOkButton.onClick.RemoveListener(HideNotification);
                
                MyLogger.Log("🔗 События панели конфиденциальности отписаны", MyLogger.LogCategory.UI);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка отписки от событий: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnGlobalDataSharingChanged(bool value)
        {
            if (_isUpdatingUI) return;
            
            _allowGlobalDataSharing = value;
            SaveToPlayerPrefs();
            UpdateDataUsageText();
            MyLogger.Log($"🔒 Глобальный сбор данных: {value}", MyLogger.LogCategory.Regional);
        }
        
        private void OnLocationTrackingChanged(bool value)
        {
            if (_isUpdatingUI) return;
            
            _allowLocationTracking = value;
            SaveToPlayerPrefs();
            UpdateRegionControls();
            MyLogger.Log($"🗺️ Отслеживание местоположения: {value}", MyLogger.LogCategory.Location);
        }
        
        private void OnAnonymizeDataChanged(bool value)
        {
            if (_isUpdatingUI) return;
            
            _anonymizeData = value;
            SaveToPlayerPrefs();
            UpdateDataUsageText();
            MyLogger.Log($"🔒 Анонимизация данных: {value}", MyLogger.LogCategory.Regional);
        }
        
        private void OnUseManualRegionChanged(bool value)
        {
            if (_isUpdatingUI) return;
            
            _useManualRegionSelection = value;
            SaveToPlayerPrefs();
            UpdateRegionControls();
            MyLogger.Log($"🗺️ Ручной выбор региона: {value}", MyLogger.LogCategory.Regional);
        }
        
        private void OnRegionChanged(int index)
        {
            if (_isUpdatingUI || index < 0 || index >= _availableRegions.Count) return;
            
            var selectedRegion = _availableRegions[index];
            
            // Если выбран автоматический режим
            if (selectedRegion.Id == "auto")
            {
                _useManualRegionSelection = false;
                _manuallySelectedRegion = "";
                if (_useManualRegionToggle != null)
                    _useManualRegionToggle.isOn = false;
            }
            else
            {
                _useManualRegionSelection = true;
                _manuallySelectedRegion = selectedRegion.Id;
                if (_useManualRegionToggle != null)
                    _useManualRegionToggle.isOn = true;
            }
            
            SaveToPlayerPrefs();
            UpdateRegionControls();
            MyLogger.Log($"🗺️ Выбран регион: {selectedRegion.DisplayName} ({selectedRegion.Id})", MyLogger.LogCategory.Regional);
        }
        
        private void OnRefreshLocationClicked()
        {
            ShowNotification("🗺️ Местоположение обновлено");
        }
        
        private void OnSaveClicked()
        {
            try
            {
                SaveToPlayerPrefs();
                ShowNotification("💾 Настройки сохранены");
                MyLogger.Log("💾 Настройки конфиденциальности сохранены пользователем", MyLogger.LogCategory.Regional);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка сохранения настроек: {ex.Message}", MyLogger.LogCategory.Regional);
                ShowNotification("❌ Ошибка сохранения настроек");
            }
        }
        
        private void OnResetClicked()
        {
            try
            {
                _allowGlobalDataSharing = true;
                _allowLocationTracking = true;
                _anonymizeData = false;
                _useManualRegionSelection = false;
                _manuallySelectedRegion = "";
                
                SaveToPlayerPrefs();
                LoadCurrentSettings();
                ShowNotification("🔄 Настройки сброшены");
                MyLogger.Log("🔄 Настройки конфиденциальности сброшены", MyLogger.LogCategory.Regional);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка сброса настроек: {ex.Message}", MyLogger.LogCategory.Regional);
                ShowNotification("❌ Ошибка сброса настроек");
            }
        }
        
        private void OnRevokeAllClicked()
        {
            try
            {
                _allowGlobalDataSharing = false;
                _allowLocationTracking = false;
                _anonymizeData = true;
                
                SaveToPlayerPrefs();
                LoadCurrentSettings();
                ShowNotification("🚫 Все согласия отозваны");
                MyLogger.Log("🚫 Все согласия отозваны пользователем", MyLogger.LogCategory.Regional);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка отзыва согласий: {ex.Message}", MyLogger.LogCategory.Regional);
                ShowNotification("❌ Ошибка отзыва согласий");
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
                
                // Загружаем из PlayerPrefs
                _allowGlobalDataSharing = PlayerPrefs.GetInt("Privacy_AllowGlobalSharing", 1) == 1;
                _allowLocationTracking = PlayerPrefs.GetInt("Privacy_AllowLocation", 1) == 1;
                _anonymizeData = PlayerPrefs.GetInt("Privacy_AnonymizeData", 0) == 1;
                _useManualRegionSelection = PlayerPrefs.GetInt("Privacy_UseManualRegion", 0) == 1;
                _manuallySelectedRegion = PlayerPrefs.GetString("Privacy_ManualRegion", "");
                
                // Обновляем UI
                if (_allowGlobalDataSharingToggle != null)
                    _allowGlobalDataSharingToggle.isOn = _allowGlobalDataSharing;
                
                if (_allowLocationTrackingToggle != null)
                    _allowLocationTrackingToggle.isOn = _allowLocationTracking;
                
                if (_anonymizeDataToggle != null)
                    _anonymizeDataToggle.isOn = _anonymizeData;
                
                if (_useManualRegionToggle != null)
                    _useManualRegionToggle.isOn = _useManualRegionSelection;
                
                UpdateRegionDropdown();
                UpdateRegionControls();
                UpdateDataUsageText();
                
                _isUpdatingUI = false;
                
                MyLogger.Log("📱 Настройки конфиденциальности загружены в UI", MyLogger.LogCategory.UI);
            }
            catch (Exception ex)
            {
                _isUpdatingUI = false;
                MyLogger.LogError($"❌ Ошибка загрузки настроек в UI: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }
        
        private void SaveToPlayerPrefs()
        {
            try
            {
                PlayerPrefs.SetInt("Privacy_AllowGlobalSharing", _allowGlobalDataSharing ? 1 : 0);
                PlayerPrefs.SetInt("Privacy_AllowLocation", _allowLocationTracking ? 1 : 0);
                PlayerPrefs.SetInt("Privacy_AnonymizeData", _anonymizeData ? 1 : 0);
                PlayerPrefs.SetInt("Privacy_UseManualRegion", _useManualRegionSelection ? 1 : 0);
                PlayerPrefs.SetString("Privacy_ManualRegion", _manuallySelectedRegion);
                PlayerPrefs.Save();
                
                MyLogger.Log("💾 Настройки конфиденциальности сохранены в PlayerPrefs", MyLogger.LogCategory.Regional);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка сохранения в PlayerPrefs: {ex.Message}", MyLogger.LogCategory.Regional);
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
                bool canSelectRegion = _allowLocationTracking || _useManualRegionSelection;
                
                if (_regionDropdown != null)
                    _regionDropdown.interactable = canSelectRegion;
                
                if (_refreshLocationButton != null)
                    _refreshLocationButton.interactable = _allowLocationTracking;
                
                UpdateCurrentLocationText();
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка обновления контролов региона: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }
        
        private void UpdateCurrentLocationText()
        {
            if (_currentLocationText == null) return;
            
            try
            {
                string locationText = "📍 ";
                
                if (_useManualRegionSelection && !string.IsNullOrEmpty(_manuallySelectedRegion))
                {
                    var region = _availableRegions.FirstOrDefault(r => r.Id == _manuallySelectedRegion);
                    locationText += $"Выбран: {region?.DisplayName ?? "Неизвестно"}";
                }
                else if (_allowLocationTracking)
                {
                    locationText += "Определяется автоматически";
                }
                else
                {
                    locationText += "Местоположение отключено";
                }
                
                _currentLocationText.text = locationText;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка обновления текста местоположения: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }
        
        private void UpdateInfoTexts()
        {
            try
            {
                if (_privacyInfoText != null)
                {
                    _privacyInfoText.text = "🔒 Настройки конфиденциальности\n\n" +
                        "Здесь вы можете управлять тем, какие данные собираются и как они используются в приложении.\n\n" +
                        "• Глобальная статистика - ваши эмоции участвуют в общей статистике\n" +
                        "• Геолокация - определение вашего региона для местной статистики\n" +
                        "• Анонимизация - удаление личных данных перед отправкой";
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка обновления информационных текстов: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }
        
        private void UpdateDataUsageText()
        {
            if (_dataUsageText == null) return;
            
            try
            {
                string usageText = "📊 Использование данных:\n\n";
                
                if (_allowGlobalDataSharing)
                {
                    usageText += "✅ Ваши эмоции участвуют в глобальной статистике\n";
                    
                    if (_anonymizeData)
                    {
                        usageText += "🔒 Данные анонимизируются перед отправкой\n";
                    }
                    else
                    {
                        usageText += "⚠️ Данные отправляются без анонимизации\n";
                    }
                }
                else
                {
                    usageText += "🚫 Ваши данные НЕ участвуют в глобальной статистике\n";
                }
                
                if (_allowLocationTracking)
                {
                    usageText += "📍 Геолокация используется для определения региона\n";
                }
                else
                {
                    usageText += "🚫 Геолокация отключена\n";
                }
                
                _dataUsageText.text = usageText;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка обновления текста использования данных: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }
        
        #endregion
        
        #region Notifications
        
        private void ShowNotification(string message)
        {
            try
            {
                if (_notificationPanel != null && _notificationText != null)
                {
                    _notificationText.text = message;
                    _notificationPanel.SetActive(true);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка показа уведомления: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }
        
        private void HideNotification()
        {
            try
            {
                if (_notificationPanel != null)
                {
                    _notificationPanel.SetActive(false);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка скрытия уведомления: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Получить текущие настройки конфиденциальности
        /// </summary>
        public bool GetAllowGlobalDataSharing() => _allowGlobalDataSharing;
        public bool GetAllowLocationTracking() => _allowLocationTracking;
        public bool GetAnonymizeData() => _anonymizeData;
        public bool GetUseManualRegionSelection() => _useManualRegionSelection;
        public string GetManuallySelectedRegion() => _manuallySelectedRegion;
        
        /// <summary>
        /// Получить эффективный RegionId с учетом настроек пользователя
        /// </summary>
        public string GetEffectiveRegionId(string gpsRegionId)
        {
            if (_useManualRegionSelection && !string.IsNullOrEmpty(_manuallySelectedRegion))
            {
                return _manuallySelectedRegion;
            }
            
            if (_allowLocationTracking && !string.IsNullOrEmpty(gpsRegionId))
            {
                return gpsRegionId;
            }
            
            if (!string.IsNullOrEmpty(_manuallySelectedRegion))
            {
                return _manuallySelectedRegion;
            }
            
            return "default";
        }
        
        #endregion
    }
} 
