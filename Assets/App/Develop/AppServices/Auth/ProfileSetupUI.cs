using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;

namespace App.Develop.AppServices.Auth
{
    public class ProfileSetupUI : MonoBehaviour, IInjectable
    {
        [Header("UI")]
        [SerializeField] private TMP_InputField _nicknameInput;

        [SerializeField] private TMP_Dropdown _genderDropdown;
        [SerializeField] private GameObject _popupPanel;
        [SerializeField] private TMP_Text _popupText;

        private FirebaseAuth _auth;
        private FirebaseFirestore _db;
        private SceneSwitcher _sceneSwitcher;

        private bool _initialized = false;

        public void Inject(DIContainer container)
        {
            _auth = FirebaseAuth.DefaultInstance;
            _db = FirebaseFirestore.DefaultInstance;
            _sceneSwitcher = container.Resolve<SceneSwitcher>();
            _initialized = true;

            Debug.Log("✅ ProfileSetupUI успешно инициализирован через Inject()");
        }

        public void OnContinueProfile()
        {
            if (!_initialized)
            {
                Debug.LogWarning("⚠️ ProfileSetupUI не инициализирован");
                return;
            }

            string nickname = _nicknameInput.text.Trim();
            string gender = _genderDropdown.options[_genderDropdown.value].text.ToLower();

            if (string.IsNullOrEmpty(nickname))
            {
                ShowPopup("Введите никнейм!");
                return;
            }

            SaveProfileAndGo(nickname, gender);
        }

        public void OnSkipProfile()
        {
            if (!_initialized)
            {
                Debug.LogWarning("⚠️ ProfileSetupUI не инициализирован");
                return;
            }

            GoToPersonalArea();
        }

        private void SaveProfileAndGo(string nickname, string gender)
        {
            var user = _auth.CurrentUser;

            if (user == null)
            {
                ShowPopup("Пользователь не найден.");
                return;
            }

            var docRef = _db.Collection("users").Document(user.UserId);

            var updates = new Dictionary<string, object>
            {
                { "nickname", nickname },
                { "gender", gender }
            };

            docRef.SetAsync(updates, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log("✅ Профиль успешно сохранён.");
                    GoToPersonalArea();
                }
                else
                {
                    ShowPopup("Ошибка сохранения профиля:\n" + task.Exception?.Message);
                }
            });
        }

        private void GoToPersonalArea()
        {
            _sceneSwitcher.ProcessSwitchSceneFor(new OutputAuthSceneArgs(new PersonalAreaInputArgs()));
        }

        private void ShowPopup(string message)
        {
            _popupPanel.SetActive(true);
            _popupText.text = message;

            CancelInvoke(nameof(HidePopup));
            Invoke(nameof(HidePopup), 3f);
        }

        private void HidePopup()
        {
            _popupPanel.SetActive(false);
        }
    }
}
