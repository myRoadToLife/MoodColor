using System.Collections;
using App.Develop.AppServices.Firebase.Common.SecureStorage;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
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
        private DatabaseReference _database;
        private string _plainPassword = "";

        public void Inject(DIContainer container)
        {
            _sceneSwitcher = container.Resolve<SceneSwitcher>();
            _auth = FirebaseAuth.DefaultInstance;
            _database = container.Resolve<DatabaseReference>();

            InitializeUI();
        }

        private void InitializeUI()
        {
            Debug.Log("✅ InitializeUI вызван");

            if (_logoutButton == null) Debug.LogError("❌ _logoutButton не установлен!");
            if (_showDeleteButton == null) Debug.LogError("❌ _showDeleteButton не установлен!");
            if (_confirmDeleteButton == null) Debug.LogError("❌ _confirmDeleteButton не установлен!");
            if (_passwordInput == null) Debug.LogError("❌ _passwordInput не установлен!");
            if (_popupPanel == null || _popupText == null) Debug.LogError("❌ Popup элементы не установлены!");

            _confirmDeletePanel.SetActive(false);

            SetupButtons();
            SetupToggles();
            SetupInputFields();

            SetPasswordVisibility(false);
        }

        private void SetupButtons()
        {
            _logoutButton.onClick.RemoveAllListeners();
            _logoutButton.onClick.AddListener(Logout);

            _showDeleteButton.onClick.RemoveAllListeners();
            _showDeleteButton.onClick.AddListener(ShowDeleteConfirmation);

            _cancelDeleteButton.onClick.RemoveAllListeners();
            _cancelDeleteButton.onClick.AddListener(CancelDelete);

            _confirmDeleteButton.onClick.RemoveAllListeners();
            _confirmDeleteButton.onClick.AddListener(ConfirmDelete);
        }

        private void SetupToggles()
        {
            _showPasswordToggle.onValueChanged.RemoveAllListeners();
            _showPasswordToggle.isOn = false;
            _showPasswordToggle.onValueChanged.AddListener(OnToggleShowPassword);
        }

        private void SetupInputFields()
        {
            _passwordInput.onValueChanged.RemoveAllListeners();
            _passwordInput.onValueChanged.AddListener(OnPasswordChanged);
            _passwordInput.contentType = TMP_InputField.ContentType.Password;
            _passwordInput.ForceLabelUpdate();
        }

        private void OnPasswordChanged(string newText)
        {
            Debug.Log($"⌨ Введённый пароль изменён: {newText}");
            _plainPassword = newText;
        }

        private void OnToggleShowPassword(bool isVisible)
        {
            Debug.Log($"🔁 Toggle пароль: {(isVisible ? "Показать" : "Скрыть")}");
            SetPasswordVisibility(isVisible);
        }

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
            Debug.Log("🔘 Logout нажата");

            _auth.SignOut();
            ShowPopup("Вы вышли из аккаунта.");

            _sceneSwitcher.ProcessSwitchSceneFor(
                new OutputPersonalAreaScreenArgs(new AuthSceneInputArgs()));
        }

        private void ShowDeleteConfirmation()
        {
            Debug.Log("🔘 Показать подтверждение удаления");
            _confirmDeletePanel.SetActive(true);
            SetPasswordVisibility(_showPasswordToggle.isOn);
        }

        private void CancelDelete()
        {
            Debug.Log("🔘 Отмена удаления");
            _confirmDeletePanel.SetActive(false);
        }

        private void ConfirmDelete()
        {
            Debug.Log("🔘 Подтвердить удаление");

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

            _database
                .Child("users")
                .Child(uid)
                .RemoveValueAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompletedSuccessfully)
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
                    }
                    else
                    {
                        ShowPopup("Ошибка при удалении данных пользователя.");
                        Debug.LogError(task.Exception);
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
