using System.Collections.Generic;
using System.Threading.Tasks;
using App.App.Develop.Scenes.PersonalAreaScene.UI.Components;

namespace App.Develop.CommonServices.Regional
{
    /// <summary>
    /// Интерфейс сервиса региональной статистики эмоций
    /// </summary>
    public interface IRegionalStatsService
    {
        /// <summary>
        /// Получить статистику эмоций по всем районам
        /// </summary>
        /// <returns>Словарь с названием района и его статистикой</returns>
        Task<Dictionary<string, RegionalEmotionStats>> GetAllRegionalStats();
        
        /// <summary>
        /// Получить статистику эмоций для конкретного района
        /// </summary>
        /// <param name="regionName">Название района</param>
        /// <returns>Статистика района или null, если район не найден</returns>
        Task<RegionalEmotionStats> GetRegionalStats(string regionName);
        
        /// <summary>
        /// Обновить статистику района
        /// </summary>
        /// <param name="regionName">Название района</param>
        /// <param name="stats">Новая статистика</param>
        /// <returns>True, если обновление прошло успешно</returns>
        Task<bool> UpdateRegionalStats(string regionName, RegionalEmotionStats stats);
        
        /// <summary>
        /// Получить список всех доступных районов
        /// </summary>
        /// <returns>Список названий районов</returns>
        Task<List<string>> GetAvailableRegions();
        
        /// <summary>
        /// Инициализировать сервис
        /// </summary>
        void Initialize();
    }
} 