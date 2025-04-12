using App.Develop.AuthScene;
using App.Develop.CommonServices.SceneManagement;
using Firebase.Auth;
using Firebase.Extensions;
using TMPro;
using UnityEngine;

namespace App.Develop.AppServices.Auth
{
    public class AuthManager : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_InputField _emailInput;

        [SerializeField] private TMP_InputField _passwordInput;
        [SerializeField] private TMP_Text _statusText;

        [SerializeField] private GameObject _popupMessagePanel;
        [SerializeField] private TMP_Text _popupMessageText;

        private FirebaseAuth _auth;
        private SceneSwitcher _sceneSwitcher;

        private void Start()
        {
            _auth = FirebaseAuth.DefaultInstance;
            _popupMessagePanel.SetActive(false);

            _sceneSwitcher = FindFirstObjectByType<DiContainerHolder>()?.Container.Resolve<SceneSwitcher>();

            if (_sceneSwitcher == null)
            {
                Debug.LogError("SceneSwitcher не найден через DIContainerHolder.");
            }
        }

        public void RegisterUser()
        {
            string email = _emailInput.text;
            string password = _passwordInput.text;

            _auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    ShowPopup("Ошибка регистрации: " + task.Exception?.Flatten().InnerException?.Message);
                }
                else
                {
                    FirebaseUser newUser = task.Result.User;
                    ShowPopup("Регистрация успешна!");
                    // тут можно добавить создание документа Firestore

                    GoToPersonalArea();
                }
            });
        }

        public void LoginUser()
        {
            string email = _emailInput.text;
            string password = _passwordInput.text;

            _auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    ShowPopup("Ошибка входа: " + task.Exception?.Flatten().InnerException?.Message);
                }
                else
                {
                    FirebaseUser user = task.Result.User;
                    ShowPopup("Вход успешен! Добро пожаловать 😊");

                    GoToPersonalArea();
                }
            });
        }

        private void ShowPopup(string message)
        {
            _popupMessagePanel.SetActive(true);
            _popupMessageText.text = message;

            CancelInvoke(nameof(HidePopup));
            Invoke(nameof(HidePopup), 2.5f);
        }

        private void HidePopup()
        {
            _popupMessagePanel.SetActive(false);
        }

        private void GoToPersonalArea()
        {
            if (_sceneSwitcher != null)
            {
                _sceneSwitcher.ProcessSwitchSceneFor(new OutputAuthSceneArgs(new PersonalAreaInputArgs()));
            }
            else
            {
                Debug.LogError("SceneSwitcher не установлен!");
            }
        }
    }
}
