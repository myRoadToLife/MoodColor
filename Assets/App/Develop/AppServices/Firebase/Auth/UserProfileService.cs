using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Auth
{
    public class UserProfileService
    {
        private readonly FirebaseManager _firebaseManager;
        private DatabaseReference _userProfileRef;
        private bool _isProfileComplete;

        public UserProfileService(FirebaseManager firebaseManager)
        {
            _firebaseManager = firebaseManager;
        }

        public void Initialize(string userId)
        {
            _userProfileRef = _firebaseManager.GetUserReference(userId);
            _userProfileRef.ValueChanged += HandleProfileChanged;
        }

        private void HandleProfileChanged(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError(args.DatabaseError.Message);
                return;
            }

            var snapshot = args.Snapshot;
            if (snapshot == null || !snapshot.Exists)
            {
                _isProfileComplete = false;
                return;
            }

            var profile = snapshot.Value as Dictionary<string, object>;
            _isProfileComplete = CheckProfileCompleteness(profile);
        }

        private bool CheckProfileCompleteness(Dictionary<string, object> profile)
        {
            if (profile == null) return false;

            var requiredFields = new[] { "email", "created_at", "total_points", "last_emotion" };
            foreach (var field in requiredFields)
            {
                if (!profile.ContainsKey(field) || profile[field] == null)
                    return false;
            }

            return true;
        }

        public bool IsProfileComplete() => _isProfileComplete;

        public void CheckUserProfileFilled(string userId, Action onProfileIncomplete, Action onProfileComplete)
        {
            var userRef = _firebaseManager.GetUserReference(userId);
            userRef.GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("Failed to check profile: " + task.Exception?.Message);
                    onProfileIncomplete?.Invoke();
                    return;
                }

                var snapshot = task.Result;
                if (!snapshot.Exists)
                {
                    onProfileIncomplete?.Invoke();
                    return;
                }

                var profile = snapshot.Value as Dictionary<string, object>;
                if (CheckProfileCompleteness(profile))
                {
                    onProfileComplete?.Invoke();
                }
                else
                {
                    onProfileIncomplete?.Invoke();
                }
            });
        }

        public void UpdateProfile(string userId, Dictionary<string, object> updates, Action onSuccess, Action<string> onError)
        {
            var userRef = _firebaseManager.GetUserReference(userId);
            userRef.UpdateChildrenAsync(updates)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                        onError?.Invoke(task.Exception?.Flatten().InnerException?.Message);
                    else
                        onSuccess?.Invoke();
                });
        }

        public void Cleanup()
        {
            if (_userProfileRef != null)
            {
                _userProfileRef.ValueChanged -= HandleProfileChanged;
            }
        }
    }
}
