// Assets/App/Develop/AppServices/Firebase/Auth/AuthManager.cs

using System;
using System.Threading.Tasks;
using App.Develop.CommonServices.Firebase.Auth.Services;
using App.Develop.CommonServices.Firebase.Common.SecureStorage;
using UnityEngine;
using App.Develop.AppServices.Auth;
using App.Develop.CommonServices.SceneManagement;
using App.Develop.DI;
using App.Develop.Utils.Logging;
using App.Develop.Scenes.PersonalAreaScene;
using Firebase.Auth;

namespace App.Develop.CommonServices.Firebase.Auth
{
    // Определяем интерфейс для AuthManager
    public interface IAuthManager
    {
        void Initialize(AuthUIController uiController);
        void RegisterUser(string email, string password, bool rememberMe);
        void LoginUser(string email, string password, bool rememberMe);
        void LoginWithGoogle();
        string GetLastUsedEmail();
        void CheckEmailVerification();
        void SendEmailVerification();
        void ClearStoredCredentials();
        void Logout();
    }

    // Реализация AuthManager как обычного класса
    public class AuthManager : IAuthManager, IInjectable, IDisposable
    {
        private AuthUIController _uiController;
        private IAuthService _authService;
        private UserProfileService _profileService;
        private CredentialStorage _credentialStorage;
        private ValidationService _validationService;
        private SceneSwitcher _sceneSwitcher;
        private bool _isProcessing;

        // Конструктор по умолчанию для DI-контейнера
        public AuthManager()
        {
        }

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
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"🔴 Ошибка инициализации AuthManager: {ex}", MyLogger.LogCategory.Firebase);
            }
        }

        public void Initialize(AuthUIController uiController)
        {
            if (uiController == null)
                throw new ArgumentNullException(nameof(uiController));

            _uiController = uiController;
            _uiController.Initialize(this);
            
            // Показываем панель авторизации для ручного входа
            // Автоматический вход теперь обрабатывается в Bootstrap для улучшения UX
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
                    _isProcessing = false;
                    return;
                }

                if (!_validationService.IsValidPassword(password))
                {
                    _uiController.ShowPopup("Пароль должен содержать 8–12 символов, цифры, строчные и заглавные буквы");
                    _isProcessing = false;
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
                MyLogger.LogError($"🔴 Ошибка при регистрации: {ex}", MyLogger.LogCategory.Firebase);
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
                    _isProcessing = false;
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
                MyLogger.LogError($"🔴 Ошибка при входе: {ex}", MyLogger.LogCategory.Firebase);
                _uiController.ShowPopup("Произошла ошибка при входе");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        public async void LoginWithGoogle()
        {
            Debug.Log("🟡 [AUTH-MANAGER] Начинаем LoginWithGoogle()");
            if (_isProcessing || _uiController == null) 
            {
                Debug.Log("🔴 [AUTH-MANAGER] Процесс уже выполняется или UI контроллер null");
                return;
            }
            _isProcessing = true;

            try
            {
                Debug.Log("🟡 [AUTH-MANAGER] Вызываем _authService.LoginWithGoogle()");
                var result = await _authService.LoginWithGoogle();

                if (result.success)
                {
                    SecurePlayerPrefs.SetBool("explicit_logout", false);
                    SecurePlayerPrefs.Save();
                    _uiController.ShowPopup("Вход через Google выполнен!");
                    _sceneSwitcher.ProcessSwitchSceneFor(new OutputAuthSceneArgs(new PersonalAreaInputArgs()));
                }
                else
                {
                    _uiController.ShowPopup($"Ошибка входа через Google: {result.error}");
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"🔴 Ошибка при входе через Google: {ex}", MyLogger.LogCategory.Firebase);
                _uiController.ShowPopup("Произошла ошибка при входе через Google");
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
                MyLogger.LogError($"🔴 Ошибка при получении последнего email: {ex}", MyLogger.LogCategory.Firebase);
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
                MyLogger.LogError($"🔴 Ошибка при проверке верификации email: {ex}", MyLogger.LogCategory.Firebase);
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
                MyLogger.LogError($"🔴 Ошибка при отправке письма верификации: {ex}", MyLogger.LogCategory.Firebase);
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
                MyLogger.LogError($"🔴 Ошибка при очистке сохраненных данных: {ex}", MyLogger.LogCategory.Firebase);
            }
        }

        public void Logout()
        {
            try
            {
                if (_authService != null)
                {
                    _authService.SignOut();
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"🔴 Ошибка при выходе: {ex}", MyLogger.LogCategory.Firebase);
            }
        }

        public void Dispose()
        {
            Logout();
            _uiController = null;
        }
    }
}
