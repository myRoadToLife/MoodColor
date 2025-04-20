using System.Collections;
using App.Develop.AppServices.Auth;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using App.Develop.Scenes.PersonalAreaScene.Extensions;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.Develop.Scenes.PersonalAreaScene.Settings
{
    public class AccountDeletionManager : MonoBehaviour, IInjectable
    {
        [Header("Panels")]
        [SerializeField] private GameObject _popupPanel;

        [SerializeField] private TMP_Text _popupText;
        [SerializeField] private GameObject _confirmDeletePanel;

        [Header("Controls")]
        [SerializeField] private Button _logoutButton;

        [SerializeField] private Button _showDeleteButton;
        [SerializeField] private Button _cancelDeleteButton;
        [SerializeField] private Button _confirmDeleteButton;
        [SerializeField] private Toggle _showPasswordToggle;
        [SerializeField] private TMP_InputField _passwordInput;

        private SceneSwitcher _sceneSwitcher;
        private FirebaseAuth _auth;
        private FirebaseFirestore _db;
        private string _plainPassword = "";

        public void Inject(DIContainer container)
        {
            // 1) Разрешаем зависимости
            _sceneSwitcher = container.Resolve<SceneSwitcher>();
            _auth = FirebaseAuth.DefaultInstance;
            _db = FirebaseFirestore.DefaultInstance;

            // 2) Подготовка UI
            InitializeUI();
        }

        private void InitializeUI()
        {
            // Скрываем подтверждение по-умолчанию
            _confirmDeletePanel.SetActive(false);

            // Кнопки
            _logoutButton.SetupButton(Logout);
            _showDeleteButton.SetupButton(ShowDeleteConfirmation);
            _cancelDeleteButton.SetupButton(() => _confirmDeletePanel.SetActive(false));
            _confirmDeleteButton.SetupButton(ConfirmDelete);

            // Toggle и поле ввода
            _showPasswordToggle.SetupToggle(OnToggleShowPassword, defaultState: false);
            _passwordInput.SetupPasswordField(OnPasswordChanged);

            // Первоначально пароль скрыт
            SetPasswordVisibility(false);
        }

        private void OnPasswordChanged(string newText) =>
            _plainPassword = newText;

        private void OnToggleShowPassword(bool isVisible) =>
            SetPasswordVisibility(isVisible);

        private void SetPasswordVisibility(bool isVisible)
        {
            _passwordInput.DeactivateInputField();

            _passwordInput.contentType = isVisible
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.Password;

            StartCoroutine(RefreshPasswordField());
        }

        private IEnumerator RefreshPasswordField()
        {
            _passwordInput.text = "";
            _passwordInput.ForceLabelUpdate();
            yield return new WaitForEndOfFrame();
            _passwordInput.text = _plainPassword;
            _passwordInput.ForceLabelUpdate();
            _passwordInput.caretPosition = _plainPassword.Length;
            _passwordInput.ActivateInputField();
        }

        private void Logout()
        {
            _auth.SignOut();
            ShowPopup("Вы вышли из аккаунта.");

            _sceneSwitcher.ProcessSwitchSceneFor(
                new OutputPersonalAreaScreenArgs(new AuthSceneInputArgs()));
        }

        private void ShowDeleteConfirmation()
        {
            _confirmDeletePanel.SetActive(true);
            // Обновляем видимость сразу же, если toggle включён
            SetPasswordVisibility(_showPasswordToggle.isOn);
        }

        private void ConfirmDelete()
        {
            if (string.IsNullOrWhiteSpace(_plainPassword))
            {
                ShowPopup("Введите пароль для подтверждения.");
                return;
            }

            var email = _auth.CurrentUser?.Email;

            if (string.IsNullOrEmpty(email))
            {
                ShowPopup("Email не найден. Повторите вход.");
                return;
            }

            var credential = EmailAuthProvider.GetCredential(email, _plainPassword);

            _auth.CurrentUser.ReauthenticateAsync(credential)
                .ContinueWithOnMainThread(task =>
                {
                    if (!task.IsCompletedSuccessfully)
                    {
                        ShowPopup("Неверный пароль. Попробуйте ещё раз.");
                        return;
                    }

                    DeleteUserData();
                });
        }

        private void DeleteUserData()
        {
            var uid = _auth.CurrentUser?.UserId;

            if (string.IsNullOrEmpty(uid))
            {
                ShowPopup("Ошибка: пользователь не авторизован.");
                return;
            }

            _db.Collection("users").Document(uid)
                .DeleteAsync().ContinueWithOnMainThread(_ =>
                {
                    if (_auth.CurrentUser != null)
                    {
                        DeleteFirebaseUser();
                    }
                    else
                    {
                        ShowPopup("Ошибка: пользователь не авторизован при удалении.");
                        Debug.LogError("FirebaseAuth.CurrentUser is null before DeleteFirebaseUser.");
                    }
                });
        }


        private void DeleteFirebaseUser()
        {
            _auth.CurrentUser.DeleteAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    ShowPopup("Аккаунт удалён.");
                    CleanupStoredCredentials();

                    _sceneSwitcher.ProcessSwitchSceneFor(
                        new OutputPersonalAreaScreenArgs(new AuthSceneInputArgs()));
                }
                else
                {
                    ShowPopup("Ошибка при удалении аккаунта.");
                    Debug.LogError(task.Exception);
                }
            });
        }

        private void CleanupStoredCredentials()
        {
            SecurePlayerPrefs.DeleteKey("email");
            SecurePlayerPrefs.DeleteKey("password");
            SecurePlayerPrefs.DeleteKey("remember_me");
            SecurePlayerPrefs.Save();
        }

        private void ShowPopup(string message)
        {
            if (_popupPanel == null || _popupText == null) return;
            _popupText.text = message;
            _popupPanel.SetActive(true);
            CancelInvoke(nameof(HidePopup));
            Invoke(nameof(HidePopup), 3f);
        }

        private void HidePopup() =>
            _popupPanel?.SetActive(false);
    }
}
