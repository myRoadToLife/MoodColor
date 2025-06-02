using System;
using System.Threading.Tasks;
using App.Develop.CommonServices.UI;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.Scenes.PersonalAreaScene.Infrastructure;
using App.Develop.Scenes.PersonalAreaScene.Settings;
using App.Develop.Utils.Logging;

namespace App.Develop.Scenes.PersonalAreaScene.UI
{
    /// <summary>
    /// Presenter для управления логикой личного кабинета
    /// Отвечает за координацию между View и бизнес-логикой
    /// </summary>
    public class PersonalAreaPresenter
    {
        private readonly IPersonalAreaView _view;
        private readonly PanelManager _panelManager;
        private readonly IPersonalAreaService _service;

        public PersonalAreaPresenter(
            IPersonalAreaView view,
            PanelManager panelManager,
            IPersonalAreaService service)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _panelManager = panelManager ?? throw new ArgumentNullException(nameof(panelManager));
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Инициализирует presenter и подписывается на события view
        /// </summary>
        public void Initialize()
        {
            SubscribeToViewEvents();
            MyLogger.Log("✅ PersonalAreaPresenter инициализирован", MyLogger.LogCategory.UI);
        }

        /// <summary>
        /// Освобождает ресурсы и отписывается от событий
        /// </summary>
        public void Dispose()
        {
            UnsubscribeFromViewEvents();
            MyLogger.Log("🗑️ PersonalAreaPresenter освобожден", MyLogger.LogCategory.UI);
        }

        private void SubscribeToViewEvents()
        {
            _view.OnLogEmotionRequested += HandleLogEmotionAsync;
            _view.OnHistoryRequested += HandleHistoryAsync;
            _view.OnFriendsRequested += HandleFriendsAsync;
            _view.OnWorkshopRequested += HandleWorkshopAsync;
            _view.OnSettingsRequested += HandleSettingsAsync;
            _view.OnQuitRequested += HandleQuitAsync;
        }

        private void UnsubscribeFromViewEvents()
        {
            _view.OnLogEmotionRequested -= HandleLogEmotionAsync;
            _view.OnHistoryRequested -= HandleHistoryAsync;
            _view.OnFriendsRequested -= HandleFriendsAsync;
            _view.OnWorkshopRequested -= HandleWorkshopAsync;
            _view.OnSettingsRequested -= HandleSettingsAsync;
            _view.OnQuitRequested -= HandleQuitAsync;
        }

        private async Task HandleLogEmotionAsync()
        {
            try
            {
                bool success = await _panelManager.TogglePanelAsync<LogEmotionPanelController>(AssetAddresses.LogEmotionPanel);
                if (!success)
                {
                    MyLogger.LogError("Не удалось открыть панель записи эмоций", MyLogger.LogCategory.UI);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при открытии панели записи эмоций: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }

        private async Task HandleHistoryAsync()
        {
            try
            {
                bool success = await _panelManager.TogglePanelAsync<HistoryPanelController>(AssetAddresses.HistoryPanel);
                if (!success)
                {
                    MyLogger.LogError("Не удалось открыть панель истории", MyLogger.LogCategory.UI);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при открытии панели истории: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }

        private async Task HandleFriendsAsync()
        {
            try
            {
                bool success = await _panelManager.TogglePanelAsync<FriendsPanelController>(AssetAddresses.FriendsPanel);
                if (!success)
                {
                    MyLogger.LogError("Не удалось открыть панель друзей", MyLogger.LogCategory.UI);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при открытии панели друзей: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }

        private async Task HandleWorkshopAsync()
        {
            try
            {
                bool success = await _panelManager.TogglePanelAsync<WorkshopPanelController>(AssetAddresses.WorkshopPanel);
                if (!success)
                {
                    MyLogger.LogError("Не удалось открыть панель мастерской", MyLogger.LogCategory.UI);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при открытии панели мастерской: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }

        private async Task HandleSettingsAsync()
        {
            try
            {
                bool success = await _panelManager.TogglePanelAsync<SettingsPanelController>(AssetAddresses.SettingsPanel);
                if (!success)
                {
                    MyLogger.LogError("Не удалось открыть панель настроек", MyLogger.LogCategory.UI);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при открытии панели настроек: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }

        private async Task HandleQuitAsync()
        {
            try
            {
                await _view.ShowQuitConfirmationAsync();
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"Ошибка при обработке выхода из приложения: {ex.Message}", MyLogger.LogCategory.UI);
            }
        }
    }
}