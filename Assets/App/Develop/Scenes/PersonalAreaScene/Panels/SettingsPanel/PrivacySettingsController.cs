using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using App.Develop.DI;
using App.Develop.Utils.Logging;
using App.Develop.CommonServices.Privacy;
using PrivacyRegionOption = App.Develop.CommonServices.Privacy.RegionOption;

namespace App.Develop.Scenes.PersonalAreaScene.Settings
{
    /// <summary>
    /// Контроллер панели настроек конфиденциальности
    /// </summary>
    public class PrivacySettingsController : MonoBehaviour, IInjectable
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
        
        private IPrivacyService _privacyService;
        private PrivacySettings _currentSettings;
        private List<PrivacyRegionOption> _availableRegions;
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
        
        #region IInjectable
        
        public void Inject(DIContainer container)
        {
            try
            {
                _privacyService = container.Resolve<IPrivacyService>();
                
                InitializeUI();
                SubscribeEvents();
                LoadCurrentSettings();
                
                _isInitialized = true;
                MyLogger.Log("✅ PrivacySettingsController инициализирован", MyLogger.LogCategory.UI);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка инициализации PrivacySettingsController: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeUI()
        {
            try
            {
                // Получаем доступные регионы
                _availableRegions = _privacyService?.GetAvailableRegions();
                if (_availableRegions == null)
                    _availableRegions = new List<PrivacyRegionOption>();
                
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
                
                // Подписываемся на изменения настроек
                if (_privacyService != null)
                    _privacyService.OnPrivacySettingsChanged += OnPrivacySettingsChanged;
                
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
                
                // Отписываемся от изменений настроек
                if (_privacyService != null)
                    _privacyService.OnPrivacySettingsChanged -= OnPrivacySettingsChanged;
                
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
            if (_isUpdatingUI || _privacyService == null) return;
            
            _privacyService.AllowGlobalDataSharing = value;
            UpdateDataUsageText();
            MyLogger.Log($"🔒 Глобальный сбор данных: {value}", MyLogger.LogCategory.Regional);
        }
        
        private void OnLocationTrackingChanged(bool value)
        {
            if (_isUpdatingUI || _privacyService == null) return;
            
            _privacyService.AllowLocationTracking = value;
            UpdateRegionControls();
            MyLogger.Log($"🗺️ Отслеживание местоположения: {value}", MyLogger.LogCategory.Location);
        }
        
        private void OnAnonymizeDataChanged(bool value)
        {
            if (_isUpdatingUI || _privacyService == null) return;
            
            _privacyService.AnonymizeData = value;
            UpdateDataUsageText();
            MyLogger.Log($"🔒 Анонимизация данных: {value}", MyLogger.LogCategory.Regional);
        }
        
        private void OnUseManualRegionChanged(bool value)
        {
            if (_isUpdatingUI || _privacyService == null) return;
            
            _privacyService.UseManualRegionSelection = value;
            UpdateRegionControls();
            MyLogger.Log($"🗺️ Ручной выбор региона: {value}", MyLogger.LogCategory.Regional);
        }
        
        private void OnRegionChanged(int index)
        {
            if (_isUpdatingUI || _privacyService == null || index < 0 || index >= _availableRegions.Count) return;
            
            var selectedRegion = _availableRegions[index];
            
            // Если выбран автоматический режим
            if (selectedRegion.Id == "auto")
            {
                _privacyService.UseManualRegionSelection = false;
                _privacyService.ManuallySelectedRegion = "";
            }
            else
            {
                _privacyService.UseManualRegionSelection = true;
                _privacyService.ManuallySelectedRegion = selectedRegion.Id;
            }
            
            UpdateRegionControls();
            MyLogger.Log($"🗺️ Выбран регион: {selectedRegion.DisplayName} ({selectedRegion.Id})", MyLogger.LogCategory.Regional);
        }
        
        private void OnRefreshLocationClicked()
        {
            // TODO: Обновить текущее местоположение через LocationService
            ShowNotification("🗺️ Местоположение обновлено");
        }
        
        private void OnSaveClicked()
        {
            try
            {
                _privacyService?.SaveSettings();
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
                var defaultSettings = new PrivacySettings();
                _privacyService?.ApplySettings(defaultSettings);
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
                _privacyService?.RevokeAllConsents();
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
        
        private void OnPrivacySettingsChanged(PrivacySettings settings)
        {
            _currentSettings = settings;
            LoadCurrentSettings();
        }
        
        #endregion
        
        #region UI Updates
        
        private void LoadCurrentSettings()
        {
            try
            {
                if (_privacyService == null) return;
                
                _isUpdatingUI = true;
                
                _currentSettings = _privacyService.GetCurrentSettings();
                
                // Обновляем основные настройки
                if (_allowGlobalDataSharingToggle != null)
                    _allowGlobalDataSharingToggle.isOn = _currentSettings.AllowGlobalDataSharing;
                
                if (_allowLocationTrackingToggle != null)
                    _allowLocationTrackingToggle.isOn = _currentSettings.AllowLocationTracking;
                
                if (_anonymizeDataToggle != null)
                    _anonymizeDataToggle.isOn = _currentSettings.AnonymizeData;
                
                if (_useManualRegionToggle != null)
                    _useManualRegionToggle.isOn = _currentSettings.UseManualRegionSelection;
                
                // Обновляем выбор региона
                UpdateRegionDropdown();
                UpdateRegionControls();
                UpdateInfoTexts();
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
        
        private void UpdateRegionDropdown()
        {
            if (_regionDropdown == null || _currentSettings == null) return;
            
            try
            {
                // Находим индекс текущего региона
                int selectedIndex = 0;
                
                if (_currentSettings.UseManualRegionSelection && !string.IsNullOrEmpty(_currentSettings.ManuallySelectedRegion))
                {
                    selectedIndex = _availableRegions.FindIndex(r => r.Id == _currentSettings.ManuallySelectedRegion);
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
                if (_currentSettings == null) return;
                
                // Включаем/отключаем dropdown в зависимости от настроек
                bool canSelectRegion = _currentSettings.AllowLocationTracking || _currentSettings.UseManualRegionSelection;
                
                if (_regionDropdown != null)
                    _regionDropdown.interactable = canSelectRegion;
                
                if (_refreshLocationButton != null)
                    _refreshLocationButton.interactable = _currentSettings.AllowLocationTracking;
                
                // Обновляем текст текущего местоположения
                UpdateCurrentLocationText();
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка обновления контролов региона: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }
        
        private void UpdateCurrentLocationText()
        {
            if (_currentLocationText == null || _currentSettings == null) return;
            
            try
            {
                string locationText = "📍 ";
                
                if (_currentSettings.UseManualRegionSelection && !string.IsNullOrEmpty(_currentSettings.ManuallySelectedRegion))
                {
                    var region = _availableRegions.FirstOrDefault(r => r.Id == _currentSettings.ManuallySelectedRegion);
                    locationText += $"Выбран: {region?.DisplayName ?? "Неизвестно"}";
                }
                else if (_currentSettings.AllowLocationTracking)
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
            if (_dataUsageText == null || _currentSettings == null) return;
            
            try
            {
                string usageText = "📊 Использование данных:\n\n";
                
                if (_currentSettings.AllowGlobalDataSharing)
                {
                    usageText += "✅ Ваши эмоции участвуют в глобальной статистике\n";
                    
                    if (_currentSettings.AnonymizeData)
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
                
                if (_currentSettings.AllowLocationTracking)
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
    }
} 
