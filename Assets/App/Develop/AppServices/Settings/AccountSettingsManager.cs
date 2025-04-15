using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using System.Collections;
using App.Develop.AppServices.Auth;

namespace App.Develop.AppServices.Settings
{
    public class AccountSettingsManager : MonoBehaviour, IInjectable
    {
        [Header("UI")]
        [SerializeField] private GameObject _popupPanel;

        [SerializeField] private TMP_Text _popupText;

        [Header("Delete Confirmation")]
        [SerializeField] private GameObject _confirmDeletePanel;

        [Header("Password Input")]
        [SerializeField] private TMP_InputField _passwordConfirmInput;

        [SerializeField] private Toggle _showPasswordToggle;

        private string _originalPassword = "";
        private SceneSwitcher _sceneSwitcher;
        private FirebaseAuth _auth;
        private FirebaseFirestore _db;

        public void Inject(DIContainer container)
        {
            _sceneSwitcher = container.Resolve<SceneSwitcher>();
            _auth = FirebaseAuth.DefaultInstance;
            _db = FirebaseFirestore.DefaultInstance;

            _confirmDeletePanel.SetActive(false);

            if (_showPasswordToggle != null)
            {
                _showPasswordToggle.isOn = false;
            }

            if (_passwordConfirmInput != null)
            {
                _passwordConfirmInput.onValueChanged.AddListener(OnPasswordInputChanged);
                _passwordConfirmInput.contentType = TMP_InputField.ContentType.Password;
                _passwordConfirmInput.ForceLabelUpdate();
            }

            SetPasswordVisibility(false);
        }

        public void OnPasswordInputChanged(string newText)
        {
            _originalPassword = newText;
            Debug.Log($"[DEBUG] OnPasswordInputChanged: новый ввод = '{newText}'");
        }

        public void OnToggleShowPassword(bool isVisible)
        {
            Debug.Log($"[DEBUG] OnToggleShowPassword: isVisible = {isVisible}. Текущий _originalPassword = '{_originalPassword}'");
            SetPasswordVisibility(isVisible);
        }

        private void SetPasswordVisibility(bool isVisible)
        {
            if (_passwordConfirmInput == null)
            {
                Debug.LogError("[DEBUG] _passwordConfirmInput == null!");
                return;
            }

            _passwordConfirmInput.DeactivateInputField();

            _passwordConfirmInput.contentType = isVisible
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.Password;

            StartCoroutine(DelayedRefresh());
        }

        private IEnumerator DelayedRefresh()
        {
            _passwordConfirmInput.text = "";
            _passwordConfirmInput.ForceLabelUpdate();
            Debug.Log("[DEBUG] DelayedRefresh: очистили текст.");

            yield return new WaitForEndOfFrame();

            _passwordConfirmInput.text = _originalPassword;
            _passwordConfirmInput.ForceLabelUpdate();
            Debug.Log($"[DEBUG] DelayedRefresh: восстановили текст '{_originalPassword}'");

            _passwordConfirmInput.caretPosition = _passwordConfirmInput.text.Length;
            _passwordConfirmInput.ActivateInputField();
        }

        public void Logout()
        {
            _auth.SignOut();
            ShowPopup("Вы вышли из аккаунта.");
            _sceneSwitcher.ProcessSwitchSceneFor(new OutputPersonalAreaScreenArgs(new AuthSceneInputArgs()));
        }

        public void ShowDeleteConfirmation()
        {
            _confirmDeletePanel.SetActive(true);
            SetPasswordVisibility(_showPasswordToggle != null && _showPasswordToggle.isOn);
        }

        public void CancelDelete()
        {
            _confirmDeletePanel.SetActive(false);
        }

        public void ConfirmDelete()
        {
            if (_passwordConfirmInput == null)
            {
                Debug.LogError("❌ _passwordConfirmInput не привязан!");
                return;
            }

            string password = _passwordConfirmInput.text.Trim();
            string email = _auth.CurrentUser?.Email;

            if (string.IsNullOrEmpty(password))
            {
                ShowPopup("Введите пароль для подтверждения.");
                return;
            }

            if (string.IsNullOrEmpty(email))
            {
                ShowPopup("Email не найден. Повторите вход.");
                return;
            }

            var credential = EmailAuthProvider.GetCredential(email, password);

            _auth.CurrentUser.ReauthenticateAsync(credential).ContinueWithOnMainThread(reAuthTask =>
            {
                if (!reAuthTask.IsCompletedSuccessfully)
                {
                    ShowPopup("Неверный пароль. Повторите попытку.");
                    Debug.LogError("❌ Reauth failed: " + reAuthTask.Exception?.Message);
                    return;
                }

                _db.Collection("users").Document(_auth.CurrentUser.UserId)
                    .DeleteAsync().ContinueWithOnMainThread(docTask =>
                    {
                        if (!docTask.IsCompletedSuccessfully)
                        {
                            Debug.LogWarning("⚠️ Firestore delete failed: " + docTask.Exception?.Message);
                        }

                        _auth.CurrentUser.DeleteAsync().ContinueWithOnMainThread(deleteTask =>
                        {
                            if (deleteTask.IsCompletedSuccessfully)
                            {
                                ShowPopup("Аккаунт удалён.");

                                // 🔐 Удаляем сохранённые логин/пароль
                                SecurePlayerPrefs.DeleteKey("email");
                                SecurePlayerPrefs.DeleteKey("password");
                                SecurePlayerPrefs.DeleteKey("remember_me");
                                SecurePlayerPrefs.Save();

                                _sceneSwitcher.ProcessSwitchSceneFor(
                                    new OutputPersonalAreaScreenArgs(new AuthSceneInputArgs()));
                            }
                            else
                            {
                                ShowPopup("Ошибка при удалении аккаунта.");
                                Debug.LogError("❌ Delete failed: " + deleteTask.Exception?.Message);
                            }
                        });
                    });
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
