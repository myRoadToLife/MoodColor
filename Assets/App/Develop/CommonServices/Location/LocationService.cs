using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using App.Develop.Utils.Logging;
using App.Develop.DI;

namespace App.Develop.CommonServices.Location
{
    /// <summary>
    /// Сервис геолокации для определения местоположения пользователя
    /// </summary>
    public class LocationService : MonoBehaviour, ILocationService, IInitializable
    {
        #region Private Fields
        
        private LocationData _currentLocation;
        private LocationData _cachedLocation;
        private bool _isTracking;
        private bool _isInitialized;
        private LocationPermissionStatus _permissionStatus;
        private DateTime _lastLocationUpdate;
        private readonly Dictionary<string, string> _regionCache = new Dictionary<string, string>();
        
        // Настройки геолокации
        private const float DESIRED_ACCURACY_IN_METERS = 10f;
        private const float UPDATE_DISTANCE_IN_METERS = 50f;
        private const int LOCATION_TIMEOUT_SECONDS = 30;
        private const int CACHE_EXPIRY_MINUTES = 10;
        
        // Предопределенные регионы Беларуси (в реальном проекте можно загружать из конфигурации)
        private readonly Dictionary<string, RegionBounds> _regions = new Dictionary<string, RegionBounds>
        {
            // Минск (столица) - разделен на районы
            ["minsk_center"] = new RegionBounds("Центральный район Минска", 53.9006, 27.5590, 0.03),
            ["minsk_north"] = new RegionBounds("Северные районы Минска", 53.9506, 27.5590, 0.08),
            ["minsk_south"] = new RegionBounds("Южные районы Минска", 53.8506, 27.5590, 0.08),
            ["minsk_east"] = new RegionBounds("Восточные районы Минска", 53.9006, 27.6590, 0.08),
            ["minsk_west"] = new RegionBounds("Западные районы Минска", 53.9006, 27.4590, 0.08),
            
            // Брестская область
            ["brest"] = new RegionBounds("Брест", 52.0977, 23.7340, 0.05),
            ["baranovichi"] = new RegionBounds("Барановичи", 53.1327, 26.0139, 0.03),
            ["pinsk"] = new RegionBounds("Пинск", 52.1229, 26.0951, 0.03),
            ["brest_region"] = new RegionBounds("Брестская область", 52.5, 24.5, 1.5),
            
            // Витебская область
            ["vitebsk"] = new RegionBounds("Витебск", 55.1904, 30.2049, 0.05),
            ["polotsk"] = new RegionBounds("Полоцк", 55.4870, 28.7856, 0.03),
            ["orsha"] = new RegionBounds("Орша", 54.5081, 30.4172, 0.03),
            ["vitebsk_region"] = new RegionBounds("Витебская область", 55.0, 29.0, 1.5),
            
            // Гомельская область
            ["gomel"] = new RegionBounds("Гомель", 52.4345, 30.9754, 0.05),
            ["mozyr"] = new RegionBounds("Мозырь", 52.0493, 29.2456, 0.03),
            ["rechitsa"] = new RegionBounds("Речица", 52.3616, 30.3913, 0.03),
            ["gomel_region"] = new RegionBounds("Гомельская область", 52.5, 29.5, 1.5),
            
            // Гродненская область
            ["grodno"] = new RegionBounds("Гродно", 53.6884, 23.8258, 0.05),
            ["lida"] = new RegionBounds("Лида", 53.8971, 25.2985, 0.03),
            ["slonim"] = new RegionBounds("Слоним", 53.0879, 25.3188, 0.03),
            ["grodno_region"] = new RegionBounds("Гродненская область", 53.5, 24.5, 1.5),
            
            // Минская область (исключая сам Минск)
            ["borisov"] = new RegionBounds("Борисов", 54.2279, 28.5050, 0.03),
            ["soligorsk"] = new RegionBounds("Солигорск", 52.7874, 27.5414, 0.03),
            ["molodechno"] = new RegionBounds("Молодечно", 54.3107, 26.8504, 0.03),
            ["minsk_region"] = new RegionBounds("Минская область", 53.5, 27.5, 1.5),
            
            // Могилевская область
            ["mogilev"] = new RegionBounds("Могилев", 53.9168, 30.3449, 0.05),
            ["bobruisk"] = new RegionBounds("Бобруйск", 53.1459, 29.2214, 0.03),
            ["krichev"] = new RegionBounds("Кричев", 53.7132, 31.3074, 0.03),
            ["mogilev_region"] = new RegionBounds("Могилевская область", 53.5, 30.0, 1.5),
            
            // Fallback регион
            ["default"] = new RegionBounds("Неизвестный регион", 0, 0, 180)
        };
        
        #endregion
        
        #region Events
        
        public event Action<LocationData> OnLocationChanged;
        public event Action<bool> OnPermissionStatusChanged;
        
        #endregion
        
        #region Properties
        
