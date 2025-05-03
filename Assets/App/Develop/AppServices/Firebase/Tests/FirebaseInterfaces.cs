#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace App.Develop.AppServices.Firebase.Tests
{
    /// <summary>
    /// Интерфейс для сервиса Firebase базы данных
    /// </summary>
    public interface IFirebaseDatabaseService
    {
        Task<T> GetDataAsync<T>(string path);
        Task<bool> SetDataAsync<T>(string path, T data);
        Task<bool> UpdateDataAsync<T>(string path, T data);
        Task<bool> DeleteDataAsync(string path);
        Task<bool> PushDataAsync<T>(string path, T data);
        void AddRealtimeListener<T>(string path, Action<T> onDataChanged);
        void RemoveRealtimeListener(string path);
        Task<bool> SaveDataAsync<T>(string path, T data);
    }

    /// <summary>
    /// Интерфейс для управления соединением Firebase
    /// </summary>
    public interface IFirebaseConnectionManager
    {
        bool IsConnected { get; }
        void ForceDisconnect();
        void ForceReconnect();
        Task<bool> WaitForConnectionAsync(float timeoutSeconds);
    }

    /// <summary>
    /// Интерфейс для управления кэшем Firebase
    /// </summary>
    public interface IFirebaseCacheManager
    {
        Task<bool> ClearCacheAsync();
        Task<long> GetCacheSizeAsync();
        Task<bool> SetCacheSizeLimitAsync(long sizeInBytes);
        bool IsCachingEnabled { get; set; }
        Task<T> GetCachedDataAsync<T>(string path);
    }
    
    /// <summary>
    /// Интерфейс для стратегии разрешения конфликтов
    /// </summary>
    public interface IConflictResolutionStrategy
    {
        Task<T> ResolveConflictAsync<T>(string path, T localData, T remoteData) where T : class;
        bool CanResolveConflict<T>(T localData, T remoteData) where T : class;
    }
    
    /// <summary>
    /// Интерфейс для сервиса базы данных
    /// </summary>
    public interface IDatabaseService
    {
        Task<T> GetDataAsync<T>(string path) where T : class;
        Task<bool> SetDataAsync<T>(string path, T data) where T : class;
        Task<bool> UpdateDataAsync<T>(string path, T data) where T : class;
        Task<bool> DeleteDataAsync(string path);
        Task<bool> SaveDataAsync<T>(string path, T data) where T : class;
    }
}
#endif 