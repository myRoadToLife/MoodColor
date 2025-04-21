using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace App.Develop.AppServices.Auth
{
    public class AuthService
    {
        private readonly FirebaseManager _firebaseManager;
        
        public AuthService(FirebaseManager firebaseManager)
        {
            _firebaseManager = firebaseManager;
        }

        public void RegisterUser(string email, string password, Action onSuccess, Action<string> onError)
        {
            _firebaseManager.Auth.CreateUserWithEmailAndPasswordAsync(email, password)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        onError?.Invoke(task.Exception?.Flatten().InnerException?.Message);
                        return;
                    }

                    var user = task.Result.User;
                    CreateUserProfile(user.UserId, email);
                    onSuccess?.Invoke();
                });
        }

        private void CreateUserProfile(string userId, string email)
        {
            var userRef = _firebaseManager.GetUserReference(userId);
            var userData = new Dictionary<string, object>
            {
                { "email", email },
                { "created_at", ServerValue.Timestamp },
                { "total_points", 0 },
                { "last_emotion", "" },
                { "emotions", new Dictionary<string, object>() },
                { "statistics", new Dictionary<string, object>() },
                { "friends", new Dictionary<string, object>() },
                { "customization", new Dictionary<string, object>() }
            };

            userRef.SetValueAsync(userData);
        }

        public void LoginUser(string email, string password, Action<FirebaseUser> onSuccess, Action<string> onError)
        {
            _firebaseManager.Auth.SignInWithEmailAndPasswordAsync(email, password)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                        onError?.Invoke(task.Exception?.Flatten().InnerException?.Message);
                    else
                        onSuccess?.Invoke(_firebaseManager.Auth.CurrentUser);
                });
        }

        public void SendEmailVerification(Action onSuccess = null, Action onError = null)
        {
            var user = _firebaseManager.Auth.CurrentUser;
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
            var user = _firebaseManager.Auth.CurrentUser;
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
