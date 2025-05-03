#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Tests
{
    /// <summary>
    /// Простой контейнер зависимостей для тестов Firebase
    /// </summary>
    public class FirebaseTestContainer : MonoBehaviour
    {
        #region Singleton
        private static FirebaseTestContainer m_Instance;
        
        public static FirebaseTestContainer Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    var go = new GameObject("FirebaseTestContainer");
                    m_Instance = go.AddComponent<FirebaseTestContainer>();
                    DontDestroyOnLoad(go);
                }
                
                return m_Instance;
            }
        }
        #endregion
        
        #region Private Fields
        private readonly Dictionary<Type, object> m_Services = new Dictionary<Type, object>();
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            if (m_Instance != null && m_Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            m_Instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeServices();
        }
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Возвращает сервис указанного типа
        /// </summary>
        public T Resolve<T>() where T : class
        {
            if (m_Services.TryGetValue(typeof(T), out object service))
            {
                return service as T;
            }
            
            Debug.LogError($"Service of type {typeof(T).Name} not registered");
            return null;
        }
        
        /// <summary>
        /// Регистрирует сервис указанного типа
        /// </summary>
        public void Register<T>(T implementation) where T : class
        {
            m_Services[typeof(T)] = implementation;
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Инициализирует все тестовые сервисы по умолчанию
        /// </summary>
        private void InitializeServices()
        {
            // Создаем экземпляры тестовых сервисов
            var connectionManager = new TestFirebaseConnectionManager();
            var databaseService = new TestDatabaseService();
            var firebaseDatabaseService = new TestFirebaseDatabaseService();
            var cacheManager = new TestFirebaseCacheManager();
            var conflictResolver = new TestConflictResolutionStrategy();
            
            // Регистрируем сервисы в контейнере
            Register<IFirebaseConnectionManager>(connectionManager);
            Register<IDatabaseService>(databaseService);
            Register<IFirebaseDatabaseService>(firebaseDatabaseService);
            Register<IFirebaseCacheManager>(cacheManager);
            Register<IConflictResolutionStrategy>(conflictResolver);
            
            Debug.Log("Firebase test services initialized");
        }
        #endregion
    }
    
    /// <summary>
    /// Статический класс для упрощенного доступа к контейнеру зависимостей
    /// </summary>
    public static class DIContainer
    {
        /// <summary>
        /// Возвращает сервис указанного типа
        /// </summary>
        public static T Resolve<T>() where T : class
        {
            return FirebaseTestContainer.Instance.Resolve<T>();
        }
        
        /// <summary>
        /// Регистрирует сервис указанного типа
        /// </summary>
        public static void Register<T>(T implementation) where T : class
        {
            FirebaseTestContainer.Instance.Register(implementation);
        }
    }
}
#endif 