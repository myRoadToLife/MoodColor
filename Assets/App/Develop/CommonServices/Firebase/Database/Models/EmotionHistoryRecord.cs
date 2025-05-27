using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion;
using Newtonsoft.Json;

namespace App.Develop.CommonServices.Firebase.Database.Models
{
    [Serializable]
    public class EmotionHistoryRecord
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

        [JsonProperty("eventType")]
        public string EventType { get; set; }

        [JsonProperty("localId")]
        public string LocalId { get; set; }

        [JsonProperty("syncStatus")]
        public SyncStatus SyncStatus { get; set; }

        [JsonProperty("latitude")]
        public double? Latitude { get; set; }

        [JsonProperty("longitude")]
        public double? Longitude { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        [JsonIgnore]
        public DateTime RecordTime => DateTime.FromFileTimeUtc(Timestamp);

        public EmotionHistoryRecord()
        {
            Id = Guid.NewGuid().ToString();
            Timestamp = DateTime.UtcNow.ToFileTimeUtc();
            SyncStatus = SyncStatus.NotSynced;
            Tags = new List<string>();
        }

        public EmotionHistoryRecord(EmotionData emotion, EmotionEventType eventType) : this()
        {
            Type = emotion.Type;
            Value = emotion.Value;
            Intensity = emotion.Intensity;
            ColorHex = emotion.ColorHex;
            Note = emotion.Note;
            RegionId = emotion.RegionId;
            Timestamp = emotion.Timestamp;
            EventType = eventType.ToString();
            LocalId = emotion.Id;
        }

        public EmotionHistoryRecord(EmotionHistoryEntry entryToSave) : this()
        {
            Id = entryToSave.SyncId;
            Type = entryToSave.EmotionData.Type;
            Value = entryToSave.EmotionData.Value;
            Intensity = entryToSave.EmotionData.Intensity;
            ColorHex = entryToSave.EmotionData.ColorHex;
            Note = entryToSave.Note;
            RegionId = entryToSave.EmotionData.RegionId;
            Timestamp = entryToSave.Timestamp.ToFileTimeUtc();
            EventType = entryToSave.EventType.ToString();
            LocalId = entryToSave.EmotionData.Id;
            SyncStatus = entryToSave.IsSynced ? SyncStatus.Synced : SyncStatus.NotSynced;
        }

        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                ["id"] = Id,
                ["type"] = Type,
                ["value"] = Value,
                ["intensity"] = Intensity,
                ["colorHex"] = ColorHex,
                ["timestamp"] = Timestamp,
                ["eventType"] = EventType,
                ["syncStatus"] = SyncStatus.ToString()
            };

            if (!string.IsNullOrEmpty(Note))
                dict["note"] = Note;

            if (!string.IsNullOrEmpty(RegionId))
                dict["regionId"] = RegionId;

            if (!string.IsNullOrEmpty(LocalId))
                dict["localId"] = LocalId;

            if (Latitude.HasValue && Longitude.HasValue)
            {
                dict["latitude"] = Latitude.Value;
                dict["longitude"] = Longitude.Value;
            }

            if (Tags != null && Tags.Count > 0)
                dict["tags"] = Tags.ToList();

            return dict;
        }

        public EmotionData ToEmotionData()
        {
            var emotionData = new EmotionData
            {
                Id = LocalId ?? Id,
                Type = Type,
                Value = Value,
                Intensity = Intensity,
                ColorHex = ColorHex,
                Note = Note,
                RegionId = RegionId,
                Timestamp = Timestamp
            };
            
            return emotionData;
        }
    }

    public enum SyncStatus
    {
        NotSynced,
        Syncing,
        Synced,
        SyncFailed,
        Conflict
    }
} 