using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Develop.CommonServices.Firebase.Common.Cache;
using App.Develop.CommonServices.Firebase.Database.Models;
using App.Develop.CommonServices.Firebase.Database.Services;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Develop.CommonServices.Emotion;
using App.Tests.EditMode.TestHelpers;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace App.Tests.EditMode.Firebase
{
    [TestFixture]
    public class FirebaseMockTests
    {
        #region Private Fields
        private FirebaseCacheManager _cacheManager;
        private MockSaveLoadService _saveLoadService;
        #endregion

        [SetUp]
        public void Setup()
        {
            Debug.Log("Настройка тестов с моками Firebase...");
            
            // Инициализируем компоненты
            // Для тестов создаем простую реализацию ISaveLoadService
            _saveLoadService = new MockSaveLoadService();
            _cacheManager = new FirebaseCacheManager(_saveLoadService);
            
            Debug.Log("Настройка тестов с моками Firebase завершена");
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Log("Очистка после тестов с моками Firebase...");
            
            // Освобождаем ресурсы
            _cacheManager = null;
            _saveLoadService = null;
            
            Debug.Log("Очистка после тестов с моками Firebase завершена");
        }
        
        // Простой тест для проверки работы SaveLoadService
        [Test]
        public void MockSaveLoadService_CanStoreAndRetrieveData()
        {
            // Arrange
            string testKey = "testKey";
            string testValue = "testValue";
            
            // Act
            bool saveResult = _saveLoadService.SaveData(testKey, testValue);
            string retrievedValue = _saveLoadService.LoadData(testKey);
            
            // Assert
            Assert.IsTrue(saveResult);
            Assert.AreEqual(testValue, retrievedValue);
        }
    }
    
    // Простая реализация ISaveLoadService для тестов
    public class MockSaveLoadService : ISaveLoadService
    {
        private Dictionary<string, string> _cachedData = new Dictionary<string, string>();
        
        public bool SaveData(string key, string data)
        {
            _cachedData[key] = data;
            return true;
        }
        
        public string LoadData(string key)
        {
            return _cachedData.TryGetValue(key, out var data) ? data : null;
        }
        
        public bool DeleteData(string key)
        {
            return _cachedData.Remove(key);
        }
        
        public bool HasKey(string key)
        {
            return _cachedData.ContainsKey(key);
        }
        
        // Явная реализация интерфейса для избежания конфликтов
        void ISaveLoadService.Save<TDada>(TDada data)
        {
            try
            {
                string key = typeof(TDada).Name;
                string json = JsonConvert.SerializeObject(data);
                SaveData(key, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка при сохранении данных: {e.Message}");
            }
        }
        
        // Явная реализация интерфейса для избежания конфликтов
        bool ISaveLoadService.TryLoad<TDada>(out TDada data)
        {
            data = default;
            try
            {
                string key = typeof(TDada).Name;
                if (!HasKey(key))
                {
                    return false;
                }
                
                string json = LoadData(key);
                if (string.IsNullOrEmpty(json))
                {
                    return false;
                }
                
                data = JsonConvert.DeserializeObject<TDada>(json);
                return data != null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка при загрузке данных: {e.Message}");
                return false;
            }
        }
    }
} 