// Assets/App/Develop/AppServices/Firebase/Common/SecureStorage/SecurePlayerPrefs.cs
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Common.SecureStorage
{
    public static class SecurePlayerPrefs
    {
        private static string _encryptionKey;
        private static bool _isInitialized;
        private static readonly string _deviceSalt = SystemInfo.deviceUniqueIdentifier;
        private static readonly string CHECK_KEY = "_secure_key_check";
        private static readonly string CHECK_VALUE = "SECURE_DATA_CHECK_2023";

        // Метод инициализации с безопасным ключом
        public static void Init(string securityKey)
        {
            if (string.IsNullOrEmpty(securityKey))
                throw new ArgumentException("Security key cannot be empty", nameof(securityKey));

            try
            {
                // Генерируем новый ключ шифрования
                string newKey = GenerateKey(securityKey + _deviceSalt);
                
                // Проверяем, есть ли сохраненный хеш ключа
                if (PlayerPrefs.HasKey(CHECK_KEY))
                {
                    // Получаем сохраненное проверочное значение
                    string storedCheck = PlayerPrefs.GetString(CHECK_KEY);
                    
                    // Временно устанавливаем ключ
                    _encryptionKey = newKey;
                    _isInitialized = true;
                    
                    try
                    {
                        // Пробуем расшифровать проверочное значение
                        string decrypted = DecryptString(storedCheck);
                        if (decrypted != CHECK_VALUE)
                        {
                            throw new Exception("Ключ шифрования изменился");
                        }
                        Debug.Log("✅ Проверка ключа шифрования успешна");
                    }
                    catch
                    {
                        // Если не удалось расшифровать, значит ключ изменился
                        Debug.LogWarning("⚠️ Обнаружено изменение ключа шифрования. Очистка данных.");
                        PlayerPrefs.DeleteAll();
                        
                        // Сохраняем новое проверочное значение
                        string encryptedCheck = EncryptString(CHECK_VALUE);
                        PlayerPrefs.SetString(CHECK_KEY, encryptedCheck);
                        PlayerPrefs.Save();
                    }
                }
                else
                {
                    // Первичная инициализация
                    _encryptionKey = newKey;
                    _isInitialized = true;
                    
                    // Сохраняем проверочное значение
                    string encryptedCheck = EncryptString(CHECK_VALUE);
                    PlayerPrefs.SetString(CHECK_KEY, encryptedCheck);
                    PlayerPrefs.Save();
                    Debug.Log("✅ Первичная инициализация SecurePlayerPrefs выполнена");
                }
                
                Debug.Log("✅ SecurePlayerPrefs инициализирован успешно");
            }
            catch (Exception ex)
            {
                // Если произошла ошибка, очищаем все данные для безопасности
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                
                Debug.LogError($"❌ Ошибка инициализации SecurePlayerPrefs: {ex.Message}");
                throw;
            }
        }

        // Генерация ключа с помощью SHA256
        private static string GenerateKey(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(hashBytes);
            }
        }

        // Проверка инициализации
        private static void CheckInitialization()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("SecurePlayerPrefs не инициализирован. Вызовите Init() перед использованием.");
            }
        }

        // Новый публичный метод для проверки инициализации
        public static bool IsInitialized()
        {
            return _isInitialized;
        }

        // Методы для работы с разными типами данных
        public static void SetString(string key, string value)
        {
            CheckInitialization();
            
            try
            {
                string encryptedValue = EncryptString(value);
                PlayerPrefs.SetString(key, encryptedValue);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка сохранения строки для ключа {key}: {ex.Message}");
                throw;
            }
        }

        public static string GetString(string key, string defaultValue = "")
        {
            CheckInitialization();
            
            if (!PlayerPrefs.HasKey(key))
                return defaultValue;

            try
            {
                string encryptedValue = PlayerPrefs.GetString(key);
                return DecryptString(encryptedValue);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"⚠️ Не удалось расшифровать значение для ключа {key}: {ex.Message}");
                return defaultValue;
            }
        }

        public static void SetInt(string key, int value)
        {
            SetString(key, value.ToString());
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
            string strValue = GetString(key, defaultValue.ToString());
            if (int.TryParse(strValue, out int result))
                return result;
            return defaultValue;
        }

        public static void SetFloat(string key, float value)
        {
            SetString(key, value.ToString("R"));
        }

        public static float GetFloat(string key, float defaultValue = 0f)
        {
            string strValue = GetString(key, defaultValue.ToString("R"));
            if (float.TryParse(strValue, out float result))
                return result;
            return defaultValue;
        }

        public static void SetBool(string key, bool value)
        {
            SetInt(key, value ? 1 : 0);
        }

        public static bool GetBool(string key, bool defaultValue = false)
        {
            return GetInt(key, defaultValue ? 1 : 0) != 0;
        }

        public static bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public static void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }

        public static void DeleteAll()
        {
            // Сохраняем текущую инициализацию
            bool wasInitialized = _isInitialized;
            string oldKey = _encryptionKey;
            
            // Удаляем все ключи кроме проверочного
            string checkValue = null;
            if (wasInitialized && PlayerPrefs.HasKey(CHECK_KEY))
            {
                checkValue = PlayerPrefs.GetString(CHECK_KEY);
            }
            
            PlayerPrefs.DeleteAll();
            
            // Восстанавливаем ключ проверки, если был инициализирован
            if (wasInitialized && checkValue != null)
            {
                // Восстанавливаем состояние
                _isInitialized = true;
                _encryptionKey = oldKey;
                
                // Создаем новый проверочный ключ
                string encryptedCheck = EncryptString(CHECK_VALUE);
                PlayerPrefs.SetString(CHECK_KEY, encryptedCheck);
            }
        }

        public static void Save()
        {
            PlayerPrefs.Save();
        }

        // Шифрование с использованием AES-128
        private static string EncryptString(string plainText)
        {
            try
            {
                // Получаем стабильный ключ нужной длины
                byte[] keyBytes;
                using (var sha256 = SHA256.Create())
                {
                    keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(_encryptionKey));
                }
                
                // Используем первые 16 байт для AES-128
                byte[] key = new byte[16];
                Array.Copy(keyBytes, key, 16);
                
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.GenerateIV();
                    byte[] iv = aes.IV;

                    using (MemoryStream ms = new MemoryStream())
                    {
                        // Сначала пишем вектор инициализации
                        ms.Write(iv, 0, iv.Length);

                        using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }

                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка шифрования: {ex.Message}");
                throw;
            }
        }

        // Дешифрование
        private static string DecryptString(string cipherText)
        {
            try
            {
                // Получаем стабильный ключ нужной длины
                byte[] keyBytes;
                using (var sha256 = SHA256.Create())
                {
                    keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(_encryptionKey));
                }
                
                // Используем первые 16 байт для AES-128
                byte[] key = new byte[16];
                Array.Copy(keyBytes, key, 16);
                
                byte[] fullCipher = Convert.FromBase64String(cipherText);

                // Проверяем, что у нас достаточно данных для IV и сообщения
                if (fullCipher.Length < 16)
                {
                    throw new CryptographicException("Недостаточно данных для дешифрования");
                }

                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    
                    // Первые 16 байт - вектор инициализации
                    byte[] iv = new byte[16];
                    Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                    aes.IV = iv;

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            // Пропускаем вектор инициализации
                            cs.Write(fullCipher, iv.Length, fullCipher.Length - iv.Length);
                        }

                        return Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка расшифровки: {ex.Message}");
                throw;
            }
        }
    }
}