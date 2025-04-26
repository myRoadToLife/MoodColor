using System;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.CommonServices.UI;
using App.Develop.DI;
using App.Develop.Scenes.PersonalAreaScene.Settings;
using App.Develop.Scenes.PersonalAreaScene.UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace App.Develop.Scenes.PersonalAreaScene
{
    public class PersonalAreaManager : MonoBehaviour
    {
        private const string DEFAULT_USERNAME = "Username";
        private const string DeletionAccount_PANEL_PATH = "UI/DeletionAccountPanel";

        [SerializeField] private PersonalAreaUIController _ui;

        private IPersonalAreaService _service;
        private SceneSwitcher _sceneSwitcher;
        private MonoFactory _factory;
        private PanelManager _panelManager;

        public void Inject(DIContainer container, MonoFactory factory)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (_ui == null) throw new MissingComponentException("PersonalAreaUIController не назначен в инспекторе");

            _service = container.Resolve<IPersonalAreaService>();
            _sceneSwitcher = container.Resolve<SceneSwitcher>();
            _factory = factory;
            _panelManager = container.Resolve<PanelManager>();

            InitializeUI();
        }

        private void InitializeUI()
        {
            try
            {
                _ui.Initialize();
                SetupUserProfile();
                SetupEmotionJars();
                SetupStatistics();
                SetupButtons();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка при инициализации UI: {ex.Message}");
                throw;
            }
        }

        private void SetupButtons()
        {
            _ui.OnLogEmotion += () => Debug.Log("📝 Логируем эмоцию");
            _ui.OnOpenHistory += () => Debug.Log("📜 История");
            _ui.OnOpenFriends += () => Debug.Log("👥 Друзья");
            _ui.OnOpenWorkshop += () => Debug.Log("🛠️ Мастерская");
            _ui.OnOpenSettings += ShowSettingsPanel;
        }

        private void ShowSettingsPanel()
        {
            _panelManager.TogglePanel<AccountDeletionManager>(DeletionAccount_PANEL_PATH);
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

                if (variable == null)
                {
                    Debug.LogWarning($"Не удалось получить переменную для эмоции {type}");
                    continue;
                }

                _ui.SetJar(type, variable.Value.Value);
                variable.Changed += (_, newData) => _ui.SetJar(type, newData.Value);
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
                _ui.ClearAll();
            }

            // Отписываемся от событий
            _ui.OnLogEmotion -= () => Debug.Log("📝 Логируем эмоцию");
            _ui.OnOpenHistory -= () => Debug.Log("📜 История");
            _ui.OnOpenFriends -= () => Debug.Log("👥 Друзья");
            _ui.OnOpenWorkshop -= () => Debug.Log("🛠️ Мастерская");
            _ui.OnOpenSettings -= ShowSettingsPanel;
        }

        private void OnEnable()
        {
            // Подписываемся на событие выгрузки сцены
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            // Отписываемся от события выгрузки сцены
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            // Очищаем ссылки при выгрузке сцены
            _service = null;
        }
    }
}
