using System;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Auth
{
    public class AuthService
    {
        private readonly FirebaseManager _firebaseManager;
        private FirebaseAuth _auth;
        
        public AuthService(FirebaseManager firebaseManager)
        {
            _firebaseManager = firebaseManager;
            _auth = FirebaseAuth.DefaultInstance;
        }

        public async Task RegisterUser(string email, string password, Action<FirebaseUser> onSuccess, Action<string> onError)
        {
            try
            {
                var result = await _auth.CreateUserWithEmailAndPasswordAsync(email, password);
                if (result != null && result.User != null)
                {
                    await CreateUserProfile(result.User);
                    onSuccess?.Invoke(result.User);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error registering user: {e.Message}");
                onError?.Invoke(e.Message);
            }
        }

        private async Task CreateUserProfile(FirebaseUser user)
        {
            try
            {
                var userRef = _firebaseManager.GetUserReference(user.UserId);
                await userRef.Child("profile").SetValueAsync(new
                {
                    email = user.Email,
                    createdAt = DateTime.UtcNow.ToString("o"),
                    isEmailVerified = false
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"Error creating user profile: {e.Message}");
            }
        }

        public async Task LoginUser(string email, string password, Action<FirebaseUser> onSuccess, Action<string> onError)
        {
            try
            {
                var result = await _auth.SignInWithEmailAndPasswordAsync(email, password);
                if (result != null && result.User != null)
                {
                    onSuccess?.Invoke(result.User);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error logging in: {e.Message}");
                onError?.Invoke(e.Message);
            }
        }

        public async Task SendEmailVerification(FirebaseUser user, Action onSuccess, Action<string> onError)
        {
            try
            {
                await user.SendEmailVerificationAsync();
                onSuccess?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error sending email verification: {e.Message}");
                onError?.Invoke(e.Message);
            }
        }

        public async Task<bool> CheckEmailVerified(FirebaseUser user)
        {
            try
            {
                await user.ReloadAsync();
                return user.IsEmailVerified;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error checking email verification: {e.Message}");
                return false;
            }
        }
    }
}
