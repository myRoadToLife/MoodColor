using Firebase.Auth;
using Firebase.Database;
using UnityEngine;

namespace App.Develop.AppServices.Firebase
{
    public class FirebaseManager
    {
        private readonly FirebaseAuth _auth;
        private readonly FirebaseDatabase _database;

        public FirebaseManager()
        {
            _auth = FirebaseAuth.DefaultInstance;
            _database = FirebaseDatabase.DefaultInstance;
        }

        public FirebaseAuth Auth => _auth;
        public FirebaseDatabase Database => _database;

        public DatabaseReference GetUserReference(string userId)
        {
            return _database.GetReference($"users/{userId}");
        }

        public DatabaseReference GetEmotionsReference(string userId)
        {
            return _database.GetReference($"users/{userId}/emotions");
        }

        public DatabaseReference GetStatisticsReference(string userId)
        {
            return _database.GetReference($"users/{userId}/statistics");
        }

        public DatabaseReference GetFriendsReference(string userId)
        {
            return _database.GetReference($"users/{userId}/friends");
        }

        public DatabaseReference GetCustomizationReference(string userId)
        {
            return _database.GetReference($"users/{userId}/customization");
        }
    }
} 