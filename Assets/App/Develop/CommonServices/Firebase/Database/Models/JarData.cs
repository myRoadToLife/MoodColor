using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace App.Develop.AppServices.Firebase.Database.Models
{
    [Serializable]
    public class JarData
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("capacity")]
        public int Capacity { get; set; }

        [JsonProperty("currentAmount")]
        public int CurrentAmount { get; set; }

        [JsonProperty("customization")]
        public JarCustomization Customization { get; set; } = new JarCustomization();

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["type"] = Type,
                ["level"] = Level,
                ["capacity"] = Capacity,
                ["currentAmount"] = CurrentAmount,
                ["customization"] = Customization?.ToDictionary()
            };
        }
    }


    [Serializable]
    public class JarCustomization
    {
        [JsonProperty("color")]
        public string Color { get; set; } = "default";

        [JsonProperty("pattern")]
        public string Pattern { get; set; } = "default";

        [JsonProperty("effects")]
        public List<string> Effects { get; set; } = new List<string>();

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["color"] = Color,
                ["pattern"] = Pattern,
                ["effects"] = Effects
            };
        }
    }

}
