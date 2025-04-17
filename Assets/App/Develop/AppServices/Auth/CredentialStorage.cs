using UnityEngine;

namespace App.Develop.AppServices.Auth
{
    public class CredentialStorage
    {
        public CredentialStorage(string securityKey) => SecurePlayerPrefs.Init(securityKey);

        public void SaveCredentials(string email, string password, bool rememberMe)
        {
            SecurePlayerPrefs.SetString("email", email);
            if (rememberMe)
            {
                SecurePlayerPrefs.SetString("password", password);
                SecurePlayerPrefs.SetInt("remember_me", 1);
            }
            else
            {
                SecurePlayerPrefs.DeleteKey("password");
                SecurePlayerPrefs.SetInt("remember_me", 0);
            }
            SecurePlayerPrefs.Save();
        }

        public string GetSavedEmail()      => SecurePlayerPrefs.GetString("email", "");
        public string GetSavedPassword()   => SecurePlayerPrefs.GetString("password", "");
        public bool   IsRememberMeEnabled() => SecurePlayerPrefs.GetInt("remember_me", 0) == 1;

        public void ClearStoredCredentials()
        {
            SecurePlayerPrefs.DeleteKey("email");
            SecurePlayerPrefs.DeleteKey("password");
            SecurePlayerPrefs.DeleteKey("remember_me");
            SecurePlayerPrefs.Save();
        }
    }
}
