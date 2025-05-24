// Assets/App/Develop/CommonServices/Firebase/Auth/CredentialStorage.cs
using System;
using App.Develop.CommonServices.Firebase.Common.SecureStorage;
using Firebase.Auth;
using UnityEngine;
using App.Develop.Utils.Logging;

namespace App.Develop.CommonServices.Firebase.Auth
{
    public class CredentialStorage
    {
        private const string EMAIL_KEY = "user_email";
        private const string PASSWORD_KEY = "user_password";
        private const string REMEMBER_KEY = "remember_me";
        private const string LAST_EMAIL_KEY = "last_email"; // Новый ключ для последнего использованного email

        private readonly string _securityKey;

        public CredentialStorage(string securityKey)
        {
            if (string.IsNullOrEmpty(securityKey))
                throw new ArgumentException("Security key cannot be empty", nameof(securityKey));

            _securityKey = securityKey;
            SecurePlayerPrefs.Init(_securityKey);
        }

        public void SaveCredentials(string email, string password, bool rememberMe)
        {
            try
            {
                // Всегда сохраняем последний использованный email
                SecurePlayerPrefs.SetString(LAST_EMAIL_KEY, email);
                
                SecurePlayerPrefs.SetBool(REMEMBER_KEY, rememberMe);

                if (rememberMe)
                {
                    SecurePlayerPrefs.SetString(EMAIL_KEY, email);
                    SecurePlayerPrefs.SetString(PASSWORD_KEY, password);
                }
                else
                {
                    SecurePlayerPrefs.DeleteKey(EMAIL_KEY);
                    SecurePlayerPrefs.DeleteKey(PASSWORD_KEY);
                }

                SecurePlayerPrefs.Save();
                MyLogger.Log("✅ Учетные данные сохранены", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка сохранения учетных данных: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }

        public string GetSavedEmail()
        {
            try
            {
                return SecurePlayerPrefs.HasKey(EMAIL_KEY) 
                    ? SecurePlayerPrefs.GetString(EMAIL_KEY) 
                    : string.Empty;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка получения email: {ex.Message}", MyLogger.LogCategory.Firebase);
                return string.Empty;
            }
        }
        
        // Новый метод для получения последнего использованного email
        public string GetLastUsedEmail()
        {
            try
            {
                return SecurePlayerPrefs.HasKey(LAST_EMAIL_KEY)
                    ? SecurePlayerPrefs.GetString(LAST_EMAIL_KEY)
                    : string.Empty;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка получения последнего email: {ex.Message}", MyLogger.LogCategory.Firebase);
                return string.Empty;
            }
        }

        public string GetSavedPassword()
        {
            try
            {
                return SecurePlayerPrefs.HasKey(PASSWORD_KEY) 
                    ? SecurePlayerPrefs.GetString(PASSWORD_KEY) 
                    : string.Empty;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка получения пароля: {ex.Message}", MyLogger.LogCategory.Firebase);
                return string.Empty;
            }
        }

        public bool IsRememberMeEnabled()
        {
            try
            {
                return SecurePlayerPrefs.HasKey(REMEMBER_KEY) && SecurePlayerPrefs.GetBool(REMEMBER_KEY);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка получения настройки 'Запомнить меня': {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }

        public void ClearStoredCredentials()
        {
            try
            {
                // Не удаляем LAST_EMAIL_KEY при очистке учетных данных
                SecurePlayerPrefs.DeleteKey(EMAIL_KEY);
                SecurePlayerPrefs.DeleteKey(PASSWORD_KEY);
                SecurePlayerPrefs.SetBool(REMEMBER_KEY, false);
                SecurePlayerPrefs.Save();
                MyLogger.Log("✅ Учетные данные очищены", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка очистки учетных данных: {ex.Message}", MyLogger.LogCategory.Firebase);
            }
        }
    }
}
