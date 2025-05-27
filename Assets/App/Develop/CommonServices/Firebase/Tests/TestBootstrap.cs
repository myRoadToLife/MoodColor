#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Tests
{
    /// <summary>
    /// Класс для инициализации тестового окружения
    /// </summary>
    public class TestBootstrap : MonoBehaviour
    {
        [SerializeField] private bool _initializeOnAwake = true;
        
        private static bool _isInitialized;
        
        private void Awake()
        {
            if (_initializeOnAwake && !_isInitialized)
            {
                InitializeTestEnvironment();
            }
        }
        
        /// <summary>
        /// Инициализирует тестовое окружение
        /// </summary>
        public void InitializeTestEnvironment()
        {
            if (_isInitialized)
            {
                Debug.Log("Test environment already initialized");
                return;
            }
            
            Debug.Log("Initializing Firebase test environment...");
            
            // Создаем тестовые реализации всех сервисов
            TestFirebaseConnectionManager connectionManager = new TestFirebaseConnectionManager();
            TestDatabaseService databaseService = new TestDatabaseService();
            TestFirebaseDatabaseService firebaseDatabaseService = new TestFirebaseDatabaseService();
            TestFirebaseCacheManager cacheManager = new TestFirebaseCacheManager();
            TestConflictResolutionStrategy conflictResolver = new TestConflictResolutionStrategy();
            
            // Создаем GameObject для хранения ссылок на сервисы
            GameObject servicesContainer = new GameObject("Firebase Test Services");
            DontDestroyOnLoad(servicesContainer);
            
            // Добавляем тестовые сервисы на GameObject
            TestConnectionManagerComponent connectionManagerComponent = servicesContainer.AddComponent<TestConnectionManagerComponent>();
            connectionManagerComponent.Initialize(connectionManager);
            
            TestDatabaseServiceComponent databaseServiceComponent = servicesContainer.AddComponent<TestDatabaseServiceComponent>();
            databaseServiceComponent.Initialize(databaseService);
            
            TestFirebaseDatabaseServiceComponent firebaseDatabaseServiceComponent = servicesContainer.AddComponent<TestFirebaseDatabaseServiceComponent>();
            firebaseDatabaseServiceComponent.Initialize(firebaseDatabaseService);
            
            TestCacheManagerComponent cacheManagerComponent = servicesContainer.AddComponent<TestCacheManagerComponent>();
            cacheManagerComponent.Initialize(cacheManager);
            
            TestConflictResolverComponent conflictResolverComponent = servicesContainer.AddComponent<TestConflictResolverComponent>();
            conflictResolverComponent.Initialize(conflictResolver);
            
            _isInitialized = true;
            
            Debug.Log("Firebase test environment initialized successfully");
        }
    }
    
    /// <summary>
    /// Компонент для хранения ссылки на менеджер соединений
    /// </summary>
    public class TestConnectionManagerComponent : MonoBehaviour
    {
        private static IFirebaseConnectionManager _instance;
        
        public static IFirebaseConnectionManager Instance => _instance;
        
        public void Initialize(IFirebaseConnectionManager manager)
        {
            _instance = manager;
        }
    }
    
    /// <summary>
    /// Компонент для хранения ссылки на сервис базы данных
    /// </summary>
    public class TestDatabaseServiceComponent : MonoBehaviour
    {
        private static IDatabaseService _instance;
        
        public static IDatabaseService Instance => _instance;
        
        public void Initialize(IDatabaseService service)
        {
            _instance = service;
        }
    }
    
    /// <summary>
    /// Компонент для хранения ссылки на сервис Firebase
    /// </summary>
    public class TestFirebaseDatabaseServiceComponent : MonoBehaviour
    {
        private static IFirebaseDatabaseService _instance;
        
        public static IFirebaseDatabaseService Instance => _instance;
        
        public void Initialize(IFirebaseDatabaseService service)
        {
            _instance = service;
        }
    }
    
    /// <summary>
    /// Компонент для хранения ссылки на менеджер кэша
    /// </summary>
    public class TestCacheManagerComponent : MonoBehaviour
    {
        private static IFirebaseCacheManager _instance;
        
        public static IFirebaseCacheManager Instance => _instance;
        
        public void Initialize(IFirebaseCacheManager manager)
        {
            _instance = manager;
        }
    }
    
    /// <summary>
    /// Компонент для хранения ссылки на разрешитель конфликтов
    /// </summary>
    public class TestConflictResolverComponent : MonoBehaviour
    {
        private static IConflictResolutionStrategy _instance;
        
        public static IConflictResolutionStrategy Instance => _instance;
        
        public void Initialize(IConflictResolutionStrategy resolver)
        {
            _instance = resolver;
        }
    }
}
#endif 