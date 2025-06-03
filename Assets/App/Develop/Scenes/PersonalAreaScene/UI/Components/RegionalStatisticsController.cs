using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using App.App.Develop.Scenes.PersonalAreaScene.UI.Components;
using App.Develop.CommonServices.Regional;
using App.Develop.DI;
using App.Develop.Scenes.PersonalAreaScene.Settings;
using App.Develop.Utils.Logging;
using UnityEngine;

namespace App.App.Develop.Scenes.PersonalAreaScene.UI.Components
{
    /// <summary>
    /// Контроллер для управления региональной статистикой в интерфейсе
    /// </summary>
    public class RegionalStatisticsController : MonoBehaviour, IInjectable
    {
        #region SerializeFields
        [Header("UI Components")]
        [SerializeField] private StatisticsView _statisticsView;
        [SerializeField] private GameObject _loadingIndicator;
        #endregion

        #region Private Fields
        private IRegionalStatsService _regionalStatsService;
        private ISettingsManager _settingsManager;
        private bool _isInitialized;
        private string _currentSelectedRegion;
        #endregion

        #region Events
        public event Action<string> OnRegionChanged;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            if (_isInitialized)
            {
                LoadRegionalData();
            }
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }
        #endregion

        #region IInjectable Implementation
        public void Inject(DIContainer container)
        {
            try
            {
                _regionalStatsService = container.Resolve<IRegionalStatsService>();
                _settingsManager = container.Resolve<ISettingsManager>();

                Initialize();
                _isInitialized = true;

                MyLogger.Log("✅ RegionalStatisticsController успешно инициализирован", MyLogger.LogCategory.UI);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка инициализации RegionalStatisticsController: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Обновляет отображаемую региональную статистику
        /// </summary>
        public async void RefreshRegionalStats()
        {
            if (!_isInitialized) return;

            await LoadRegionalData();
        }

        /// <summary>
        /// Показывает статистику для конкретного региона
        /// </summary>
        /// <param name="regionName">Название региона</param>
        public async void ShowRegionStats(string regionName)
        {
            if (string.IsNullOrEmpty(regionName) || !_isInitialized) return;

            _currentSelectedRegion = regionName;
            await LoadSpecificRegionData(regionName);
        }

        /// <summary>
        /// Показывает статистику всех регионов
        /// </summary>
        public async void ShowAllRegionsStats()
        {
            if (!_isInitialized) return;

            _currentSelectedRegion = null;
            await LoadAllRegionsData();
        }
        #endregion

        #region Private Methods
        private void Initialize()
        {
            if (_statisticsView == null)
            {
                MyLogger.LogWarning("StatisticsView не назначен в инспекторе", MyLogger.LogCategory.UI);
            }

            // Получаем текущий выбранный регион из настроек
            var settings = _settingsManager?.GetCurrentSettings();
            _currentSelectedRegion = settings?.selectedRegion;
        }

        private void SubscribeToEvents()
        {
            if (_settingsManager != null)
            {
                _settingsManager.OnSettingsChanged += OnSettingsChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_settingsManager != null)
            {
                _settingsManager.OnSettingsChanged -= OnSettingsChanged;
            }
        }

        private void OnSettingsChanged(SettingsData settings)
        {
            if (settings.selectedRegion != _currentSelectedRegion)
            {
                _currentSelectedRegion = settings.selectedRegion;
                LoadRegionalData();
                OnRegionChanged?.Invoke(_currentSelectedRegion);
            }
        }

        private async Task LoadRegionalData()
        {
            if (string.IsNullOrEmpty(_currentSelectedRegion))
            {
                await LoadAllRegionsData();
            }
            else
            {
                await LoadSpecificRegionData(_currentSelectedRegion);
            }
        }

        private async Task LoadSpecificRegionData(string regionName)
        {
            try
            {
                ShowLoadingIndicator(true);

                var regionStats = await _regionalStatsService.GetRegionalStats(regionName);

                if (regionStats != null)
                {
                    var statsDict = new Dictionary<string, RegionalEmotionStats>
                    {
                        { regionName, regionStats }
                    };

                    _statisticsView?.SetRegionalStats(statsDict);
                    MyLogger.Log($"Загружена статистика для региона: {regionName}", MyLogger.LogCategory.UI);
                }
                else
                {
                    MyLogger.LogWarning($"Не удалось загрузить статистику для региона: {regionName}", MyLogger.LogCategory.UI);
                    _statisticsView?.SetRegionalStats(new Dictionary<string, RegionalEmotionStats>());
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка загрузки статистики региона {regionName}: {ex.Message}", MyLogger.LogCategory.UI);
                _statisticsView?.SetRegionalStats(new Dictionary<string, RegionalEmotionStats>());
            }
            finally
            {
                ShowLoadingIndicator(false);
            }
        }

        private async Task LoadAllRegionsData()
        {
            try
            {
                ShowLoadingIndicator(true);

                var allStats = await _regionalStatsService.GetAllRegionalStats();

                if (allStats != null && allStats.Count > 0)
                {
                    _statisticsView?.SetRegionalStats(allStats);
                    MyLogger.Log($"Загружена статистика для {allStats.Count} регионов", MyLogger.LogCategory.UI);
                }
                else
                {
                    MyLogger.LogWarning("Не удалось загрузить региональную статистику", MyLogger.LogCategory.UI);
                    _statisticsView?.SetRegionalStats(new Dictionary<string, RegionalEmotionStats>());
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка загрузки региональной статистики: {ex.Message}", MyLogger.LogCategory.UI);
                _statisticsView?.SetRegionalStats(new Dictionary<string, RegionalEmotionStats>());
            }
            finally
            {
                ShowLoadingIndicator(false);
            }
        }

        private void ShowLoadingIndicator(bool show)
        {
            if (_loadingIndicator != null)
            {
                _loadingIndicator.SetActive(show);
            }
        }
        #endregion
    }
}