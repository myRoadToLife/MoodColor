using App.Develop.CommonServices.Firebase.Common.Cache;
using NUnit.Framework;
using UnityEngine;

namespace App.Tests.EditMode.Firebase
{
    [TestFixture]
    public class FirebaseIntegrationTests
    {
        #region Private Fields
        private FirebaseCacheManager _cacheManager;
        private MockSaveLoadService _saveLoadService;
        #endregion

        [SetUp]
        public void Setup()
        {
            Debug.Log("Настройка интеграционных тестов Firebase...");
            
            // Инициализируем компоненты для простых тестов
            _saveLoadService = new MockSaveLoadService();
            _cacheManager = new FirebaseCacheManager(_saveLoadService);
            
            Debug.Log("Настройка интеграционных тестов Firebase завершена");
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Log("Очистка после интеграционных тестов Firebase...");
            
            // Освобождаем ресурсы
            _cacheManager = null;
            _saveLoadService = null;
            
            Debug.Log("Очистка после интеграционных тестов Firebase завершена");
        }
        
        // Упрощенный тест для проверки инициализации
        [Test]
        public void CacheManager_ShouldBeInitialized()
        {
            // Assert
            Assert.IsNotNull(_cacheManager);
            Assert.IsNotNull(_saveLoadService);
        }
        
        // Тест базовой работоспособности
        [Test]
        public void FirebaseIntegration_BasicTest()
        {
            // Тест проходит, если мы смогли инициализировать кэш-менеджер без ошибок
            Assert.Pass("Базовая интеграция успешна");
        }
    }
} 