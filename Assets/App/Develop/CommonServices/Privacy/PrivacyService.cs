using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using App.Develop.Utils.Logging;
using App.Develop.DI;

namespace App.Develop.CommonServices.Privacy
{
    /// <summary>
    /// Сервис управления настройками конфиденциальности пользователя
    /// </summary>
    public class PrivacyService : IPrivacyService, IInitializable
    {
        #region Private Fields
        
        private PrivacySettings _currentSettings;
        private List<RegionOption> _availableRegions;
        
        // Ключи для сохранения в PlayerPrefs
        private const string PREFS_ALLOW_GLOBAL_SHARING = "Privacy_AllowGlobalSharing";
        private const string PREFS_ALLOW_LOCATION = "Privacy_AllowLocation";
        private const string PREFS_ANONYMIZE_DATA = "Privacy_AnonymizeData";
        private const string PREFS_MANUAL_REGION = "Privacy_ManualRegion";
        private const string PREFS_USE_MANUAL_REGION = "Privacy_UseManualRegion";
        private const string PREFS_SHOWN_CONSENT = "Privacy_ShownConsent";
        
        #endregion
        
        #region Events
        
        public event Action<PrivacySettings> OnPrivacySettingsChanged;
        
        #endregion
        
        #region Properties
        
        public bool AllowGlobalDataSharing
        {
            get => _currentSettings?.AllowGlobalDataSharing ?? true;
            set
            {
                if (_currentSettings != null && _currentSettings.AllowGlobalDataSharing != value)
                {
                    _currentSettings.AllowGlobalDataSharing = value;
                    SaveSettings();
                    OnPrivacySettingsChanged?.Invoke(_currentSettings);
                    MyLogger.Log($"🔒 AllowGlobalDataSharing изменено на: {value}", MyLogger.LogCategory.Regional);
                }
            }
        }
        
        public bool AllowLocationTracking
        {
            get => _currentSettings?.AllowLocationTracking ?? true;
            set
            {
                if (_currentSettings != null && _currentSettings.AllowLocationTracking != value)
                {
                    _currentSettings.AllowLocationTracking = value;
                    SaveSettings();
                    OnPrivacySettingsChanged?.Invoke(_currentSettings);
                    MyLogger.Log($"🗺️ AllowLocationTracking изменено на: {value}", MyLogger.LogCategory.Location);
                }
            }
        }
        
        public bool AnonymizeData
        {
            get => _currentSettings?.AnonymizeData ?? false;
            set
            {
                if (_currentSettings != null && _currentSettings.AnonymizeData != value)
                {
                    _currentSettings.AnonymizeData = value;
                    SaveSettings();
                    OnPrivacySettingsChanged?.Invoke(_currentSettings);
                    MyLogger.Log($"🔒 AnonymizeData изменено на: {value}", MyLogger.LogCategory.Regional);
                }
            }
        }
        
        public string ManuallySelectedRegion
        {
            get => _currentSettings?.ManuallySelectedRegion ?? "";
            set
            {
                if (_currentSettings != null && _currentSettings.ManuallySelectedRegion != value)
                {
                    _currentSettings.ManuallySelectedRegion = value;
                    SaveSettings();
                    OnPrivacySettingsChanged?.Invoke(_currentSettings);
                    MyLogger.Log($"🗺️ ManuallySelectedRegion изменено на: {value}", MyLogger.LogCategory.Regional);
                }
            }
        }
        
        public bool UseManualRegionSelection
        {
            get => _currentSettings?.UseManualRegionSelection ?? false;
            set
            {
                if (_currentSettings != null && _currentSettings.UseManualRegionSelection != value)
                {
                    _currentSettings.UseManualRegionSelection = value;
                    SaveSettings();
                    OnPrivacySettingsChanged?.Invoke(_currentSettings);
                    MyLogger.Log($"🗺️ UseManualRegionSelection изменено на: {value}", MyLogger.LogCategory.Regional);
                }
            }
        }
        
        public bool HasShownInitialConsent
        {
            get => _currentSettings?.HasShownInitialConsent ?? false;
            set
            {
                if (_currentSettings != null && _currentSettings.HasShownInitialConsent != value)
                {
                    _currentSettings.HasShownInitialConsent = value;
                    SaveSettings();
                }
            }
        }
        
        #endregion
        
        #region IInitializable
        
        public void Initialize()
        {
            try
            {
                MyLogger.Log("🔒 Инициализация PrivacyService...", MyLogger.LogCategory.Regional);
                
                InitializeAvailableRegions();
                LoadSettings();
                
                MyLogger.Log("✅ PrivacyService инициализирован успешно", MyLogger.LogCategory.Regional);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка инициализации PrivacyService: {ex.Message}", MyLogger.LogCategory.Regional);
            }
        }
        
        #endregion
        
        #region IPrivacyService Implementation
        
