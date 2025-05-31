using System.Collections.Generic;
using System.Threading.Tasks;
using App.Develop.CommonServices.Firebase.Database.Models;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Интерфейс сервиса для управления сессиями пользователя в базе данных Firebase
    /// </summary>
    public interface ISessionManagementService
    {
        /// <summary>
        /// Получает информацию о всех активных сессиях пользователя
        /// </summary>
        /// <returns>Словарь активных сессий, где ключ - это deviceId</returns>
        Task<Dictionary<string, ActiveSessionData>> GetActiveSessions();
        
        /// <summary>
        /// Регистрирует новую активную сессию для текущего устройства
        /// </summary>
        /// <returns>True, если сессия успешно зарегистрирована</returns>
        Task<bool> RegisterActiveSession();
        
        /// <summary>
        /// Очищает все активные сессии пользователя
        /// </summary>
        /// <returns>True, если сессии успешно очищены</returns>
        Task<bool> ClearActiveSessions();
        
        /// <summary>
        /// Очищает активную сессию конкретного устройства
        /// </summary>
        /// <param name="deviceId">Идентификатор устройства</param>
        /// <returns>True, если сессия успешно очищена</returns>
        Task<bool> ClearActiveSession(string deviceId);
        
        /// <summary>
        /// Проверяет существование активной сессии с другого устройства
        /// </summary>
        /// <param name="currentDeviceId">ID текущего устройства</param>
        /// <returns>True, если существует активная сессия с другого устройства</returns>
        Task<bool> CheckActiveSessionExists(string currentDeviceId);
        
        /// <summary>
        /// Обновляет активную сессию для указанного устройства
        /// </summary>
        /// <param name="deviceId">ID устройства</param>
        /// <returns>True, если сессия успешно обновлена</returns>
        Task<bool> UpdateActiveSession(string deviceId);
    }
} 