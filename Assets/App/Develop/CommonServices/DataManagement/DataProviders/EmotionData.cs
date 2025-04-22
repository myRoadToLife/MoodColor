using System;
using System.Collections.Generic;
using App.Develop.CommonServices.Emotion;
using Newtonsoft.Json;
using UnityEngine;
using App.Develop.CommonServices.DataManagement.DataProviders;

namespace App.Develop.CommonServices.DataManagement.DataProviders
{
    [Serializable]
    public class EmotionData : IEquatable<EmotionData>
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public int Value { get; set; }

        [JsonProperty("intensity")]
        public int Intensity { get; set; }

        [JsonConverter(typeof(ColorHexConverter))]
        [JsonProperty("colorHex")]
        public string ColorHex { get; set; }

        [JsonProperty("note")]
        public string Note { get; set; }

        [JsonProperty("regionId")]
        public string RegionId { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonIgnore]
        public Color Color
        {
            get
            {
                if (string.IsNullOrEmpty(ColorHex)) return Color.clear;
                ColorUtility.TryParseHtmlString(ColorHex, out Color color);
                return color;
            }
            set => ColorHex = "#" + ColorUtility.ToHtmlStringRGBA(value);
        }

        public EmotionData()
        {
            Id = Guid.NewGuid().ToString();
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public EmotionData(string type, int intensity = 0, int value = 0, string note = null, Color? color = null, string regionId = null)
        {
            Id = Guid.NewGuid().ToString();
            Type = type;
            Intensity = intensity;
            Value = value;
            Note = note;
            Color = color ?? Color.clear;
            RegionId = regionId;
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public bool Equals(EmotionData other)
        {
            if (other is null) return false;
            return Id == other.Id && 
                   Type == other.Type && 
                   Value == other.Value && 
                   Intensity == other.Intensity;
        }

        public override bool Equals(object obj) => Equals(obj as EmotionData);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (Type?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ Value.GetHashCode();
                hashCode = (hashCode * 397) ^ Intensity.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(EmotionData left, EmotionData right) =>
            ReferenceEquals(left, right) || (left?.Equals(right) ?? false);

        public static bool operator !=(EmotionData left, EmotionData right) =>
            !(left == right);
    }
    
}