        public async Task<bool> RequestDataCollectionConsent()
        {
            try
            {
                MyLogger.Log("🔒 Запрос согласия пользователя на сбор данных", MyLogger.LogCategory.Regional);
                
                // В реальном приложении здесь должен быть показ диалога согласия
                // Пока что возвращаем true и помечаем что согласие было показано
                HasShownInitialConsent = true;
                
                MyLogger.Log("✅ Согласие пользователя получено", MyLogger.LogCategory.Regional);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при запросе согласия: {ex.Message}", MyLogger.LogCategory.Regional);
                return false;
            }
        }
        
        public void RevokeAllConsents()
        {
            try
            {
                MyLogger.Log("🔒 Отзыв всех согласий пользователя", MyLogger.LogCategory.Regional);
                
                AllowGlobalDataSharing = false;
                AllowLocationTracking = false;
                AnonymizeData = true;
                
                MyLogger.Log("✅ Все согласия отозваны", MyLogger.LogCategory.Regional);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при отзыве согласий: {ex.Message}", MyLogger.LogCategory.Regional);
            }
        }
        
        public PrivacySettings GetCurrentSettings()
        {
            return _currentSettings?.Clone() ?? new PrivacySettings();
        }
        
        public void ApplySettings(PrivacySettings settings)
        {
            try
            {
                if (settings == null)
                {
                    MyLogger.LogWarning("⚠️ Попытка применить null настройки", MyLogger.LogCategory.Regional);
                    return;
                }
                
                MyLogger.Log("🔒 Применение новых настроек конфиденциальности", MyLogger.LogCategory.Regional);
                
                _currentSettings = settings.Clone();
                SaveSettings();
                OnPrivacySettingsChanged?.Invoke(_currentSettings);
                
                MyLogger.Log("✅ Настройки конфиденциальности применены", MyLogger.LogCategory.Regional);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при применении настроек: {ex.Message}", MyLogger.LogCategory.Regional);
            }
        }
        
        public List<RegionOption> GetAvailableRegions()
        {
            return new List<RegionOption>(_availableRegions);
        }
        
        public bool ShouldContributeToGlobalStats()
        {
            return AllowGlobalDataSharing && (_currentSettings?.AllowGlobalDataSharing ?? true);
        }
        
        public string GetEffectiveRegionId(string gpsRegionId)
        {
            try
            {
                // Если пользователь выбрал ручной режим и указал регион
                if (UseManualRegionSelection && !string.IsNullOrEmpty(ManuallySelectedRegion))
                {
                    MyLogger.Log($"🗺️ Используем ручной выбор региона: {ManuallySelectedRegion}", MyLogger.LogCategory.Regional);
                    return ManuallySelectedRegion;
                }
                
                // Если разрешено использование геолокации и есть GPS данные
                if (AllowLocationTracking && !string.IsNullOrEmpty(gpsRegionId))
                {
                    MyLogger.Log($"🗺️ Используем регион по GPS: {gpsRegionId}", MyLogger.LogCategory.Location);
                    return gpsRegionId;
                }
                
                // Fallback - если есть ручной выбор, используем его
                if (!string.IsNullOrEmpty(ManuallySelectedRegion))
                {
                    MyLogger.Log($"🗺️ Fallback - используем ручной регион: {ManuallySelectedRegion}", MyLogger.LogCategory.Regional);
                    return ManuallySelectedRegion;
                }
                
                // Последний fallback - default регион
                MyLogger.Log("🗺️ Используем default регион", MyLogger.LogCategory.Regional);
                return "default";
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка при определении эффективного региона: {ex.Message}", MyLogger.LogCategory.Regional);
                return "default";
            }
        }
        
        public void SaveSettings()
        {
            try
            {
                if (_currentSettings == null) return;
                
                PlayerPrefs.SetInt(PREFS_ALLOW_GLOBAL_SHARING, _currentSettings.AllowGlobalDataSharing ? 1 : 0);
                PlayerPrefs.SetInt(PREFS_ALLOW_LOCATION, _currentSettings.AllowLocationTracking ? 1 : 0);
                PlayerPrefs.SetInt(PREFS_ANONYMIZE_DATA, _currentSettings.AnonymizeData ? 1 : 0);
                PlayerPrefs.SetString(PREFS_MANUAL_REGION, _currentSettings.ManuallySelectedRegion);
                PlayerPrefs.SetInt(PREFS_USE_MANUAL_REGION, _currentSettings.UseManualRegionSelection ? 1 : 0);
                PlayerPrefs.SetInt(PREFS_SHOWN_CONSENT, _currentSettings.HasShownInitialConsent ? 1 : 0);
                PlayerPrefs.Save();
                
                MyLogger.Log("💾 Настройки конфиденциальности сохранены", MyLogger.LogCategory.Regional);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка сохранения настроек: {ex.Message}", MyLogger.LogCategory.Regional);
            }
        }
        
