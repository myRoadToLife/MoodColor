using App.Develop.AppServices.Auth.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace App.Develop.AppServices.Auth
{
    public class AuthUIController : MonoBehaviour
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

        private AuthManager _authManager;

        public void Initialize(AuthManager authManager)
        {
            _authManager = authManager;
            
            _authPanelAnimator.Hide();
            _emailVerificationAnimator.Hide();
            _profilePanelAnimator.Hide();
            _popupPanel.SetActive(false);
        }

        public void ShowAuthPanel()
        {
            _authPanelAnimator.Show();
            _emailVerificationAnimator.Hide();
            _profilePanelAnimator.Hide();
        }

        public void ShowEmailVerificationPanel()
        {
            _authPanelAnimator.Hide();
            _emailVerificationAnimator.Show();
            _profilePanelAnimator.Hide();
        }

        public void ShowProfilePanel()
        {
            _authPanelAnimator.Hide();
            _emailVerificationAnimator.Hide();
            _profilePanelAnimator.Show();
        }

        public void LoadSavedCredentials(string email, string password, bool rememberMe)
        {
            _emailInput.text = email;
            _passwordInput.text = password;
            _rememberMeToggle.isOn = rememberMe;
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
    }
}