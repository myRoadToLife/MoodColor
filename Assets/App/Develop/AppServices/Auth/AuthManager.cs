using App.Develop.AppServices.Auth.UI;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;

namespace App.Develop.AppServices.Auth
{
    public class AuthManager : MonoBehaviour, IInjectable
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_InputField _emailInput;
        [SerializeField] private TMP_InputField _passwordInput;
        [SerializeField] private Toggle _rememberMeToggle;

        [Header("Animators")]
        [SerializeField] private UIAnimator _authPanelAnimator;
        [SerializeField] private UIAnimator _emailVerificationAnimator;
        [SerializeField] private UIAnimator _profilePanelAnimator;

        [Header("Popup")]
        [SerializeField] private GameObject _popupPanel;
        [SerializeField] private TMP_Text _popupText;

        private SceneSwitcher _sceneSwitcher;
        private FirebaseAuth _auth;

        public void Inject(DIContainer container)
        {
            _sceneSwitcher = container.Resolve<SceneSwitcher>();
            _auth = FirebaseAuth.DefaultInstance;

            SecurePlayerPrefs.Init("UltraSecretKey!🔥");
            Debug.Log("🔐 SecurePlayerPrefs инициализирован");

            _authPanelAnimator.Show();
            _emailVerificationAnimator.Hide();
            _profilePanelAnimator.Hide();
            _popupPanel.SetActive(false);

            LoadSavedCredentials();
        }

        public void RegisterUser()
        {
            string email = _emailInput.text.Trim();
            string password = _passwordInput.text.Trim();

            if (!IsValidEmail(email))
            {
                ShowPopup("Введите корректный email");
                return;
            }

            if (!IsValidPassword(password))
            {
                ShowPopup("Пароль должен содержать 8–12 символов, цифры, строчные и заглавные буквы");
                return;
            }

            _auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    ShowPopup("Ошибка регистрации: " + task.Exception?.Flatten().InnerException?.Message);
                    return;
                }

                SaveCredentials(email, password);
                SendEmailVerification();

                ShowPopup("Регистрация успешна! Подтвердите email.");
                _authPanelAnimator.Hide();
                _emailVerificationAnimator.Show();
            });
        }

        public void LoginUser()
        {
            string email = _emailInput.text.Trim();
            string password = _passwordInput.text.Trim();

            if (!IsValidEmail(email))
            {
                ShowPopup("Введите корректный email");
                return;
            }

            _auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    ShowPopup("Ошибка входа: " + task.Exception?.Flatten().InnerException?.Message);
                    return;
                }

                SaveCredentials(email, password);

                var user = _auth.CurrentUser;

                if (!user.IsEmailVerified)
                {
                    ShowPopup("Подтвердите email перед входом. Письмо отправлено повторно.");
                    SendEmailVerification();
                    _authPanelAnimator.Hide();
                    _emailVerificationAnimator.Show();
                    return;
                }

                CheckUserProfileFilled(user.UserId);
            });
        }

        public void OnCheckEmailVerified()
        {
            _auth.CurrentUser.ReloadAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully && _auth.CurrentUser.IsEmailVerified)
                {
                    ShowPopup("Email подтверждён!");
                    _emailVerificationAnimator.Hide();
                    _profilePanelAnimator.Show();
                }
                else
                {
                    ShowPopup("Email пока не подтверждён.");
                }
            });
        }

        private void CheckUserProfileFilled(string uid)
        {
            FirebaseFirestore.DefaultInstance
                .Collection("users")
                .Document(uid)
                .GetSnapshotAsync().ContinueWithOnMainThread(task =>
                {
                    if (!task.Result.Exists ||
                        !task.Result.ContainsField("nickname") ||
                        !task.Result.ContainsField("gender"))
                    {
                        ShowPopup("Пожалуйста, заполните профиль.");
                        _authPanelAnimator.Hide();
                        _profilePanelAnimator.Show();
                    }
                    else
                    {
                        ShowPopup("Вход выполнен!");
                        _sceneSwitcher.ProcessSwitchSceneFor(new OutputAuthSceneArgs(new PersonalAreaInputArgs()));
                    }
                });
        }

        public void SendEmailVerification()
        {
            var user = _auth.CurrentUser;
            if (user == null) return;

            user.SendEmailVerificationAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log("📨 Email подтверждение отправлено.");
                    ShowPopup("Письмо отправлено. Проверьте почту.");
                }
                else
                {
                    Debug.LogError("❌ Ошибка отправки письма: " + task.Exception?.Message);
                    ShowPopup("Ошибка при отправке письма.");
                }
            });
        }

        private void SaveCredentials(string email, string password)
        {
            SecurePlayerPrefs.SetString("email", email);
            Debug.Log($"💾 Сохраняем email: {email}");

            if (_rememberMeToggle != null && _rememberMeToggle.isOn)
            {
                SecurePlayerPrefs.SetString("password", password);
                SecurePlayerPrefs.SetInt("remember_me", 1);
                Debug.Log("✅ Пароль сохранён (remember_me включён)");
            }
            else
            {
                SecurePlayerPrefs.DeleteKey("password");
                SecurePlayerPrefs.SetInt("remember_me", 0);
                Debug.Log("ℹ️ Пароль не сохранён (remember_me выключен)");
            }

            SecurePlayerPrefs.Save();
        }

        private void LoadSavedCredentials()
        {
            string savedEmail = SecurePlayerPrefs.GetString("email", "");
            string savedPassword = SecurePlayerPrefs.GetString("password", "");
            bool remember = SecurePlayerPrefs.GetInt("remember_me", 0) == 1;

            Debug.Log($"📥 Загрузка: email={savedEmail}, remember={remember}");

            _emailInput.text = savedEmail;
            _passwordInput.text = remember ? savedPassword : "";
            _rememberMeToggle.isOn = remember;
        }

        public void ClearStoredCredentials()
        {
            Debug.Log("🧹 Удаление сохранённых данных авторизации (email, password, remember_me)");
            SecurePlayerPrefs.DeleteKey("email");
            SecurePlayerPrefs.DeleteKey("password");
            SecurePlayerPrefs.DeleteKey("remember_me");
            SecurePlayerPrefs.Save();

            _emailInput.text = "";
            _passwordInput.text = "";
            if (_rememberMeToggle != null)
                _rememberMeToggle.isOn = false;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPassword(string password)
        {
            if (password.Length < 8 || password.Length > 12)
                return false;

            bool hasUpper = false, hasLower = false, hasDigit = false;

            foreach (char c in password)
            {
                if (char.IsUpper(c)) hasUpper = true;
                if (char.IsLower(c)) hasLower = true;
                if (char.IsDigit(c)) hasDigit = true;
            }

            return hasUpper && hasLower && hasDigit;
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
