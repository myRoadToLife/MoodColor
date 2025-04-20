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

        public void Inject(DIContainer container)
        {
            _sceneSwitcher = container.Resolve<SceneSwitcher>();
            _auth = FirebaseAuth.DefaultInstance;
            _db = FirebaseFirestore.DefaultInstance;
        }

        public void OnContinueProfile()
        {
            string nickname = _nicknameInput.text.Trim();
            string gender = _genderDropdown.options[_genderDropdown.value].text.ToLower();

            if (!IsValidNickname(nickname))
            {
                ShowPopup("Никнейм должен содержать только латинские буквы без пробелов");
                return;
            }

            SaveProfileAndGo(nickname, gender);
        }

        public void OnSkipProfile()
        {
            GoToPersonalArea();
        }

        private bool IsValidNickname(string nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname)) return false;

            foreach (char c in nickname)
            {
                if (!char.IsLetter(c) || c > 127) // латиница, без юникода/кириллицы
                    return false;
            }

            return true;
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
