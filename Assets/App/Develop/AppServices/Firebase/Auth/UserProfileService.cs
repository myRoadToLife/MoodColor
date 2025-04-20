using Firebase.Extensions;
using Firebase.Firestore;
using System;

namespace App.Develop.AppServices.Auth
{
    public class UserProfileService
    {
        private readonly FirebaseFirestore _db;
        public UserProfileService(FirebaseFirestore db) => _db = db;

        public void CheckUserProfileFilled(string uid, Action onProfileIncomplete, Action onProfileComplete)
        {
            _db.Collection("users").Document(uid)
                .GetSnapshotAsync().ContinueWithOnMainThread(task =>
                {
                    var doc = task.Result;
                    if (!doc.Exists 
                        || !doc.ContainsField("nickname") 
                        || !doc.ContainsField("gender"))
                        onProfileIncomplete?.Invoke();
                    else
                        onProfileComplete?.Invoke();
                });
        }
    }
}
