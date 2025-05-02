using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Develop.AppServices.Firebase.Auth.Services;
using App.Develop.AppServices.Firebase.Common.SecureStorage;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

namespace App.Develop.Scenes.PersonalAreaScene.Settings
{
    /// <summary>
    /// Вспомогательный класс для работы с удалением аккаунта пользователя.
    /// Содержит логику удаления данных пользователя и учетной записи Firebase.
    /// </summary>
    public class AccountDeletionHelper
    {
        #region Private Fields
        private readonly DatabaseReference _database;
        private readonly IAuthStateService _authStateService;
        #endregion
        
        #region Public Events
        public event Action<string> OnMessage;
        public event Action<string> OnError;
        public event Action OnUserDeleted;
        public event Action OnRedirectToAuth;
        #endregion
        
        #region Constructor
        public AccountDeletionHelper(DatabaseReference database, IAuthStateService authStateService)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _authStateService = authStateService ?? throw new ArgumentNullException(nameof(authStateService));
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Удаляет аккаунт пользователя
        /// </summary>
        public void DeleteAccount(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                OnError?.Invoke("Введите пароль для подтверждения.");
                return;
            }
            
            if (!_authStateService.IsAuthenticated)
            {
                OnError?.Invoke("Ошибка: пользователь не авторизован. Повторите вход.");
                OnRedirectToAuth?.Invoke();
                return;
            }
            
            var user = _authStateService.CurrentUser;
            var email = user.Email;
            Debug.Log($"📧 Текущий пользователь: {email ?? "null"}, UID: {user.UserId}");
            
            if (string.IsNullOrEmpty(email))
            {
                OnError?.Invoke("Ошибка: не удалось получить email. Повторите вход в аккаунт.");
                return;
            }
            
            try
            {
                var credential = EmailAuthProvider.GetCredential(email, password);
                
                user.ReauthenticateAsync(credential).ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError($"❌ Ошибка реаутентификации: {task.Exception?.GetBaseException()?.Message}");
                        OnError?.Invoke("Неверный пароль или ошибка аутентификации.");
                        return;
                    }
                    
                    DeleteUserData();
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Исключение при аутентификации: {ex.Message}");
                OnError?.Invoke("Произошла ошибка. Повторите попытку позже.");
            }
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Удаляет данные пользователя из базы данных
        /// </summary>
        private void DeleteUserData()
        {
            if (!_authStateService.IsAuthenticated)
            {
                OnError?.Invoke("Ошибка: сессия истекла. Войдите снова.");
                OnRedirectToAuth?.Invoke();
                return;
            }
            
            var uid = _authStateService.CurrentUser.UserId;
            Debug.Log($"🗑️ Удаление данных пользователя: {uid}");
            
            _database
                .Child("users")
                .Child(uid)
                .RemoveValueAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError($"❌ Ошибка при удалении данных: {task.Exception?.GetBaseException()?.Message}");
                        Debug.LogWarning("⚠️ Продолжаем с удалением аккаунта, несмотря на ошибку с базой данных");
                    }
                    else
                    {
                        Debug.Log("✅ Данные пользователя успешно удалены");
                    }
                    
                    if (!_authStateService.IsAuthenticated)
                    {
                        OnError?.Invoke("Ошибка: сессия истекла в процессе удаления. Данные удалены, но аккаунт сохранен.");
                        OnRedirectToAuth?.Invoke();
                        return;
                    }
                    
                    DeleteFirebaseUser();
                });
        }
        
        /// <summary>
        /// Удаляет учетную запись пользователя в Firebase
        /// </summary>
        private void DeleteFirebaseUser()
        {
            if (!_authStateService.IsAuthenticated)
            {
                OnError?.Invoke("Ошибка: сессия истекла. Войдите снова.");
                OnRedirectToAuth?.Invoke();
                return;
            }
            
            Debug.Log($"🗑️ Удаление аккаунта Firebase: {_authStateService.CurrentUser.Email}");
            
            _authStateService.CurrentUser.DeleteAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"❌ Ошибка при удалении аккаунта: {task.Exception?.GetBaseException()?.Message}");
                    
                    bool requiresReauth = task.Exception?.InnerExceptions.Any(ex => 
                        ex.Message.Contains("requires recent authentication") || 
                        ex.Message.Contains("requires a recent login")) ?? false;
                    
                    if (requiresReauth)
                    {
                        OnError?.Invoke("Для удаления аккаунта необходима повторная авторизация. Пожалуйста, войдите снова.");
                        OnRedirectToAuth?.Invoke();
                    }
                    else
                    {
                        OnError?.Invoke("Не удалось удалить аккаунт. Повторите попытку позже.");
                    }
                    
                    return;
                }
                
                Debug.Log("✅ Аккаунт успешно удален");
                CleanupStoredCredentials();
                OnMessage?.Invoke("Аккаунт успешно удален.");
                OnUserDeleted?.Invoke();
            });
        }
        
        /// <summary>
        /// Очищает сохраненные учетные данные
        /// </summary>
        private void CleanupStoredCredentials()
        {
            SecurePlayerPrefs.DeleteKey("email");
            SecurePlayerPrefs.DeleteKey("password");
            SecurePlayerPrefs.DeleteKey("remember_me");
            SecurePlayerPrefs.Save();
        }
        #endregion
    }
} 