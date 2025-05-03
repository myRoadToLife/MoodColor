using UnityEngine;

namespace App.Tests.EditMode.TestHelpers
{
    /// <summary>
    /// Конфигурация для тестирования Firebase. 
    /// Используется для хранения URL тестовой базы данных и других настроек тестирования.
    /// </summary>
    public static class FirebaseTestConfig 
    {
        /// <summary>
        /// URL тестовой базы данных Firebase.
        /// Должен отличаться от продакшн базы данных!
        /// </summary>
        public static string TestDatabaseUrl = "https://mood-color-test.firebaseio.com/";
        
        /// <summary>
        /// Префикс для тестовых данных в базе данных
        /// </summary>
        public static string TestEnvironmentPrefix = "test_environment";
        
        /// <summary>
        /// ID тестового пользователя
        /// </summary>
        public static string TestUserId = "test_user_integration";
        
        /// <summary>
        /// Тестовый email
        /// </summary>
        public static string TestEmail = "test@example.com";
        
        /// <summary>
        /// Получить полный путь к тестовым данным
        /// </summary>
        /// <param name="path">Дополнительный путь</param>
        /// <returns>Полный путь к тестовым данным</returns>
        public static string GetTestPath(string path = null)
        {
            return string.IsNullOrEmpty(path) 
                ? TestEnvironmentPrefix 
                : $"{TestEnvironmentPrefix}/{path}";
        }
    }
} 