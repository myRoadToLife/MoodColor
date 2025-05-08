using System;
using App.Develop.CommonServices.AssetManagement;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.CommonServices.UI;
using App.Develop.DI;
using App.Develop.Scenes.PersonalAreaScene.Settings;
using App.Develop.Scenes.PersonalAreaScene.UI;
using UnityEngine;

namespace App.Develop.Scenes.PersonalAreaScene
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
            _ui.OnLogEmotion += HandleLogEmotion;
            _ui.OnOpenHistory += HandleOpenHistory;
            _ui.OnOpenFriends += HandleOpenFriends;
            _ui.OnOpenWorkshop += HandleOpenWorkshop;
            _ui.OnOpenSettings += ShowSettingsPanel;
        }

        private void HandleLogEmotion() => Debug.Log("📝 Логируем эмоцию");
        private void HandleOpenHistory() => Debug.Log("📜 История");
        private void HandleOpenFriends() => Debug.Log("👥 Друзья");
        private void HandleOpenWorkshop() => Debug.Log("🛠️ Мастерская");

        private void ShowSettingsPanel()
        {
            Debug.Log("🔄 [PersonalAreaManager] Показываем панель настроек...");
            
            try
            {
                if (_panelManager == null)
                {
                    Debug.LogError("❌ [PersonalAreaManager] PanelManager отсутствует!");
                    return;
                }
                
                Debug.Log($"🔄 [PersonalAreaManager] Переключаем панель по пути: {AssetPaths.PanelSettings}");
                
                bool panelShown = _panelManager.TogglePanel<SettingsPanelController>(AssetPaths.PanelSettings);
                
                Debug.Log(panelShown 
                    ? "✅ [PersonalAreaManager] Панель настроек успешно отображена" 
                    : "❌ [PersonalAreaManager] Не удалось отобразить панель настроек");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ [PersonalAreaManager] Ошибка при отображении панели настроек: {ex.Message}\n{ex.StackTrace}");
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
            if (!_isInitialized) return;

            if (_ui != null)
            {
                _ui.OnLogEmotion -= HandleLogEmotion;
                _ui.OnOpenHistory -= HandleOpenHistory;
                _ui.OnOpenFriends -= HandleOpenFriends;
                _ui.OnOpenWorkshop -= HandleOpenWorkshop;
                _ui.OnOpenSettings -= ShowSettingsPanel;
                _ui.ClearAll();
            }

            _isInitialized = false;
        }
    }
}
