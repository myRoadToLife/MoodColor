using System.Threading.Tasks;
using App.Develop.CommonServices.Firebase.Database.Models;

namespace App.Develop.CommonServices.Firebase.Database.Services
{
    /// <summary>
    /// Интерфейс сервиса для работы с профилями пользователей в Firebase Database
    /// </summary>
    public interface IUserProfileDatabaseService
    {
        /// <summary>
        /// Получает профиль пользователя
        /// </summary>
        /// <param name="userId">ID пользователя (если не указан, используется текущий)</param>
        Task<UserProfile> GetUserProfile(string userId = null);
        
        /// <summary>
        /// Создает профиль пользователя
        /// </summary>
        /// <param name="profile">Данные профиля</param>
        /// <param name="userId">ID пользователя (если не указан, используется текущий)</param>
        Task CreateUserProfile(UserProfile profile, string userId = null);
        
        /// <summary>
        /// Обновляет профиль пользователя
        /// </summary>
        /// <param name="profile">Данные профиля</param>
        /// <param name="userId">ID пользователя (если не указан, используется текущий)</param>
        Task UpdateUserProfile(UserProfile profile, string userId = null);
        
        /// <summary>
        /// Обновляет поле профиля пользователя
        /// </summary>
        /// <param name="field">Название поля</param>
        /// <param name="value">Новое значение</param>
        /// <param name="userId">ID пользователя (если не указан, используется текущий)</param>
        Task UpdateUserProfileField(string field, object value, string userId = null);
        
        /// <summary>
        /// Проверяет существование профиля пользователя
        /// </summary>
        /// <param name="userId">ID пользователя (если не указан, используется текущий)</param>
        Task<bool> UserProfileExists(string userId = null);
        
        /// <summary>
        /// Проверяет существование никнейма
        /// </summary>
        /// <param name="nickname">Никнейм для проверки</param>
        Task<bool> NicknameExists(string nickname);
        
        /// <summary>
        /// Проверяет доступность никнейма
        /// </summary>
        /// <param name="nickname">Никнейм для проверки</param>
        /// <returns>Кортеж с флагом доступности и сообщением об ошибке</returns>
        Task<(bool available, string error)> CheckNicknameAvailability(string nickname);
    }
} 