using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Auth;
using UnityEngine;
using App.Develop.CommonServices.Firebase.Database.Models;
using UserProfile = App.Develop.CommonServices.Firebase.Database.Models.UserProfile;

namespace App.Develop.CommonServices.Social
{
    public class FirebaseSocialService : MonoBehaviour, ISocialService
    {
        private DatabaseReference _database;
        private FirebaseAuth _auth;
        private string _userId;

        public event Action<string, FriendshipStatus> OnFriendshipStatusChanged;
        public event Action<SocialNotification> OnNotificationReceived;

        public void Initialize(FirebaseDatabase database, FirebaseAuth auth)
        {
            _database = database.RootReference;
            _auth = auth;
            _userId = _auth.CurrentUser?.UserId;

            // Подписываемся на уведомления в реальном времени
            ListenToNotifications();
            ListenToFriendshipChanges();
        }

        private void OnDestroy()
        {
            // Отписываемся от событий Firebase
            if (!string.IsNullOrEmpty(_userId))
            {
                _database.Child("notifications").Child(_userId).ValueChanged -= HandleNotification;
                _database.Child("friendships").Child(_userId).ValueChanged -= HandleFriendshipChange;
            }
        }

        private void ListenToNotifications()
        {
            if (string.IsNullOrEmpty(_userId)) return;

            _database.Child("notifications").Child(_userId).ValueChanged += HandleNotification;
        }

        private void HandleNotification(object sender, ValueChangedEventArgs args)
        {
            try
            {
                if (args?.Snapshot == null) return;
                if (!args.Snapshot.Exists) return;

                var rawJson = args.Snapshot.GetRawJsonValue();
                if (string.IsNullOrEmpty(rawJson)) return;

                var notification = JsonUtility.FromJson<SocialNotification>(rawJson);
                if (notification == null) return;
            
                OnNotificationReceived?.Invoke(notification);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error handling notification: {e.Message}");
            }
        }

        private void ListenToFriendshipChanges()
        {
            if (string.IsNullOrEmpty(_userId)) return;

            _database.Child("friendships").Child(_userId).ValueChanged += HandleFriendshipChange;
        }

