using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace App.Develop.CommonServices.Firebase.RemoteConfig.Services
{
    /// <summary>
    /// Интерфейс сервиса для работы с Firebase Remote Config
    /// </summary>
    public interface IFirebaseRemoteConfigService
    {
        /// <summary>
        /// Событие, вызываемое при обновлении конфигурации
        /// </summary>
        event Action OnConfigUpdated;
        
        /// <summary>
        /// Получение последнего времени обновления
        /// </summary>
        DateTime LastFetchTime { get; }
        
        /// <summary>
        /// Инициализирует сервис и загружает значения по умолчанию
        /// </summary>
        /// <param name="defaults">Словарь значений по умолчанию</param>
        void Initialize(Dictionary<string, object> defaults = null);
        
        /// <summary>
        /// Асинхронно загружает и активирует Remote Config
        /// </summary>
        /// <param name="cacheExpirationInSeconds">Время в секундах, после которого кэш истекает</param>
        Task<bool> FetchAndActivateAsync(long cacheExpirationInSeconds = 3600);
        
        /// <summary>
        /// Получает значение из Remote Config в виде строки
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="defaultValue">Значение по умолчанию</param>
        string GetString(string key, string defaultValue = "");
        
        /// <summary>
        /// Получает значение из Remote Config в виде булева
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="defaultValue">Значение по умолчанию</param>
        bool GetBool(string key, bool defaultValue = false);
        
        /// <summary>
        /// Получает значение из Remote Config в виде числа с плавающей точкой
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="defaultValue">Значение по умолчанию</param>
        float GetFloat(string key, float defaultValue = 0f);
        
        /// <summary>
        /// Получает значение из Remote Config в виде целого числа
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="defaultValue">Значение по умолчанию</param>
        int GetInt(string key, int defaultValue = 0);
        
        /// <summary>
        /// Получает значение из Remote Config в виде длинного целого числа
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="defaultValue">Значение по умолчанию</param>
        long GetLong(string key, long defaultValue = 0L);
        
        /// <summary>
        /// Получает значение из Remote Config в виде JSON-объекта
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <param name="key">Ключ</param>
        /// <param name="defaultValue">Значение по умолчанию</param>
        T GetJson<T>(string key, T defaultValue = default);
        
        /// <summary>
        /// Получает все ключи Remote Config
        /// </summary>
        IEnumerable<string> GetAllKeys();
    }
}