using System;
using Newtonsoft.Json;

namespace App.Develop.AppServices.Firebase.Database.Models
{
    [Serializable]
    public class EmotionSyncSettings
    {
        [JsonProperty("autoSync")]
        public bool AutoSync { get; set; } = true;
        
        [JsonProperty("syncInterval")]
        public int SyncIntervalMinutes { get; set; } = 30;
        
        [JsonProperty("syncOnWifi")]
        public bool SyncOnWifiOnly { get; set; } = false;
        
        [JsonProperty("lastSyncTimestamp")]
        public long LastSyncTimestamp { get; set; }
        
        [JsonProperty("maxRecordsPerSync")]
        public int MaxRecordsPerSync { get; set; } = 100;
        
        [JsonProperty("saveLocation")]
        public bool SaveLocation { get; set; } = false;
        
        [JsonProperty("conflictStrategy")]
        public ConflictResolutionStrategy ConflictStrategy { get; set; } = ConflictResolutionStrategy.ServerWins;
        
        [JsonProperty("backupEnabled")]
        public bool BackupEnabled { get; set; } = true;
        
        [JsonProperty("backupIntervalDays")]
        public int BackupIntervalDays { get; set; } = 7;
        
        [JsonProperty("lastBackupTimestamp")]
        public long LastBackupTimestamp { get; set; }
        
        [JsonProperty("maxCacheRecords")]
        public int MaxCacheRecords { get; set; } = 5000;
        
        [JsonIgnore]
        public DateTime LastSyncTime
        {
            get => LastSyncTimestamp > 0 ? DateTime.FromFileTimeUtc(LastSyncTimestamp) : DateTime.MinValue;
            set => LastSyncTimestamp = value.ToFileTimeUtc();
        }
        
        [JsonIgnore]
        public DateTime LastBackupTime
        {
            get => LastBackupTimestamp > 0 ? DateTime.FromFileTimeUtc(LastBackupTimestamp) : DateTime.MinValue;
            set => LastBackupTimestamp = value.ToFileTimeUtc();
        }
        
        [JsonIgnore]
        public TimeSpan SyncInterval => TimeSpan.FromMinutes(SyncIntervalMinutes);
        
        [JsonIgnore]
        public TimeSpan BackupInterval => TimeSpan.FromDays(BackupIntervalDays);
        
        public EmotionSyncSettings()
        {
            LastSyncTimestamp = 0;
            LastBackupTimestamp = 0;
        }
    }
    
    public enum ConflictResolutionStrategy
    {
        ServerWins,
        ClientWins,
        MostRecent,
        KeepBoth,
        AskUser
    }
} 