// Assets/App/Develop/AppServices/Firebase/Auth/Services/AuthService.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Develop.CommonServices.Firebase.Database.Services;
using App.Develop.CommonServices.Firebase.Database.Models;
using Firebase;
using Firebase.Auth;
using UnityEngine;
using App.Develop.CommonServices.Firebase.Common.SecureStorage;
using App.Develop.Utils.Logging;
using Google;

namespace App.Develop.CommonServices.Firebase.Auth.Services
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
                    MyLogger.Log("📧 Письмо с подтверждением отправлено!", MyLogger.LogCategory.Firebase);
                }
                catch (Exception emailEx)
                {
                    MyLogger.LogError($"❌ Не удалось отправить письмо: {emailEx.Message}", MyLogger.LogCategory.Firebase);
                    return (false, "Не удалось отправить письмо с подтверждением email");
                }

                return (true, null);
            }
            catch (FirebaseException ex)
            {
                MyLogger.LogError($"❌ Ошибка регистрации: {ex.Message}", MyLogger.LogCategory.Firebase);
                return (false, GetFriendlyErrorMessage(ex));
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"🔴 Неожиданная ошибка при регистрации: {ex}", MyLogger.LogCategory.Firebase);
                return (false, "Произошла неожиданная ошибка");
            }
        }


        public async Task<(bool success, string error)> LoginUser(string email, string password)
        {
            try
            {
                MyLogger.Log($"🔑 [AUTH-LOGIN] Начинаем процесс входа для: {email}", MyLogger.LogCategory.Firebase);
                
                var result = await _auth.SignInWithEmailAndPasswordAsync(email, password);

                if (result?.User == null)
                {
                    MyLogger.LogError("❌ [AUTH-LOGIN] Пользователь равен null после входа", MyLogger.LogCategory.Firebase);
                    return (false, "Неизвестная ошибка при входе");
                }

                // Обновляем ID пользователя в сервисе базы данных
                _databaseService.UpdateUserId(result.User.UserId);
                MyLogger.Log($"🔑 [AUTH-LOGIN] UserId обновлен: {result.User.UserId.Substring(0, Math.Min(8, result.User.UserId.Length))}...", MyLogger.LogCategory.Firebase);

                // Обновляем информацию о пользователе перед проверкой верификации
                await result.User.ReloadAsync();
                MyLogger.Log($"🔑 [AUTH-LOGIN] Данные пользователя обновлены", MyLogger.LogCategory.Firebase);

                if (!result.User.IsEmailVerified)
                {
                    MyLogger.LogWarning($"⚠️ [AUTH-LOGIN] Email не подтвержден: {email}", MyLogger.LogCategory.Firebase);
                    return (false, "Пожалуйста, подтвердите email");
                }

                // Получаем текущий идентификатор устройства
                string currentDeviceId = ActiveSessionData.GetCurrentDeviceId();
                
                // Логирование для отладки
                MyLogger.Log($"🔑 [AUTH-LOGIN] Попытка входа с устройства ID: {currentDeviceId}", MyLogger.LogCategory.Firebase);
                
                if (string.IsNullOrEmpty(currentDeviceId))
                {
                    MyLogger.LogError("❌ [AUTH-LOGIN] Не удалось получить уникальный идентификатор устройства", MyLogger.LogCategory.Firebase);
                    return (false, "Не удалось идентифицировать устройство");
                }

                // Проверяем, существует ли уже активная сессия с другого устройства
                MyLogger.Log($"🔑 [AUTH-LOGIN] Проверяем наличие активных сессий с других устройств", MyLogger.LogCategory.Firebase);
                bool sessionExists = await _databaseService.CheckActiveSessionExists(currentDeviceId);
                
                if (sessionExists)
                {
                    MyLogger.Log($"⚠️ [AUTH-LOGIN] Обнаружена активная сессия с другого устройства для пользователя {result.User.Email}", MyLogger.LogCategory.Firebase);
                    
                    // Если сессия существует и она не принадлежит текущему устройству, запрещаем вход
                    // Сначала выходим из системы, чтобы пользователь не остался авторизованным
                    _auth.SignOut();
                    _databaseService.UpdateUserId(null);
                    
                    return (false, "Вы уже вошли в аккаунт с другого устройства. Пожалуйста, выйдите из аккаунта на другом устройстве и повторите попытку.");
                }

                // Регистрируем новую активную сессию для текущего устройства
                MyLogger.Log($"🔑 [AUTH-LOGIN] Регистрируем новую сессию для устройства {currentDeviceId}", MyLogger.LogCategory.Firebase);
                bool sessionRegistered = await _databaseService.RegisterActiveSession();
                
                if (!sessionRegistered)
                {
                    MyLogger.LogError("❌ [AUTH-LOGIN] Не удалось зарегистрировать активную сессию", MyLogger.LogCategory.Firebase);
                    // Можно продолжить вход, даже если регистрация сессии не удалась
                }

                MyLogger.Log($"✅ [AUTH-LOGIN] Успешный вход для пользователя {result.User.Email} с устройства {currentDeviceId}", MyLogger.LogCategory.Firebase);
                return (true, null);
            }
            catch (FirebaseException ex)
            {
                MyLogger.LogError($"❌ [AUTH-LOGIN] Ошибка входа: {ex.Message}", MyLogger.LogCategory.Firebase);
                return (false, GetFriendlyErrorMessage(ex));
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [AUTH-LOGIN] Неожиданная ошибка входа: {ex.Message}", MyLogger.LogCategory.Firebase);
                MyLogger.LogError($"❌ [AUTH-LOGIN] Stack trace: {ex.StackTrace}", MyLogger.LogCategory.Firebase);
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
                    MyLogger.LogWarning("⚠️ Нет текущего пользователя для отправки верификации", MyLogger.LogCategory.Firebase);
                    return false;
                }

                string emailType = "verification"; // или "reset"
                string key = $"{user.Email}_{emailType}";
                
                if (_lastEmailSentTime.TryGetValue(key, out DateTime lastTime))
                {
                    var timeSince = DateTime.Now - lastTime;
                    if (timeSince.TotalSeconds < MIN_EMAIL_INTERVAL_SECONDS)
                    {
                        MyLogger.LogWarning($"⚠️ Слишком частые запросы на отправку писем. Пожалуйста, подождите {MIN_EMAIL_INTERVAL_SECONDS - (int)timeSince.TotalSeconds} секунд", MyLogger.LogCategory.Firebase);
                        return false;
                    }
                }
                
                // Записываем время отправки
                _lastEmailSentTime[key] = DateTime.Now;

                await user.SendEmailVerificationAsync();
                MyLogger.Log("✅ Письмо с подтверждением отправлено", MyLogger.LogCategory.Firebase);
                return true;
            }
            catch (Exception ex)
            {
                // Проверяем сообщение об ошибке на блокировку из-за активности
                if (ex.Message.Contains("blocked") && ex.Message.Contains("unusual activity"))
                {
                    // Письмо отправлено, но Firebase сообщает о блокировке
                    MyLogger.LogWarning($"⚠️ Письмо отправлено, но Firebase сообщает о блокировке: {ex.Message}", MyLogger.LogCategory.Firebase);
                    return true; // Возвращаем true, так как письмо фактически отправлено
                }
                
                MyLogger.LogError($"❌ Ошибка отправки письма: {ex.Message}", MyLogger.LogCategory.Firebase);
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
                    MyLogger.LogWarning("⚠️ Нет текущего пользователя для проверки верификации", MyLogger.LogCategory.Firebase);
                    return false;
                }

                // Обновляем информацию о пользователе
                await user.ReloadAsync();
                return user.IsEmailVerified;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка проверки верификации email: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        public async Task<bool> ResetPassword(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    MyLogger.LogWarning("⚠️ Email не может быть пустым", MyLogger.LogCategory.Firebase);
                    return false;
                }

                string emailType = "reset"; // или "verification"
                string key = $"{email}_{emailType}";
                
                if (_lastEmailSentTime.TryGetValue(key, out DateTime lastTime))
                {
                    var timeSince = DateTime.Now - lastTime;
                    if (timeSince.TotalSeconds < MIN_EMAIL_INTERVAL_SECONDS)
                    {
                        MyLogger.LogWarning($"⚠️ Слишком частые запросы на отправку писем. Пожалуйста, подождите {MIN_EMAIL_INTERVAL_SECONDS - (int)timeSince.TotalSeconds} секунд", MyLogger.LogCategory.Firebase);
                        return false;
                    }
                }
                
                // Записываем время отправки
                _lastEmailSentTime[key] = DateTime.Now;

                await _auth.SendPasswordResetEmailAsync(email);
                MyLogger.Log($"✅ Письмо для сброса пароля отправлено на {email}", MyLogger.LogCategory.Firebase);
                return true;
            }
            catch (Exception ex)
            {
                // Проверяем сообщение об ошибке на блокировку из-за активности
                if (ex.Message.Contains("blocked") && ex.Message.Contains("unusual activity"))
                {
                    // Письмо отправлено, но Firebase сообщает о блокировке
                    MyLogger.LogWarning($"⚠️ Письмо отправлено, но Firebase сообщает о блокировке: {ex.Message}", MyLogger.LogCategory.Firebase);
                    return true; // Возвращаем true, так как письмо фактически отправлено
                }
                
                MyLogger.LogError($"❌ Ошибка сброса пароля: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        public async Task<(bool success, string error)> LoginWithGoogle()
        {
            try
            {
                // Согласно официальной документации Firebase:
                // https://firebase.google.com/docs/auth/unity/google-signin?hl=ru
                // Для Google Sign-In в Unity нужно:
                // 1. Следовать инструкциям для Android и iOS+ чтобы получить токен идентификатора
                // 2. Использовать Google Play Games Services или другой способ получения токенов
                // 3. Затем обменять токены на Firebase credentials
                return (false, "Google Sign-In требует дополнительной настройки. Используйте вход по email/паролю.");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка входа через Google: {ex.Message}");
            }
        }



        public async Task SignOut()
        {
            try
            {
                MyLogger.Log($"🔑 [AUTH-LOGOUT] Начинаем процесс выхода из системы", MyLogger.LogCategory.Firebase);
                
                // Получаем текущий ID устройства перед выходом
                string deviceId = ActiveSessionData.GetCurrentDeviceId();
                
                if (_auth.CurrentUser != null && !string.IsNullOrEmpty(deviceId))
                {
                    MyLogger.Log($"🔑 [AUTH-LOGOUT] Очищаем активную сессию для устройства {deviceId}", MyLogger.LogCategory.Firebase);
                    
                    try
                    {
                        // Здесь мы явно очищаем только сессию текущего устройства
                        bool sessionCleared = await _databaseService.ClearActiveSession(deviceId);
                        MyLogger.Log($"🔑 [AUTH-LOGOUT] Результат очистки сессии: {(sessionCleared ? "Успешно" : "Неудачно")}", MyLogger.LogCategory.Firebase);
                    }
                    catch (Exception sessionEx)
                    {
                        MyLogger.LogError($"❌ [AUTH-LOGOUT] Ошибка при очистке сессии: {sessionEx.Message}", MyLogger.LogCategory.Firebase);
                        MyLogger.LogError($"❌ [AUTH-LOGOUT] Stack trace: {sessionEx.StackTrace}", MyLogger.LogCategory.Firebase);
                        // Продолжаем процесс выхода, даже если очистка сессии не удалась
                    }
                }
                else
                {
                    MyLogger.LogWarning($"⚠️ [AUTH-LOGOUT] Пропускаем очистку сессии: CurrentUser={_auth.CurrentUser != null}, DeviceId={deviceId}", MyLogger.LogCategory.Firebase);
                }

                // Выходим из аккаунта Firebase
                _auth.SignOut();
                MyLogger.Log($"🔑 [AUTH-LOGOUT] Firebase SignOut выполнен", MyLogger.LogCategory.Firebase);
                
                // Сбрасываем UserId в базе данных
                _databaseService.UpdateUserId(null);
                MyLogger.Log($"🔑 [AUTH-LOGOUT] UserId сброшен", MyLogger.LogCategory.Firebase);
                
                // Сохраняем флаг явного выхода
                SecurePlayerPrefs.SetBool("explicit_logout", true);
                SecurePlayerPrefs.Save();
                
                MyLogger.Log("✅ [AUTH-LOGOUT] Пользователь вышел из аккаунта", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [AUTH-LOGOUT] Ошибка при выходе из аккаунта: {ex.Message}", MyLogger.LogCategory.Firebase);
                MyLogger.LogError($"❌ [AUTH-LOGOUT] Stack trace: {ex.StackTrace}", MyLogger.LogCategory.Firebase);
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

