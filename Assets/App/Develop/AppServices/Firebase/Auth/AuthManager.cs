// Assets/App/Develop/AppServices/Firebase/Auth/AuthManager.cs

using System;
using App.Develop.AppServices.Auth;
using App.Develop.AppServices.Firebase.Auth.Services;
using App.Develop.AppServices.Firebase.Common.SecureStorage;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Auth
{
    public class AuthManager : MonoBehaviour, IInjectable
    {
        [SerializeField] private AuthUIController _uiController;

        private IAuthService _authService;
        private UserProfileService _profileService;
        private CredentialStorage _credentialStorage;
        private ValidationService _validationService;
        private SceneSwitcher _sceneSwitcher;
        private bool _isProcessing;

        public void Inject(DIContainer container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            try
            {
                _sceneSwitcher = container.Resolve<SceneSwitcher>();
                _authService = container.Resolve<IAuthService>();
                _profileService = container.Resolve<UserProfileService>();
                _credentialStorage = container.Resolve<CredentialStorage>();
                _validationService = container.Resolve<ValidationService>();

                if (_uiController == null)
                {
                    Debug.LogError("🔴 AuthUIController не присвоен в инспекторе!");
                    return;
                }

                InitializeUI();
            }
            catch (Exception ex)
            {
                Debug.LogError($"🔴 Ошибка инициализации AuthManager: {ex}");
            }
        }

        private void InitializeUI()
        {
            _uiController.Initialize(this);
            _uiController.ShowAuthPanel();

            var email = _credentialStorage.GetSavedEmail();
            var password = _credentialStorage.GetSavedPassword();
            var remember = _credentialStorage.IsRememberMeEnabled();
            _uiController.LoadSavedCredentials(email, password, remember);
        }

        public async void RegisterUser(string email, string password, bool rememberMe)
        {
            if (_isProcessing || _uiController == null) return;
            _isProcessing = true;

            try
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

                var result = await _authService.RegisterUser(email, password);

                if (result.success)
                {
                    _credentialStorage.SaveCredentials(email, password, rememberMe);
                    await _authService.ResendVerificationEmail();
                    _uiController.ShowPopup("Регистрация успешна! Подтвердите email.");
                    _uiController.ShowEmailVerificationPanel();
                }
                else
                {
                    _uiController.ShowPopup($"Ошибка регистрации: {result.error}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"🔴 Ошибка при регистрации: {ex}");
                _uiController.ShowPopup("Произошла ошибка при регистрации");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        public async void LoginUser(string email, string password, bool rememberMe)
        {
            if (_isProcessing || _uiController == null) return;
            _isProcessing = true;

            try
            {
                if (!_validationService.IsValidEmail(email))
                {
                    _uiController.ShowPopup("Введите корректный email");
                    return;
                }

                var result = await _authService.LoginUser(email, password);

                if (result.success)
                {
                    _credentialStorage.SaveCredentials(email, password, rememberMe);
                    SecurePlayerPrefs.SetBool("explicit_logout", false);
                    SecurePlayerPrefs.Save();
                    _uiController.ShowPopup("Вход выполнен!");
                    _sceneSwitcher.ProcessSwitchSceneFor(new OutputAuthSceneArgs(new PersonalAreaInputArgs()));
                }
                else
                {
                    _uiController.ShowPopup($"Ошибка входа: {result.error}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"🔴 Ошибка при входе: {ex}");
                _uiController.ShowPopup("Произошла ошибка при входе");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        public string GetLastUsedEmail()
        {
            try
            {
                return _credentialStorage.GetLastUsedEmail();
            }
            catch (Exception ex)
            {
                Debug.LogError($"🔴 Ошибка при получении последнего email: {ex}");
                return string.Empty;
            }
        }


        public async void CheckEmailVerification()
        {
            if (_isProcessing || _uiController == null) return;
            _isProcessing = true;

            try
            {
                var isVerified = await _authService.IsEmailVerified();

                if (isVerified)
                {
                    _uiController.ShowPopup("Email подтвержден!");
                    _uiController.ShowProfilePanel();
                }
                else
                {
                    _uiController.ShowPopup("Email еще не подтвержден. Проверьте почту.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"🔴 Ошибка при проверке верификации email: {ex}");
                _uiController.ShowPopup("Произошла ошибка при проверке email");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        public async void SendEmailVerification()
        {
            if (_isProcessing || _uiController == null) return;
            _isProcessing = true;

            try
            {
                if (await _authService.ResendVerificationEmail())
                {
                    _uiController.ShowPopup("Письмо отправлено. Проверьте почту.");
                }
                else
                {
                    _uiController.ShowPopup("Ошибка при отправке письма.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"🔴 Ошибка при отправке письма верификации: {ex}");
                _uiController.ShowPopup("Произошла ошибка при отправке письма");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        public void ClearStoredCredentials()
        {
            try
            {
                _credentialStorage.ClearStoredCredentials();

                if (_uiController != null)
                {
                    _uiController.ClearCredentialFields();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"🔴 Ошибка при очистке учетных данных: {ex}");

                if (_uiController != null)
                {
                    _uiController.ShowPopup("Произошла ошибка при очистке данных");
                }
            }
        }

        private void OnDestroy()
        {
            _authService = null;
            _profileService = null;
            _credentialStorage = null;
            _validationService = null;
            _sceneSwitcher = null;
        }
    }
}
