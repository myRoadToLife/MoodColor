using System;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine;

namespace App.Develop.AppServices.Firebase
{
    public class FirestoreManager
    {
        private readonly FirebaseFirestore _db;

        public FirestoreManager()
        {
            _db = FirebaseFirestore.DefaultInstance;
        }

        public void CreateNewUserDocument(string userId, string email, Action onSuccess, Action<string> onFailure)
        {
            var userRef = _db.Collection("users").Document(userId);

            Dictionary<string, object> userData = new()
            {
                { "email", email },
                { "created_at", Timestamp.GetCurrentTimestamp() },
                { "total_points", 0 },
                { "last_emotion", "" },
                { "emotions", new List<string>() }
            };

            userRef.SetAsync(userData).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log("Профиль пользователя успешно создан в Firestore");
                    onSuccess?.Invoke();
                }
                else
                {
                    Debug.LogError("Ошибка при создании профиля в Firestore: " + task.Exception?.Message);
                    onFailure?.Invoke(task.Exception?.Message);
                }
            });
        }
    }
}
