using Firebase.Auth;
using Firebase.Database;
using UnityEngine;

namespace App.Develop.AppServices.Firebase
{
    public class FirebaseManager
    {
        private FirebaseAuth _auth;
        private FirebaseDatabase _database;
        private DatabaseReference _rootReference;

        public FirebaseAuth Auth => _auth;
        public FirebaseDatabase Database => _database;

        public FirebaseManager()
        {
            InitializeFirebase();
        }

        private void InitializeFirebase()
        {
            _auth = FirebaseAuth.DefaultInstance;
            _database = FirebaseDatabase.DefaultInstance;
            _rootReference = _database.RootReference;

            // Устанавливаем правила для эмулятора, если он используется
            if (Application.isEditor)
            {
                _database.SetPersistenceEnabled(false);
            }
        }

        public DatabaseReference GetUserReference(string userId)
        {
            return _rootReference.Child("users").Child(userId);
        }

        public DatabaseReference GetEmotionsReference(string userId)
        {
            return GetUserReference(userId).Child("emotions");
        }

        public DatabaseReference GetStatisticsReference(string userId)
        {
            return GetUserReference(userId).Child("statistics");
        }

        public DatabaseReference GetFriendsReference(string userId)
        {
            return GetUserReference(userId).Child("friends");
        }

        public DatabaseReference GetCustomizationReference(string userId)
        {
            return GetUserReference(userId).Child("customization");
        }
    }
} 