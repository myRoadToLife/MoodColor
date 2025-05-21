using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using App.Develop.CommonServices.Firebase.Database.Models;

namespace App.Develop.CommonServices.Social
{
    /// <summary>
    /// Основной интерфейс социальной системы
    /// </summary>
    public interface ISocialService
    {
        /// <summary>
        /// Событие изменения статуса дружбы
        /// </summary>
        event Action<string, FriendshipStatus> OnFriendshipStatusChanged;
        
        /// <summary>
        /// Событие получения нового уведомления
        /// </summary>
        event Action<SocialNotification> OnNotificationReceived;
        
        /// <summary>
        /// Добавить пользователя в друзья
        /// </summary>
        Task<bool> AddFriend(string userId);
        
        /// <summary>
        /// Удалить пользователя из друзей
        /// </summary>
        Task<bool> RemoveFriend(string userId);
        
        /// <summary>
        /// Получить список друзей
        /// </summary>
        Task<Dictionary<string, UserProfile>> GetFriendsList();
        
        /// <summary>
        /// Поиск пользователей
        /// </summary>
        Task<Dictionary<string, UserProfile>> SearchUsers(string searchQuery, int maxResults = 20);
        
        /// <summary>
        /// Получить статистику по региону
        /// </summary>
        Task<RegionalStats> GetRegionalStats(string regionCode);
        
        /// <summary>
        /// Отправить реакцию на эмоцию друга
        /// </summary>
        Task<bool> SendEmotionReaction(string userId, string emotionId, ReactionType reaction);
        
        /// <summary>
        /// Получить настройки приватности
        /// </summary>
        Task<PrivacySettings> GetPrivacySettings();
        
        /// <summary>
        /// Обновить настройки приватности
        /// </summary>
        Task<bool> UpdatePrivacySettings(PrivacySettings settings);
        
        /// <summary>
        /// Принять запрос в друзья
        /// </summary>
        Task<bool> AcceptFriendRequest(string userId);
    }

    public enum FriendshipStatus
    {
        None,
        Pending,
        Friend,
        Blocked
    }

    public enum ReactionType
    {
        Like,
        Support,
        Hug,
        Celebrate
    }

    public class SocialNotification
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }

    public enum NotificationType
    {
        FriendRequest,
        FriendAccepted,
        EmotionReaction,
        Achievement,
        System
    }

    public class PrivacySettings
    {
        public bool ShowEmotionsToFriends { get; set; }
        public bool ShowEmotionsToPublic { get; set; }
        public bool AllowFriendRequests { get; set; }
        public bool ShowOnlineStatus { get; set; }
        public bool AllowEmotionReactions { get; set; }
    }

    public class RegionalStats
    {
        public string RegionCode { get; set; }
        public Dictionary<string, int> EmotionDistribution { get; set; }
        public int TotalUsers { get; set; }
        public DateTime LastUpdated { get; set; }
    }
} 