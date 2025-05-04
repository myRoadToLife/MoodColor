using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using App.Develop.CommonServices.DataManagement;
using App.Develop.CommonServices.DataManagement.DataProviders;
using Newtonsoft.Json;
using UnityEngine;

namespace App.Develop.AppServices.Firebase.Common.Cache
{
    /// <summary>
    /// Менеджер кэширования данных Firebase с поддержкой офлайн режима
    /// </summary>
    public class FirebaseCacheManager
    {
        #region Private Fields
        private readonly IDataRepository _dataRepository;
        private readonly TimeSpan _defaultCacheExpiration = TimeSpan.FromHours(24);
        private readonly Dictionary<string, CacheItem> _memoryCache = new Dictionary<string, CacheItem>();
        #endregion

        #region Constructor
        public FirebaseCacheManager(ISaveLoadService saveLoadService)
        {
            if (saveLoadService == null)
                throw new ArgumentNullException(nameof(saveLoadService));
            
            // Получаем доступ к IDataRepository через замыкание
            // Поскольку мы не можем напрямую получить IDataRepository из ISaveLoadService,
            // нам нужно использовать его косвенно через SaveLoadService
            if (saveLoadService is SaveLoadService sls && 
                typeof(SaveLoadService).GetField("_dataRepository", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance)?.GetValue(sls) is IDataRepository repo)
            {
                _dataRepository = repo;
            }
            else
            {
                _dataRepository = new InMemoryDataRepository();
                Debug.LogWarning("⚠️ Не удалось получить IDataRepository, будет использован InMemoryDataRepository");
            }
            
            LoadCache();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Получает данные из кэша или выполняет функцию для их получения
        /// </summary>
        /// <typeparam name="T">Тип данных</typeparam>
        /// <param name="key">Ключ кэша</param>
        /// <param name="fetchFunction">Функция для получения данных</param>
        /// <param name="expiration">Время истечения кэша</param>
        /// <param name="forceRefresh">Принудительное обновление кэша</param>
        /// <returns>Результат из кэша или полученный через функцию</returns>
        public async Task<T> GetOrFetchAsync<T>(string key, Func<Task<T>> fetchFunction, 
            TimeSpan? expiration = null, bool forceRefresh = false)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Ключ кэша не может быть пустым", nameof(key));

            if (fetchFunction == null)
                throw new ArgumentNullException(nameof(fetchFunction));

            // Проверяем кэш в памяти
            if (!forceRefresh && _memoryCache.TryGetValue(key, out CacheItem cacheItem) && 
                !IsCacheExpired(cacheItem))
            {
                Debug.Log($"✅ Данные получены из памяти для ключа: {key}");
                return JsonConvert.DeserializeObject<T>(cacheItem.Data);
            }

            // Пытаемся получить данные через функцию
            try
            {
                T data = await fetchFunction();
                
                if (data != null)
                {
                    // Кэшируем результат
                    string json = JsonConvert.SerializeObject(data);
                    CacheData(key, json, expiration ?? _defaultCacheExpiration);
                    Debug.Log($"✅ Новые данные загружены и кэшированы для ключа: {key}");
                    return data;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"⚠️ Ошибка при получении данных: {ex.Message}. Возвращаем кэшированные данные.");
                
                // Если возникла ошибка, проверяем диск-кэш
                CacheItem diskCache = LoadFromDiskCache(key);
                if (diskCache != null && !IsCacheExpired(diskCache))
                {
                    Debug.Log($"✅ Данные восстановлены из диск-кэша для ключа: {key}");
                    // Обновляем кэш в памяти
                    _memoryCache[key] = diskCache;
                    return JsonConvert.DeserializeObject<T>(diskCache.Data);
                }
            }

            // Проверяем устаревший кэш как последний вариант
            if (_memoryCache.TryGetValue(key, out CacheItem expiredCache))
            {
                Debug.LogWarning($"⚠️ Используем устаревший кэш для ключа: {key}");
                return JsonConvert.DeserializeObject<T>(expiredCache.Data);
            }

            Debug.LogError($"❌ Не удалось получить данные для ключа: {key}");
            return default;
        }

        /// <summary>
        /// Добавляет данные в кэш
        /// </summary>
        /// <typeparam name="T">Тип данных</typeparam>
        /// <param name="key">Ключ кэша</param>
        /// <param name="data">Данные для кэширования</param>
        /// <param name="expiration">Время истечения кэша</param>
        public void Set<T>(string key, T data, TimeSpan? expiration = null)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Ключ кэша не может быть пустым", nameof(key));

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            string json = JsonConvert.SerializeObject(data);
            CacheData(key, json, expiration ?? _defaultCacheExpiration);
            Debug.Log($"✅ Данные добавлены в кэш для ключа: {key}");
        }

