#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Tests
{
    /// <summary>
    /// Класс для инициализации тестового окружения
    /// </summary>
    public class TestBootstrap : MonoBehaviour
    {
        [SerializeField] private bool m_InitializeOnAwake = true;
        
        private static bool m_IsInitialized;
        
        private void Awake()
        {
            if (m_InitializeOnAwake && !m_IsInitialized)
            {
                InitializeTestEnvironment();
            }
        }
        
        /// <summary>
        /// Инициализирует тестовое окружение
        /// </summary>
        public void InitializeTestEnvironment()
        {
            if (m_IsInitialized)
            {
                Debug.Log("Test environment already initialized");
                return;
            }
            
            Debug.Log("Initializing Firebase test environment...");
            
            // Создаем тестовые реализации всех сервисов
            var connectionManager = new TestFirebaseConnectionManager();
            var databaseService = new TestDatabaseService();
            var firebaseDatabaseService = new TestFirebaseDatabaseService();
            var cacheManager = new TestFirebaseCacheManager();
            var conflictResolver = new TestConflictResolutionStrategy();
            
            // Создаем GameObject для хранения ссылок на сервисы
            var servicesContainer = new GameObject("Firebase Test Services");
            DontDestroyOnLoad(servicesContainer);
            
            // Добавляем тестовые сервисы на GameObject
            var connectionManagerComponent = servicesContainer.AddComponent<TestConnectionManagerComponent>();
            connectionManagerComponent.Initialize(connectionManager);
            
            var databaseServiceComponent = servicesContainer.AddComponent<TestDatabaseServiceComponent>();
            databaseServiceComponent.Initialize(databaseService);
            
            var firebaseDatabaseServiceComponent = servicesContainer.AddComponent<TestFirebaseDatabaseServiceComponent>();
            firebaseDatabaseServiceComponent.Initialize(firebaseDatabaseService);
            
            var cacheManagerComponent = servicesContainer.AddComponent<TestCacheManagerComponent>();
            cacheManagerComponent.Initialize(cacheManager);
            
            var conflictResolverComponent = servicesContainer.AddComponent<TestConflictResolverComponent>();
            conflictResolverComponent.Initialize(conflictResolver);
            
            m_IsInitialized = true;
            
            Debug.Log("Firebase test environment initialized successfully");
        }
    }
    
    /// <summary>
    /// Компонент для хранения ссылки на менеджер соединений
    /// </summary>
    public class TestConnectionManagerComponent : MonoBehaviour
    {
        private static IFirebaseConnectionManager m_Instance;
        
        public static IFirebaseConnectionManager Instance => m_Instance;
        
        public void Initialize(IFirebaseConnectionManager manager)
        {
            m_Instance = manager;
        }
    }
    
    /// <summary>
    /// Компонент для хранения ссылки на сервис базы данных
    /// </summary>
    public class TestDatabaseServiceComponent : MonoBehaviour
    {
        private static IDatabaseService m_Instance;
        
        public static IDatabaseService Instance => m_Instance;
        
        public void Initialize(IDatabaseService service)
        {
            m_Instance = service;
        }
    }
    
    /// <summary>
    /// Компонент для хранения ссылки на сервис Firebase
    /// </summary>
    public class TestFirebaseDatabaseServiceComponent : MonoBehaviour
    {
        private static IFirebaseDatabaseService m_Instance;
        
        public static IFirebaseDatabaseService Instance => m_Instance;
        
        public void Initialize(IFirebaseDatabaseService service)
        {
            m_Instance = service;
        }
    }
    
    /// <summary>
    /// Компонент для хранения ссылки на менеджер кэша
    /// </summary>
    public class TestCacheManagerComponent : MonoBehaviour
    {
        private static IFirebaseCacheManager m_Instance;
        
        public static IFirebaseCacheManager Instance => m_Instance;
        
        public void Initialize(IFirebaseCacheManager manager)
        {
            m_Instance = manager;
        }
    }
    
    /// <summary>
    /// Компонент для хранения ссылки на разрешитель конфликтов
    /// </summary>
    public class TestConflictResolverComponent : MonoBehaviour
    {
        private static IConflictResolutionStrategy m_Instance;
        
        public static IConflictResolutionStrategy Instance => m_Instance;
        
        public void Initialize(IConflictResolutionStrategy resolver)
        {
            m_Instance = resolver;
        }
    }
}
#endif 