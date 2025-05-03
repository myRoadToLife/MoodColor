#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Tests
{
    /// <summary>
    /// Простой локатор сервисов для тестирования Firebase.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> s_Services = new Dictionary<Type, object>();

        /// <summary>
        /// Регистрирует сервис в локаторе.
        /// </summary>
        /// <typeparam name="T">Тип сервиса (обычно интерфейс)</typeparam>
        /// <param name="service">Экземпляр сервиса</param>
        public static void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                Debug.LogError($"Cannot register null service of type {typeof(T).Name}");
                return;
            }

            Type type = typeof(T);
            if (s_Services.ContainsKey(type))
            {
                Debug.LogWarning($"Service of type {type.Name} is already registered. Overriding...");
            }

            s_Services[type] = service;
            Debug.Log($"Service {type.Name} registered successfully");
        }

        /// <summary>
        /// Получает зарегистрированный сервис указанного типа.
        /// </summary>
        /// <typeparam name="T">Тип сервиса для получения</typeparam>
        /// <returns>Экземпляр сервиса или null, если не найден</returns>
        public static T Get<T>() where T : class
        {
            Type type = typeof(T);
            if (s_Services.TryGetValue(type, out object service))
            {
                return service as T;
            }

            Debug.LogWarning($"Service of type {type.Name} not found. Creating mock implementation.");
            
            // Автоматическое создание мок-реализаций, если сервис не найден
            if (typeof(T) == typeof(IFirebaseDatabaseService))
            {
                RegisterMockFirebaseDatabaseService();
                return s_Services[type] as T;
            }
            else if (typeof(T) == typeof(IFirebaseConnectionManager))
            {
                RegisterMockFirebaseConnectionManager();
                return s_Services[type] as T;
            }
            else if (typeof(T) == typeof(IFirebaseCacheManager))
            {
                RegisterMockFirebaseCacheManager();
                return s_Services[type] as T;
            }
            else if (typeof(T) == typeof(IDatabaseService))
            {
                RegisterMockDatabaseService();
                return s_Services[type] as T;
            }
            else if (typeof(T) == typeof(IConflictResolutionStrategy))
            {
                RegisterMockConflictResolver();
                return s_Services[type] as T;
            }
            
            return null;
        }

        /// <summary>
        /// Удаляет сервис указанного типа из локатора.
        /// </summary>
        /// <typeparam name="T">Тип сервиса для удаления</typeparam>
        public static void Unregister<T>() where T : class
        {
            Type type = typeof(T);
            if (s_Services.ContainsKey(type))
            {
                s_Services.Remove(type);
                Debug.Log($"Service {type.Name} unregistered");
            }
        }

        /// <summary>
        /// Очищает все зарегистрированные сервисы.
        /// </summary>
        public static void Clear()
        {
            s_Services.Clear();
            Debug.Log("All services cleared");
        }
        
        #region Helper Methods
        private static void RegisterMockFirebaseDatabaseService()
        {
            Register<IFirebaseDatabaseService>(new MockFirebaseDatabaseService());
        }
        
        private static void RegisterMockFirebaseConnectionManager()
        {
            Register<IFirebaseConnectionManager>(new MockFirebaseConnectionManager());
        }
        
        private static void RegisterMockFirebaseCacheManager()
        {
            Register<IFirebaseCacheManager>(new MockFirebaseCacheManager());
        }
        
        private static void RegisterMockDatabaseService()
        {
            // Создаем и регистрируем мок для DatabaseService
            // Предполагаем, что такой класс уже существует в проекте
            var mockDatabaseService = MockImplementationFactory.CreateMockDatabaseService();
            Register<IDatabaseService>(mockDatabaseService);
        }
        
        private static void RegisterMockConflictResolver()
        {
            // Создаем и регистрируем мок для ConflictResolver
            // Предполагаем, что такой класс уже существует в проекте
            var mockConflictResolver = MockImplementationFactory.CreateMockConflictResolver();
            Register<IConflictResolutionStrategy>(mockConflictResolver);
        }
        #endregion
    }
    
    /// <summary>
    /// Фабрика для создания мок-реализаций различных интерфейсов
    /// </summary>
    public static class MockImplementationFactory
    {
        public static IDatabaseService CreateMockDatabaseService()
        {
            return new MockDatabaseServiceImpl();
        }
        
        public static IConflictResolutionStrategy CreateMockConflictResolver()
        {
            return new MockConflictResolverImpl();
        }
        
        private class MockDatabaseServiceImpl : IDatabaseService
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
            
            public Task<bool> SaveDataAsync<T>(string path, T data) where T : class
            {
                Debug.Log($"[Mock] SaveDataAsync: {path}");
                m_Database[path] = data;
                return Task.FromResult(true);
            }
        }
        
        private class MockConflictResolverImpl : IConflictResolutionStrategy
        {
            public bool CanResolveConflict<T>(T localData, T remoteData) where T : class
            {
                return true;
            }
            
            public Task<T> ResolveConflictAsync<T>(string path, T localData, T remoteData) where T : class
            {
                Debug.Log($"[Mock] ResolveConflictAsync: {path}");
                return Task.FromResult(localData);
            }
        }
    }
}
#endif 