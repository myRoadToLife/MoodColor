using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using App.Develop.AppServices.Firebase.Database.Interfaces;
using App.Develop.AppServices.Firebase.Database.Models;
using App.Develop.AppServices.Firebase.Database.Services;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion;
using Firebase.Database;
using UnityEngine;
using App.Develop.AppServices.Firebase.Common.Cache;

namespace App.Develop.AppServices.Firebase.Database.Interfaces
{
    /// <summary>
    /// Реализация интерфейса IDatabaseService, адаптирующая DatabaseService
    /// </summary>
    public class DatabaseServiceImpl : IDatabaseService
    {
        #region Fields
        private readonly DatabaseService _databaseService;
        private string _userId;
        #endregion
        
        #region Properties
        public string UserId => _userId;
        
        public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);
        #endregion
        
        #region Constructor
        public DatabaseServiceImpl(DatabaseReference databaseReference, FirebaseCacheManager cacheManager, string userId = null)
        {
            _databaseService = new DatabaseService(databaseReference, cacheManager);
            _userId = userId;
            
            if (!string.IsNullOrEmpty(userId))
            {
                _databaseService.UpdateUserId(userId);
            }
        }
        #endregion
        
        #region Authentication
        public void SetCurrentUser(string userId)
        {
            _userId = userId;
            _databaseService.UpdateUserId(userId);
        }
        #endregion
        
        #region User Profile
        public async Task<UserProfile> GetUserProfile(string userId = null)
        {
            return await _databaseService.GetUserProfile(userId);
        }
        
        public async Task CreateUserProfile(string userId, string email, string nickname = null)
        {
            await _databaseService.CreateNewUser(userId, email);
            // Если есть никнейм, дополнительно обновляем профиль
            if (!string.IsNullOrEmpty(nickname))
            {
                var profile = await GetUserProfile(userId);
                if (profile != null)
                {
                    var updates = new Dictionary<string, object>
                    {
                        ["profile/nickname"] = nickname
                    };
                    await _databaseService.UpdateUserData(updates);
                }
            }
        }
        
        public async Task UpdateUserProfile(UserProfile profile)
        {
            var updates = new Dictionary<string, object>
            {
                ["profile/email"] = profile.Email,
                ["profile/nickname"] = profile.Nickname,
                ["profile/gender"] = profile.Gender,
                ["profile/totalPoints"] = profile.TotalPoints,
                ["profile/settings"] = profile.Settings?.ToDictionary()
            };
            
            await _databaseService.UpdateUserData(updates);
        }
        
        public async Task UpdateUserSettings(UserSettings settings)
        {
            var updates = new Dictionary<string, object>
            {
                ["profile/settings"] = settings.ToDictionary()
            };
            
            await _databaseService.UpdateUserData(updates);
        }
        
        public async Task DeleteUserProfile()
        {
            // Реализовать, если требуется
            Debug.LogWarning("DeleteUserProfile не реализован");
            await Task.CompletedTask;
        }
        #endregion
        
        #region Emotions
        public async Task<Dictionary<string, EmotionData>> GetUserEmotions()
        {
            return await _databaseService.GetUserEmotions();
        }
        
        public async Task UpdateCurrentEmotion(string emotionType, float intensity)
        {
            await _databaseService.UpdateCurrentEmotion(emotionType, intensity);
        }
        
        public async Task<CurrentEmotion> GetCurrentEmotion()
        {
            // Реализовать адаптацию к методам DatabaseService
            Debug.LogWarning("GetCurrentEmotion не реализован");
            return new CurrentEmotion();
        }
        #endregion
        
        #region Jars
        public async Task<Dictionary<string, JarData>> GetUserJars()
        {
            return await _databaseService.GetUserJars();
        }
        
        public async Task UpdateJar(string jarType, JarData jarData)
        {
            // Реализовать адаптацию к методам DatabaseService
            var updates = new Dictionary<string, object>
            {
                [$"jars/{jarType}"] = jarData.ToDictionary()
            };
            
            await _databaseService.UpdateUserData(updates);
        }
        
        public async Task UpdateJarAmount(string jarType, int amount)
        {
            await _databaseService.UpdateJarAmount(jarType, amount);
        }
        #endregion
        
