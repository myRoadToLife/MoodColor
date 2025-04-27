using System;
using System.Collections.Generic;
using App.Develop.CommonServices.Emotion;
using Newtonsoft.Json;
using UnityEngine;

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
        public float Value { get; set; }

        [JsonProperty("intensity")]
        public float Intensity { get; set; }

        [JsonProperty("colorHex")]
        public string ColorHex { get; set; }

        [JsonProperty("note")]
        public string Note { get; set; }

        [JsonProperty("regionId")]
        public string RegionId { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("color")]
        [JsonConverter(typeof(ColorHexConverter))]
        public Color Color { get; set; }

        // Новые поля для системы эмоций
        [JsonProperty("maxCapacity")]
        public float MaxCapacity { get; set; } = 100f;

        [JsonProperty("fillRate")]
        public float FillRate { get; set; } = 1f;

        [JsonProperty("drainRate")]
        public float DrainRate { get; set; } = 0.5f;

        [JsonProperty("bubbleThreshold")]
        public float BubbleThreshold { get; set; } = 80f;

        [JsonIgnore]
        public DateTime LastUpdate 
        { 
            get => DateTime.FromFileTimeUtc(Timestamp);
            set => Timestamp = value.ToFileTimeUtc();
        }

        public EmotionData()
        {
            Id = Guid.NewGuid().ToString();
            Timestamp = DateTime.UtcNow.ToFileTimeUtc();
            Color = Color.white;
            Value = 0f;
            Intensity = 0f;
        }

        public EmotionData(string type, float intensity = 0, float value = 0, string note = null, Color? color = null, string regionId = null)
        {
            Id = Guid.NewGuid().ToString();
            Type = type;
            Intensity = intensity;
            Value = value;
            Note = note;
            Color = color ?? Color.white;
            RegionId = regionId;
            Timestamp = DateTime.UtcNow.ToFileTimeUtc();
        }

        public EmotionData Clone()
        {
            return new EmotionData
            {
                Id = Id,
                Type = Type,
                Value = Value,
                Intensity = Intensity,
                Color = Color,
                ColorHex = ColorHex,
                Note = Note,
                RegionId = RegionId,
                Timestamp = Timestamp,
                MaxCapacity = MaxCapacity,
                FillRate = FillRate,
                DrainRate = DrainRate,
                BubbleThreshold = BubbleThreshold
            };
        }

        public bool Equals(EmotionData other)
        {
            if (other is null) return false;
            return Id == other.Id && 
                   Type == other.Type && 
                   Math.Abs(Value - other.Value) < float.Epsilon && 
                   Math.Abs(Intensity - other.Intensity) < float.Epsilon;
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
