using System;

namespace App.Develop.Services.Friends
{
    [Serializable]
    public class UserModel
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string AvatarUrl { get; set; }
        public bool IsOnline { get; set; }
        public bool IsFriend { get; set; }
        public bool HasPendingRequest { get; set; }
        
        public UserModel(string id, string username, string avatarUrl = null, bool isOnline = false, bool isFriend = false, bool hasPendingRequest = false)
        {
            Id = id;
            Username = username;
            AvatarUrl = avatarUrl;
            IsOnline = isOnline;
            IsFriend = isFriend;
            HasPendingRequest = hasPendingRequest;
        }
    }
}