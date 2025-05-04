#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Tests
{
    /// <summary>
    /// Тестовая реализация сервиса базы данных Firebase
    /// </summary>
    public class MockFirebaseDatabaseService : IFirebaseDatabaseService
    {
        private Dictionary<string, object> m_Database = new Dictionary<string, object>();
        private Dictionary<string, List<Action<object>>> m_Listeners = new Dictionary<string, List<Action<object>>>();
        
        public Task<T> GetDataAsync<T>(string path)
        {
            Debug.Log($"[Mock] GetDataAsync: {path}");
            
            if (m_Database.TryGetValue(path, out object data) && data is T typedData)
            {
                return Task.FromResult(typedData);
            }
            
            return Task.FromResult<T>(default);
        }
        
        public Task<bool> SetDataAsync<T>(string path, T data)
        {
            Debug.Log($"[Mock] SetDataAsync: {path}");
            m_Database[path] = data;
            
            // Оповещаем слушателей
            NotifyListeners(path, data);
            
            return Task.FromResult(true);
        }
        
        public Task<bool> UpdateDataAsync<T>(string path, T data)
        {
            Debug.Log($"[Mock] UpdateDataAsync: {path}");
            
            if (!m_Database.ContainsKey(path))
            {
                return Task.FromResult(false);
            }
            
            m_Database[path] = data;
            
            // Оповещаем слушателей
            NotifyListeners(path, data);
            
            return Task.FromResult(true);
        }
        
        public Task<bool> DeleteDataAsync(string path)
        {
            Debug.Log($"[Mock] DeleteDataAsync: {path}");
            
            bool removed = m_Database.Remove(path);
            
            // Оповещаем слушателей
            if (removed)
            {
                NotifyListeners(path, null);
            }
            
            return Task.FromResult(removed);
        }
        
        public Task<bool> PushDataAsync<T>(string path, T data)
        {
            Debug.Log($"[Mock] PushDataAsync: {path}");
            
            string uniquePath = $"{path}/{Guid.NewGuid()}";
            m_Database[uniquePath] = data;
            
            // Оповещаем слушателей
            NotifyListeners(path, data);
            
            return Task.FromResult(true);
        }
        
        public Task<bool> SaveDataAsync<T>(string path, T data)
        {
            Debug.Log($"[Mock] SaveDataAsync: {path}");
            
            // В мок-реализации сохранение аналогично установке значения
            m_Database[path] = data;
            
            // Оповещаем слушателей
            NotifyListeners(path, data);
            
            return Task.FromResult(true);
        }
        
        public void AddRealtimeListener<T>(string path, Action<T> onDataChanged)
        {
            Debug.Log($"[Mock] AddRealtimeListener: {path}");
            
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
            Debug.Log($"[Mock] RemoveRealtimeListener: {path}");
            
            if (m_Listeners.ContainsKey(path))
            {
                m_Listeners.Remove(path);
            }
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

    /// <summary>
    /// Тестовая реализация менеджера соединений Firebase
    /// </summary>
    public class MockFirebaseConnectionManager : IFirebaseConnectionManager
    {
        public bool IsConnected { get; private set; } = true;
        
        public void ForceDisconnect()
        {
            IsConnected = false;
            Debug.Log("[Mock] ForceDisconnect");
        }
        
        public void ForceReconnect()
        {
            IsConnected = true;
            Debug.Log("[Mock] ForceReconnect");
        }
        
        public Task<bool> WaitForConnectionAsync(float timeoutSeconds)
        {
            Debug.Log($"[Mock] WaitForConnectionAsync: {timeoutSeconds}s");
            return Task.FromResult(IsConnected);
        }
    }

    /// <summary>
    /// Тестовая реализация менеджера кэша Firebase
    /// </summary>
    public class MockFirebaseCacheManager : IFirebaseCacheManager
    {
        private Dictionary<string, object> m_Cache = new Dictionary<string, object>();
        
        public bool IsCachingEnabled { get; set; } = true;
        
        public Task<bool> ClearCacheAsync()
        {
            Debug.Log("[Mock] ClearCacheAsync");
            m_Cache.Clear();
            return Task.FromResult(true);
        }
        
        public Task<T> GetCachedDataAsync<T>(string path)
        {
            Debug.Log($"[Mock] GetCachedDataAsync: {path}");
            
            if (m_Cache.TryGetValue(path, out object data) && data is T typedData)
            {
                return Task.FromResult(typedData);
            }
            
            return Task.FromResult<T>(default);
        }
        
        public Task<long> GetCacheSizeAsync()
        {
            Debug.Log("[Mock] GetCacheSizeAsync");
            return Task.FromResult((long)m_Cache.Count);
        }
        
        public Task<bool> SetCacheSizeLimitAsync(long sizeInBytes)
        {
            Debug.Log($"[Mock] SetCacheSizeLimitAsync: {sizeInBytes}");
            return Task.FromResult(true);
        }
    }
    
    /// <summary>
    /// Тестовая реализация сервиса базы данных
    /// </summary>
    public class MockDatabaseService : IDatabaseService
    {
        private Dictionary<string, object> m_Database = new Dictionary<string, object>();
        
        public Task<T> GetDataAsync<T>(string path) where T : class
        {
            Debug.Log($"[Mock] GetDataAsync: {path}");
            
            if (m_Database.TryGetValue(path, out object data) && data is T typedData)
            {
                return Task.FromResult(typedData);
            }
            
            return Task.FromResult<T>(null);
        }
        
        public Task<bool> SaveDataAsync<T>(string path, T data) where T : class
        {
            Debug.Log($"[Mock] SaveDataAsync: {path}");
            m_Database[path] = data;
            return Task.FromResult(true);
        }
        
        public Task<bool> SetDataAsync<T>(string path, T data) where T : class
        {
            Debug.Log($"[Mock] SetDataAsync: {path}");
            m_Database[path] = data;
            return Task.FromResult(true);
        }
        
        public Task<bool> UpdateDataAsync<T>(string path, T data) where T : class
        {
            Debug.Log($"[Mock] UpdateDataAsync: {path}");
            
            if (!m_Database.ContainsKey(path))
            {
                return Task.FromResult(false);
            }
            
            m_Database[path] = data;
            return Task.FromResult(true);
        }
        
        public Task<bool> DeleteDataAsync(string path)
        {
            Debug.Log($"[Mock] DeleteDataAsync: {path}");
            
            bool removed = m_Database.Remove(path);
            return Task.FromResult(removed);
        }
    }
    
    /// <summary>
    /// Тестовая реализация стратегии разрешения конфликтов
    /// </summary>
    public class MockConflictResolver : IConflictResolutionStrategy
    {
        public bool CanResolveConflict<T>(T localData, T remoteData) where T : class
        {
            // В тестовом режиме всегда успешно разрешаем конфликты
            return true;
        }
        
        public Task<T> ResolveConflictAsync<T>(string path, T localData, T remoteData) where T : class
        {
            Debug.Log($"[Mock] ResolveConflictAsync: {path}");
            
            // Имитируем задержку разрешения конфликта
            Task.Delay(100).Wait();
            
            // В тестовой реализации всегда предпочитаем локальные данные
            return Task.FromResult(localData);
        }
    }
}
#endif 