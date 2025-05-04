#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Tests
{
    /// <summary>
    /// Статический класс для доступа к тестовым сервисам Firebase
    /// </summary>
    public static class FirebaseServiceLocator
    {
        private static IFirebaseConnectionManager s_ConnectionManager;
        private static IDatabaseService s_DatabaseService;
        private static IFirebaseDatabaseService s_FirebaseDatabaseService;
        private static IFirebaseCacheManager s_CacheManager;
        private static IConflictResolutionStrategy s_ConflictResolver;
        
        private static bool s_IsInitialized;
        
        /// <summary>
        /// Инициализирует все тестовые сервисы
        /// </summary>
        public static void Initialize()
        {
            if (s_IsInitialized)
            {
                Debug.Log("Firebase service locator already initialized");
                return;
            }
            
            s_ConnectionManager = new TestFirebaseConnectionManager();
            s_DatabaseService = new TestDatabaseService();
            s_FirebaseDatabaseService = new TestFirebaseDatabaseService();
            s_CacheManager = new TestFirebaseCacheManager();
            s_ConflictResolver = new TestConflictResolutionStrategy();
            
            s_IsInitialized = true;
            
            Debug.Log("Firebase service locator initialized successfully");
        }
        
        /// <summary>
        /// Возвращает тестовый менеджер соединений Firebase
        /// </summary>
        public static IFirebaseConnectionManager GetConnectionManager()
        {
            EnsureInitialized();
            return s_ConnectionManager;
        }
        
        /// <summary>
        /// Возвращает тестовый сервис базы данных
        /// </summary>
        public static IDatabaseService GetDatabaseService()
        {
            EnsureInitialized();
            return s_DatabaseService;
        }
        
        /// <summary>
        /// Возвращает тестовый Firebase сервис базы данных
        /// </summary>
        public static IFirebaseDatabaseService GetFirebaseDatabaseService()
        {
            EnsureInitialized();
            return s_FirebaseDatabaseService;
        }
        
        /// <summary>
        /// Возвращает тестовый менеджер кэша Firebase
        /// </summary>
        public static IFirebaseCacheManager GetCacheManager()
        {
            EnsureInitialized();
            return s_CacheManager;
        }
        
        /// <summary>
        /// Возвращает тестовую стратегию разрешения конфликтов
        /// </summary>
        public static IConflictResolutionStrategy GetConflictResolver()
        {
            EnsureInitialized();
            return s_ConflictResolver;
        }
        
        /// <summary>
        /// Убеждается, что локатор сервисов инициализирован
        /// </summary>
        private static void EnsureInitialized()
        {
            if (!s_IsInitialized)
            {
                Initialize();
            }
        }
    }
}
#endif 