        #region Emotion History
        public async Task<List<EmotionHistoryRecord>> GetEmotionHistory(DateTime? startDate = null, DateTime? endDate = null, int limit = 100)
        {
            return await _databaseService.GetEmotionHistory(startDate, endDate, limit);
        }
        
        public async Task<List<EmotionHistoryRecord>> GetEmotionHistoryByType(string emotionType, DateTime? startDate = null, DateTime? endDate = null, int limit = 100)
        {
            return await _databaseService.GetEmotionHistoryByType(emotionType, startDate, endDate, limit);
        }
        
        public async Task AddEmotionHistoryRecord(EmotionHistoryRecord record)
        {
            await _databaseService.AddEmotionHistoryRecord(record);
        }
        
        public async Task AddEmotionHistoryRecord(EmotionData emotion, EmotionEventType eventType)
        {
            await _databaseService.AddEmotionHistoryRecord(emotion, eventType);
        }
        
        public async Task AddEmotionHistoryBatch(List<EmotionHistoryRecord> records)
        {
            await _databaseService.AddEmotionHistoryBatch(records);
        }
        
        public async Task<List<EmotionHistoryRecord>> GetUnsyncedEmotionRecords(int limit = 100)
        {
            return await _databaseService.GetUnsyncedEmotionRecords(limit);
        }
        
        public async Task UpdateEmotionSyncStatus(string recordId, SyncStatus status)
        {
            await _databaseService.UpdateEmotionSyncStatus(recordId, status);
        }
        
        public async Task DeleteEmotionHistoryRecord(string recordId)
        {
            await _databaseService.DeleteEmotionHistoryRecord(recordId);
        }
        
        public async Task<Dictionary<string, int>> GetEmotionStatistics(DateTime startDate, DateTime endDate)
        {
            return await _databaseService.GetEmotionStatistics(startDate, endDate);
        }
        #endregion
        
        #region Sync Settings
        public async Task<EmotionSyncSettings> GetSyncSettings()
        {
            return await _databaseService.GetSyncSettings();
        }
        
        public async Task UpdateSyncSettings(EmotionSyncSettings settings)
        {
            await _databaseService.UpdateSyncSettings(settings);
        }
        #endregion
        
        #region Listeners
        public async Task<DatabaseReference> SubscribeToEmotions(EventHandler<EmotionData> callback)
        {
            // Реализовать адаптацию к методам DatabaseService
            Debug.LogWarning("SubscribeToEmotions не реализован");
            return null;
        }
        
        public async Task<DatabaseReference> SubscribeToCurrentEmotion(EventHandler<CurrentEmotion> callback)
        {
            // Реализовать адаптацию к методам DatabaseService
            Debug.LogWarning("SubscribeToCurrentEmotion не реализован");
            return null;
        }
        
        public async Task<DatabaseReference> SubscribeToJars(EventHandler<Dictionary<string, JarData>> callback)
        {
            // Реализовать адаптацию к методам DatabaseService
            Debug.LogWarning("SubscribeToJars не реализован");
            return null;
        }
        
        public async Task<DatabaseReference> SubscribeToSyncSettings(EventHandler<EmotionSyncSettings> callback)
        {
            // Реализовать адаптацию к методам DatabaseService
            Debug.LogWarning("SubscribeToSyncSettings не реализован");
            return null;
        }
        
        public void Unsubscribe(DatabaseReference reference)
        {
            // Реализовать адаптацию к методам DatabaseService
            Debug.LogWarning("Unsubscribe не реализован");
        }
        #endregion
        
        #region General
        public async Task UpdateUserData(Dictionary<string, object> updates)
        {
            await _databaseService.UpdateUserData(updates);
        }
        
        public async Task<string> CreateBackup()
        {
            return await _databaseService.CreateBackup();
        }
        
        public async Task RestoreFromBackup(string backupId)
        {
            await _databaseService.RestoreFromBackup(backupId);
        }
        #endregion
        
        #region IDisposable
        public void Dispose()
        {
            _databaseService?.Dispose();
        }
        #endregion
    }
} 