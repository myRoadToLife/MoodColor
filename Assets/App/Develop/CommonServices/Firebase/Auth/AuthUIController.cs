using App.Develop.AppServices.Auth.UI;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.Develop.CommonServices.Firebase.Auth
{
    public class AuthUIController : MonoBehaviour
    {
        [Header("UI Elements - Common")]
        [SerializeField] private TMP_InputField _emailInput;
        [SerializeField] private TMP_InputField _passwordInput;
        [SerializeField] private Toggle _rememberMeToggle;
        [SerializeField] private TMP_Text _messageText; // For errors or info

        [Header("Login Panel")]
        [SerializeField] private GameObject _loginPanel;
        [SerializeField] private Button _loginButton;
        [SerializeField] private Button _googleSignInButton; // Google Sign-In button
        [SerializeField] private Button _switchToRegisterButton; // From login to register form
        [SerializeField] private Button _forgotPasswordButton;

        [Header("Register Panel")]
        [SerializeField] private GameObject _registerPanel;
        [SerializeField] private TMP_InputField _confirmPasswordInput;
        [SerializeField] private Button _registerButton;
        [SerializeField] private Button _switchToLoginButton; // From register to login form

        [Header("Reset Password Panel")]
        [SerializeField] private GameObject _resetPasswordPanel;
        [SerializeField] private TMP_InputField _resetPasswordEmailInput;
        [SerializeField] private Button _sendResetEmailButton;
        [SerializeField] private Button _backToLoginButton; // From reset password to login

        [Header("Animators")]
        [SerializeField] private UIAnimator _authPanelAnimator;
        [SerializeField] private UIAnimator _emailVerificationAnimator;
        [SerializeField] private UIAnimator _profilePanelAnimator;

        [Header("Popup")]
        [SerializeField] private GameObject _popupPanel;
        [SerializeField] private TMP_Text _popupText;

        // Событие для слабого связывания с ProfileSetupUI
        public event Action OnProfileSetupRequested;

        private IAuthManager _authManager;

        public void Initialize(IAuthManager authManager)
        {
            _authManager = authManager ?? throw new ArgumentNullException(nameof(authManager));
            
            // Инициализируем панели в безопасном режиме 
            // (без анимации и без попыток запуска корутин на неактивных объектах)
            if (_authPanelAnimator != null)
                _authPanelAnimator.SetVisibleState();
                
            if (_emailVerificationAnimator != null)
                _emailVerificationAnimator.SetHiddenState();
                
            if (_profilePanelAnimator != null)
                _profilePanelAnimator.SetHiddenState();
                
            if (_popupPanel != null)
                _popupPanel.SetActive(false);
        }

        public void ShowAuthPanel()
        {
            if (_authPanelAnimator != null)
                _authPanelAnimator.Show();
                
            if (_emailVerificationAnimator != null && _emailVerificationAnimator.gameObject.activeSelf)
                _emailVerificationAnimator.Hide();
                
            if (_profilePanelAnimator != null && _profilePanelAnimator.gameObject.activeSelf)
                _profilePanelAnimator.Hide();
        }

        public void ShowEmailVerificationPanel()
        {
            if (_authPanelAnimator != null && _authPanelAnimator.gameObject.activeSelf)
                _authPanelAnimator.Hide();
                
            if (_emailVerificationAnimator != null)
                _emailVerificationAnimator.Show();
                
            if (_profilePanelAnimator != null && _profilePanelAnimator.gameObject.activeSelf)
                _profilePanelAnimator.Hide();
        }

        public void ShowProfilePanel()
        {
            if (_authPanelAnimator != null && _authPanelAnimator.gameObject.activeSelf)
                _authPanelAnimator.Hide();
                
            if (_emailVerificationAnimator != null && _emailVerificationAnimator.gameObject.activeSelf)
                _emailVerificationAnimator.Hide();
                
            if (_profilePanelAnimator != null)
                _profilePanelAnimator.Show();
        }

        public void LoadSavedCredentials(string email, string password, bool rememberMe)
        {
            // Если есть сохраненный email (когда "Запомнить меня" включено), используем его
            if (!string.IsNullOrEmpty(email))
            {
                _emailInput.text = email;
                _passwordInput.text = password;
                _rememberMeToggle.isOn = rememberMe;
            }
            else
            {
                // Иначе загружаем последний использованный email
                string lastEmail = _authManager.GetLastUsedEmail();
                if (!string.IsNullOrEmpty(lastEmail))
                {
                    _emailInput.text = lastEmail;
                    _passwordInput.text = "";
                    _rememberMeToggle.isOn = false;
                }
            }
        }

        public void ClearCredentialFields()
        {
            _emailInput.text = "";
            _passwordInput.text = "";
            if (_rememberMeToggle != null)
                _rememberMeToggle.isOn = false;
        }

        public void ShowPopup(string message)
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

        // UI Event Handlers
        public void OnRegisterButtonClicked()
        {
            _authManager.RegisterUser(_emailInput.text.Trim(), _passwordInput.text.Trim(), _rememberMeToggle.isOn);
        }

        public void OnLoginButtonClicked()
        {
            _authManager.LoginUser(_emailInput.text.Trim(), _passwordInput.text.Trim(), _rememberMeToggle.isOn);
        }

        public void OnGoogleSignInButtonClicked()
        {
            Debug.Log("🔵 [AUTH-UI] Нажата кнопка Google Sign-In");
            _authManager.LoginWithGoogle();
        }

        public void OnCheckEmailVerifiedButtonClicked()
        {
            _authManager.CheckEmailVerification();
        }

        public void OnSendVerificationEmailButtonClicked()
        {
            _authManager.SendEmailVerification();
        }

        public void OnClearCredentialsButtonClicked()
        {
            _authManager.ClearStoredCredentials();
        }

        public void OnContinueProfileButtonClicked()
        {
            // Вместо прямого GetComponent используем систему событий
            OnProfileSetupRequested?.Invoke();
        }
    }
}