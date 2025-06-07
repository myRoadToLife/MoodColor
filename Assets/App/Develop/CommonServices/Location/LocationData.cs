using System;
using Newtonsoft.Json;
using UnityEngine;

namespace App.Develop.CommonServices.Location
{
    /// <summary>
    /// Модель данных местоположения пользователя
    /// </summary>
    [Serializable]
    public class LocationData
    {
        [JsonProperty("latitude")]
        public double Latitude { get; set; }
        
        [JsonProperty("longitude")]
        public double Longitude { get; set; }
        
        [JsonProperty("accuracy")]
        public float Accuracy { get; set; }
        
        [JsonProperty("regionId")]
        public string RegionId { get; set; }
        
        [JsonProperty("regionName")]
        public string RegionName { get; set; }
        
        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }
        
        [JsonProperty("isValid")]
        public bool IsValid { get; set; }
        
        [JsonIgnore]
        public DateTime RecordTime => DateTime.FromFileTimeUtc(Timestamp);
        
        public LocationData()
        {
            Timestamp = DateTime.UtcNow.ToFileTimeUtc();
            IsValid = false;
        }
        
        public LocationData(double latitude, double longitude, float accuracy = 0f)
        {
            Latitude = latitude;
            Longitude = longitude;
            Accuracy = accuracy;
            Timestamp = DateTime.UtcNow.ToFileTimeUtc();
            IsValid = true;
        }
        
        /// <summary>
        /// Проверяет, действительны ли координаты
        /// </summary>
        public bool IsValidCoordinates()
        {
            return Latitude >= -90 && Latitude <= 90 && 
                   Longitude >= -180 && Longitude <= 180;
        }
        
        /// <summary>
        /// Вычисляет расстояние до другой точки в метрах
        /// </summary>
        public double DistanceTo(LocationData other)
        {
            if (other == null || !IsValidCoordinates() || !other.IsValidCoordinates())
                return double.MaxValue;
                
            const double earthRadius = 6371000; // метры
            
            double lat1Rad = Mathf.Deg2Rad * (float)Latitude;
            double lat2Rad = Mathf.Deg2Rad * (float)other.Latitude;
            double deltaLatRad = Mathf.Deg2Rad * (float)(other.Latitude - Latitude);
            double deltaLonRad = Mathf.Deg2Rad * (float)(other.Longitude - Longitude);
            
            double a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                      Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                      Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return earthRadius * c;
        }
        
        /// <summary>
        /// Создает копию данных о местоположении
        /// </summary>
        public LocationData Clone()
        {
            return new LocationData
            {
                Latitude = Latitude,
                Longitude = Longitude,
                Accuracy = Accuracy,
                RegionId = RegionId,
                RegionName = RegionName,
                Timestamp = Timestamp,
                IsValid = IsValid
            };
        }
        
        public override string ToString()
        {
            return $"Location(Lat: {Latitude:F6}, Lon: {Longitude:F6}, Region: {RegionName ?? "Unknown"}, Accuracy: {Accuracy:F1}m)";
        }
    }
    
    /// <summary>
    /// Статус разрешения на геолокацию
    /// </summary>
    public enum LocationPermissionStatus
    {
        NotDetermined,
        Denied,
        Granted,
        GrantedWhenInUse,
        GrantedAlways
    }
    
    /// <summary>
    /// Точность определения местоположения
    /// </summary>
    public enum LocationAccuracy
    {
        Low,        // ~1000м
        Medium,     // ~100м  
        High,       // ~10м
        Best        // Максимальная точность
    }
} 