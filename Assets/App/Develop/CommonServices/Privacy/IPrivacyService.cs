using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace App.Develop.CommonServices.Privacy
{
    /// <summary>
    /// Интерфейс сервиса конфиденциальности для управления согласием пользователя
    /// </summary>
    public interface IPrivacyService
    {
        /// <summary>
        /// Разрешено ли отправлять данные в глобальную статистику
        /// </summary>
        bool AllowGlobalDataSharing { get; set; }
        
        /// <summary>
        /// Разрешено ли использовать геолокацию
        /// </summary>
        bool AllowLocationTracking { get; set; }
        
        /// <summary>
        /// Анонимизировать ли данные перед отправкой
        /// </summary>
        bool AnonymizeData { get; set; }
        
        /// <summary>
        /// Вручную установленный регион пользователя (приоритет над GPS)
        /// </summary>
        string ManuallySelectedRegion { get; set; }
        
        /// <summary>
        /// Использовать ли ручной выбор региона вместо GPS
        /// </summary>
        bool UseManualRegionSelection { get; set; }
        
        /// <summary>
        /// Показано ли уже согласие при первом запуске
        /// </summary>
        bool HasShownInitialConsent { get; set; }
        
        /// <summary>
        /// Событие изменения настроек конфиденциальности
        /// </summary>
        event Action<PrivacySettings> OnPrivacySettingsChanged;
        
        /// <summary>
        /// Запросить согласие пользователя на сбор данных
        /// </summary>
        /// <returns>True, если пользователь дал согласие</returns>
        Task<bool> RequestDataCollectionConsent();
        
        /// <summary>
        /// Отозвать все согласия пользователя
        /// </summary>
        void RevokeAllConsents();
        
        /// <summary>
        /// Получить текущие настройки конфиденциальности
        /// </summary>
        PrivacySettings GetCurrentSettings();
        
        /// <summary>
        /// Применить настройки конфиденциальности
        /// </summary>
        void ApplySettings(PrivacySettings settings);
        
        /// <summary>
        /// Получить список доступных регионов для выбора
        /// </summary>
        List<RegionOption> GetAvailableRegions();
        
        /// <summary>
        /// Определить, должны ли данные пользователя попадать в глобальную статистику
        /// </summary>
        bool ShouldContributeToGlobalStats();
        
        /// <summary>
        /// Получить эффективный RegionId с учетом настроек пользователя
        /// </summary>
        /// <param name="gpsRegionId">RegionId, определенный по GPS</param>
        /// <returns>Итоговый RegionId для использования</returns>
        string GetEffectiveRegionId(string gpsRegionId);
        
        /// <summary>
        /// Сохранить настройки
        /// </summary>
        void SaveSettings();
        
        /// <summary>
        /// Загрузить настройки
        /// </summary>
        void LoadSettings();
    }
    
    /// <summary>
    /// Настройки конфиденциальности пользователя
    /// </summary>
    [Serializable]
    public class PrivacySettings
    {
        public bool AllowGlobalDataSharing { get; set; } = true;
        public bool AllowLocationTracking { get; set; } = true;
        public bool AnonymizeData { get; set; } = false;
        public string ManuallySelectedRegion { get; set; } = "";
        public bool UseManualRegionSelection { get; set; } = false;
        public bool HasShownInitialConsent { get; set; } = false;
        
        public PrivacySettings Clone()
        {
            return new PrivacySettings
            {
                AllowGlobalDataSharing = AllowGlobalDataSharing,
                AllowLocationTracking = AllowLocationTracking,
                AnonymizeData = AnonymizeData,
                ManuallySelectedRegion = ManuallySelectedRegion,
                UseManualRegionSelection = UseManualRegionSelection,
                HasShownInitialConsent = HasShownInitialConsent
            };
        }
    }
    
    /// <summary>
    /// Вариант региона для выбора пользователем
    /// </summary>
    [Serializable]
    public class RegionOption
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public bool IsDefault { get; set; }
        
        public RegionOption() { }
        
        public RegionOption(string id, string displayName, string description = "", bool isDefault = false)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            IsDefault = isDefault;
        }
        
        public override string ToString()
        {
            return DisplayName;
        }
    }
} 