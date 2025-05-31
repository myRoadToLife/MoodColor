using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using App.Develop.Utils.Logging;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using Newtonsoft.Json;
using UnityEngine;

namespace App.Develop.CommonServices.Firebase.RemoteConfig.Services
{
    /// <summary>
    /// Сервис для работы с Firebase Remote Config
    /// </summary>
    public class FirebaseRemoteConfigService : IFirebaseRemoteConfigService
    {
        #region Private Fields
        private bool _isInitialized;
        private Dictionary<string, object> _defaultValues;
        private Dictionary<string, string> _cachedJsonValues = new Dictionary<string, string>();
        #endregion
        
        #region Events
        /// <summary>
        /// Событие, вызываемое при обновлении конфигурации
        /// </summary>
        public event Action OnConfigUpdated;
        #endregion
        
        #region Properties
        /// <summary>
        /// Получение последнего времени обновления
        /// </summary>
        public DateTime LastFetchTime 
        { 
            get 
            {
                if (!_isInitialized) return DateTime.MinValue;
                
                try
                {
                    // FetchTime уже имеет тип DateTime, просто возвращаем его
                    return FirebaseRemoteConfig.DefaultInstance.Info.FetchTime;
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"❌ [RemoteConfig] Ошибка при получении времени последнего обновления: {ex.Message}", MyLogger.LogCategory.Firebase);
                    return DateTime.MinValue;
                }
            }
        }
        #endregion
        
        #region Constructor
        /// <summary>
        /// Создает новый экземпляр сервиса Firebase Remote Config
        /// </summary>
        public FirebaseRemoteConfigService()
        {
            MyLogger.Log("✅ FirebaseRemoteConfigService создан", MyLogger.LogCategory.Firebase);
        }
        #endregion
        
        #region IFirebaseRemoteConfigService Implementation
        /// <summary>
        /// Инициализирует сервис и загружает значения по умолчанию
        /// </summary>
        /// <param name="defaults">Словарь значений по умолчанию</param>
        public void Initialize(Dictionary<string, object> defaults = null)
        {
            if (_isInitialized) return;
            
            try
            {
                _defaultValues = defaults ?? new Dictionary<string, object>();
                
                // Устанавливаем параметры кэширования
                var configSettings = new ConfigSettings();
                configSettings.FetchTimeoutInMilliseconds = 60000; // 1 минута
                configSettings.MinimumFetchIntervalInMilliseconds = 3600 * 1000; // 1 час
                FirebaseRemoteConfig.DefaultInstance.SetConfigSettingsAsync(configSettings);
                
                // Устанавливаем значения по умолчанию, если они указаны
                if (_defaultValues.Count > 0)
                {
                    FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(_defaultValues).ContinueWithOnMainThread(task =>
                    {
                        if (task.IsFaulted)
                        {
                            MyLogger.LogError($"❌ [RemoteConfig] Ошибка при установке значений по умолчанию: {task.Exception}", MyLogger.LogCategory.Firebase);
                        }
                        else
                        {
                            MyLogger.Log($"✅ [RemoteConfig] Значения по умолчанию установлены ({_defaultValues.Count} значений)", MyLogger.LogCategory.Firebase);
                        }
                    });
                }
                
                _isInitialized = true;
                MyLogger.Log("✅ FirebaseRemoteConfigService инициализирован", MyLogger.LogCategory.Firebase);
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                MyLogger.LogError($"❌ Ошибка инициализации FirebaseRemoteConfigService: {ex.Message}\n{ex.StackTrace}", MyLogger.LogCategory.Firebase);
            }
        }
        
        /// <summary>
        /// Асинхронно загружает и активирует Remote Config
        /// </summary>
        /// <param name="cacheExpirationInSeconds">Время в секундах, после которого кэш истекает</param>
        public async Task<bool> FetchAndActivateAsync(long cacheExpirationInSeconds = 3600)
        {
            if (!_isInitialized)
            {
                MyLogger.LogWarning("⚠️ [RemoteConfig] Попытка загрузить конфигурацию до инициализации сервиса", MyLogger.LogCategory.Firebase);
                return false;
            }
            
            try
            {
                // Очищаем кэш JSON-значений
                _cachedJsonValues.Clear();
                
                // Загружаем данные из Firebase
                await FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.FromSeconds(cacheExpirationInSeconds));
                
                // Активируем загруженные данные
                bool success = await FirebaseRemoteConfig.DefaultInstance.ActivateAsync();
                
                if (success)
                {
                    MyLogger.Log($"✅ [RemoteConfig] Конфигурация успешно загружена и активирована. Время: {LastFetchTime}", MyLogger.LogCategory.Firebase);
                    
                    // Вызываем событие обновления конфигурации
                    OnConfigUpdated?.Invoke();
                }
                else
                {
                    MyLogger.LogError("❌ [RemoteConfig] Ошибка при активации конфигурации", MyLogger.LogCategory.Firebase);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [RemoteConfig] Ошибка при загрузке и активации конфигурации: {ex.Message}", MyLogger.LogCategory.Firebase);
                return false;
            }
        }
        
