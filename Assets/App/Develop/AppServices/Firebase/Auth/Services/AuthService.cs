// Assets/App/Develop/AppServices/Firebase/Auth/Services/AuthService.cs

using App.Develop.AppServices.Firebase.Database.Services;
using Firebase.Auth;
using System;
using System.Threading.Tasks;
using App.Develop.AppServices.Auth;
using Firebase;
using UnityEngine;
using System.Collections.Generic;

namespace App.Develop.AppServices.Firebase.Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly FirebaseAuth _auth;
        private readonly DatabaseService _databaseService;
        private readonly ValidationService _validationService;
        private Dictionary<string, DateTime> _lastEmailSentTime = new Dictionary<string, DateTime>();
        private const int MIN_EMAIL_INTERVAL_SECONDS = 60; // 1 минута между письмами

        public AuthService(
            FirebaseAuth auth,
            DatabaseService databaseService,
            ValidationService validationService)
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        }

        public async Task<(bool success, string error)> RegisterUser(string email, string password)
        {
            try
            {
                var result = await _auth.CreateUserWithEmailAndPasswordAsync(email, password);

                if (result?.User == null)
                {
                    return (false, "Неизвестная ошибка при создании пользователя");
                }

                _databaseService.UpdateUserId(result.User.UserId);
                await _databaseService.CreateNewUser(result.User.UserId, email);

                try
                {
                    await result.User.SendEmailVerificationAsync();
                    Debug.Log("📧 Письмо с подтверждением отправлено!");
                }
                catch (Exception emailEx)
                {
                    Debug.LogError($"❌ Не удалось отправить письмо: {emailEx.Message}");
                    return (false, "Не удалось отправить письмо с подтверждением email");
                }

                return (true, null);
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"❌ Ошибка регистрации: {ex.Message}");
                return (false, GetFriendlyErrorMessage(ex));
            }
            catch (Exception ex)
            {
                Debug.LogError($"🔴 Неожиданная ошибка при регистрации: {ex}");
                return (false, "Произошла неожиданная ошибка");
            }
        }


        public async Task<(bool success, string error)> LoginUser(string email, string password)
        {
            try
            {
                var result = await _auth.SignInWithEmailAndPasswordAsync(email, password);

                if (result?.User == null)
                {
                    return (false, "Неизвестная ошибка при входе");
                }

                // Обновляем ID пользователя в сервисе базы данных
                _databaseService.UpdateUserId(result.User.UserId);

                // Обновляем информацию о пользователе перед проверкой верификации
                await result.User.ReloadAsync();

                if (!result.User.IsEmailVerified)
                {
                    return (false, "Пожалуйста, подтвердите email");
                }

                return (true, null);
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"❌ Ошибка входа: {ex.Message}");
                return (false, GetFriendlyErrorMessage(ex));
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Неожиданная ошибка входа: {ex.Message}");
                return (false, "Произошла неожиданная ошибка");
            }
        }

        public async Task<bool> ResendVerificationEmail()
        {
            try
            {
                var user = _auth.CurrentUser;

                if (user == null)
                {
                    Debug.LogWarning("⚠️ Нет текущего пользователя для отправки верификации");
                    return false;
                }

                string emailType = "verification"; // или "reset"
                string key = $"{user.Email}_{emailType}";
                
                if (_lastEmailSentTime.TryGetValue(key, out DateTime lastTime))
                {
                    var timeSince = DateTime.Now - lastTime;
                    if (timeSince.TotalSeconds < MIN_EMAIL_INTERVAL_SECONDS)
                    {
                        Debug.LogWarning($"⚠️ Слишком частые запросы на отправку писем. Пожалуйста, подождите {MIN_EMAIL_INTERVAL_SECONDS - (int)timeSince.TotalSeconds} секунд");
                        return false;
                    }
                }
                
                // Записываем время отправки
                _lastEmailSentTime[key] = DateTime.Now;

                await user.SendEmailVerificationAsync();
                Debug.Log("✅ Письмо с подтверждением отправлено");
                return true;
            }
            catch (Exception ex)
            {
                // Проверяем сообщение об ошибке на блокировку из-за активности
                if (ex.Message.Contains("blocked") && ex.Message.Contains("unusual activity"))
                {
                    // Письмо отправлено, но Firebase сообщает о блокировке
                    Debug.LogWarning($"⚠️ Письмо отправлено, но Firebase сообщает о блокировке: {ex.Message}");
                    return true; // Возвращаем true, так как письмо фактически отправлено
                }
                
                Debug.LogError($"❌ Ошибка отправки письма: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsEmailVerified()
        {
            try
            {
                var user = _auth.CurrentUser;

                if (user == null)
                {
                    Debug.LogWarning("⚠️ Нет текущего пользователя для проверки верификации");
                    return false;
                }

                // Обновляем информацию о пользователе
                await user.ReloadAsync();
                return user.IsEmailVerified;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка проверки верификации email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ResetPassword(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    Debug.LogWarning("⚠️ Email не может быть пустым");
                    return false;
                }

                string emailType = "reset"; // или "verification"
                string key = $"{email}_{emailType}";
                
                if (_lastEmailSentTime.TryGetValue(key, out DateTime lastTime))
                {
                    var timeSince = DateTime.Now - lastTime;
                    if (timeSince.TotalSeconds < MIN_EMAIL_INTERVAL_SECONDS)
                    {
                        Debug.LogWarning($"⚠️ Слишком частые запросы на отправку писем. Пожалуйста, подождите {MIN_EMAIL_INTERVAL_SECONDS - (int)timeSince.TotalSeconds} секунд");
                        return false;
                    }
                }
                
                // Записываем время отправки
                _lastEmailSentTime[key] = DateTime.Now;

                await _auth.SendPasswordResetEmailAsync(email);
                Debug.Log($"✅ Письмо для сброса пароля отправлено на {email}");
                return true;
            }
            catch (Exception ex)
            {
                // Проверяем сообщение об ошибке на блокировку из-за активности
                if (ex.Message.Contains("blocked") && ex.Message.Contains("unusual activity"))
                {
                    // Письмо отправлено, но Firebase сообщает о блокировке
                    Debug.LogWarning($"⚠️ Письмо отправлено, но Firebase сообщает о блокировке: {ex.Message}");
                    return true; // Возвращаем true, так как письмо фактически отправлено
                }
                
                Debug.LogError($"❌ Ошибка сброса пароля: {ex.Message}");
                return false;
            }
        }
        

        public void SignOut()
        {
            try
            {
                _auth.SignOut();
                // Сбрасываем ID пользователя в сервисе базы данных
                _databaseService.UpdateUserId(null);
                Debug.Log("✅ Выход выполнен успешно");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка при выходе: {ex.Message}");
            }
        }


        private string GetFriendlyErrorMessage(Exception ex)
        {
            // В Firebase SDK для Unity, в сообщении об ошибке обычно содержится код ошибки
            string message = ex.Message.ToLower();

            if (message.Contains("invalid_email")) return "Некорректный формат email";
            if (message.Contains("wrong_password")) return "Неверный пароль";
            if (message.Contains("user_not_found")) return "Пользователь не найден";
            if (message.Contains("user_disabled")) return "Аккаунт заблокирован";
            if (message.Contains("too_many_requests")) return "Слишком много попыток. Попробуйте позже";
            if (message.Contains("operation_not_allowed")) return "Вход с email/паролем отключен";
            if (message.Contains("requires_recent_login")) return "Требуется повторный вход";
            if (message.Contains("weak_password")) return "Слишком простой пароль";
            if (message.Contains("email_already_in_use")) return "Email уже используется";

            return "Произошла ошибка. Попробуйте позже";
        }
    }
}
