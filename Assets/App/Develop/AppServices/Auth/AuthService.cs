using Firebase.Auth;
using Firebase.Extensions;
using System;
using UnityEngine;

namespace App.Develop.AppServices.Auth
{
    public class AuthService
    {
        private readonly FirebaseAuth _auth;

        public AuthService()
        {
            _auth = FirebaseAuth.DefaultInstance;
        }

        public void RegisterUser(string email, string password, Action onSuccess, Action<string> onError)
        {
            _auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    onError?.Invoke(task.Exception?.Flatten().InnerException?.Message);
                    return;
                }

                onSuccess?.Invoke();
            });
        }

        public void LoginUser(string email, string password, Action<FirebaseUser> onSuccess, Action<string> onError)
        {
            _auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    onError?.Invoke(task.Exception?.Flatten().InnerException?.Message);
                    return;
                }

                onSuccess?.Invoke(_auth.CurrentUser);
            });
        }

        public void CheckEmailVerified(Action onVerified, Action onNotVerified)
        {
            if (_auth.CurrentUser == null)
            {
                onNotVerified?.Invoke();
                return;
            }

            _auth.CurrentUser.ReloadAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully && _auth.CurrentUser.IsEmailVerified)
                {
                    onVerified?.Invoke();
                }
                else
                {
                    onNotVerified?.Invoke();
                }
            });
        }

        public void SendEmailVerification(Action onSuccess = null, Action onError = null)
        {
            var user = _auth.CurrentUser;

            if (user == null)
            {
                onError?.Invoke();
                return;
            }

            user.SendEmailVerificationAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log("📨 Email подтверждение отправлено.");
                    onSuccess?.Invoke();
                }
                else
                {
                    Debug.LogError("❌ Ошибка отправки письма: " + task.Exception?.Message);
                    onError?.Invoke();
                }
            });
        }
    }
}