        public void LoadSettings()
        {
            try
            {
                _currentSettings = new PrivacySettings
                {
                    AllowGlobalDataSharing = PlayerPrefs.GetInt(PREFS_ALLOW_GLOBAL_SHARING, 1) == 1,
                    AllowLocationTracking = PlayerPrefs.GetInt(PREFS_ALLOW_LOCATION, 1) == 1,
                    AnonymizeData = PlayerPrefs.GetInt(PREFS_ANONYMIZE_DATA, 0) == 1,
                    ManuallySelectedRegion = PlayerPrefs.GetString(PREFS_MANUAL_REGION, ""),
                    UseManualRegionSelection = PlayerPrefs.GetInt(PREFS_USE_MANUAL_REGION, 0) == 1,
                    HasShownInitialConsent = PlayerPrefs.GetInt(PREFS_SHOWN_CONSENT, 0) == 1
                };
                
                MyLogger.Log($"📱 Настройки конфиденциальности загружены: GlobalSharing={_currentSettings.AllowGlobalDataSharing}, Location={_currentSettings.AllowLocationTracking}, ManualRegion={_currentSettings.ManuallySelectedRegion}", MyLogger.LogCategory.Regional);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка загрузки настроек: {ex.Message}", MyLogger.LogCategory.Regional);
                _currentSettings = new PrivacySettings();
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void InitializeAvailableRegions()
        {
            _availableRegions = new List<RegionOption>
            {
                // Автоматический режим
                new RegionOption("auto", "🤖 Автоматически", "Определение по GPS", true),
                
                // Минск (столица)
                new RegionOption("minsk_center", "🏛️ Минск - Центр", "Центральная часть Минска"),
                new RegionOption("minsk_north", "🏘️ Минск - Север", "Северные районы Минска"),
                new RegionOption("minsk_south", "🏢 Минск - Юг", "Южные районы Минска"),
                new RegionOption("minsk_east", "🌅 Минск - Восток", "Восточные районы Минска"),
                new RegionOption("minsk_west", "🌇 Минск - Запад", "Западные районы Минска"),
                
                // Брестская область
                new RegionOption("brest", "🏰 Брест", "Город Брест"),
                new RegionOption("baranovichi", "🌾 Барановичи", "Город Барановичи"),
                new RegionOption("pinsk", "🌲 Пинск", "Город Пинск"),
                new RegionOption("brest_region", "🏞️ Брестская область", "Другие населенные пункты Брестской области"),
                
                // Витебская область  
                new RegionOption("vitebsk", "🏛️ Витебск", "Город Витебск"),
                new RegionOption("polotsk", "🏔️ Полоцк", "Город Полоцк"),
                new RegionOption("orsha", "🌲 Орша", "Город Орша"),
                new RegionOption("vitebsk_region", "🍃 Витебская область", "Другие населенные пункты Витебской области"),
                
                // Гомельская область
                new RegionOption("gomel", "🏭 Гомель", "Город Гомель"),
                new RegionOption("mozyr", "⚡ Мозырь", "Город Мозырь"),
                new RegionOption("rechitsa", "🌾 Речица", "Город Речица"),
                new RegionOption("gomel_region", "🌻 Гомельская область", "Другие населенные пункты Гомельской области"),
                
                // Гродненская область
                new RegionOption("grodno", "🏰 Гродно", "Город Гродно"),
                new RegionOption("lida", "🌸 Лида", "Город Лида"),
                new RegionOption("slonim", "🌳 Слоним", "Город Слоним"),
                new RegionOption("grodno_region", "🏞️ Гродненская область", "Другие населенные пункты Гродненской области"),
                
                // Минская область
                new RegionOption("borisov", "🏭 Борисов", "Город Борисов"),
                new RegionOption("soligorsk", "⚙️ Солигорск", "Город Солигорск"),
                new RegionOption("molodechno", "🌾 Молодечно", "Город Молодечно"),
                new RegionOption("minsk_region", "🌿 Минская область", "Другие населенные пункты Минской области"),
                
                // Могилевская область
                new RegionOption("mogilev", "🏛️ Могилев", "Город Могилев"),
                new RegionOption("bobruisk", "🏭 Бобруйск", "Город Бобруйск"),
                new RegionOption("krichev", "🌾 Кричев", "Город Кричев"),
                new RegionOption("mogilev_region", "🌻 Могилевская область", "Другие населенные пункты Могилевской области"),
                
                // Соседние страны и международные варианты
                new RegionOption("other_belarus", "🇧🇾 Другой регион Беларуси", "Другие населенные пункты Беларуси"),
                new RegionOption("russia", "🇷🇺 Россия", "Российская Федерация"),
                new RegionOption("ukraine", "🇺🇦 Украина", "Украина"),
                new RegionOption("lithuania", "🇱🇹 Литва", "Литва"),
                new RegionOption("latvia", "🇱🇻 Латвия", "Латвия"),
                new RegionOption("estonia", "🇪🇪 Эстония", "Эстония"),
                new RegionOption("poland", "🇵🇱 Польша", "Польша"),
                new RegionOption("international", "🌍 Другая страна", "За пределами указанных стран"),
                new RegionOption("prefer_not_say", "🤐 Предпочитаю не указывать", "Не указывать регион")
            };
        }
        
        #endregion
    }
} 