using System;
using Newtonsoft.Json;
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Database.Models
{
    [Serializable]
    public class EmotionData
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("intensity")]
        public int Intensity { get; set; }

        [JsonProperty("colorHex")]
        public string ColorHex { get; set; }

        [JsonProperty("note")]
        public string Note { get; set; }

        [JsonProperty("regionId")]
        public string RegionId { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonIgnore]
        public Color UnityColor
        {
            get
            {
                ColorUtility.TryParseHtmlString(ColorHex, out Color color);
                return color;
            }
            set => ColorHex = "#" + ColorUtility.ToHtmlStringRGBA(value);
        }
    }
}
