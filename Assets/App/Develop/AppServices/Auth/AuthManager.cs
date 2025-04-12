using Firebase.Auth;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;

namespace App.Develop.AppServices.Auth
{
    public class AuthManager : MonoBehaviour, IInjectable
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_InputField _emailInput;

        [SerializeField] private TMP_InputField _passwordInput;
        [SerializeField] private GameObject _authPanel;
        [SerializeField] private GameObject _popupPanel;
        [SerializeField] private TMP_Text _popupText;
        [SerializeField] private GameObject _profilePanel;

        private SceneSwitcher _sceneSwitcher;
        private FirebaseAuth _auth;

        public void Inject(DIContainer container)
        {
            _sceneSwitcher = container.Resolve<SceneSwitcher>();
            _auth = FirebaseAuth.DefaultInstance;
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
                    return;
                }

                ShowPopup("Регистрация успешна!");

                _authPanel.SetActive(false);
                _profilePanel.SetActive(true);
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
                    return;
                }

                ShowPopup("Вход выполнен!");
                _sceneSwitcher.ProcessSwitchSceneFor(new OutputAuthSceneArgs(new PersonalAreaInputArgs()));
            });
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
