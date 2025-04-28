using System;
using System.Collections.Generic;
using App.Develop.CommonServices.DataManagement.DataProviders;
using Newtonsoft.Json;

namespace App.Develop.AppServices.Firebase.Database.Models
{
    [Serializable]
    public class UserData
    {
        [JsonProperty("profile")]
        public UserProfile Profile { get; set; }

        [JsonProperty("emotions")]
        public Dictionary<string, EmotionData> Emotions { get; set; }

        [JsonProperty("jars")]
        public Dictionary<string, JarData> Jars { get; set; }

        [JsonProperty("currentEmotion")]
        public CurrentEmotion CurrentEmotion { get; set; }
        
        [JsonProperty("emotionHistory")]
        public Dictionary<string, EmotionHistoryRecord> EmotionHistory { get; set; }
        
        [JsonProperty("syncSettings")]
        public EmotionSyncSettings SyncSettings { get; set; }
        
        [JsonProperty("lastDailySummaryDate")]
        public string LastDailySummaryDate { get; set; }

        public UserData()
        {
            Profile = new UserProfile();
            Emotions = new Dictionary<string, EmotionData>();
            Jars = new Dictionary<string, JarData>();
            CurrentEmotion = new CurrentEmotion();
            EmotionHistory = new Dictionary<string, EmotionHistoryRecord>();
            SyncSettings = new EmotionSyncSettings();
            LastDailySummaryDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        }
    }

    [Serializable]
    public class UserProfile
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("nickname")]
        public string Nickname { get; set; }

        [JsonProperty("gender")]
        public string Gender { get; set; }

        [JsonProperty("totalPoints")]
        public int TotalPoints { get; set; }

        [JsonProperty("createdAt")]
        public long CreatedAt { get; set; }

        [JsonProperty("lastActive")]
        public long LastActive { get; set; }

        [JsonProperty("settings")]
        public UserSettings Settings { get; set; } = new UserSettings();
    }

    [Serializable]
    public class UserSettings
    {
        public bool Notifications { get; set; } = true;
        public string Theme { get; set; } = "default";
        public bool Sound { get; set; } = true;

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["notifications"] = Notifications,
                ["theme"] = Theme,
                ["sound"] = Sound
            };
        }
    }


    [Serializable]
    public class CurrentEmotion
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("intensity")]
        public int Intensity { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }
    }
}