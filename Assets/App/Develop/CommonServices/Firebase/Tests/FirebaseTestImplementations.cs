#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Tests
{
    /// <summary>
    /// Тестовая реализация менеджера подключений Firebase
    /// </summary>
    public class TestFirebaseConnectionManager : IFirebaseConnectionManager
    {
        public bool IsConnected { get; private set; } = false;
        
        public Task<bool> WaitForConnectionAsync(float timeoutSeconds)
        {
            return Task.FromResult(IsConnected);
        }
        
        public void ForceDisconnect()
        {
            IsConnected = false;
            Debug.Log("[Test] Forced disconnect from Firebase");
        }
        
        public void ForceReconnect()
        {
            IsConnected = true;
            Debug.Log("[Test] Forced reconnect to Firebase");
        }
    }
    
    /// <summary>
    /// Тестовая реализация сервиса базы данных
    /// </summary>
    public class TestDatabaseService : IDatabaseService
    {
        private Dictionary<string, object> m_Database = new Dictionary<string, object>();
        
        public Task<T> GetDataAsync<T>(string path) where T : class
        {
            Debug.Log($"[Test] GetDataAsync: {path}");
            
            if (m_Database.TryGetValue(path, out object data) && data is T typedData)
            {
                return Task.FromResult(typedData);
            }
            
            return Task.FromResult<T>(null);
        }
        
        public Task<bool> SetDataAsync<T>(string path, T data) where T : class
        {
            Debug.Log($"[Test] SetDataAsync: {path}");
            m_Database[path] = data;
            return Task.FromResult(true);
        }
        
        public Task<bool> UpdateDataAsync<T>(string path, T data) where T : class
        {
            Debug.Log($"[Test] UpdateDataAsync: {path}");
            
            if (!m_Database.ContainsKey(path))
            {
                return Task.FromResult(false);
            }
            
            m_Database[path] = data;
            return Task.FromResult(true);
        }
        
        public Task<bool> DeleteDataAsync(string path)
        {
            Debug.Log($"[Test] DeleteDataAsync: {path}");
            
            bool removed = m_Database.Remove(path);
            return Task.FromResult(removed);
        }
        
        public Task<bool> SaveDataAsync<T>(string path, T data) where T : class
        {
            Debug.Log($"[Test] SaveDataAsync: {path}");
            m_Database[path] = data;
            return Task.FromResult(true);
        }
    }
    
    /// <summary>
    /// Тестовая реализация менеджера кэша Firebase
    /// </summary>
    public class TestFirebaseCacheManager : IFirebaseCacheManager
    {
        private Dictionary<string, object> m_Cache = new Dictionary<string, object>();
        
        public bool IsCachingEnabled { get; set; } = true;
        
        public Task<T> GetCachedDataAsync<T>(string path)
        {
            Debug.Log($"[Test] GetCachedDataAsync: {path}");
            
            if (m_Cache.TryGetValue(path, out object data) && data is T typedData)
            {
                return Task.FromResult(typedData);
            }
            
            return Task.FromResult<T>(default);
        }
        
        public Task<bool> ClearCacheAsync()
        {
            Debug.Log("[Test] ClearCacheAsync");
            m_Cache.Clear();
            return Task.FromResult(true);
        }
        
        public Task<long> GetCacheSizeAsync()
        {
            Debug.Log("[Test] GetCacheSizeAsync");
            return Task.FromResult((long)m_Cache.Count);
        }
        
        public Task<bool> SetCacheSizeLimitAsync(long sizeInBytes)
        {
            Debug.Log($"[Test] SetCacheSizeLimitAsync: {sizeInBytes}");
            return Task.FromResult(true);
        }
    }
    
    /// <summary>
    /// Тестовая реализация стратегии разрешения конфликтов
    /// </summary>
    public class TestConflictResolutionStrategy : IConflictResolutionStrategy
    {
        public bool CanResolveConflict<T>(T localData, T remoteData) where T : class
        {
            // Для тестов считаем, что можем разрешить любой конфликт
            return true;
        }
        
        public async Task<T> ResolveConflictAsync<T>(string path, T localData, T remoteData) where T : class
        {
            Debug.Log($"[Test] ResolveConflictAsync: {path}");
            
            // Имитация задержки на обработку конфликта
            await Task.Delay(200);
            
            // В тестовой реализации всегда предпочитаем локальные данные
            return localData;
        }
    }
    
    /// <summary>
    /// Тестовая реализация сервиса Firebase
    /// </summary>
    public class TestFirebaseDatabaseService : IFirebaseDatabaseService
    {
        private Dictionary<string, object> m_Database = new Dictionary<string, object>();
        private Dictionary<string, List<Action<object>>> m_Listeners = new Dictionary<string, List<Action<object>>>();
        
        public Task<T> GetDataAsync<T>(string path)
        {
            Debug.Log($"[Test] GetDataAsync: {path}");
            
            if (m_Database.TryGetValue(path, out object data) && data is T typedData)
            {
                return Task.FromResult(typedData);
            }
            
            return Task.FromResult<T>(default);
        }
        
        public Task<bool> SetDataAsync<T>(string path, T data)
        {
            Debug.Log($"[Test] SetDataAsync: {path}");
            m_Database[path] = data;
            
            // Оповещаем слушателей об изменении данных
            NotifyListeners(path, data);
            
            return Task.FromResult(true);
        }
        
        public Task<bool> UpdateDataAsync<T>(string path, T data)
        {
            Debug.Log($"[Test] UpdateDataAsync: {path}");
            
            if (!m_Database.ContainsKey(path))
            {
                return Task.FromResult(false);
            }
            
            m_Database[path] = data;
            
            // Оповещаем слушателей об изменении данных
            NotifyListeners(path, data);
            
            return Task.FromResult(true);
        }
        
        public Task<bool> DeleteDataAsync(string path)
        {
            Debug.Log($"[Test] DeleteDataAsync: {path}");
            
            bool removed = m_Database.Remove(path);
            
            // Оповещаем слушателей об удалении данных
            if (removed)
            {
                NotifyListeners(path, null);
            }
            
            return Task.FromResult(removed);
        }
        
        public Task<bool> PushDataAsync<T>(string path, T data)
        {
            Debug.Log($"[Test] PushDataAsync: {path}");
            
            string uniquePath = $"{path}/{Guid.NewGuid()}";
            m_Database[uniquePath] = data;
            
            // Оповещаем слушателей об изменении данных
            NotifyListeners(path, data);
            
            return Task.FromResult(true);
        }
        
        public void AddRealtimeListener<T>(string path, Action<T> onDataChanged)
        {
            Debug.Log($"[Test] AddRealtimeListener: {path}");
            
            if (!m_Listeners.TryGetValue(path, out var listeners))
            {
                listeners = new List<Action<object>>();
                m_Listeners[path] = listeners;
            }
            
            listeners.Add(obj => 
            {
                if (obj is T typedObj)
                {
                    onDataChanged(typedObj);
                }
            });
        }
        
        public void RemoveRealtimeListener(string path)
        {
            Debug.Log($"[Test] RemoveRealtimeListener: {path}");
            
            if (m_Listeners.ContainsKey(path))
            {
                m_Listeners.Remove(path);
            }
        }
        
        public Task<bool> SaveDataAsync<T>(string path, T data)
        {
            Debug.Log($"[Test] SaveDataAsync: {path}");
            m_Database[path] = data;
            
            // Оповещаем слушателей об изменении данных
            NotifyListeners(path, data);
            
            return Task.FromResult(true);
        }
        
        private void NotifyListeners(string path, object data)
        {
            if (m_Listeners.TryGetValue(path, out var listeners))
            {
                foreach (var listener in listeners)
                {
                    listener?.Invoke(data);
                }
            }
        }
    }
}
#endif 