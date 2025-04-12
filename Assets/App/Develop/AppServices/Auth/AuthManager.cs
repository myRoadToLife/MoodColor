using Firebase.Auth;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using App.Develop.AppServices.Firebase;
using App.Develop.AuthScene;

namespace App.Develop.AppServices.Auth
{
    public class AuthManager : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_InputField _emailInput;
        [SerializeField] private TMP_InputField _passwordInput;
        [SerializeField] private TMP_Text _popupMessageText;
        [SerializeField] private GameObject _popupPanel;

        private FirebaseAuth _auth;
        private SceneSwitcher _sceneSwitcher;
        private FirestoreManager _firestoreManager;
        private DIContainer _container;

        private void Start()
        {
            _auth = FirebaseAuth.DefaultInstance;
            _popupPanel.SetActive(false);

            _container = FindFirstObjectByType<DIContainerHolder>()?.Container;

            if (_container == null)
            {
                Debug.LogError("DIContainerHolder не найден в сцене.");
                return;
            }

            _sceneSwitcher = _container.Resolve<SceneSwitcher>();
            _firestoreManager = _container.Resolve<FirestoreManager>();
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

                FirebaseUser newUser = task.Result.User;
                ShowPopup("Регистрация успешна!");

                _firestoreManager.CreateNewUserDocument(
                    newUser.UserId,
                    newUser.Email,
                    onSuccess: GoToPersonalArea,
                    onFailure: error => ShowPopup("Ошибка Firestore: " + error)
                );
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
                GoToPersonalArea();
            });
        }

        private void GoToPersonalArea()
        {
            _container.Resolve<SceneSwitcher>()
                .ProcessSwitchSceneFor(new OutputAuthSceneArgs(new PersonalAreaInputArgs()));
        }

        private void ShowPopup(string message)
        {
            _popupPanel.SetActive(true);
            _popupMessageText.text = message;

            CancelInvoke(nameof(HidePopup));
            Invoke(nameof(HidePopup), 3f);
        }

        private void HidePopup()
        {
            _popupPanel.SetActive(false);
        }
    }
}
