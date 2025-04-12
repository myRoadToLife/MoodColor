using System.Collections.Generic;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

namespace App.Develop.AppServices.Firebase
{
    public class FirestoreManager : MonoBehaviour
    {
        public static FirestoreManager Instance;
        private FirebaseFirestore _db;

        private void Awake()
        {
            Instance = this;
            _db = FirebaseFirestore.DefaultInstance;
        }

        public void CreateNewUserDocument(string userId, string email)
        {
            DocumentReference docRef = _db.Collection("users").Document(userId);

            Dictionary<string, object> userData = new Dictionary<string, object>
            {
                { "email", email },
                { "created_at", Timestamp.GetCurrentTimestamp() },
                { "total_points", 0 },
                { "last_emotion", "" },
                { "emotions", new List<string>() }
            };

            docRef.SetAsync(userData).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("Профиль пользователя создан в Firestore.");
                }
            });
        }
    }
}
