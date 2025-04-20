using Firebase.Auth;
using Firebase.Extensions;
using System;
using UnityEngine;

namespace App.Develop.AppServices.Auth
{
    public class AuthService
    {
        private readonly FirebaseAuth _auth;
        public AuthService(FirebaseAuth auth) => _auth = auth;

        public void RegisterUser(string email, string password, Action onSuccess, Action<string> onError)
        {
            _auth.CreateUserWithEmailAndPasswordAsync(email, password)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                        onError?.Invoke(task.Exception?.Flatten().InnerException?.Message);
                    else
                        onSuccess?.Invoke();
                });
        }

        public void LoginUser(string email, string password, Action<FirebaseUser> onSuccess, Action<string> onError)
        {
            _auth.SignInWithEmailAndPasswordAsync(email, password)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                        onError?.Invoke(task.Exception?.Flatten().InnerException?.Message);
                    else
                        onSuccess?.Invoke(_auth.CurrentUser);
                });
        }

        public void SendEmailVerification(Action onSuccess = null, Action onError = null)
        {
            var user = _auth.CurrentUser;
            if (user == null) { onError?.Invoke(); return; }
            user.SendEmailVerificationAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompletedSuccessfully) onSuccess?.Invoke();
                    else { Debug.LogError(task.Exception); onError?.Invoke(); }
                });
        }

        public void CheckEmailVerified(Action onVerified, Action onNotVerified)
        {
            var user = _auth.CurrentUser;
            if (user == null) { onNotVerified?.Invoke(); return; }
            user.ReloadAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompletedSuccessfully && user.IsEmailVerified)
                        onVerified?.Invoke();
                    else
                        onNotVerified?.Invoke();
                });
        }
    }
}
