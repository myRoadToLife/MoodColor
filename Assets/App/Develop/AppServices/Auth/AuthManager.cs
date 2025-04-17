using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using UnityEngine;

namespace App.Develop.AppServices.Auth
{
    public class AuthManager : MonoBehaviour, IInjectable
    {
        [SerializeField] private AuthUIController _uiController;

        private AuthService _authService;
        private UserProfileService _profileService;
        private CredentialStorage _credentialStorage;
        private ValidationService _validationService;
        private SceneSwitcher _sceneSwitcher;

        public void Inject(DIContainer container)
        {
            _sceneSwitcher = container.Resolve<SceneSwitcher>();

            _authService = new AuthService();
            _profileService = new UserProfileService();
            _credentialStorage = new CredentialStorage("UltraSecretKey!🔥");
            _validationService = new ValidationService();

            _uiController.Initialize(this);
            _uiController.ShowAuthPanel();

            _uiController.LoadSavedCredentials(_credentialStorage.GetSavedEmail(),
                _credentialStorage.GetSavedPassword(),
                _credentialStorage.IsRememberMeEnabled());
        }

        public void RegisterUser(string email, string password, bool rememberMe)
        {
            if (!_validationService.IsValidEmail(email))
            {
                _uiController.ShowPopup("Введите корректный email");
                return;
            }

            if (!_validationService.IsValidPassword(password))
            {
                _uiController.ShowPopup("Пароль должен содержать 8--12 символов, цифры, строчные и заглавные буквы");
                return;
            }

            _authService.RegisterUser(email, password,
                onSuccess: () =>
                {
                    _credentialStorage.SaveCredentials(email, password, rememberMe);
                    _authService.SendEmailVerification();
                    _uiController.ShowPopup("Регистрация успешна! Подтвердите email.");
                    _uiController.ShowEmailVerificationPanel();
                },
                onError: (errorMessage) => { _uiController.ShowPopup("Ошибка регистрации: " + errorMessage); });
        }

        public void LoginUser(string email, string password, bool rememberMe)
        {
            if (!_validationService.IsValidEmail(email))
            {
                _uiController.ShowPopup("Введите корректный email");
                return;
            }

            _authService.LoginUser(email, password,
                onSuccess: (user) =>
                {
                    _credentialStorage.SaveCredentials(email, password, rememberMe);

                    if (!user.IsEmailVerified)
                    {
                        _uiController.ShowPopup("Подтвердите email перед входом. Письмо отправлено повторно.");
                        _authService.SendEmailVerification();
                        _uiController.ShowEmailVerificationPanel();
                        return;
                    }

                    CheckUserProfileFilled(user.UserId);
                },
                onError: (errorMessage) => { _uiController.ShowPopup("Ошибка входа: " + errorMessage); });
        }

        public void CheckEmailVerification()
        {
            _authService.CheckEmailVerified(
                onVerified: () =>
                {
                    _uiController.ShowPopup("Email подтверждён!");
                    _uiController.ShowProfilePanel();
                },
                onNotVerified: () => { _uiController.ShowPopup("Email пока не подтверждён."); });
        }

        private void CheckUserProfileFilled(string uid)
        {
            _profileService.CheckUserProfileFilled(uid,
                onProfileIncomplete: () =>
                {
                    _uiController.ShowPopup("Пожалуйста, заполните профиль.");
                    _uiController.ShowProfilePanel();
                },
                onProfileComplete: () =>
                {
                    _uiController.ShowPopup("Вход выполнен!");
                    _sceneSwitcher.ProcessSwitchSceneFor(new OutputAuthSceneArgs(new PersonalAreaInputArgs()));
                });
        }

        public void SendEmailVerification()
        {
            _authService.SendEmailVerification(
                onSuccess: () => { _uiController.ShowPopup("Письмо отправлено. Проверьте почту."); },
                onError: () => { _uiController.ShowPopup("Ошибка при отправке письма."); });
        }

        public void ClearStoredCredentials()
        {
            _credentialStorage.ClearStoredCredentials();
            _uiController.ClearCredentialFields();
        }
    }
}
