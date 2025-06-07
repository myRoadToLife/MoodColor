using System;
using System.Threading.Tasks;
using UnityEngine;

namespace App.Develop.CommonServices.Location
{
    /// <summary>
    /// Интерфейс сервиса геолокации для определения местоположения пользователя
    /// </summary>
    public interface ILocationService
    {
        /// <summary>
        /// Получить текущее местоположение пользователя
        /// </summary>
        /// <returns>Данные о местоположении или null, если не удалось определить</returns>
        Task<LocationData> GetCurrentLocationAsync();
        
        /// <summary>
        /// Получить ID региона по координатам
        /// </summary>
        /// <param name="latitude">Широта</param>
        /// <param name="longitude">Долгота</param>
        /// <returns>Идентификатор региона</returns>
        Task<string> GetRegionIdAsync(double latitude, double longitude);
        
        /// <summary>
        /// Проверить, есть ли разрешение на использование геолокации
        /// </summary>
        bool IsLocationPermissionGranted { get; }
        
        /// <summary>
        /// Запросить разрешение на использование геолокации
        /// </summary>
        /// <returns>True, если разрешение получено</returns>
        Task<bool> RequestLocationPermissionAsync();
        
        /// <summary>
        /// Проверить, включена ли служба геолокации на устройстве
        /// </summary>
        bool IsLocationServiceEnabled { get; }
        
        /// <summary>
        /// Событие изменения местоположения
        /// </summary>
        event Action<LocationData> OnLocationChanged;
        
        /// <summary>
        /// Событие изменения статуса разрешений
        /// </summary>
        event Action<bool> OnPermissionStatusChanged;
        
        /// <summary>
        /// Инициализировать сервис
        /// </summary>
        Task InitializeAsync();
        
        /// <summary>
        /// Начать отслеживание местоположения
        /// </summary>
        void StartLocationTracking();
        
        /// <summary>
        /// Остановить отслеживание местоположения
        /// </summary>
        void StopLocationTracking();
        
        /// <summary>
        /// Очистить кэш местоположений
        /// </summary>
        void ClearLocationCache();
    }
} 