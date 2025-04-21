using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using Firebase.Auth;
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Auth
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
            _authService = container.Resolve<AuthService>();
            _profileService = container.Resolve<UserProfileService>();
            _credentialStorage = container.Resolve<CredentialStorage>();
            _validationService = container.Resolve<ValidationService>();

            if (_uiController == null)
            {
                Debug.LogError("🔴 В AuthManager._uiController не присвоен AuthUIController!");
                return;
            }

            _uiController.Initialize(this);
            _uiController.ShowAuthPanel();

            var email = _credentialStorage.GetSavedEmail();
            var password = _credentialStorage.GetSavedPassword();
            var remember = _credentialStorage.IsRememberMeEnabled();
            _uiController.LoadSavedCredentials(email, password, remember);
        }

        public async void RegisterUser(string email, string password, bool rememberMe)
        {
            if (!_validationService.IsValidEmail(email))
            {
                _uiController.ShowPopup("Введите корректный email");
                return;
            }

            if (!_validationService.IsValidPassword(password))
            {
                _uiController.ShowPopup("Пароль должен содержать 8–12 символов, цифры, строчные и заглавные буквы");
                return;
            }

            await _authService.RegisterUser(email, password,
                onSuccess: user =>
                {
                    _uiController.SetCurrentUser(user);
                    _credentialStorage.SaveCredentials(email, password, rememberMe);
                    _authService.SendEmailVerification(user,
                        onSuccess: () =>
                        {
                            _uiController.ShowPopup("Регистрация успешна! Подтвердите email.");
                            _uiController.ShowEmailVerificationPanel();
                        },
                        onError: error => _uiController.ShowPopup("Ошибка отправки письма: " + error));
                },
                onError: error => _uiController.ShowPopup("Ошибка регистрации: " + error));
        }

        public async void LoginUser(string email, string password, bool rememberMe)
        {
            if (!_validationService.IsValidEmail(email))
            {
                _uiController.ShowPopup("Введите корректный email");
                return;
            }

            await _authService.LoginUser(email, password,
                onSuccess: user =>
                {
                    _uiController.SetCurrentUser(user);
                    _credentialStorage.SaveCredentials(email, password, rememberMe);

                    if (!user.IsEmailVerified)
                    {
                        _uiController.ShowPopup("Подтвердите email перед входом. Письмо отправлено повторно.");
                        _authService.SendEmailVerification(user,
                            onSuccess: () => _uiController.ShowEmailVerificationPanel(),
                            onError: error => _uiController.ShowPopup("Ошибка отправки письма: " + error));
                        return;
                    }

                    CheckUserProfileFilled(user.UserId);
                },
                onError: error => _uiController.ShowPopup("Ошибка входа: " + error));
        }

        public async void CheckEmailVerification(FirebaseUser user)
        {
            var isVerified = await _authService.CheckEmailVerified(user);
            if (isVerified)
            {
                _uiController.ShowPopup("Email подтверждён!");
                _uiController.ShowProfilePanel();
            }
            else
            {
                _uiController.ShowPopup("Email пока не подтверждён.");
            }
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
                    _sceneSwitcher.ProcessSwitchSceneFor(
                        new OutputAuthSceneArgs(new PersonalAreaInputArgs()));
                });
        }

        public void ClearStoredCredentials()
        {
            _credentialStorage.ClearStoredCredentials();
            _uiController.ClearCredentialFields();
        }
    }
}
