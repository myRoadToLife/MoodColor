using System;
using App.Develop.DI;
using App.Develop.Utils.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Контроллер панели настроек конфиденциальности
/// </summary>
public class PrivacyPanel : MonoBehaviour, IInjectable
{
    #region Serialized Fields
        
    [Header("Основные настройки")]
    [SerializeField] private Toggle _allowGlobalDataSharingToggle;
    [SerializeField] private Toggle _allowLocationTrackingToggle;
    [SerializeField] private Toggle _anonymizeDataToggle;
        
    [Header("Кнопки управления")]
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _resetButton;
    [SerializeField] private Button _closeButton;
        
    [Header("Уведомления")]
    [SerializeField] private GameObject _notificationPanel;
    [SerializeField] private TMP_Text _notificationText;
        
    #endregion
        
    #region Private Fields
        
    // Настройки конфиденциальности
    private bool _allowGlobalDataSharing = true;
    private bool _allowLocationTracking = true;
    private bool _anonymizeData = false;
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
        MyLogger.Log("✅ PrivacyPanel инициализирован", MyLogger.LogCategory.UI);
    }
        
    #endregion
        
    #region Initialization
        
    private void InitializeUI()
    {
        try
        {
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
            if (_allowGlobalDataSharingToggle != null)
                _allowGlobalDataSharingToggle.onValueChanged.AddListener(OnGlobalDataSharingChanged);
                
            if (_allowLocationTrackingToggle != null)
                _allowLocationTrackingToggle.onValueChanged.AddListener(OnLocationTrackingChanged);
                
            if (_anonymizeDataToggle != null)
                _anonymizeDataToggle.onValueChanged.AddListener(OnAnonymizeDataChanged);
                
            if (_saveButton != null)
                _saveButton.onClick.AddListener(OnSaveClicked);
                
            if (_resetButton != null)
                _resetButton.onClick.AddListener(OnResetClicked);
                
            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseClicked);
                
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
            if (_allowGlobalDataSharingToggle != null)
                _allowGlobalDataSharingToggle.onValueChanged.RemoveListener(OnGlobalDataSharingChanged);
                
            if (_allowLocationTrackingToggle != null)
                _allowLocationTrackingToggle.onValueChanged.RemoveListener(OnLocationTrackingChanged);
                
            if (_anonymizeDataToggle != null)
                _anonymizeDataToggle.onValueChanged.RemoveListener(OnAnonymizeDataChanged);
                
            if (_saveButton != null)
                _saveButton.onClick.RemoveListener(OnSaveClicked);
                
            if (_resetButton != null)
                _resetButton.onClick.RemoveListener(OnResetClicked);
                
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(OnCloseClicked);
                
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
        MyLogger.Log($"🔒 Глобальный сбор данных: {value}", MyLogger.LogCategory.Regional);
    }
        
    private void OnLocationTrackingChanged(bool value)
    {
        if (_isUpdatingUI) return;
            
        _allowLocationTracking = value;
        SaveToPlayerPrefs();
        MyLogger.Log($"🔒 Отслеживание локации: {value}", MyLogger.LogCategory.Regional);
    }
        
    private void OnAnonymizeDataChanged(bool value)
    {
        if (_isUpdatingUI) return;
            
        _anonymizeData = value;
        SaveToPlayerPrefs();
        MyLogger.Log($"🔒 Анонимизация данных: {value}", MyLogger.LogCategory.Regional);
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
                
            _allowGlobalDataSharing = PlayerPrefs.GetInt("Privacy_AllowGlobalSharing", 1) == 1;
            _allowLocationTracking = PlayerPrefs.GetInt("Privacy_AllowLocation", 1) == 1;
            _anonymizeData = PlayerPrefs.GetInt("Privacy_AnonymizeData", 0) == 1;
                
            if (_allowGlobalDataSharingToggle != null)
                _allowGlobalDataSharingToggle.isOn = _allowGlobalDataSharing;
                
            if (_allowLocationTrackingToggle != null)
                _allowLocationTrackingToggle.isOn = _allowLocationTracking;
                
            if (_anonymizeDataToggle != null)
                _anonymizeDataToggle.isOn = _anonymizeData;
                
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
    /// Получить текущие настройки конфиденциальности
    /// </summary>
    public bool GetAllowGlobalDataSharing() => _allowGlobalDataSharing;
    public bool GetAllowLocationTracking() => _allowLocationTracking;
    public bool GetAnonymizeData() => _anonymizeData;
        
    #endregion
}