        private void HandleFriendshipChange(object sender, ValueChangedEventArgs args)
        {
            try
            {
                if (args?.Snapshot == null) return;
                if (!args.Snapshot.Exists) return;

                var children = args.Snapshot.Children?.ToList();
                if (children == null || !children.Any()) return;

                foreach (var child in children)
                {
                    if (child == null) continue;
                    
                    var friendId = child.Key;
                    var statusValue = child.Value?.ToString();
                    
                    if (string.IsNullOrEmpty(friendId) || string.IsNullOrEmpty(statusValue)) continue;

                    if (Enum.TryParse(statusValue, out FriendshipStatus status))
                    {
                        OnFriendshipStatusChanged?.Invoke(friendId, status);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error handling friendship change: {e.Message}");
            }
        }

        public async Task<bool> AddFriend(string userId)
        {
            try
            {
                var updates = new Dictionary<string, object>
                {
                    [$"friendships/{_userId}/{userId}"] = FriendshipStatus.Pending.ToString(),
                    [$"notifications/{userId}"] = new SocialNotification
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = "Новый запрос в друзья",
                        Message = "У вас новый запрос в друзья",
                        Type = NotificationType.FriendRequest,
                        CreatedAt = DateTime.UtcNow,
                        Data = new Dictionary<string, string> { { "senderId", _userId } }
                    }
                };

                await _database.UpdateChildrenAsync(updates);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error adding friend: {e.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveFriend(string userId)
        {
            try
            {
                var updates = new Dictionary<string, object>
                {
                    [$"friendships/{_userId}/{userId}"] = null,
                    [$"friendships/{userId}/{_userId}"] = null
                };

                await _database.UpdateChildrenAsync(updates);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error removing friend: {e.Message}");
                return false;
            }
        }

        public async Task<Dictionary<string, UserProfile>> GetFriendsList()
        {
            try
            {
                var snapshot = await _database.Child("friendships")
                    .Child(_userId)
                    .GetValueAsync();

                if (!snapshot.Exists) return new Dictionary<string, UserProfile>();

                var friends = new Dictionary<string, UserProfile>();
                foreach (var child in snapshot.Children)
                {
                    if ((FriendshipStatus)Enum.Parse(typeof(FriendshipStatus), 
                        child.Value.ToString()) == FriendshipStatus.Friend)
                    {
                        string friendId = child.Key;
                        var friendProfile = await GetUserProfile(friendId);
                        if (friendProfile != null)
                        {
                            friends[friendId] = friendProfile;
                        }
                    }
                }

                return friends;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting friends list: {e.Message}");
                return new Dictionary<string, UserProfile>();
            }
        }

        public async Task<Dictionary<string, UserProfile>> SearchUsers(string searchQuery, int maxResults = 20)
        {
            try
            {
                var snapshot = await _database.Child("users")
                    .OrderByChild("displayName")
                    .StartAt(searchQuery)
                    .EndAt(searchQuery + "\uf8ff")
                    .LimitToFirst(maxResults)
                    .GetValueAsync();

                if (!snapshot.Exists) return new Dictionary<string, UserProfile>();

                var results = new Dictionary<string, UserProfile>();
                foreach (var child in snapshot.Children)
                {
                    string userId = child.Key;
                    UserProfile profile = JsonUtility.FromJson<UserProfile>(child.GetRawJsonValue());
                    if (profile != null)
                    {
                        results[userId] = profile;
                    }
                }

                return results;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error searching users: {e.Message}");
                return new Dictionary<string, UserProfile>();
            }
        }

        public async Task<RegionalStats> GetRegionalStats(string regionCode)
        {
            try
            {
                var snapshot = await _database.Child("regionalStats")
                    .Child(regionCode)
                    .GetValueAsync();

                if (!snapshot.Exists) return null;

                return JsonUtility.FromJson<RegionalStats>(snapshot.GetRawJsonValue());
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting regional stats: {e.Message}");
                return null;
            }
        }

        public async Task<bool> SendEmotionReaction(string userId, string emotionId, ReactionType reaction)
        {
            try
            {
                var reactionData = new Dictionary<string, object>
                {
                    ["userId"] = _userId,
                    ["emotionId"] = emotionId,
                    ["reactionType"] = reaction.ToString(),
                    ["timestamp"] = DateTime.UtcNow.ToString("O")
                };

                await _database.Child("emotionReactions")
                    .Child(userId)
                    .Child(emotionId)
                    .Child(_userId)
                    .SetValueAsync(reactionData);

                // Отправляем уведомление
                var notification = new SocialNotification
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Новая реакция",
                    Message = $"Кто-то отреагировал на вашу эмоцию",
                    Type = NotificationType.EmotionReaction,
                    CreatedAt = DateTime.UtcNow,
                    Data = new Dictionary<string, string>
                    {
                        { "emotionId", emotionId },
                        { "reactionType", reaction.ToString() }
                    }
                };

                await _database.Child("notifications")
                    .Child(userId)
                    .Push()
                    .SetValueAsync(JsonUtility.ToJson(notification));

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error sending emotion reaction: {e.Message}");
                return false;
            }
        }

        public async Task<PrivacySettings> GetPrivacySettings()
        {
            try
            {
                var snapshot = await _database.Child("privacySettings")
                    .Child(_userId)
                    .GetValueAsync();

                if (!snapshot.Exists)
                {
                    return new PrivacySettings
                    {
                        ShowEmotionsToFriends = true,
                        ShowEmotionsToPublic = false,
                        AllowFriendRequests = true,
                        ShowOnlineStatus = true,
                        AllowEmotionReactions = true
                    };
                }

                return JsonUtility.FromJson<PrivacySettings>(snapshot.GetRawJsonValue());
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting privacy settings: {e.Message}");
                return null;
            }
        }

        public async Task<bool> UpdatePrivacySettings(PrivacySettings settings)
        {
            try
            {
                await _database.Child("privacySettings")
                    .Child(_userId)
                    .SetValueAsync(JsonUtility.ToJson(settings));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error updating privacy settings: {e.Message}");
                return false;
            }
        }

        private async Task<UserProfile> GetUserProfile(string userId)
        {
            try
            {
                var snapshot = await _database.Child("users")
                    .Child(userId)
                    .GetValueAsync();

                if (!snapshot.Exists) return null;

                return JsonUtility.FromJson<UserProfile>(snapshot.GetRawJsonValue());
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting user profile: {e.Message}");
                return null;
            }
        }

        public async Task<bool> AcceptFriendRequest(string userId)
        {
            try
            {
                var updates = new Dictionary<string, object>
                {
                    [$"friendships/{_userId}/{userId}"] = FriendshipStatus.Friend.ToString(),
                    [$"friendships/{userId}/{_userId}"] = FriendshipStatus.Friend.ToString(),
                    [$"notifications/{userId}"] = new SocialNotification
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = "Запрос в друзья принят",
                        Message = "Ваш запрос в друзья был принят",
                        Type = NotificationType.FriendAccepted,
                        CreatedAt = DateTime.UtcNow,
                        Data = new Dictionary<string, string> { { "receiverId", _userId } }
                    }
                };

                await _database.UpdateChildrenAsync(updates);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error accepting friend request: {e.Message}");
                return false;
            }
        }
    }
} 