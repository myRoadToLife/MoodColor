using UnityEngine;

namespace App.Develop.AppServices.Auth
{
    public class CredentialStorage
    {
        public CredentialStorage(string securityKey)
        {
            SecurePlayerPrefs.Init(securityKey);
            Debug.Log("🔐 SecurePlayerPrefs инициализирован");
        }

        public void SaveCredentials(string email, string password, bool rememberMe)
        {
            SecurePlayerPrefs.SetString("email", email);
            Debug.Log($"💾 Сохраняем email: {email}");

            if (rememberMe)
            {
                SecurePlayerPrefs.SetString("password", password);
                SecurePlayerPrefs.SetInt("remember_me", 1);
                Debug.Log("✅ Пароль сохранён (remember_me включён)");
            }
            else
            {
                SecurePlayerPrefs.DeleteKey("password");
                SecurePlayerPrefs.SetInt("remember_me", 0);
                Debug.Log("ℹ️ Пароль не сохранён (remember_me выключен)");
            }

            SecurePlayerPrefs.Save();
        }

        public string GetSavedEmail()
        {
            return SecurePlayerPrefs.GetString("email", "");
        }

        public string GetSavedPassword()
        {
            return SecurePlayerPrefs.GetString("password", "");
        }

        public bool IsRememberMeEnabled()
        {
            return SecurePlayerPrefs.GetInt("remember_me", 0) == 1;
        }

        public void ClearStoredCredentials()
        {
            Debug.Log("🧹 Удаление сохранённых данных авторизации (email, password, remember_me)");
            SecurePlayerPrefs.DeleteKey("email");
            SecurePlayerPrefs.DeleteKey("password");
            SecurePlayerPrefs.DeleteKey("remember_me");
            SecurePlayerPrefs.Save();
        }
    }
}
