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
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        /// <summary>
        /// Регистрирует сервис в локаторе.
        /// </summary>
        /// <typeparam name="T">Тип сервиса (обычно интерфейс)</typeparam>
        /// <param name="service">Экземпляр сервиса</param>
        public static void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service), $"Cannot register null service of type {typeof(T).Name}");
            }

            Type type = typeof(T);
            if (_services.ContainsKey(type))
            {
                // Warning about overriding is removed as per instruction (it's informational)
            }

            _services[type] = service;
        }

        /// <summary>
        /// Получает зарегистрированный сервис указанного типа.
        /// </summary>
        /// <typeparam name="T">Тип сервиса для получения</typeparam>
        /// <returns>Экземпляр сервиса или null, если не найден</returns>
        public static T Get<T>() where T : class
        {
            Type type = typeof(T);
            if (_services.TryGetValue(type, out object serviceInstance))
            {
                return serviceInstance as T;
            }

            // Informational message about mock creation is removed

            // Автоматическое создание мок-реализаций, если сервис не найден
            if (typeof(T) == typeof(IFirebaseDatabaseService))
            {
                RegisterMockFirebaseDatabaseService();
                return _services[type] as T;
            }
            else if (typeof(T) == typeof(IFirebaseConnectionManager))
            {
                RegisterMockFirebaseConnectionManager();
                return _services[type] as T;
            }
            else if (typeof(T) == typeof(IFirebaseCacheManager))
            {
                RegisterMockFirebaseCacheManager();
                return _services[type] as T;
            }
            else if (typeof(T) == typeof(IDatabaseService))
            {
                RegisterMockDatabaseService();
                return _services[type] as T;
            }
            else if (typeof(T) == typeof(IConflictResolutionStrategy))
            {
                RegisterMockConflictResolver();
                return _services[type] as T;
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
            if (_services.ContainsKey(type))
            {
                _services.Remove(type);
            }
        }

        /// <summary>
        /// Очищает все зарегистрированные сервисы.
        /// </summary>
        public static void Clear()
        {
            _services.Clear();
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
            IDatabaseService mockDatabaseService = MockImplementationFactory.CreateMockDatabaseService();
            Register<IDatabaseService>(mockDatabaseService);
        }

        private static void RegisterMockConflictResolver()
        {
            // Создаем и регистрируем мок для ConflictResolver
            // Предполагаем, что такой класс уже существует в проекте
            IConflictResolutionStrategy mockConflictResolver = MockImplementationFactory.CreateMockConflictResolver();
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
            private Dictionary<string, object> _database = new Dictionary<string, object>();

            public Task<T> GetDataAsync<T>(string path) where T : class
            {
                if (_database.TryGetValue(path, out object data) && data is T typedData)
                {
                    return Task.FromResult(typedData);
                }
                return Task.FromResult<T>(null);
            }

            public Task<bool> SetDataAsync<T>(string path, T data) where T : class
            {
                _database[path] = data;
                return Task.FromResult(true);
            }

            public Task<bool> UpdateDataAsync<T>(string path, T data) where T : class
            {
                if (!_database.ContainsKey(path))
                {
                    return Task.FromResult(false);
                }
                _database[path] = data;
                return Task.FromResult(true);
            }

            public Task<bool> DeleteDataAsync(string path)
            {
                bool removed = _database.Remove(path);
                return Task.FromResult(removed);
            }

            public Task<bool> SaveDataAsync<T>(string path, T data) where T : class
            {
                _database[path] = data;
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
                return Task.FromResult(localData);
            }
        }
    }
}
#endif 