        /// <summary>
        /// Получает значение из Remote Config в виде строки
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="defaultValue">Значение по умолчанию</param>
        public string GetString(string key, string defaultValue = "")
        {
            if (!_isInitialized || string.IsNullOrEmpty(key))
                return defaultValue;
            
            try
            {
                return FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [RemoteConfig] Ошибка при получении строкового значения для ключа {key}: {ex.Message}", MyLogger.LogCategory.Firebase);
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Получает значение из Remote Config в виде булева
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="defaultValue">Значение по умолчанию</param>
        public bool GetBool(string key, bool defaultValue = false)
        {
            if (!_isInitialized || string.IsNullOrEmpty(key))
                return defaultValue;
            
            try
            {
                return FirebaseRemoteConfig.DefaultInstance.GetValue(key).BooleanValue;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [RemoteConfig] Ошибка при получении булева значения для ключа {key}: {ex.Message}", MyLogger.LogCategory.Firebase);
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Получает значение из Remote Config в виде числа с плавающей точкой
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="defaultValue">Значение по умолчанию</param>
        public float GetFloat(string key, float defaultValue = 0f)
        {
            if (!_isInitialized || string.IsNullOrEmpty(key))
                return defaultValue;
            
            try
            {
                return (float)FirebaseRemoteConfig.DefaultInstance.GetValue(key).DoubleValue;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [RemoteConfig] Ошибка при получении значения float для ключа {key}: {ex.Message}", MyLogger.LogCategory.Firebase);
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Получает значение из Remote Config в виде целого числа
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="defaultValue">Значение по умолчанию</param>
        public int GetInt(string key, int defaultValue = 0)
        {
            if (!_isInitialized || string.IsNullOrEmpty(key))
                return defaultValue;
            
            try
            {
                return (int)FirebaseRemoteConfig.DefaultInstance.GetValue(key).LongValue;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [RemoteConfig] Ошибка при получении значения int для ключа {key}: {ex.Message}", MyLogger.LogCategory.Firebase);
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Получает значение из Remote Config в виде длинного целого числа
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="defaultValue">Значение по умолчанию</param>
        public long GetLong(string key, long defaultValue = 0L)
        {
            if (!_isInitialized || string.IsNullOrEmpty(key))
                return defaultValue;
            
            try
            {
                return FirebaseRemoteConfig.DefaultInstance.GetValue(key).LongValue;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [RemoteConfig] Ошибка при получении значения long для ключа {key}: {ex.Message}", MyLogger.LogCategory.Firebase);
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Получает значение из Remote Config в виде JSON-объекта
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <param name="key">Ключ</param>
        /// <param name="defaultValue">Значение по умолчанию</param>
        public T GetJson<T>(string key, T defaultValue = default)
        {
            if (!_isInitialized || string.IsNullOrEmpty(key))
                return defaultValue;
            
            try
            {
                string jsonString;
                
                // Проверяем, есть ли значение в кэше
                if (!_cachedJsonValues.TryGetValue(key, out jsonString))
                {
                    // Если нет, получаем из Firebase и кэшируем
                    jsonString = FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
                    _cachedJsonValues[key] = jsonString;
                }
                
                if (string.IsNullOrEmpty(jsonString))
                    return defaultValue;
                
                // Десериализуем JSON
                T result = JsonConvert.DeserializeObject<T>(jsonString);
                return result != null ? result : defaultValue;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [RemoteConfig] Ошибка при получении JSON-значения для ключа {key}: {ex.Message}", MyLogger.LogCategory.Firebase);
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Получает все ключи Remote Config
        /// </summary>
        public IEnumerable<string> GetAllKeys()
        {
            if (!_isInitialized)
                return Enumerable.Empty<string>();
            
            try
            {
                return FirebaseRemoteConfig.DefaultInstance.Keys;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ [RemoteConfig] Ошибка при получении всех ключей: {ex.Message}", MyLogger.LogCategory.Firebase);
                return Enumerable.Empty<string>();
            }
        }
        #endregion
    }
} 