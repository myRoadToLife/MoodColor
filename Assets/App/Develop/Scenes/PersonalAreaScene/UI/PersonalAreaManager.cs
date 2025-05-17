using System;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.CommonServices.UI;
using App.Develop.DI;
using App.Develop.Scenes.PersonalAreaScene.Infrastructure;
using App.Develop.Scenes.PersonalAreaScene.Settings;
using Logger = App.Develop.Utils.Logging.Logger;
using UnityEngine;
using System.Threading.Tasks;
using App.Develop.CommonServices.GameSystem;

namespace App.Develop.Scenes.PersonalAreaScene.UI
{
    public class PersonalAreaManager : MonoBehaviour
    {
        private const string DEFAULT_USERNAME = "Username";
     
        [SerializeField] private PersonalAreaUIController _ui;

        private IPersonalAreaService _service;
        private SceneSwitcher _sceneSwitcher;
        private MonoFactory _factory;
        private PanelManager _panelManager;
        private IPointsService _pointsService;
        private bool _isInitialized;

        // Поля для хранения делегатов событий
        private Action _onLogEmotionHandler;
        private Action _onOpenHistoryHandler;
        private Action _onOpenFriendsHandler;
        private Action _onOpenWorkshopHandler;
        private Action _onOpenSettingsHandler;

        public void Inject(DIContainer container, MonoFactory factory)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (_ui == null) throw new ArgumentNullException(nameof(_ui), "PersonalAreaUIController не назначен в инспекторе");

