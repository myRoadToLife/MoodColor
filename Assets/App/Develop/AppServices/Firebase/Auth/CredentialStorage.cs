// Assets/App/Develop/AppServices/Firebase/Auth/CredentialStorage.cs
using System;
using App.Develop.AppServices.Firebase.Common.SecureStorage;
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Auth
{
    public class CredentialStorage
    {
        private const string EMAIL_KEY = "user_email";
        private const string PASSWORD_KEY = "user_password";
        private const string REMEMBER_KEY = "remember_me";

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
                Debug.Log("✅ Учетные данные сохранены");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка сохранения учетных данных: {ex.Message}");
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
                Debug.LogError($"❌ Ошибка получения email: {ex.Message}");
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
                Debug.LogError($"❌ Ошибка получения пароля: {ex.Message}");
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
                Debug.LogError($"❌ Ошибка получения настройки 'Запомнить меня': {ex.Message}");
                return false;
            }
        }

        public void ClearStoredCredentials()
        {
            try
            {
                SecurePlayerPrefs.DeleteKey(EMAIL_KEY);
                SecurePlayerPrefs.DeleteKey(PASSWORD_KEY);
                SecurePlayerPrefs.SetBool(REMEMBER_KEY, false);
                SecurePlayerPrefs.Save();
                Debug.Log("✅ Учетные данные очищены");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка очистки учетных данных: {ex.Message}");
            }
        }
    }
}