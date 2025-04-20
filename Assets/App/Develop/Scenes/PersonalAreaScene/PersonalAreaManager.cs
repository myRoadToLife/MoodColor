using System;
using App.Develop.CommonServices.Emotion;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using App.Develop.Scenes.PersonalAreaScene.Settings;
using UnityEngine;

namespace App.Develop.Scenes.PersonalAreaScene
{
    public class PersonalAreaManager : MonoBehaviour, IInjectable
    {
        [SerializeField] private PersonalAreaUIController _ui;

        private PersonalAreaService _service;
        private SceneSwitcher _sceneSwitcher;

        public void Inject(DIContainer container)
        {
            _service = container.Resolve<PersonalAreaService>();
            _sceneSwitcher = container.Resolve<SceneSwitcher>();

            _ui.Initialize();
            _ui.SetUsername("Username"); // TODO: Заменить на реальные данные
            _ui.SetCurrentEmotion(null); // Пока нет спрайта эмоции

            foreach (EmotionTypes type in Enum.GetValues(typeof(EmotionTypes)))
            {
                if (type == EmotionTypes.Disgust) continue; // ❌ временно исключаем

                var variable = _service.GetEmotionVariable(type);
                _ui.SetJar(type, variable.Value.Value);

                variable.Changed += (_, newData) => _ui.SetJar(type, newData.Value);
            }

            _ui.SetPoints(0);   // TODO: Заменить на реальные данные
            _ui.SetEntries(0);  // TODO: Заменить на реальные данные

            _ui.OnLogEmotion += () => Debug.Log("📝 Логируем эмоцию");
            _ui.OnOpenHistory += () => Debug.Log("📜 История");
            _ui.OnOpenFriends += () => Debug.Log("👥 Друзья");
            _ui.OnOpenSettings += ShowSettingsPanel;
            _ui.OnOpenWorkshop += () => Debug.Log("🛠️ Мастерская");
        }

        private void ShowSettingsPanel()
        {
            AccountDeletionManager settingsPrefab = Resources.Load<AccountDeletionManager>("UI/SettingsPanel");
            if (settingsPrefab != null)
            {
                Instantiate(settingsPrefab);
            }
            else
            {
                Debug.LogError("❌ SettingsPanel префаб не найден в Resources/UI/SettingsPanel");
            }
        }
    }
}