            _service = container.Resolve<IPersonalAreaService>();
            _sceneSwitcher = container.Resolve<SceneSwitcher>();
            _factory = factory;
            _panelManager = container.Resolve<PanelManager>();
            _pointsService = container.Resolve<IPointsService>();

            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                _ui.Initialize();
                SetupUserProfile();
                SetupEmotionJars();
                SetupStatisticsAsync();
                SetupButtons();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при инициализации UI: {ex.Message}");
                throw;
            }
        }

        private void SetupButtons()
        {
            _onLogEmotionHandler = async () => await HandleLogEmotionAsync();
            _ui.OnLogEmotion += _onLogEmotionHandler;

            _onOpenHistoryHandler = async () => await HandleOpenHistoryAsync();
            _ui.OnOpenHistory += _onOpenHistoryHandler;

            _onOpenFriendsHandler = async () => await HandleOpenFriendsAsync();
            _ui.OnOpenFriends += _onOpenFriendsHandler;

            _onOpenWorkshopHandler = async () => await HandleOpenWorkshopAsync();
            _ui.OnOpenWorkshop += _onOpenWorkshopHandler;

            _onOpenSettingsHandler = async () => await ShowSettingsPanelAsync();
            _ui.OnOpenSettings += _onOpenSettingsHandler;
        }

        private async Task HandleLogEmotionAsync() 
        {
            await ShowLogEmotionPanelAsync();
        }
        
        private async Task HandleOpenHistoryAsync() 
        {
            await ShowHistoryPanelAsync();
        }
        
        private async Task HandleOpenFriendsAsync() 
        {
            await ShowFriendsPanelAsync();
        }
        
        private async Task HandleOpenWorkshopAsync() 
        {
            await ShowWorkshopPanelAsync();
        }

        private async Task ShowLogEmotionPanelAsync()
        {
            bool panelShown = await _panelManager.TogglePanelAsync<LogEmotionPanelController>(AssetAddresses.LogEmotionPanel);

            if (!panelShown)
            {
                Logger.LogError("❌ [PersonalAreaManager] Не удалось отобразить/скрыть панель логирования эмоций.");
            }
        }

        private async Task ShowHistoryPanelAsync()
        {
            bool panelShown = await _panelManager.TogglePanelAsync<HistoryPanelController>(AssetAddresses.HistoryPanel);

            if (!panelShown)
            {
                Logger.LogError("❌ [PersonalAreaManager] Не удалось отобразить/скрыть панель истории.");
            }
        }

        private async Task ShowFriendsPanelAsync()
        {
            bool panelShown = await _panelManager.TogglePanelAsync<FriendsPanelController>(AssetAddresses.FriendsPanel);

            if (!panelShown)
            {
                Logger.LogError("❌ [PersonalAreaManager] Не удалось отобразить/скрыть панель друзей.");
            }
        }

        private async Task ShowWorkshopPanelAsync()
        {
            bool panelShown = await _panelManager.TogglePanelAsync<WorkshopPanelController>(AssetAddresses.WorkshopPanel);

            if (!panelShown)
            {
                Logger.LogError("❌ [PersonalAreaManager] Не удалось отобразить/скрыть панель мастерской.");
            }
        }

        private async Task ShowSettingsPanelAsync()
        {
            Logger.Log($"[PersonalAreaManager instance {this.GetInstanceID()}] ShowSettingsPanelAsync called. PanelManager type: {_panelManager?.GetType().Name ?? "null"}");
            Logger.Log($"[PersonalAreaManager instance {this.GetInstanceID()}] Attempting to toggle settings panel.");

            bool panelIsNowVisible = await _panelManager.TogglePanelAsync<SettingsPanelController>(AssetAddresses.SettingsPanel);

            Logger.Log($"[PersonalAreaManager instance {this.GetInstanceID()}] ToggleSettingsPanelAsync completed. Panel is now {(panelIsNowVisible ? "visible" : "not visible")}.");
        }

        private void SetupUserProfile()
        {
            _ui.SetUsername(DEFAULT_USERNAME); // TODO: подгрузить из профиля
            _ui.SetCurrentEmotion(null); // TODO: или передать дефолтный Sprite
        }

        private void SetupEmotionJars()
        {
            foreach (EmotionTypes type in Enum.GetValues(typeof(EmotionTypes)))
            {
                var variable = _service.GetEmotionVariable(type);
                if (variable != null)
                {
                    _ui.SetJarFloat(type, variable.Value.Value);
                    variable.Changed += (_, newData) => _ui.SetJarFloat(type, newData.Value);
                }
            }
        }

        private void SetupStatistics()
        {
            // Этот метод устаревший, используется только для обратной совместимости
            SetupStatisticsAsync();
        }

        private async void SetupStatisticsAsync()
        {
            if (_pointsService == null)
            {
                Logger.LogWarning("[PersonalAreaManager] PointsService не внедрен. Статистика не будет обновлена.");
                _ui.SetPoints(0);
                _ui.SetEntries(0);
                return;
            }

            try
            {
                // Отображаем текущие данные сразу, чтобы избежать пустого UI
                // Даже если это будут нулевые значения до загрузки
                UpdateStatisticsView();
                
                // Подписываемся на обновления данных
                _pointsService.OnPointsChanged += HandlePointsChanged;
                
                // Загружаем актуальные данные из Firebase
                Logger.Log("[PersonalAreaManager] Загружаем данные статистики из Firebase...");
                await _pointsService.InitializeAsync();
                
                // После завершения загрузки из Firebase обновляем представление
                Logger.Log("[PersonalAreaManager] Данные статистики загружены из Firebase");
                UpdateStatisticsView();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[PersonalAreaManager] Ошибка загрузки данных из Firebase: {ex.Message}");
                // Если произошла ошибка, все равно отображаем UI с доступными данными
                UpdateStatisticsView();
            }
        }

        private void UpdateStatisticsView()
        {
            if (_ui == null || _pointsService == null) return;

            int currentPoints = _pointsService.CurrentPoints;
            int entriesCount = _pointsService.GetTransactionsHistory()?.Count ?? 0;

            _ui.SetPoints(currentPoints);
            _ui.SetEntries(entriesCount);
            Logger.Log($"[PersonalAreaManager] Статистика обновлена: Очки={currentPoints}, Записи={entriesCount}");
        }

        private void HandlePointsChanged(int newPointsValue)
        {
            UpdateStatisticsView();
        }

        private void OnDestroy()
        {
            Logger.Log($"[PersonalAreaManager instance {this.GetInstanceID()}] OnDestroy called.");
            if (_ui != null)
            {
                if (_onLogEmotionHandler != null) _ui.OnLogEmotion -= _onLogEmotionHandler;
                if (_onOpenHistoryHandler != null) _ui.OnOpenHistory -= _onOpenHistoryHandler;
                if (_onOpenFriendsHandler != null) _ui.OnOpenFriends -= _onOpenFriendsHandler;
                if (_onOpenWorkshopHandler != null) _ui.OnOpenWorkshop -= _onOpenWorkshopHandler;
                if (_onOpenSettingsHandler != null) _ui.OnOpenSettings -= _onOpenSettingsHandler;
            }

            if (_pointsService != null)
            {
                _pointsService.OnPointsChanged -= HandlePointsChanged;
            }
        }
    }
} 