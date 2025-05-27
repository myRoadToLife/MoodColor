using System;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.CommonServices.UI;
using App.Develop.DI;
using App.Develop.Scenes.PersonalAreaScene.Infrastructure;
using App.Develop.Scenes.PersonalAreaScene.Settings;
using UnityEngine;
using System.Threading.Tasks;
using App.Develop.CommonServices.GameSystem;
using App.Develop.CommonServices.Firebase.Database.Services;
using System.Collections;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.Utils.Logging;
using App.Develop.Utils.Reactive;
using App.Develop.CommonServices.DataManagement.DataProviders;

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
        private IDatabaseService _databaseService;
        private bool _isInitialized;

        // Поля для хранения делегатов событий
        private Action _onLogEmotionHandler;
        private Action _onOpenHistoryHandler;
        private Action _onOpenFriendsHandler;
        private Action _onOpenWorkshopHandler;
        private Action _onOpenSettingsHandler;
        private Action _onQuitApplicationHandler;

        public void Inject(DIContainer container, MonoFactory factory)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (_ui == null) throw new ArgumentNullException(nameof(_ui), "PersonalAreaUIController is not assigned in the inspector");

            _service = container.Resolve<IPersonalAreaService>();
            _sceneSwitcher = container.Resolve<SceneSwitcher>();
            _factory = factory;
            _panelManager = container.Resolve<PanelManager>();
            _pointsService = container.Resolve<IPointsService>();
            
            // Пытаемся получить DatabaseService для доступа к профилю пользователя
            try
            {
                _databaseService = container.Resolve<IDatabaseService>();
            }
            catch (Exception)
            {
                // LogWarning was here, removed. Nickname will not be loaded if service is unavailable.
            }

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
                throw new InvalidOperationException($"Error during UI initialization: {ex.Message}", ex);
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
            
            _onQuitApplicationHandler = HandleQuitApplication;
            _ui.OnQuitApplication += _onQuitApplicationHandler;
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
                throw new InvalidOperationException("[PersonalAreaManager] Failed to toggle LogEmotionPanel.");
            }
        }

        private async Task ShowHistoryPanelAsync()
        {
            bool panelShown = await _panelManager.TogglePanelAsync<HistoryPanelController>(AssetAddresses.HistoryPanel);

            if (!panelShown)
            {
                throw new InvalidOperationException("[PersonalAreaManager] Failed to toggle HistoryPanel.");
            }
        }

        private async Task ShowFriendsPanelAsync()
        {
            bool panelShown = await _panelManager.TogglePanelAsync<FriendsPanelController>(AssetAddresses.FriendsPanel);

            if (!panelShown)
            {
                throw new InvalidOperationException("[PersonalAreaManager] Failed to toggle FriendsPanel.");
            }
        }

        private async Task ShowWorkshopPanelAsync()
        {
            bool panelShown = await _panelManager.TogglePanelAsync<WorkshopPanelController>(AssetAddresses.WorkshopPanel);

            if (!panelShown)
            {
                throw new InvalidOperationException("[PersonalAreaManager] Failed to toggle WorkshopPanel.");
            }
        }

        private async Task ShowSettingsPanelAsync()
        {
            bool panelIsNowVisible = await _panelManager.TogglePanelAsync<SettingsPanelController>(AssetAddresses.SettingsPanel);
            // Original logs about visibility removed. If toggle fails, it might throw from PanelManager or return false.
            // Here, we assume if it doesn't throw, the operation itself was "successful" in attempting the toggle.
        }

        private void SetupUserProfile()
        {
            // Устанавливаем дефолтное имя, пока загружаем реальное
            _ui.SetUsername(DEFAULT_USERNAME);
            _ui.SetCurrentEmotion(null);
            
            // Асинхронно загружаем профиль
            LoadUserProfileAsync();
        }
        
        private async void LoadUserProfileAsync()
        {
            if (_databaseService == null)
            {
                return;
            }
            
            try
            {
                // Проверяем, авторизован ли пользователь
                if (!_databaseService.IsAuthenticated)
                {
                    return;
                }
                
                // Загружаем профиль пользователя
                UserProfile userProfile = await _databaseService.GetUserProfile();
                
                if (userProfile != null && !string.IsNullOrEmpty(userProfile.Nickname))
                {
                    // Устанавливаем никнейм пользователя
                    _ui.SetUsername(userProfile.Nickname);
                }
                else
                {
                    // LogWarning removed, default name remains.
                }
            }
            catch (Exception ex)
            {
                // LogError converted to throw. If profile loading is critical, this is appropriate.
                // If app can continue with default name, this should be a non-throwing log.
                // Forcing stricter error handling for now.
                throw new Exception($"Error loading user profile: {ex.Message}", ex);
            }
        }

        private void SetupEmotionJars()
        {
            foreach (EmotionTypes type in Enum.GetValues(typeof(EmotionTypes)))
            {
                IReadOnlyVariable<EmotionData> variable = _service.GetEmotionVariable(type);
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
                await _pointsService.InitializeAsync();
                
                // После завершения загрузки из Firebase обновляем представление
                UpdateStatisticsView();
            }
            catch (Exception ex)
            {
                // Если произошла ошибка, все равно отображаем UI с доступными данными
                UpdateStatisticsView();
                throw new Exception($"Error loading statistics data from Firebase: {ex.Message}", ex);
            }
        }

        private void UpdateStatisticsView()
        {
            if (_ui == null || _pointsService == null) return;

            int currentPoints = _pointsService.CurrentPoints;
            int entriesCount = _pointsService.GetTransactionsHistory()?.Count ?? 0;

            _ui.SetPoints(currentPoints);
            _ui.SetEntries(entriesCount);
        }

        private void HandlePointsChanged(int newPointsValue)
        {
            UpdateStatisticsView();
        }

        private void HandleQuitApplication()
        {
            StartCoroutine(ConfirmAndQuitApplication());
        }
        
        private IEnumerator ConfirmAndQuitApplication()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorUtility.DisplayDialog(
                "Закрытие приложения", 
                "Вы уверены, что хотите закрыть приложение?", 
                "Да", "Нет"))
            {
                UnityEditor.EditorApplication.isPlaying = false;
            }
#else
            Application.Quit();
#endif
            
            yield return null;
        }

        private void OnDestroy()
        {
            MyLogger.Log($"[PersonalAreaManager instance {this.GetInstanceID()}] OnDestroy called.", MyLogger.LogCategory.UI);
            if (_ui != null)
            {
                if (_onLogEmotionHandler != null) _ui.OnLogEmotion -= _onLogEmotionHandler;
                if (_onOpenHistoryHandler != null) _ui.OnOpenHistory -= _onOpenHistoryHandler;
                if (_onOpenFriendsHandler != null) _ui.OnOpenFriends -= _onOpenFriendsHandler;
                if (_onOpenWorkshopHandler != null) _ui.OnOpenWorkshop -= _onOpenWorkshopHandler;
                if (_onOpenSettingsHandler != null) _ui.OnOpenSettings -= _onOpenSettingsHandler;
                if (_onQuitApplicationHandler != null) _ui.OnQuitApplication -= _onQuitApplicationHandler;
            }

            if (_pointsService != null)
            {
                _pointsService.OnPointsChanged -= HandlePointsChanged;
            }
        }
    }
} 