using System;

namespace App.Develop.CommonServices.DataManagement.DataProviders
{
    [Serializable]
    public class SyncStatusData : ISaveData
    {
        public bool IsLastSyncSuccessful { get; set; }
        public long LastSyncTimestamp { get; set; }
        public string SyncErrorMessage { get; set; }
        
        public SyncStatusData()
        {
            // По умолчанию синхронизация не была успешной
            IsLastSyncSuccessful = false;
            LastSyncTimestamp = 0;
            SyncErrorMessage = string.Empty;
        }
    }
} 