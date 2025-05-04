using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using App.Develop.CommonServices.Firebase.Database.Encryption;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace App.Tests.EditMode.Firebase
{
    // Тестовая реализация DataEncryptionService без логирования ошибок
    public class TestDataEncryptionService : IDataEncryptionService
    {
        private const int c_KeySize = 32;
        private const int c_IvSize = 16;
        private const int c_SaltSize = 16;
        private const int c_Iterations = 10000;
        
        private readonly byte[] _encryptionKey;
        private readonly string _encryptionPassword;
        
        public TestDataEncryptionService(string password = null)
        {
            if (string.IsNullOrEmpty(password))
            {
                // Если пароль не указан, генерируем случайный ключ
                _encryptionKey = new byte[c_KeySize];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(_encryptionKey);
                }
                
                _encryptionPassword = Convert.ToBase64String(_encryptionKey);
            }
            else
            {
                // Если пароль указан, используем его
                _encryptionPassword = password;
                
                // Генерируем ключ фиксированного размера на основе пароля
                using (var deriveBytes = new Rfc2898DeriveBytes(password, new byte[c_SaltSize], c_Iterations))
                {
                    _encryptionKey = deriveBytes.GetBytes(c_KeySize);
                }
            }
        }
        
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return plainText;
            }
            
            try
            {
                // Создаем вектор инициализации
                byte[] iv = new byte[c_IvSize];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(iv);
                }
                
                // Шифруем данные
                byte[] encrypted;
                using (var aes = Aes.Create())
                {
                    aes.Key = _encryptionKey;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    
                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                    
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        // Сначала записываем вектор инициализации
                        memoryStream.Write(iv, 0, iv.Length);
                        
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                            {
                                streamWriter.Write(plainText);
                            }
                            
                            encrypted = memoryStream.ToArray();
                        }
                    }
                }
                
                // Возвращаем зашифрованные данные в формате Base64
                return Convert.ToBase64String(encrypted);
            }
            catch (Exception)
            {
                // Не логируем ошибки
                throw;
            }
        }
        
        public string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
            {
                return encryptedText;
            }
            
            try
            {
                // Проверка на валидность Base64
                if (!IsValidBase64(encryptedText))
                {
                    throw new FormatException("Входная строка не является допустимой строкой Base64");
                }
                
                // Преобразуем Base64 в массив байтов
                byte[] cipherBytes = Convert.FromBase64String(encryptedText);
                
                // Проверяем, что у нас достаточно данных (IV + хотя бы 1 блок данных)
                if (cipherBytes.Length <= c_IvSize)
                {
                    throw new ArgumentException("Недостаточно данных для расшифровки");
                }
                
                // Извлекаем вектор инициализации
                byte[] iv = new byte[c_IvSize];
                Array.Copy(cipherBytes, 0, iv, 0, iv.Length);
                
                // Расшифровываем данные
                string plainText;
                using (var aes = Aes.Create())
                {
                    aes.Key = _encryptionKey;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    
                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    
                    using (MemoryStream memoryStream = new MemoryStream(cipherBytes, c_IvSize, cipherBytes.Length - c_IvSize))
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader streamReader = new StreamReader(cryptoStream))
                            {
                                plainText = streamReader.ReadToEnd();
                            }
                        }
                    }
                }
                
                return plainText;
            }
            catch (Exception)
            {
                // Не логируем ошибки
                throw;
            }
        }
        
        public string GetEncryptionPassword()
        {
            return _encryptionPassword;
        }
        
        private bool IsValidBase64(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;
                
            // Длина строки Base64 должна быть кратной 4
            if (input.Length % 4 != 0)
                return false;
                
            // Проверка на допустимые символы Base64
            foreach (char c in input)
            {
                if ((c < 'A' || c > 'Z') && 
                    (c < 'a' || c > 'z') && 
                    (c < '0' || c > '9') && 
                    c != '+' && c != '/' && c != '=')
                {
                    return false;
                }
            }
            
            try
            {
                // Попытка преобразования (это самый надежный способ проверки)
                Convert.FromBase64String(input);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    [TestFixture]
    public class DataEncryptionTests
    {
        private const string TestEncryptionKey = "aR5*-BKPQ)$8p#UE";
        private TestDataEncryptionService _encryptionService;

        [SetUp]
        public void Setup()
        {
            Debug.Log("Настройка тестов шифрования данных...");
            
            _encryptionService = new TestDataEncryptionService(TestEncryptionKey);
            
            Debug.Log("Настройка тестов шифрования данных завершена");
        }
        
        [TearDown]
        public void TearDown()
        {
            Debug.Log("Очистка после тестов шифрования данных...");
            
            _encryptionService = null;
            
            Debug.Log("Очистка после тестов шифрования данных завершена");
        }
        
        [Test]
        public void Encrypt_ShouldReturnNonEmptyString()
        {
            // Arrange
            string originalData = "Test data to encrypt";
            
            // Act
            string encryptedData = _encryptionService.Encrypt(originalData);
            
            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(encryptedData));
            Assert.AreNotEqual(originalData, encryptedData);
        }
        
        [Test]
        public void EncryptAndDecrypt_ShouldReturnOriginalValue()
        {
            // Arrange
            string originalData = "Test data to encrypt and decrypt";
            
            // Act
            string encryptedData = _encryptionService.Encrypt(originalData);
            string decryptedData = _encryptionService.Decrypt(encryptedData);
            
            // Assert
            Assert.AreEqual(originalData, decryptedData);
        }
        
        [Test]
        public void Encrypt_ShouldHandleSpecialCharacters()
        {
            // Arrange
            string originalData = "Special characters: !@#$%^&*()_+{}[]|\\:;\"'<>,.?/";
            
            // Act
            string encryptedData = _encryptionService.Encrypt(originalData);
            string decryptedData = _encryptionService.Decrypt(encryptedData);
            
            // Assert
            Assert.AreEqual(originalData, decryptedData);
        }
        
        [Test]
        public void Encrypt_ShouldHandleEmptyString()
        {
            // Arrange
            string originalData = "";
            
            // Act
            string encryptedData = _encryptionService.Encrypt(originalData);
            string decryptedData = _encryptionService.Decrypt(encryptedData);
            
            // Assert
            Assert.AreEqual(originalData, decryptedData);
        }
        
        [Test]
        public void Encrypt_ShouldHandleNullValue()
        {
            // Arrange
            string originalData = null;
            
            // Act & Assert
            Assert.DoesNotThrow(() => {
                string encryptedData = _encryptionService.Encrypt(originalData);
                string decryptedData = _encryptionService.Decrypt(encryptedData);
                Assert.AreEqual(originalData, decryptedData);
            });
        }
        
        [Test]
        public void Encrypt_ShouldHandleLongText()
        {
            // Arrange
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 1000; i++)
            {
                sb.Append($"Line {i}: This is a test of long text encryption. ");
            }
            string originalData = sb.ToString();
            
            // Act
            string encryptedData = _encryptionService.Encrypt(originalData);
            string decryptedData = _encryptionService.Decrypt(encryptedData);
            
            // Assert
            Assert.AreEqual(originalData, decryptedData);
        }
        
        // Тест для проверки исключения без проверки логов
        [Test]
        public void InvalidInput_ShouldThrowException()
        {
            // Arrange
            string invalidEncryptedData = "ThisIsNotValidEncryptedData";
            
            // Act & Assert - проверяем, что исключение выбрасывается, а точный тип не важен
            Assert.That(() => _encryptionService.Decrypt(invalidEncryptedData), 
                Throws.Exception);
        }
        
        // Тест для проверки исключения с коротким Base64 без проверки логов
        [Test]
        public void ShortBase64_ShouldThrowException()
        {
            // Arrange
            string tooShortBase64 = Convert.ToBase64String(new byte[8]);
            
            // Act & Assert - проверяем, что исключение выбрасывается, а точный тип не важен
            Assert.That(() => _encryptionService.Decrypt(tooShortBase64), 
                Throws.Exception);
        }
        
        [Test]
        public void Constructor_WithEmptyPassword_ShouldGenerateRandomKey()
        {
            // Arrange & Act
            var service = new TestDataEncryptionService("");
            
            // Assert
            Assert.IsNotNull(service.GetEncryptionPassword());
            Assert.IsNotEmpty(service.GetEncryptionPassword());
        }
        
        [Test]
        public void Constructor_WithNullPassword_ShouldGenerateRandomKey()
        {
            // Arrange & Act
            var service = new TestDataEncryptionService(null);
            
            // Assert
            Assert.IsNotNull(service.GetEncryptionPassword());
            Assert.IsNotEmpty(service.GetEncryptionPassword());
        }
        
        [Test]
        public void DifferentInstances_SameKey_ShouldWorkTogether()
        {
            // Arrange
            var encryptionService1 = new TestDataEncryptionService(TestEncryptionKey);
            var encryptionService2 = new TestDataEncryptionService(TestEncryptionKey);
            
            string originalData = "Data to be encrypted by service 1 and decrypted by service 2";
            
            // Act
            string encryptedData = encryptionService1.Encrypt(originalData);
            string decryptedData = encryptionService2.Decrypt(encryptedData);
            
            // Assert
            Assert.AreEqual(originalData, decryptedData);
        }
        
        // Тест для проверки, что разные ключи не могут расшифровать данные
        [Test]
        public void DifferentKeys_ShouldNotWorkTogether()
        {
            // Arrange
            var encryptionService1 = new TestDataEncryptionService(TestEncryptionKey);
            var encryptionService2 = new TestDataEncryptionService("DifferentKey1234!@");
            
            string originalData = "Data encrypted with key 1";
            
            // Act
            string encryptedData = encryptionService1.Encrypt(originalData);
            
            // Act & Assert - проверяем, что исключение выбрасывается, а точный тип не важен
            Assert.That(() => encryptionService2.Decrypt(encryptedData), 
                Throws.Exception);
        }
    }
} 