using Firebase.Extensions;
using Firebase.Firestore;
using System;

namespace App.Develop.AppServices.Auth
{
    public class UserProfileService
    {
        public void CheckUserProfileFilled(string uid, Action onProfileIncomplete, Action onProfileComplete)
        {
            FirebaseFirestore.DefaultInstance
                .Collection("users")
                .Document(uid)
                .GetSnapshotAsync().ContinueWithOnMainThread(task =>
                {
                    if (!task.Result.Exists ||
                        !task.Result.ContainsField("nickname") ||
                        !task.Result.ContainsField("gender"))
                    {
                        onProfileIncomplete?.Invoke();
                    }
                    else
                    {
                        onProfileComplete?.Invoke();
                    }
                });
        }
    }
}
