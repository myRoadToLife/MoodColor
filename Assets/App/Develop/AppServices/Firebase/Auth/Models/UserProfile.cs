using System;
using System.Collections.Generic;
using App.Develop.AppServices.Firebase.Database.Models;

namespace App.Develop.AppServices.Firebase.Auth.Models
{
    [Serializable]
    public class UserProfile
    {
        public string Email { get; set; }
        public string Nickname { get; set; }
        public long CreatedAt { get; set; }
        public long LastActive { get; set; }
        public int TotalPoints { get; set; }
        public UserSettings Settings { get; set; }

        public UserProfile()
        {
            Settings = new UserSettings();
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["email"] = Email,
                ["nickname"] = Nickname,
                ["createdAt"] = CreatedAt,
                ["lastActive"] = LastActive,
                ["totalPoints"] = TotalPoints,
                ["settings"] = Settings?.ToDictionary()
            };
        }
    }
}
