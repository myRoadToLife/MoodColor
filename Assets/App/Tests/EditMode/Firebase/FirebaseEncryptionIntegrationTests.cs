using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using App.Develop.AppServices.Firebase.Common.Cache;
using App.Develop.AppServices.Firebase.Database.Encryption;
using App.Develop.AppServices.Firebase.Database.Models;
using App.Develop.AppServices.Firebase.Database.Services;
using App.Develop.CommonServices.DataManagement.DataProviders;
using App.Tests.EditMode.Firebase;
using App.Tests.EditMode.TestHelpers;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace App.Tests.EditMode.Firebase
{
    [TestFixture]
    public class FirebaseEncryptionIntegrationTests
    {
        #region Private Fields
        private DataEncryptionService _encryptionService;
        
        // Тестовый ключ шифрования
        private const string TestEncryptionKey = "TestSecretKey123456";
        #endregion

        [SetUp]
        public void Setup()
        {
            Debug.Log("Настройка тестов шифрования Firebase...");
            
            // Создаем сервис шифрования с тестовым ключом
            _encryptionService = new DataEncryptionService(TestEncryptionKey);
            
            Debug.Log("Настройка тестов шифрования Firebase завершена");
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Log("Очистка после тестов шифрования Firebase...");
            
            _encryptionService = null;
            
            Debug.Log("Очистка после тестов шифрования Firebase завершена");
        }
        
        [Test]
        public void ShouldEncryptAndDecryptData()
        {
            // Arrange
            var testData = new EmotionData
            {
                Type = "Joy",
                Value = 0.8f,
                Intensity = 0.7f,
                LastUpdate = DateTime.UtcNow
            };
            
            // Act - шифруем данные
            string json = JsonConvert.SerializeObject(testData);
            string encrypted = _encryptionService.Encrypt(json);
            
            // Проверяем, что зашифрованный текст отличается от исходного
            Assert.AreNotEqual(json, encrypted);
            Assert.IsTrue(encrypted.Length > 0);
            
            // Act - расшифровываем данные
            string decrypted = _encryptionService.Decrypt(encrypted);
            var decryptedData = JsonConvert.DeserializeObject<EmotionData>(decrypted);
            
            // Assert
            Assert.AreEqual(testData.Type, decryptedData.Type);
            Assert.AreEqual(testData.Value, decryptedData.Value);
            Assert.AreEqual(testData.Intensity, decryptedData.Intensity);
        }
        
        [Test]
        public void ShouldStoreAndRetrieveEncryptedData()
        {
            // Arrange
            var testData = "Секретные данные для шифрования";
            
            // Act - шифруем данные
            string encrypted = _encryptionService.Encrypt(testData);
            
            // Проверяем, что зашифрованный текст отличается от исходного
            Assert.AreNotEqual(testData, encrypted);
            
            // Act - расшифровываем данные
            string decrypted = _encryptionService.Decrypt(encrypted);
            
            // Assert
            Assert.AreEqual(testData, decrypted);
        }
    }
} 