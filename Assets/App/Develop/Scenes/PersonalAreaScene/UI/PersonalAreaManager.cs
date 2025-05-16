using System;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.CommonServices.UI;
using App.Develop.DI;
using App.Develop.Scenes.PersonalAreaScene.Infrastructure;
using App.Develop.Scenes.PersonalAreaScene.Settings;
using App.Develop.Scenes.PersonalAreaScene.UI;
using UnityEngine;
using System.Threading.Tasks;

namespace App.Develop.Scenes.PersonalAreaScene.UI
{
    public class PersonalAreaManager : MonoBehaviour
    {
        private const string DEFAULT_USERNAME = "Username";
        private const string DeletionAccount_PANEL_PATH = "UI/DeletionAccountPanel";
        private const string PANEL_SETTINGS = "UI/SettingsPanel";

        [SerializeField] private PersonalAreaUIController _ui;

        private IPersonalAreaService _service;
        private SceneSwitcher _sceneSwitcher;
        private MonoFactory _factory;
        private PanelManager _panelManager;
        private bool _isInitialized;

        public void Inject(DIContainer container, MonoFactory factory)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (_ui == null) throw new ArgumentNullException(nameof(_ui), "PersonalAreaUIController не назначен в инспекторе");

            _service = container.Resolve<IPersonalAreaService>();
            _sceneSwitcher = container.Resolve<SceneSwitcher>();
            _factory = factory;
            _panelManager = container.Resolve<PanelManager>();

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
                SetupStatistics();
                SetupButtons();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при инициализации UI: {ex.Message}");
                throw;
            }
        }

        private void SetupButtons()
        {
            _ui.OnLogEmotion += async () => await HandleLogEmotionAsync();
            _ui.OnOpenHistory += async () => await HandleOpenHistoryAsync();
            _ui.OnOpenFriends += async () => await HandleOpenFriendsAsync();
            _ui.OnOpenWorkshop += async () => await HandleOpenWorkshopAsync();
            _ui.OnOpenSettings += async () => await ShowSettingsPanelAsync();
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
                Debug.LogError("❌ [PersonalAreaManager] Не удалось отобразить/скрыть панель логирования эмоций.");
            }
        }

        private async Task ShowHistoryPanelAsync()
        {
            bool panelShown = await _panelManager.TogglePanelAsync<HistoryPanelController>(AssetAddresses.HistoryPanel);

            if (!panelShown)
            {
                Debug.LogError("❌ [PersonalAreaManager] Не удалось отобразить/скрыть панель истории.");
            }
        }

        private async Task ShowFriendsPanelAsync()
        {
            bool panelShown = await _panelManager.TogglePanelAsync<FriendsPanelController>(AssetAddresses.FriendsPanel);

            if (!panelShown)
            {
                Debug.LogError("❌ [PersonalAreaManager] Не удалось отобразить/скрыть панель друзей.");
            }
        }

        private async Task ShowWorkshopPanelAsync()
        {
            bool panelShown = await _panelManager.TogglePanelAsync<WorkshopPanelController>(AssetAddresses.WorkshopPanel);

            if (!panelShown)
            {
                Debug.LogError("❌ [PersonalAreaManager] Не удалось отобразить/скрыть панель мастерской.");
            }
        }

        private async Task ShowSettingsPanelAsync()
        {
            bool panelShown = await _panelManager.TogglePanelAsync<SettingsPanelController>(AssetAddresses.SettingsPanel);

            if (!panelShown)
            {
                Debug.LogError("❌ [PersonalAreaManager] Не удалось отобразить/скрыть панель настроек.");
            }
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
            _ui.SetPoints(0); // TODO: заменить реальными данными
            _ui.SetEntries(0); // TODO: заменить реальными данными
        }

        private void OnDestroy()
        {
            if (_ui != null)
            {
                _ui.OnLogEmotion -= async () => await HandleLogEmotionAsync();
                _ui.OnOpenHistory -= async () => await HandleOpenHistoryAsync();
                _ui.OnOpenFriends -= async () => await HandleOpenFriendsAsync();
                _ui.OnOpenWorkshop -= async () => await HandleOpenWorkshopAsync();
                _ui.OnOpenSettings -= async () => await ShowSettingsPanelAsync();
            }
        }
    }
} 