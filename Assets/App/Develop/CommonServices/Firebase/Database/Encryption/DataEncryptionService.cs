using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Database.Encryption
{
    /// <summary>
    /// Сервис для шифрования и дешифрования данных Firebase
    /// </summary>
    public class DataEncryptionService : IDataEncryptionService
    {
        #region Constants
        // Размер ключа AES (16 байт = 128 бит, 24 байта = 192 бита, 32 байта = 256 бит)
        private const int c_KeySize = 32;
        
        // Размер вектора инициализации (всегда 16 байт для AES)
        private const int c_IvSize = 16;
        
        // Размер соли для генерации ключа
        private const int c_SaltSize = 16;
        
        // Количество итераций для PBKDF2
        private const int c_Iterations = 10000;
        #endregion

        #region Private Fields
        private readonly byte[] _encryptionKey;
        private readonly string _encryptionPassword;
        #endregion

        #region Constructor
        /// <summary>
        /// Создает новый экземпляр сервиса шифрования с указанным паролем
        /// </summary>
        /// <param name="password">Пароль для шифрования (если не указан, будет использоваться случайный ключ)</param>
        public DataEncryptionService(string password = null)
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
            
            Debug.Log("✅ DataEncryptionService инициализирован");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Шифрует строку с помощью AES
        /// </summary>
        /// <param name="plainText">Исходная строка</param>
        /// <returns>Зашифрованная строка в формате Base64</returns>
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
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка шифрования: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Дешифрует строку, зашифрованную с помощью Encrypt
        /// </summary>
        /// <param name="encryptedText">Зашифрованная строка в формате Base64</param>
        /// <returns>Расшифрованная строка</returns>
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
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка дешифрования: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Получает текущий пароль для шифрования
        /// </summary>
        /// <returns>Пароль шифрования</returns>
        public string GetEncryptionPassword()
        {
            return _encryptionPassword;
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Проверяет, является ли строка допустимой строкой Base64
        /// </summary>
        /// <param name="input">Строка для проверки</param>
        /// <returns>True, если строка представляет собой допустимый Base64</returns>
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
        #endregion
    }
} 