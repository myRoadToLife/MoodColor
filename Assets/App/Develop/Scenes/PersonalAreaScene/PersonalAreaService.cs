// PersonalAreaService.cs

using App.Develop.AppServices.Auth;
using App.Develop.AppServices.Firebase;
using App.Develop.CommonServices.DataManagement.DataProviders;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace App.Develop.Scenes.PersonalAreaScene
{
    public interface IPersonalAreaService
    {
        IReadOnlyVariable<EmotionData> GetEmotionVariable(EmotionTypes type);
        void AddEmotion(EmotionTypes type, int amount);
        void SpendEmotion(EmotionTypes type, int amount);
    }

    public class PersonalAreaService : IPersonalAreaService
    {
        private readonly FirebaseManager _firebaseManager;
        private readonly PlayerDataProvider _playerDataProvider;
        private DatabaseReference _userRef;

        public PersonalAreaService(FirebaseManager firebaseManager, PlayerDataProvider playerDataProvider)
        {
            _firebaseManager = firebaseManager;
            _playerDataProvider = playerDataProvider;
        }

        public void Initialize(string userId)
        {
            _userRef = _firebaseManager.GetUserReference(userId);
        }

        public async Task<Dictionary<string, object>> GetUserDataAsync()
        {
            var snapshot = await _userRef.GetValueAsync();
            if (!snapshot.Exists)
            {
                Debug.LogError("User data not found");
                return null;
            }

            return snapshot.Value as Dictionary<string, object>;
        }

        public async Task UpdateUserDataAsync(Dictionary<string, object> updates)
        {
            await _userRef.UpdateChildrenAsync(updates);
        }

        public async Task<List<Dictionary<string, object>>> GetEmotionsAsync()
        {
            var emotionsRef = _firebaseManager.GetEmotionsReference(_playerDataProvider.Data.UserId);
            var snapshot = await emotionsRef.GetValueAsync();
            
            if (!snapshot.Exists)
                return new List<Dictionary<string, object>>();

            var emotions = new List<Dictionary<string, object>>();
            foreach (var child in snapshot.Children)
            {
                emotions.Add(child.Value as Dictionary<string, object>);
            }

            return emotions;
        }

        public async Task<Dictionary<string, object>> GetStatisticsAsync()
        {
            var statsRef = _firebaseManager.GetStatisticsReference(_playerDataProvider.Data.UserId);
            var snapshot = await statsRef.GetValueAsync();
            
            if (!snapshot.Exists)
                return new Dictionary<string, object>();

            return snapshot.Value as Dictionary<string, object>;
        }

        public async Task<List<Dictionary<string, object>>> GetFriendsAsync()
        {
            var friendsRef = _firebaseManager.GetFriendsReference(_playerDataProvider.Data.UserId);
            var snapshot = await friendsRef.GetValueAsync();
            
            if (!snapshot.Exists)
                return new List<Dictionary<string, object>>();

            var friends = new List<Dictionary<string, object>>();
            foreach (var child in snapshot.Children)
            {
                friends.Add(child.Value as Dictionary<string, object>);
            }

            return friends;
        }

        public async Task<Dictionary<string, object>> GetCustomizationAsync()
        {
            var customizationRef = _firebaseManager.GetCustomizationReference(_playerDataProvider.Data.UserId);
            var snapshot = await customizationRef.GetValueAsync();
            
            if (!snapshot.Exists)
                return new Dictionary<string, object>();

            return snapshot.Value as Dictionary<string, object>;
        }

        public IReadOnlyVariable<EmotionData> GetEmotionVariable(EmotionTypes type)
        {
            if (!System.Enum.IsDefined(typeof(EmotionTypes), type))
            {
                Debug.LogWarning($"Неизвестный тип эмоции: {type}");
                return null;
            }

            return _emotionService.GetEmotion(type);
        }

        public void AddEmotion(EmotionTypes type, int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"Попытка добавить неположительное количество эмоций: {amount}");
                return;
            }

            if (!System.Enum.IsDefined(typeof(EmotionTypes), type))
            {
                Debug.LogWarning($"Неизвестный тип эмоции: {type}");
                return;
            }

            _emotionService.AddEmotion(type, amount);
        }

        public void SpendEmotion(EmotionTypes type, int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"Попытка потратить неположительное количество эмоций: {amount}");
                return;
            }

            if (!System.Enum.IsDefined(typeof(EmotionTypes), type))
            {
                Debug.LogWarning($"Неизвестный тип эмоции: {type}");
                return;
            }

            _emotionService.SpendEmotion(type, amount);
        }
    }
}