        public bool IsLocationPermissionGranted => 
            _permissionStatus == LocationPermissionStatus.Granted ||
            _permissionStatus == LocationPermissionStatus.GrantedWhenInUse ||
            _permissionStatus == LocationPermissionStatus.GrantedAlways;
            
        public bool IsLocationServiceEnabled => Input.location.isEnabledByUser;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus && _isTracking)
            {
                // Приложение возобновлено - обновляем местоположение
                _ = GetCurrentLocationAsync();
            }
        }
        
        private void OnDestroy()
        {
            StopLocationTracking();
        }
        
        #endregion
        
        #region IInitializable
        
        public void Initialize()
        {
            _ = InitializeAsync();
        }
        
        #endregion
        
        #region ILocationService Implementation
        
        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                MyLogger.Log("LocationService уже инициализирован", MyLogger.LogCategory.Location);
                return;
            }
            
            try
            {
                MyLogger.Log("🗺️ Инициализация LocationService...", MyLogger.LogCategory.Location);
                
                // Проверяем поддержку геолокации
                if (!SystemInfo.supportsLocationService)
                {
                    MyLogger.LogWarning("Устройство не поддерживает геолокацию", MyLogger.LogCategory.Location);
                    _permissionStatus = LocationPermissionStatus.Denied;
                    _isInitialized = true;
                    return;
                }
                
                // Загружаем кэшированное местоположение
                LoadCachedLocation();
                
                _isInitialized = true;
                MyLogger.Log("✅ LocationService инициализирован успешно", MyLogger.LogCategory.Location);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка инициализации LocationService: {ex.Message}", MyLogger.LogCategory.Location);
                _isInitialized = false;
            }
        }
        
        public async Task<LocationData> GetCurrentLocationAsync()
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }
            
            // Проверяем кэшированное местоположение
            if (_cachedLocation != null && IsCacheValid())
            {
                MyLogger.Log($"📍 Используем кэшированное местоположение: {_cachedLocation}", MyLogger.LogCategory.Location);
                return _cachedLocation;
            }
            
            try
            {
                // Запрашиваем разрешение
                bool hasPermission = await RequestLocationPermissionAsync();
                if (!hasPermission)
                {
                    MyLogger.LogWarning("Нет разрешения на геолокацию", MyLogger.LogCategory.Location);
                    return null;
                }
                
                MyLogger.Log("🔍 Запрашиваем текущее местоположение...", MyLogger.LogCategory.Location);
                
                // Запускаем службу геолокации
                Input.location.Start(DESIRED_ACCURACY_IN_METERS, UPDATE_DISTANCE_IN_METERS);
                
                // Ждем инициализации
                float timeoutTime = Time.time + LOCATION_TIMEOUT_SECONDS;
                while (Input.location.status == LocationServiceStatus.Initializing && Time.time < timeoutTime)
                {
                    await Task.Delay(100);
                }
                
                if (Input.location.status == LocationServiceStatus.Failed)
                {
                    MyLogger.LogError("❌ Не удалось запустить службу геолокации", MyLogger.LogCategory.Location);
                    return null;
                }
                
                if (Input.location.status != LocationServiceStatus.Running)
                {
                    MyLogger.LogWarning("⚠️ Служба геолокации не запущена", MyLogger.LogCategory.Location);
                    return null;
                }
                
                // Получаем координаты
                LocationInfo locationInfo = Input.location.lastData;
                var locationData = new LocationData(
                    locationInfo.latitude,
                    locationInfo.longitude,
                    locationInfo.horizontalAccuracy
                );
                
                // Определяем регион
                string regionId = await GetRegionIdAsync(locationData.Latitude, locationData.Longitude);
                locationData.RegionId = regionId;
                locationData.RegionName = GetRegionName(regionId);
                
                // Кэшируем результат
                _currentLocation = locationData;
                _cachedLocation = locationData.Clone();
                _lastLocationUpdate = DateTime.UtcNow;
                SaveLocationToCache(locationData);
                
                MyLogger.Log($"📍 Местоположение получено: {locationData}", MyLogger.LogCategory.Location);
                
                // Уведомляем о изменении
                OnLocationChanged?.Invoke(locationData);
                
                return locationData;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка получения местоположения: {ex.Message}", MyLogger.LogCategory.Location);
                return null;
            }
        }
        
        public async Task<string> GetRegionIdAsync(double latitude, double longitude)
        {
            try
            {
                string cacheKey = $"{latitude:F4}_{longitude:F4}";
                
                // Проверяем кэш
                if (_regionCache.TryGetValue(cacheKey, out string cachedRegionId))
                {
                    return cachedRegionId;
                }
                
                // Ищем подходящий регион
                foreach (var kvp in _regions)
                {
                    if (kvp.Value.ContainsPoint(latitude, longitude))
                    {
                        _regionCache[cacheKey] = kvp.Key;
                        return kvp.Key;
                    }
                }
                
                // Если не найден подходящий регион, возвращаем "default"
                _regionCache[cacheKey] = "default";
                return "default";
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка определения региона: {ex.Message}", MyLogger.LogCategory.Location);
                return "default";
            }
        }
        
        public async Task<bool> RequestLocationPermissionAsync()
        {
            try
            {
                if (!SystemInfo.supportsLocationService)
                {
                    _permissionStatus = LocationPermissionStatus.Denied;
                    return false;
                }
                
                if (!Input.location.isEnabledByUser)
                {
                    _permissionStatus = LocationPermissionStatus.Denied;
                    MyLogger.LogWarning("Геолокация отключена пользователем в настройках", MyLogger.LogCategory.Location);
                    return false;
                }
                
                _permissionStatus = LocationPermissionStatus.Granted;
                OnPermissionStatusChanged?.Invoke(true);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Ошибка запроса разрешения: {ex.Message}", MyLogger.LogCategory.Location);
                _permissionStatus = LocationPermissionStatus.Denied;
                OnPermissionStatusChanged?.Invoke(false);
                return false;
            }
        }
        
        public void StartLocationTracking()
        {
            if (_isTracking) return;
            
            MyLogger.Log("🔄 Начинаем отслеживание местоположения", MyLogger.LogCategory.Location);
            _isTracking = true;
            StartCoroutine(LocationTrackingCoroutine());
        }
        
        public void StopLocationTracking()
        {
            if (!_isTracking) return;
            
            MyLogger.Log("⏹️ Останавливаем отслеживание местоположения", MyLogger.LogCategory.Location);
            _isTracking = false;
            Input.location.Stop();
        }
        
        public void ClearLocationCache()
        {
            _cachedLocation = null;
            _regionCache.Clear();
            PlayerPrefs.DeleteKey("CachedLocation");
            MyLogger.Log("🗑️ Кэш местоположений очищен", MyLogger.LogCategory.Location);
        }
        
        #endregion
        
        #region Private Methods
        
        private bool IsCacheValid()
        {
            if (_cachedLocation == null) return false;
            
            TimeSpan timeSinceUpdate = DateTime.UtcNow - _lastLocationUpdate;
            return timeSinceUpdate.TotalMinutes < CACHE_EXPIRY_MINUTES;
        }
        
        private void LoadCachedLocation()
        {
            try
            {
                string cachedJson = PlayerPrefs.GetString("CachedLocation", "");
                if (!string.IsNullOrEmpty(cachedJson))
                {
                    _cachedLocation = JsonUtility.FromJson<LocationData>(cachedJson);
                    string lastUpdateStr = PlayerPrefs.GetString("LastLocationUpdate", "");
                    if (DateTime.TryParse(lastUpdateStr, out DateTime lastUpdate))
                    {
                        _lastLocationUpdate = lastUpdate;
                    }
                    
                    MyLogger.Log($"📱 Загружено кэшированное местоположение: {_cachedLocation}", MyLogger.LogCategory.Location);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogWarning($"⚠️ Не удалось загрузить кэшированное местоположение: {ex.Message}", MyLogger.LogCategory.Location);
            }
        }
        
        private void SaveLocationToCache(LocationData location)
        {
            try
            {
                string json = JsonUtility.ToJson(location);
                PlayerPrefs.SetString("CachedLocation", json);
                PlayerPrefs.SetString("LastLocationUpdate", DateTime.UtcNow.ToString());
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                MyLogger.LogWarning($"⚠️ Не удалось сохранить местоположение в кэш: {ex.Message}", MyLogger.LogCategory.Location);
            }
        }
        
        private string GetRegionName(string regionId)
        {
            return _regions.TryGetValue(regionId, out RegionBounds region) ? region.Name : "Неизвестный регион";
        }
        
        private IEnumerator LocationTrackingCoroutine()
        {
            while (_isTracking)
            {
                yield return new WaitForSeconds(30f); // Обновляем каждые 30 секунд
                
                if (_isTracking && IsLocationPermissionGranted)
                {
                    _ = GetCurrentLocationAsync();
                }
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Границы региона для определения принадлежности координат
    /// </summary>
    public class RegionBounds
    {
        public string Name { get; }
        public double CenterLatitude { get; }
        public double CenterLongitude { get; }
        public double RadiusDegrees { get; }
        
        public RegionBounds(string name, double centerLat, double centerLon, double radiusDegrees)
        {
            Name = name;
            CenterLatitude = centerLat;
            CenterLongitude = centerLon;
            RadiusDegrees = radiusDegrees;
        }
        
        public bool ContainsPoint(double latitude, double longitude)
        {
            double distance = Math.Sqrt(
                Math.Pow(latitude - CenterLatitude, 2) + 
                Math.Pow(longitude - CenterLongitude, 2)
            );
            return distance <= RadiusDegrees;
        }
    }
} 