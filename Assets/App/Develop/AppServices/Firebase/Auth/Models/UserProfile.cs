// Assets/App/Develop/AppServices/Firebase/Database/Models/UserProfile.cs

using System;

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
    }

    [Serializable]
    public class UserSettings
    {
        public bool Notifications { get; set; } = true;
        public string Theme { get; set; } = "default";
        public bool Sound { get; set; } = true;
    }
}
