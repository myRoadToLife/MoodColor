using System;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using App.Develop.Scenes.PersonalAreaScene.Settings;
using UnityEngine;

namespace App.Develop.Scenes.PersonalAreaScene
{
    public class PersonalAreaManager : MonoBehaviour
    {
        [SerializeField] private PersonalAreaUIController _ui;

        private PersonalAreaService _service;
        private SceneSwitcher _sceneSwitcher;
        private MonoFactory _factory;
        private IInjectable _injectableImplementation;

        public void Inject(DIContainer container, MonoFactory factory)
        {
            _service = container.Resolve<PersonalAreaService>();
            _sceneSwitcher = container.Resolve<SceneSwitcher>();
            _factory = factory;

            InitializeUI();
        }

        private void InitializeUI()
        {
            _ui.Initialize();

            _ui.SetUsername("Username"); // TODO: подгрузить из профиля
            _ui.SetCurrentEmotion(null); // TODO: или передать дефолтный Sprite

            // Подписка на эмоции
            foreach (EmotionTypes type in Enum.GetValues(typeof(EmotionTypes)))
            {
                var variable = _service.GetEmotionVariable(type);
                _ui.SetJar(type, variable.Value.Value);

                variable.Changed += (_, newData) =>
                    _ui.SetJar(type, newData.Value);
            }

            _ui.SetPoints(0); // TODO: заменить реальными данными
            _ui.SetEntries(0); // TODO: заменить реальными данными

            // Кнопки
            _ui.OnLogEmotion += () => Debug.Log("📝 Логируем эмоцию");
            _ui.OnOpenHistory += () => Debug.Log("📜 История");
            _ui.OnOpenFriends += () => Debug.Log("👥 Друзья");
            _ui.OnOpenWorkshop += () => Debug.Log("🛠️ Мастерская");
            _ui.OnOpenSettings += ShowSettingsPanel;
        }

        private GameObject _settingsPanelInstance;

        private void ShowSettingsPanel()
        {
            // 🔁 Если панель уже открыта — просто скрываем
            if (_settingsPanelInstance != null)
            {
                bool isActive = _settingsPanelInstance.activeSelf;
                _settingsPanelInstance.SetActive(!isActive);
                Debug.Log(isActive ? "🔽 Панель настроек скрыта" : "🔼 Панель настроек показана");
                return;
            }

            Debug.Log("⚙️ Открываем панель настроек");

            var prefab = Resources.Load<GameObject>("UI/DeletionAccountPanel");

            if (prefab == null)
            {
                Debug.LogError("❌ Префаб SettingsPanel не найден в Resources/UI/DeletionAccountPanel");
                return;
            }

            _settingsPanelInstance = Instantiate(prefab);

            var deletionManager = _settingsPanelInstance.GetComponentInChildren<AccountDeletionManager>();

            if (deletionManager == null)
            {
                Debug.LogError("❌ AccountDeletionManager не найден на инстанцированном префабе");
                return;
            }

            _factory.CreateOn<AccountDeletionManager>(deletionManager.gameObject);
        }
    }
}
