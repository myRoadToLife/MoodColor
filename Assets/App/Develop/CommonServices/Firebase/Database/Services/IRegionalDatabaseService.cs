using System.Collections.Generic;
using System.Threading.Tasks;
using App.Develop.CommonServices.Firebase.Database.Models;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Интерфейс сервиса для работы с региональными данными в Firebase Database
    /// </summary>
    public interface IRegionalDatabaseService
    {
        /// <summary>
        /// Получает данные всех регионов
        /// </summary>
        /// <returns>Словарь с данными регионов, где ключ - название региона</returns>
        Task<Dictionary<string, RegionData>> GetAllRegionData();
        
        /// <summary>
        /// Получает данные конкретного региона
        /// </summary>
        /// <param name="regionName">Название региона</param>
        /// <returns>Данные региона или null, если регион не найден</returns>
        Task<RegionData> GetRegionData(string regionName);
        
        /// <summary>
        /// Сохраняет данные региона
        /// </summary>
        /// <param name="regionName">Название региона</param>
        /// <param name="regionData">Данные региона</param>
        /// <returns>True, если данные успешно сохранены</returns>
        Task<bool> SaveRegionData(string regionName, RegionData regionData);
        
        /// <summary>
        /// Обновляет статистику эмоций для региона
        /// </summary>
        /// <param name="regionName">Название региона</param>
        /// <param name="emotionType">Тип эмоции</param>
        /// <param name="count">Количество эмоций</param>
        /// <returns>True, если статистика успешно обновлена</returns>
        Task<bool> UpdateRegionEmotionStats(string regionName, string emotionType, int count);
        
        /// <summary>
        /// Увеличивает счетчик эмоции в регионе
        /// </summary>
        /// <param name="regionName">Название региона</param>
        /// <param name="emotionType">Тип эмоции</param>
        /// <param name="increment">Значение для увеличения (по умолчанию 1)</param>
        /// <returns>True, если счетчик успешно увеличен</returns>
        Task<bool> IncrementRegionEmotionCount(string regionName, string emotionType, int increment = 1);
        
        /// <summary>
        /// Удаляет данные региона
        /// </summary>
        /// <param name="regionName">Название региона</param>
        /// <returns>True, если данные успешно удалены</returns>
        Task<bool> DeleteRegionData(string regionName);
        
        /// <summary>
        /// Получает список всех доступных регионов
        /// </summary>
        /// <returns>Список названий регионов</returns>
        Task<List<string>> GetAvailableRegions();
    }
} 