        /// <summary>
        /// Получает данные из кэша (если они есть и не устарели)
        /// </summary>
        /// <typeparam name="T">Тип данных</typeparam>
        /// <param name="key">Ключ кэша</param>
        /// <param name="includeExpired">Включать ли устаревшие данные</param>
        /// <returns>Кэшированные данные или default</returns>
        public T Get<T>(string key, bool includeExpired = false)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Ключ кэша не может быть пустым", nameof(key));

            if (_memoryCache.TryGetValue(key, out CacheItem cacheItem) && 
                (includeExpired || !IsCacheExpired(cacheItem)))
            {
                Debug.Log($"✅ Данные получены из кэша для ключа: {key}");
                return JsonConvert.DeserializeObject<T>(cacheItem.Data);
            }

            // Пробуем получить из диск-кэша
            CacheItem diskCache = LoadFromDiskCache(key);
            if (diskCache != null && (includeExpired || !IsCacheExpired(diskCache)))
            {
                Debug.Log($"✅ Данные получены из диск-кэша для ключа: {key}");
                // Обновляем кэш в памяти
                _memoryCache[key] = diskCache;
                return JsonConvert.DeserializeObject<T>(diskCache.Data);
            }

            Debug.LogWarning($"⚠️ Кэш не найден для ключа: {key}");
            return default;
        }

        /// <summary>
        /// Удаляет данные из кэша
        /// </summary>
        /// <param name="key">Ключ кэша</param>
        public void Remove(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Ключ кэша не может быть пустым", nameof(key));

            _memoryCache.Remove(key);
            
            // Удаляем из постоянного хранилища
            try
            {
                _dataRepository?.Remove(GetCacheKey(key));
                Debug.Log($"✅ Кэш удален для ключа: {key}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка при удалении кэша: {ex.Message}");
            }
        }

        /// <summary>
        /// Очищает весь кэш
        /// </summary>
        public void Clear()
        {
            _memoryCache.Clear();
            Debug.Log("✅ Кэш очищен");
            
            // Очистка постоянного хранилища не реализована, так как требует доступа к списку ключей
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Кэширует данные
        /// </summary>
        private void CacheData(string key, string data, TimeSpan expiration)
        {
            DateTime expirationTime = DateTime.UtcNow.Add(expiration);
            CacheItem item = new CacheItem
            {
                Data = data,
                ExpirationTime = expirationTime
            };

            _memoryCache[key] = item;
            
            // Сохраняем в постоянное хранилище
            try
            {
                string serializedItem = JsonConvert.SerializeObject(item);
                _dataRepository?.Write(GetCacheKey(key), serializedItem);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка при сохранении кэша: {ex.Message}");
            }
        }

        /// <summary>
        /// Проверяет, устарел ли кэш
        /// </summary>
        private bool IsCacheExpired(CacheItem cacheItem)
        {
            return cacheItem.ExpirationTime < DateTime.UtcNow;
        }

        /// <summary>
        /// Загружает кэш из постоянного хранилища
        /// </summary>
        private CacheItem LoadFromDiskCache(string key)
        {
            try
            {
                if (_dataRepository != null && _dataRepository.Exists(GetCacheKey(key)))
                {
                    string serializedData = _dataRepository.Read(GetCacheKey(key));
                    return JsonConvert.DeserializeObject<CacheItem>(serializedData);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Ошибка при загрузке кэша из хранилища: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Формирует ключ для постоянного хранилища
        /// </summary>
        private string GetCacheKey(string key)
        {
            return $"firebase_cache_{key}";
        }

        /// <summary>
        /// Загружает кэш при инициализации
        /// </summary>
        private void LoadCache()
        {
            // Здесь может быть дополнительная логика для предзагрузки часто используемых кэшей
            Debug.Log("✅ FirebaseCacheManager инициализирован");
        }
        #endregion

        #region Nested Classes
        /// <summary>
        /// Элемент кэша
        /// </summary>
        [Serializable]
        private class CacheItem : ISaveData
        {
            public string Data { get; set; }
            public DateTime ExpirationTime { get; set; }
        }
        
        /// <summary>
        /// Реализация IDataRepository для хранения в памяти (резервный вариант)
        /// </summary>
        private class InMemoryDataRepository : IDataRepository
        {
            private readonly Dictionary<string, string> _storage = new Dictionary<string, string>();
            
            public string Read(string key) => _storage.TryGetValue(key, out var value) ? value : null;
            
            public void Write(string key, string serializedData) => _storage[key] = serializedData;
            
            public void Remove(string key) => _storage.Remove(key);
            
            public bool Exists(string key) => _storage.ContainsKey(key);
        }
        #endregion
    }
} 